using Microsoft.EntityFrameworkCore;
using Prophix.Api.Domain;

namespace Prophix.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FpaDbContext>();

        // Prep shortcut. In a real service: `dotnet ef migrations add Init` + db.Database.MigrateAsync().
        await db.Database.EnsureCreatedAsync();
        if (await db.Accounts.AnyAsync()) return;

        Department[] depts = [new() { Name = "Sales" }, new() { Name = "Engineering" }];
        Account[] accounts =
        [
            new() { Code = "4000", Name = "Subscription Revenue", Type = AccountType.Revenue },
            new() { Code = "5000", Name = "Salaries",             Type = AccountType.Expense },
            new() { Code = "5100", Name = "Cloud Hosting",        Type = AccountType.Expense },
            new() { Code = "5200", Name = "Marketing",            Type = AccountType.Expense },
        ];
        db.AddRange(depts);
        db.AddRange(accounts);

        var monthlyBudget = new Dictionary<string, decimal>
        {
            ["4000"] = 100_000m, ["5000"] = 50_000m, ["5100"] = 8_000m, ["5200"] = 12_000m,
        };
        var rng = new Random(42); // deterministic seed => stable demo numbers
        const int year = 2026, closedMonths = 10; // actuals booked Jan..Oct

        foreach (var dept in depts)
        foreach (var acct in accounts)
        for (var m = 1; m <= 12; m++)
        {
            var budget = monthlyBudget[acct.Code];
            db.Facts.Add(new FactEntry { Account = acct, Department = dept, Year = year, Month = m, Scenario = Scenario.Budget, Amount = budget });
            if (m <= closedMonths)
            {
                var actual = Math.Round(budget * (decimal)(0.85 + rng.NextDouble() * 0.3), 2); // ±15%
                db.Facts.Add(new FactEntry { Account = acct, Department = dept, Year = year, Month = m, Scenario = Scenario.Actual, Amount = actual });
            }
        }

        await db.SaveChangesAsync(); // one SaveChanges => one transaction
    }
}
