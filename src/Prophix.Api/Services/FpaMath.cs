using Prophix.Api.Domain;

namespace Prophix.Api.Services;

public record VarianceResult(decimal Variance, decimal? VariancePct, bool Favorable);

/// Pure functions: no I/O, trivially unit-testable. Keep business rules here, not in endpoints.
public static class FpaMath
{
    /// Variance = Actual - Budget. More revenue is good; more expense is bad.
    public static VarianceResult Variance(AccountType type, decimal budget, decimal actual)
    {
        var variance = actual - budget;
        decimal? pct = budget == 0 ? null : Math.Round(variance / Math.Abs(budget), 4);
        var favorable = type == AccountType.Revenue ? variance >= 0 : variance <= 0;
        return new VarianceResult(variance, pct, favorable);
    }

    /// Straight-line run-rate: annualise YTD actuals over the months closed so far.
    public static decimal RunRateForecast(decimal ytdActual, int monthsClosed, int periodsInYear = 12)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(monthsClosed, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(monthsClosed, periodsInYear);
        return Math.Round(ytdActual / monthsClosed * periodsInYear, 2);
    }
}
