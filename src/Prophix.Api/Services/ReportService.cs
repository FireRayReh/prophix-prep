using Microsoft.EntityFrameworkCore;
using Prophix.Api.Data;
using Prophix.Api.Domain;

namespace Prophix.Api.Services;

public record VarianceLine(string Code, string Name, AccountType Type, decimal Budget, decimal Actual,
                           decimal Variance, decimal? VariancePct, bool Favorable);

public record VarianceReport(int Year, int FromMonth, int ToMonth, int? DepartmentId,
                             IReadOnlyList<VarianceLine> Lines, decimal NetIncomeBudget, decimal NetIncomeActual);

public record ForecastLine(string Code, string Name, AccountType Type, decimal YtdActual,
                           decimal FullYearBudget, decimal FullYearForecast, decimal ProjectedVariance);

public class ReportService(FpaDbContext db)
{
    public async Task<VarianceReport> GetVarianceAsync(int year, int fromMonth, int toMonth, int? departmentId,
                                                       CancellationToken ct = default)
    {
        var facts = db.Facts.AsNoTracking()
            .Where(f => f.Year == year && f.Month >= fromMonth && f.Month <= toMonth);
        if (departmentId is not null)
            facts = facts.Where(f => f.DepartmentId == departmentId);   // composable IQueryable

        // GROUP BY + conditional SUM, executed in the database (one round trip, no N+1).
        var totals = await facts
            .GroupBy(f => new { f.Account.Code, f.Account.Name, f.Account.Type })
            .Select(g => new
            {
                g.Key.Code, g.Key.Name, g.Key.Type,
                Budget = g.Sum(f => f.Scenario == Scenario.Budget ? f.Amount : 0),
                Actual = g.Sum(f => f.Scenario == Scenario.Actual ? f.Amount : 0),
            })
            .OrderBy(x => x.Code)
            .ToListAsync(ct);

        var lines = totals.Select(t =>
        {
            var v = FpaMath.Variance(t.Type, t.Budget, t.Actual);
            return new VarianceLine(t.Code, t.Name, t.Type, t.Budget, t.Actual, v.Variance, v.VariancePct, v.Favorable);
        }).ToList();

        decimal Net(Func<VarianceLine, decimal> sel) =>
            lines.Sum(l => l.Type == AccountType.Revenue ? sel(l) : -sel(l));

        return new VarianceReport(year, fromMonth, toMonth, departmentId, lines, Net(l => l.Budget), Net(l => l.Actual));
    }

    public async Task<IReadOnlyList<ForecastLine>> GetForecastAsync(int year, int monthsClosed, CancellationToken ct = default)
    {
        var rows = await db.Facts.AsNoTracking()
            .Where(f => f.Year == year)
            .GroupBy(f => new { f.Account.Code, f.Account.Name, f.Account.Type })
            .Select(g => new
            {
                g.Key.Code, g.Key.Name, g.Key.Type,
                Ytd = g.Sum(f => f.Scenario == Scenario.Actual && f.Month <= monthsClosed ? f.Amount : 0),
                Budget = g.Sum(f => f.Scenario == Scenario.Budget ? f.Amount : 0),
            })
            .OrderBy(x => x.Code)
            .ToListAsync(ct);

        return rows.Select(r =>
        {
            var fc = FpaMath.RunRateForecast(r.Ytd, monthsClosed);
            return new ForecastLine(r.Code, r.Name, r.Type, r.Ytd, r.Budget, fc, fc - r.Budget);
        }).ToList();
    }
}
