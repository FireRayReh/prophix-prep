# Interview Prep Sheet

## 1. FP&A vocabulary (say these naturally)

- **FP&A**: Financial Planning & Analysis. Budgeting, forecasting, reporting, and analysis.
- **Budget / Actual / Forecast**: these are *scenarios* (versions) of the same numbers. Here they're the `Scenario` enum.
- **Variance** = Actual − Budget. Whether it's *favorable* depends on the account type: more revenue is good, more expense is bad (`FpaMath.Variance`).
- **Run-rate / rolling forecast**: project the full year from year-to-date actuals (`FpaMath.RunRateForecast`).
- **Chart of accounts, GL (general ledger)**: the account list. Actuals usually arrive from the GL as an import (`POST /api/facts/bulk`).
- **Dimensions**: Account, Department, Time, Scenario. A fact row is one cell of a **cube** (OLAP). Prophix's product is built on cubes, so expect this topic.
- **Consolidation, allocations, close, what-if scenarios**: worth naming as natural extensions.

## 2. Stack talking points by file

**C# features in use:** primary constructors (`ReportService(FpaDbContext db)`), records for DTOs,
`required` members, collection expressions (`[...]`), pattern matching (`is { } dto`, `is < 1 or > 12`),
nullable reference types, `async`/`await` with `CancellationToken` threaded through.

**ASP.NET Core**
- DI lifetimes: Singleton, Scoped (one per request; `DbContext` is scoped), Transient. Never inject a scoped service into a singleton (captive dependency).
- Middleware order: exception handler first so it wraps everything. Then static files, routing, auth, endpoints.
- Minimal APIs vs controllers: minimal APIs are lighter and faster to start. Controllers give filters and conventions for big APIs. Both use the same pipeline.
- `TypedResults` + `Results<A,B>` give compile-time-checked responses and accurate OpenAPI output.
- `AddValidation()` (new in .NET 10) runs DataAnnotations on minimal-API parameters and returns a 400 ProblemDetails.
- Configuration layers: appsettings.json → appsettings.{Env}.json → env vars (`__` = `:`) → command line. The last one wins.

**EF Core**
- `AsNoTracking()` for read-only queries. Change tracking costs memory and CPU.
- Project with `Select` into DTOs so SQL fetches only the columns you need and you avoid lazy-load N+1.
- `IQueryable` builds the query; nothing hits the DB until `ToListAsync`/`CountAsync`/… Mind client vs server evaluation.
- `SaveChanges` is already transactional. Use an explicit `BeginTransaction` when you need several `SaveChanges` calls or raw SQL to commit together.
- Migrations (`dotnet ef migrations add X`, `database update`) instead of `EnsureCreated` in real systems.
- Concurrency: optimistic, using a row version (`[Timestamp]` / Postgres `xmin`) → `DbUpdateConcurrencyException`.
- Provider gotcha shown in the code: SQLite can't `SUM` decimals, so the code converts them only on SQLite.

**PostgreSQL**
- `numeric(18,2)` for money. Use B-tree indexes that match your WHERE/GROUP BY (see the composite index on Facts).
- Use `EXPLAIN ANALYZE` to check a slow report query.
- Large imports: `COPY` (Npgsql `BeginBinaryImport`) or EF Core bulk extensions instead of row-by-row INSERTs.
- Other topics: JSONB, partitioning by year for big fact tables, materialized views for heavy rollups.

**Testing**: the test pyramid. Pure logic gets unit tests. The HTTP + DB path gets integration tests through `WebApplicationFactory`.
Mention Testcontainers to run integration tests against real Postgres.

## 3. Practice drills (do 2–3, timed ~20 min each)

1. Add `PUT /api/accounts/{id}` with optimistic concurrency.
2. Add a `departmentId` breakdown to the forecast endpoint, with a test.
3. Add **month-over-month** variance (`GET /api/reports/trend?accountCode=4000`).
4. Add an `IMemoryCache` or `HybridCache` around the variance report and invalidate it on bulk import.
5. Replace `EnsureCreated` with an EF migration and run it against the docker Postgres.
6. Add JWT auth (`AddAuthentication().AddJwtBearer()`) plus a policy that only lets "Finance" users import.
7. Make bulk import idempotent (an upsert keyed on Account+Dept+Year+Month+Scenario).

## 4. Likely questions — have a 60-second answer ready

- `IEnumerable` vs `IQueryable`? `Task` vs `ValueTask`? What does `ConfigureAwait(false)` do, and why does ASP.NET Core rarely need it?
- How would you find and fix an N+1 query?
- `decimal` vs `double` for money, and why?
- How do you version an API without breaking existing clients?
- Describe a production incident you debugged (logs → metrics → traces).
- A report takes 30s over 50M fact rows. What do you do? (indexes, pre-aggregation, partitioning, caching, async export)
- SOLID and clean architecture: why business rules live in `FpaMath`/`ReportService`, not in endpoints.
- How would you modularise a large .NET FP&A platform? (bounded contexts, modular monolith before microservices)

## 5. Questions to ask them

- How is the cube/calculation engine split from the .NET services?
- What does the move to cloud/SaaS look like for the platform?
- How do teams handle schema changes for customers' financial models?
