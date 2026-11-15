using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Prophix.Api.Data;
using Prophix.Api.Endpoints;
using Prophix.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Dependency injection -------------------------------------------------
builder.Services.AddOpenApi();                 // built-in OpenAPI doc at /openapi/v1.json
builder.Services.AddProblemDetails();          // RFC 7807 error bodies
builder.Services.AddValidation();              // .NET 10: DataAnnotations on minimal-API params
builder.Services.AddHealthChecks();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Provider chosen by config: Sqlite for zero-setup local dev/tests, Postgres in docker/prod.
// The (sp, o) overload reads config at resolve time, so test overrides apply.
builder.Services.AddDbContext<FpaDbContext>((sp, o) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    if (cfg["Database:Provider"] == "Postgres")
        o.UseNpgsql(cfg.GetConnectionString("Postgres"));
    else
        o.UseSqlite(cfg.GetConnectionString("Sqlite") ?? "Data Source=fpa.db");
});
builder.Services.AddScoped<ReportService>();   // scoped = one per HTTP request, same as DbContext

var app = builder.Build();

// ---- Middleware pipeline (order matters) -----------------------------------
app.UseExceptionHandler();                     // unhandled exception => 500 ProblemDetails
app.UseStatusCodePages();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseDefaultFiles();
app.UseStaticFiles();                          // wwwroot/index.html mini dashboard

// ---- Endpoints -------------------------------------------------------------
app.MapHealthChecks("/health");
app.MapAccounts();
app.MapFacts();
app.MapReports();

await DbInitializer.InitializeAsync(app.Services);
app.Run();

public partial class Program;                  // lets WebApplicationFactory<Program> see it
