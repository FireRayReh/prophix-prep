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

    /// Allocate a shared cost (e.g. rent) across departments by driver (e.g. headcount).
    /// Classic trap: naive rounding makes the parts not add up to the total.
    /// Largest-remainder method: floor each share to the cent, then hand leftover cents
    /// to the largest fractional remainders. Result always sums exactly to `total`.
    public static decimal[] Allocate(decimal total, IReadOnlyList<decimal> drivers)
    {
        ArgumentOutOfRangeException.ThrowIfZero(drivers.Count);
        if (total < 0) return Allocate(-total, drivers).Select(x => -x).ToArray(); // credits: allocate magnitude, flip sign
        if (drivers.Any(d => d < 0)) throw new ArgumentException("Drivers must be non-negative.", nameof(drivers));
        var driverSum = drivers.Sum();
        if (driverSum == 0) throw new ArgumentException("Drivers must not all be zero.", nameof(drivers));

        var exact = drivers.Select(d => total * d / driverSum).ToArray();
        var result = exact.Select(e => Math.Floor(e * 100) / 100).ToArray();
        var leftoverCents = (int)Math.Round((total - result.Sum()) * 100);

        foreach (var i in Enumerable.Range(0, exact.Length)
                     .OrderByDescending(i => exact[i] - result[i]).ThenBy(i => i)
                     .Take(leftoverCents))
            result[i] += 0.01m;

        return result;
    }
}
