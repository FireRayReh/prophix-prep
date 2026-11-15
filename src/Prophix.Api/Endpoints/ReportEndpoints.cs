using Prophix.Api.Services;

namespace Prophix.Api.Endpoints;

public static class ReportEndpoints
{
    public static RouteGroupBuilder MapReports(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/reports").WithTags("Reports");

        // Budget vs Actual — the bread and butter of FP&A reporting.
        g.MapGet("/variance", async (ReportService svc, int year, int fromMonth = 1, int toMonth = 12,
                                     int? departmentId = null, CancellationToken ct = default) =>
        {
            if (fromMonth is < 1 or > 12 || toMonth is < 1 or > 12 || fromMonth > toMonth)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["months"] = ["Use 1 <= fromMonth <= toMonth <= 12."] });
            return Results.Ok(await svc.GetVarianceAsync(year, fromMonth, toMonth, departmentId, ct));
        });

        // Full-year outlook from YTD actuals.
        g.MapGet("/forecast", async (ReportService svc, int year, int monthsClosed, CancellationToken ct) =>
            monthsClosed is < 1 or > 12
                ? Results.ValidationProblem(new Dictionary<string, string[]> { ["monthsClosed"] = ["Must be 1..12."] })
                : Results.Ok(await svc.GetForecastAsync(year, monthsClosed, ct)));

        return g;
    }
}
