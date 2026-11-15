using Prophix.Api.Domain;
using Prophix.Api.Services;

namespace Prophix.Tests;

// Unit tests: pure logic, no DB, milliseconds to run.
public class FpaMathTests
{
    [Theory]
    [InlineData(AccountType.Revenue, 100, 120, true)]   // beat revenue target => favorable
    [InlineData(AccountType.Revenue, 100, 80, false)]
    [InlineData(AccountType.Expense, 100, 80, true)]    // spent less => favorable
    [InlineData(AccountType.Expense, 100, 120, false)]
    public void Variance_Favorability_Depends_On_Account_Type(AccountType type, decimal budget, decimal actual, bool favorable)
    {
        var r = FpaMath.Variance(type, budget, actual);
        Assert.Equal(actual - budget, r.Variance);
        Assert.Equal(favorable, r.Favorable);
    }

    [Fact]
    public void Variance_Pct_Is_Null_When_Budget_Is_Zero()
        => Assert.Null(FpaMath.Variance(AccountType.Expense, 0, 50).VariancePct);

    [Fact]
    public void Variance_Pct_Is_Relative_To_Budget()
        => Assert.Equal(-0.25m, FpaMath.Variance(AccountType.Revenue, 200, 150).VariancePct);

    [Fact]
    public void RunRate_Annualises_Ytd()
        => Assert.Equal(1200m, FpaMath.RunRateForecast(ytdActual: 300, monthsClosed: 3));

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void RunRate_Rejects_Invalid_Months(int months)
        => Assert.Throws<ArgumentOutOfRangeException>(() => FpaMath.RunRateForecast(100, months));
}
