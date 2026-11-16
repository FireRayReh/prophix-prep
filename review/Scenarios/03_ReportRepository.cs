// SCENARIO 3 — "Department report is slow in prod (8s). Here's my repository."
using Microsoft.EntityFrameworkCore;
using Prophix.Api.Data;
using Prophix.Api.Domain;

namespace Prophix.Review.Scenarios;

public class ReportRepository(FpaDbContext db)
{
    public async Task<Dictionary<string, decimal>> ActualsByAccount(int year, string department)
    {
        var result = new Dictionary<string, decimal>();
        var accounts = db.Accounts.ToList();

        foreach (var account in accounts)
        {
            var facts = db.Facts
                .FromSqlRaw($"SELECT * FROM \"Facts\" WHERE \"AccountId\" = {account.Id} AND \"Year\" = {year}")
                .Include(f => f.Department)
                .ToList()
                .Where(f => f.Department.Name == department && f.Scenario == Scenario.Actual);

            if (facts.Count() > 0)
                result[account.Code] = facts.Sum(f => f.Amount);
        }
        return result;
    }
}
