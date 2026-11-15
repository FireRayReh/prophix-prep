namespace Prophix.Api.Domain;

// FP&A domain in miniature: a "fact table" of amounts sliced by dimensions
// (Account x Department x Period x Scenario) — the same shape an OLAP cube uses.

public enum AccountType { Revenue, Expense }

public enum Scenario { Budget, Actual, Forecast }

public class Department
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class Account
{
    public int Id { get; set; }
    public required string Code { get; set; }   // e.g. "4000" — unique index
    public required string Name { get; set; }
    public AccountType Type { get; set; }
    public List<FactEntry> Facts { get; set; } = [];
}

public class FactEntry
{
    public long Id { get; set; }
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }              // 1..12 (fiscal period)
    public Scenario Scenario { get; set; }
    public decimal Amount { get; set; }         // money => decimal, never double
}
