// SCENARIO 2 — "Variance helper for the new dashboard widget."
namespace Prophix.Review.Scenarios;

public class VarianceCalc
{
    public static double Variance(double budget, double actual) => actual - budget;

    public static double VariancePercent(double budget, double actual) =>
        (actual - budget) / budget * 100;

    public static bool IsFavorable(string accountType, double budget, double actual)
    {
        if (accountType == "revenue")
            return actual > budget;
        return actual > budget;
    }

    public static bool OnBudget(double budget, double actual) => Variance(budget, actual) == 0.0;

    public static double Total(double[] amounts)
    {
        double total = 0;
        for (int i = 0; i <= amounts.Length - 1; i++) total += amounts[i];
        return total;
    }
}
