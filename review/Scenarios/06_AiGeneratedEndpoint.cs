// SCENARIO 6 — "Copilot/Claude generated this endpoint, tests pass, LGTM?"
// (Expect this flavour: they want to see you critically review AI output.)
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Prophix.Api.Data;
using Prophix.Api.Domain;

namespace Prophix.Review.Scenarios;

public static class AiGeneratedEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/v2/facts", (FpaDbContext db) => db.Facts.Include(f => f.Account).ToList());

        app.MapPut("/api/v2/accounts/{id}", async (int id, Account input, FpaDbContext db) =>
        {
            input.Id = id;
            db.Accounts.Update(input);
            await db.SaveChangesAsync();
            return Results.Ok(input);
        });

        app.MapPost("/api/v2/recalculate", (FpaDbContext db) =>
        {
            Task.Run(async () =>
            {
                foreach (var f in db.Facts.Where(f => f.Scenario == Scenario.Forecast))
                    f.Amount = f.Amount * 1.03m;
                await db.SaveChangesAsync();
            });
            return Results.Accepted();
        });
    }
}
