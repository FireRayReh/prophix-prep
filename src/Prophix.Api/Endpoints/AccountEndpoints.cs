using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Prophix.Api.Data;
using Prophix.Api.Domain;

namespace Prophix.Api.Endpoints;

// DTOs: never expose EF entities directly (over-posting, cycles, coupling DB schema to API contract).
public record AccountDto(int Id, string Code, string Name, AccountType Type);

public record CreateAccountRequest(
    [Required, RegularExpression(@"^\d{4}$")] string Code,
    [Required, StringLength(200, MinimumLength = 2)] string Name,
    AccountType Type);

public static class AccountEndpoints
{
    public static RouteGroupBuilder MapAccounts(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/accounts").WithTags("Accounts");

        g.MapGet("/", async (FpaDbContext db, CancellationToken ct) =>
            await db.Accounts.AsNoTracking()
                .OrderBy(a => a.Code)
                .Select(a => new AccountDto(a.Id, a.Code, a.Name, a.Type))   // projection => SELECT only these columns
                .ToListAsync(ct));

        g.MapGet("/{id:int}", async Task<Results<Ok<AccountDto>, NotFound>> (int id, FpaDbContext db, CancellationToken ct) =>
            await db.Accounts.AsNoTracking().Where(a => a.Id == id)
                .Select(a => new AccountDto(a.Id, a.Code, a.Name, a.Type))
                .FirstOrDefaultAsync(ct) is { } dto
                ? TypedResults.Ok(dto)
                : TypedResults.NotFound())
            .WithName("GetAccount");

        // Validation of [Required] etc. runs automatically (builder.Services.AddValidation()) => 400 ProblemDetails.
        g.MapPost("/", async Task<Results<CreatedAtRoute<AccountDto>, Conflict<ProblemDetailsLite>>> (
            CreateAccountRequest req, FpaDbContext db, CancellationToken ct) =>
        {
            if (await db.Accounts.AnyAsync(a => a.Code == req.Code, ct))
                return TypedResults.Conflict(new ProblemDetailsLite($"Account code {req.Code} already exists."));

            var entity = new Account { Code = req.Code, Name = req.Name, Type = req.Type };
            db.Accounts.Add(entity);
            await db.SaveChangesAsync(ct);
            var dto = new AccountDto(entity.Id, entity.Code, entity.Name, entity.Type);
            return TypedResults.CreatedAtRoute(dto, "GetAccount", new { id = entity.Id });
        });

        return g;
    }
}

public record ProblemDetailsLite(string Detail);
