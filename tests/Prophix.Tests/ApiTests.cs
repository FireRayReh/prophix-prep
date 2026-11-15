using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Prophix.Api.Endpoints;
using Prophix.Api.Services;

namespace Prophix.Tests;

// Integration tests: real HTTP pipeline + real EF Core against a throwaway SQLite file.
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"fpa-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:Sqlite", $"Data Source={_dbPath};Pooling=False");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}

public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_Is_Ok()
        => Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health")).StatusCode);

    [Fact]
    public async Task Accounts_Are_Seeded()
    {
        var accounts = await _client.GetFromJsonAsync<List<AccountDto>>("/api/accounts", Json);
        Assert.Contains(accounts!, a => a.Code == "4000");
    }

    [Fact]
    public async Task Create_Account_Validates_Input()
    {
        var res = await _client.PostAsJsonAsync("/api/accounts", new { code = "x", name = "", type = "Expense" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Create_Account_Rejects_Duplicate_Code()
    {
        var res = await _client.PostAsJsonAsync("/api/accounts", new { code = "4000", name = "Dup", type = "Revenue" });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Create_Then_Get_Account()
    {
        var res = await _client.PostAsJsonAsync("/api/accounts", new { code = "6000", name = "Travel", type = "Expense" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var got = await _client.GetFromJsonAsync<AccountDto>(res.Headers.Location, Json);
        Assert.Equal("Travel", got!.Name);
    }

    [Fact]
    public async Task Variance_Report_Sums_Budget_Per_Account()
    {
        var report = await _client.GetFromJsonAsync<VarianceReport>("/api/reports/variance?year=2026&fromMonth=1&toMonth=10", Json);
        var revenue = report!.Lines.Single(l => l.Code == "4000");
        Assert.Equal(100_000m * 10 * 2, revenue.Budget);   // 100k/month x 10 months x 2 departments
        Assert.Equal(revenue.Actual - revenue.Budget, revenue.Variance);
    }

    [Fact]
    public async Task Variance_Report_Rejects_Bad_Month_Range()
        => Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.GetAsync("/api/reports/variance?year=2026&fromMonth=10&toMonth=2")).StatusCode);
}
