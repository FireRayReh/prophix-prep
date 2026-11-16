# Code Review Answer Key

Try each scenario out loud first, with a 5-minute timer, before reading this.

## How to talk through a review (use this order every time)

1. **Intent.** "So this is meant to … right?" Restate the goal and ask one clarifying question.
2. **Correctness.** Wrong results, edge cases (empty, zero, null, negative, month 12 → 1).
3. **Failure modes.** Exceptions, async, resource leaks, concurrency.
4. **Security.** Injection, over-posting, authorization.
5. **Performance.** Round trips, N+1, loading everything into memory.
6. **Design and testability.** Dependencies, single responsibility, hidden statics/clocks.
7. **Style.** Last, and labelled as *nits*.

Sort each finding into **Blocker / Should fix / Nit**, and propose the fix, not just the problem.
Mention one thing that's *good*. Tech leads notice when you're constructive.

---

## 1. GlImporter

| Sev | Issue | Fix |
|---|---|---|
| Blocker | `async void`: the caller can't await it, and exceptions crash the process. | `async Task ImportAsync(..., CancellationToken ct)` |
| Blocker | `.Result` is sync-over-async: it blocks a thread and can deadlock. | `await client.GetStringAsync(url, ct)` |
| Blocker | `catch (Exception) {}` silently drops bad rows, so financial data goes missing with no trace. | Collect row-level errors (`line #, reason`) and return or report them. Fail the batch or quarantine bad rows. |
| Blocker | `decimal.Parse` uses the *current culture*: `1,234.56` vs `1.234,56`. Also, a comma inside a quoted field breaks `Split(',')`. | `decimal.Parse(x, CultureInfo.InvariantCulture)` or `TryParse`; use a CSV library (CsvHelper). |
| Should | `new HttpClient()` per call exhausts sockets. | Use `IHttpClientFactory` or an injected typed client. |
| Should | `StreamWriter` is never disposed, so the log may never flush. Hand-rolled file logging. | Use `ILogger<GlImporter>` with structured logging. |
| Should | Public mutable `Rows` field. The importer is stateful, so calling it twice duplicates rows. | Return an `ImportResult` and don't keep state. |
| Nit | `Split('\n')` leaves `\r` on Windows files and doesn't skip the header row or blank lines. `DateTime.Now` should be `UtcNow`/`TimeProvider`. | |

## 2. VarianceCalc

- **Blocker:** `double` for money causes binary rounding errors (`0.1 + 0.2 != 0.3`). Use `decimal`.
- **Blocker:** `IsFavorable` returns the same thing in both branches. For an expense, under budget is favorable. Testing both account types would have caught it.
- **Blocker:** `VariancePercent` divides by zero when the budget is 0. With doubles that returns `Infinity`/`NaN` instead of throwing, so the bad value spreads silently. Return `null` or "n/a". A negative budget needs `Math.Abs`.
- **Should:** `accountType` is a stringly-typed `"revenue"`, so `"Revenue"` won't match. Use an enum.
- **Should:** `== 0.0` on doubles. With decimals equality is fine; otherwise compare against a tolerance.
- **Nit:** the `Total` loop can just be `amounts.Sum()`.
- *Point to `FpaMath.Variance` in the main app as your fixed version.*

## 3. ReportRepository (the "why is it slow" one)

- **Blocker, security:** `FromSqlRaw` with interpolation is **SQL injection**. Use `FromSql($"...")` / `FromSqlInterpolated` (parameterized) or plain LINQ.
- **Blocker, performance:** **N+1**: one query per account. Plus `.ToList()` *before* `.Where` filters department and scenario **in memory** after loading every fact.
- **Should:** `facts.Count()` then `facts.Sum()` enumerates the deferred LINQ twice. `Any()` beats `Count() > 0`.
- **Should:** loading all `Accounts` with tracking. Use `AsNoTracking`. No `CancellationToken`; the method is `async` with nothing awaited.
- **Fix:** one query.
  ```csharp
  db.Facts.AsNoTracking()
    .Where(f => f.Year == year && f.Scenario == Scenario.Actual && f.Department.Name == department)
    .GroupBy(f => f.Account.Code)
    .Select(g => new { g.Key, Total = g.Sum(f => f.Amount) })
    .ToDictionaryAsync(x => x.Key, x => x.Total, ct);
  ```
  Then verify with `EXPLAIN ANALYZE` and an index on `(Year, Scenario, DepartmentId)`.
- Talk about *how you'd diagnose it*: EF logging (`LogTo`), query count, Application Insights or OpenTelemetry traces.

## 4. FxRateCache (concurrency)

- **Blocker:** a `static Dictionary` mutated from many threads is not thread-safe. Concurrent writes can corrupt it.
- **Blocker:** check-then-act race. `ContainsKey` runs *outside* the lock, and so does the read on the last line.
- **Blocker:** `lock(this)`. Anyone holding the instance can lock it too. Use a `private readonly object _gate` or, better, `ConcurrentDictionary`. You also can't `await` inside a `lock`.
- **Blocker, domain:** FX rates are **per date**, but the key ignores the date and the cache never expires, so stale rates get used forever. Consolidation needs the rate *as of the period*: month-end vs average rate.
- **Should:** sync-over-async `.GetAwaiter().GetResult()`. Make it `async`. The key `from + to` is ambiguous ("AB"+"C" = "A"+"BC"); use a tuple or record key.
- **Should:** `ToLower()` comparison. Use `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)`. `DateTime.Now` should be `TimeProvider` (testable, UTC).
- **Fix:** `IMemoryCache`/`HybridCache` with an expiry, keyed by `(from, to, date)`, which also stops duplicate concurrent fetches.

## 5. ApprovalService (design)

- **Blocker:** no state machine. You can approve a `Draft`, re-approve an `Approved` budget, or approve a `Locked` one. Enforce allowed transitions (enum + guard) and return a clear error.
- **Blocker:** the `force` flag lets anyone bypass segregation of duties (a finance audit/SOX concern). Remove it, or put it behind an authorization policy, and audit-log it.
- **Should:** a static `List<Budget>` is a global mutable store: not thread-safe or persistent, and can't be tested in isolation. Inject a repository or `DbContext`.
- **Should:** `try { First } catch { return null; }` uses exceptions for control flow and returns `null`. Use `FirstOrDefault` and return `NotFound` or a Result type.
- **Should:** `throw new Exception` should be a specific exception or a domain result.
- **Should:** `new SmtpClient` inside the method makes it untestable and `SmtpClient` is obsolete; a send failure after the status change leaves the system half-done. Inject `INotificationService`, or raise a domain event or use an outbox and send after commit.
- **Should:** boolean parameters (`Approve(1, "x", true, false)` is unreadable). Public fields. `Status` is a string; use an enum. `DateTime.Now` should be `TimeProvider`/UTC.
- **Nit:** `Console.WriteLine` with concatenation should be `ILogger` structured logging: `_log.LogInformation("Approved {BudgetId}", id)`.

## 6. AI-generated endpoint ("tests pass, LGTM?")

Open with: *"Passing tests tell me it compiles and the happy path works. Here's what I'd still check before merging AI-generated code."*

- **Blocker:** `GET` returns *every* fact with no paging or filter. That means an unbounded result, memory blow-up, and a huge JSON payload. Returning entities with `Include` can also hit JSON reference cycles (Account → Facts → Account). Project to DTOs and page.
- **Blocker:** `PUT` binds directly to the `Account` **entity**. That's **over-posting** (a client can send `Facts`), and `Update` overwrites every column. Use a request DTO, load the entity, map the allowed fields, and add concurrency checks.
- **Blocker:** **no authorization** on endpoints that change financial data.
- **Blocker:** `Task.Run` captures the **request-scoped DbContext**, which is disposed when the request ends, so you get `ObjectDisposedException`. It's also fire-and-forget: exceptions are lost and the work is lost if the server restarts. Use a `BackgroundService` + queue (or Hangfire) that creates its own scope, or a single set-based `ExecuteUpdateAsync`:
  `db.Facts.Where(f => f.Scenario == Scenario.Forecast).ExecuteUpdateAsync(s => s.SetProperty(f => f.Amount, f => f.Amount * 1.03m))`
- **Should:** `/recalculate` isn't idempotent; each call compounds another +3%. There's no `CancellationToken`, and `Results` is untyped (worse OpenAPI). And `v2` alongside what? Check the versioning strategy.
- **Meta-point:** say how you'd *use* AI here: ask it to write the failing tests for these cases, and review its diff the same way you review a human's.

## 7. C# gotchas (outputs verified by running them)

- **A:** prints `333`. The `for` variable is shared by every closure. (`foreach` gets a fresh variable per iteration since C# 5.) Fix: copy to a local inside the loop.
- **B:** prints `3`. LINQ is **deferred**; `Where` runs at `Count()`, after `Add(5)`, so it counts 3, 4, 5.
- **C:** `list[0].Add(5)` compiles but changes a **copy** (the struct is returned by value), so the list still holds 10. Arrays give a reference to the element, so `arr[0]` prints `15`. Lesson: make structs immutable (`readonly record struct`).
- **D:** throws `InvalidOperationException` (collection modified during enumeration). Fix: `accounts.RemoveAll(a => a.StartsWith('9'))`.
- **E:** `True`, but `ToUpper` depends on culture (in Turkish, `i` doesn't uppercase to `I`). Use `string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase)`. `o1 == o2` prints `False`: the boxed values are compared by **reference**. Use `Equals`.
- **F:** iterator bodies are lazy. Prints `called start 1`; nothing runs until the first `MoveNext`.
- **G:** **sequential** (about 12 × 10 ms). The `Select` is lazy, so each task starts only when it's awaited. Use `await Task.WhenAll(tasks)` for parallel, and `Parallel.ForEachAsync` or `SemaphoreSlim` to limit concurrency against a real API.
