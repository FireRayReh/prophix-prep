using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Prophix.Api.Data;
using Prophix.Api.Domain;

namespace Prophix.Api.Endpoints;

public record FactDto(long Id, string AccountCode, string Department, int Year, int Month, Scenario Scenario, decimal Amount);

public record FactInput(
    [Range(1, int.MaxValue)] int AccountId,
    [Range(1, int.MaxValue)] int DepartmentId,
    [Range(2000, 2100)] int Year,
    [Range(1, 12)] int Month,
    Scenario Scenario,
    decimal Amount);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public static class FactEndpoints
{
    public static RouteGroupBuilder MapFacts(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/facts").WithTags("Facts");

        // Paging + optional filters bound from the query string.
        g.MapGet("/", async (FpaDbContext db, int year, Scenario? scenario, int page = 1, int pageSize = 50,
                             CancellationToken ct = default) =>
        {
            pageSize = Math.Clamp(pageSize, 1, 500);
            var q = db.Facts.AsNoTracking().Where(f => f.Year == year);
            if (scenario is not null) q = q.Where(f => f.Scenario == scenario);

            var total = await q.CountAsync(ct);
            var items = await q.OrderBy(f => f.Month).ThenBy(f => f.AccountId).ThenBy(f => f.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(f => new FactDto(f.Id, f.Account.Code, f.Department.Name, f.Year, f.Month, f.Scenario, f.Amount))
                .ToListAsync(ct);
            return new PagedResult<FactDto>(items, page, pageSize, total);
        });

        // Bulk load (e.g. a GL import): all-or-nothing in one transaction.
        g.MapPost("/bulk", async (List<FactInput> rows, FpaDbContext db, CancellationToken ct) =>
        {
            var accountIds = rows.Select(r => r.AccountId).Distinct().ToList();
            var known = await db.Accounts.CountAsync(a => accountIds.Contains(a.Id), ct);
            if (known != accountIds.Count)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["accountId"] = ["Unknown account in batch."] });

            await using var tx = await db.Database.BeginTransactionAsync(ct);
            db.Facts.AddRange(rows.Select(r => new FactEntry
            {
                AccountId = r.AccountId, DepartmentId = r.DepartmentId, Year = r.Year,
                Month = r.Month, Scenario = r.Scenario, Amount = r.Amount,
            }));
            var inserted = await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Results.Ok(new { inserted });
        });

        return g;
    }
}
