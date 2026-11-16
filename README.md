# Prophix Prep: FP&A Mini (.NET 10)

A deliberately small budget-vs-actual API for interview practice. It touches the core
stack from Prophix's C#/.NET postings (C#, .NET, EF Core, PostgreSQL) in about 500 lines.

## Run it (no setup)

```bash
dotnet run --project src/Prophix.Api --urls http://localhost:5080
```

- Dashboard: http://localhost:5080/
- OpenAPI JSON: http://localhost:5080/openapi/v1.json
- Variance: http://localhost:5080/api/reports/variance?year=2026&toMonth=10
- Forecast: http://localhost:5080/api/reports/forecast?year=2026&monthsClosed=10

Uses a SQLite file by default and seeds itself with 2026 budget and Jan–Oct actuals.

## Run against PostgreSQL

```bash
docker compose up --build
```

API on http://localhost:8080. The `Database__Provider=Postgres` setting switches the provider.

## Test

```bash
dotnet test
```

Unit tests (`FpaMathTests`) cover the business rules. Integration tests (`ApiTests`) run the real
HTTP pipeline and EF Core through `WebApplicationFactory`.

## Map

| File | What to talk about |
|---|---|
| `Program.cs` | DI lifetimes, config providers, middleware order, ProblemDetails, OpenAPI, health checks |
| `Domain/Models.cs` | Fact table with dimensions (cube shape), `decimal` for money |
| `Data/FpaDbContext.cs` | Fluent config, indexes, enum conversion, provider differences |
| `Data/DbInitializer.cs` | Seeding, `EnsureCreated` vs migrations, one `SaveChanges` = one transaction |
| `Services/FpaMath.cs` | Pure business logic, variance favorability, run-rate forecast |
| `Services/ReportService.cs` | LINQ → SQL `GROUP BY` with conditional `SUM`, `AsNoTracking`, projections |
| `Endpoints/*.cs` | Minimal APIs, route groups, `TypedResults`, validation, paging, explicit transactions |
| `tests/` | xUnit `[Theory]`, `IClassFixture`, `WebApplicationFactory` |
| `Dockerfile`, `docker-compose.yml`, `.github/workflows/ci.yml` | Multi-stage build, Postgres, CI |

## Interview prep

Start with **[PREP.md](PREP.md)**. It's organised by the three interview rounds.

- `review/Scenarios/` has C# files with planted problems for code-review practice (they compile, so you can open them in an IDE). Answers are in `review/ANSWERS.md`.
- `FpaMath.Allocate` is a worked example of a 25-minute build problem.
