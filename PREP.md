# Interview Prep: Prophix (in person, 3 rounds, with tech leads)

| Round | Time | What they're judging | Your prep material |
|---|---|---|---|
| 1. Project experience | 15–20 min | Ownership, depth, trade-offs, outcomes | §1 below (fill in the worksheet) |
| 2. C# code review | 20–30 min | How you *reason*, out loud | `review/Scenarios/*.cs` → `review/ANSWERS.md` |
| 3. Design/build with AI | 20–30 min | Design thinking + judgment using AI | §3 below + this repo as a warm-up |

**Theme for all three: think out loud, ask clarifying questions, and name trade-offs.** They said so: "we're curious how you think."

---

## 1. Project Experience (15–20 min)

Prepare **2 projects**: one where you owned the design, one with a hard problem (bug, performance, deadline).
Tech leads will **drill down** ("why that over X?", "what broke?", "how did you know it worked?"). Breadth won't carry you.

### Worksheet (one per project, keep it to ~2 minutes spoken, then let them dig)
- **Context:** product, users, team size, your role. One sentence each.
- **Problem:** what was wrong or needed, *with a number* (latency, error rate, hours of manual work, $).
- **Approach:** the 2–3 key decisions *and the alternatives you rejected*.
- **Stack:** .NET version, data store, hosting, how it was tested and deployed.
- **Outcome:** a metric before → after. What you'd do differently.
- **Hard part:** a bug, a disagreement, or a constraint, and how you handled it.

### Follow-ups to rehearse
- "What would you change if you rebuilt it today?"
- "Where did it fail in production? How did you find out?" (logs → metrics → traces)
- "How did you handle disagreement with a teammate or PM?"
- "How did you make sure the data/numbers were *correct*?" (very FP&A-relevant: finance users don't tolerate wrong numbers)
- "How do you use AI tools day to day? Where do they let you down?"

---

## 2. C# Code Review (20–30 min)

**Practice loop:** open one file in `review/Scenarios/`, set a 5-min timer, and **say your review out loud**
(or type it as PR comments). Then check against `review/ANSWERS.md`. Re-do the ones you missed the next day.

| File | Main skill |
|---|---|
| `01_GlImporter.cs` | async/await, exceptions, parsing, resources |
| `02_VarianceCalc.cs` | money types, edge cases, domain logic bugs |
| `03_ReportRepository.cs` | EF Core: N+1, SQL injection, client vs server eval |
| `04_FxRateCache.cs` | thread safety, caching, singletons |
| `05_ApprovalService.cs` | design, SOLID, testability, state machines |
| `06_AiGeneratedEndpoint.cs` | critiquing AI-written code: over-posting, DbContext lifetime, fire-and-forget |
| `07_CSharpGotchas.cs` | language quick-fire: closures, deferred LINQ, structs, boxing |

**Review checklist (memorize the order):** intent → correctness/edge cases → failure modes (async, exceptions, resources, concurrency) → security → performance → design/testability → nits.
Label each finding **Blocker / Should fix / Nit** and propose the fix.

### C#/.NET concepts to explain in one breath each
- `async`/`await`: never `async void` (except event handlers); never `.Result`/`.Wait()`; pass `CancellationToken`; `Task.WhenAll` for parallel work.
- `IEnumerable` (in memory, deferred) vs `IQueryable` (expression tree → SQL). Multiple enumeration.
- `decimal` for money; `double` for science.
- Value vs reference types, boxing, `record` (value equality), `readonly struct`.
- DI lifetimes: Singleton / Scoped / Transient; captive dependency; `DbContext` is scoped and not thread-safe.
- `IDisposable` / `using`; `IHttpClientFactory`.
- Exceptions: catch specific types, don't swallow, `throw;` not `throw ex;`.
- Thread safety: `lock` on a private object, `ConcurrentDictionary`, `Interlocked`; no `await` inside `lock` (use `SemaphoreSlim`).
- EF Core: `AsNoTracking`, projection, N+1, `Include` vs `Select`, `ExecuteUpdate`/`ExecuteDelete`, migrations, optimistic concurrency.
- SOLID with real examples. Testability via interfaces and `TimeProvider` instead of `DateTime.Now`.

---

## 3. Design/Build with AI Tools (20–30 min)

### They're watching *how* you work, not just the result. Run this script:
1. **Clarify (2–3 min).** Restate the problem. Ask the tech leads about inputs, scale, edge cases, and what "done" means. Write the assumptions down.
2. **Design before prompting (3 min).** Sketch the types and the one or two key functions out loud. Choose the simplest thing that works (console app, minimal API, or just a class + tests).
3. **Drive the AI in small steps.** Small, specific prompts with your constraints: *"C# 13 / .NET 10, use decimal for money, pure function, xUnit tests for: empty input, zero drivers, rounding sums to total."* Don't ask for "build the whole thing."
4. **Read every line it produces, out loud.** Point out what you're accepting, changing, and rejecting. Catching an AI mistake on camera is a *plus*.
5. **Tests first or right after.** Run them. Edge cases are where you shine.
6. **Leave 3 min to wrap up:** what works, what you'd do next (validation, persistence, auth, performance), and the trade-offs you made.

Bounce ideas off them ("I'm thinking X vs Y because…"). They explicitly invited it, and it's part of the evaluation.

### Have these ready to type from memory
```bash
dotnet new webapi -n Demo        # or: dotnet new console / classlib
dotnet new xunit -n Demo.Tests && dotnet add Demo.Tests reference Demo
dotnet test
```

### Timed practice problems (25 min each, use your AI tool, narrate)
1. **Cost allocation.** Split a shared cost across departments by headcount so the parts sum *exactly* to the total. → Worked solution: `FpaMath.Allocate` + `AllocationTests` (largest-remainder rounding).
2. **Currency consolidation.** Subsidiaries report in CAD/USD/EUR. Convert to a reporting currency using month-end rates for balance-sheet accounts and average rates for P&L. Handle a missing rate.
3. **Budget approval workflow.** Draft → Submitted → Approved/Rejected → Locked. Valid transitions only, approver ≠ owner, an audit trail. (Compare with review scenario 5.)
4. **GL CSV import.** Parse, validate per row (unknown account, bad period, bad amount), import valid rows, and return an error report. Idempotent re-import.
5. **Spreading.** Spread an annual budget across 12 months: evenly, by a seasonality profile, or by last year's actuals. The months must sum to the annual figure.
6. **Rolling forecast.** Given actuals for closed months and budget for the rest, produce a "latest estimate" for each account. Add a `GET` endpoint for it in this repo.

### This repo as the warm-up
Know every file in `src/` well enough to rebuild it in 20 minutes with AI help. That's the muscle memory you want.
See `README.md` for the map and talking points.

---

## FP&A vocabulary (use naturally in every round)
- **Budget / Actual / Forecast** are *scenarios* (versions) of the same numbers. **Variance** = Actual − Budget; whether it's favorable depends on the account type.
- **Run-rate / rolling forecast / latest estimate**: project the year from year-to-date actuals.
- **Chart of accounts, GL, month-end close, consolidation, intercompany eliminations, allocations, spreading, what-if scenarios.**
- **Dimensions:** Account × Department × Time × Scenario (× Entity, Currency). A fact row is one cell of a **cube** (OLAP). Prophix's product is cube-based.
- Finance users need numbers that **reconcile to the cent**. Mention decimal, rounding, and audit trails whenever relevant.

## Questions to ask them
- What does a typical PR review look like here? What gets pushed back on most?
- How do your engineers use AI tools today, and where have you drawn lines?
- How is the calculation/cube engine split from the .NET services?
- What's the biggest technical challenge the team is facing this year?
