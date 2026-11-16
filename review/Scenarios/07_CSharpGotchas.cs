// SCENARIO 7 — Quick-fire: "What does this print / what's wrong?" (language-level questions)
namespace Prophix.Review.Scenarios;

public struct Money { public decimal Amount; public void Add(decimal x) => Amount += x; }

public class Gotchas
{
    public void A()
    {
        var actions = new List<Action>();
        for (int i = 0; i < 3; i++) actions.Add(() => Console.Write(i));
        actions.ForEach(a => a());                        // prints?
    }

    public void B()
    {
        var periods = new List<int> { 1, 2, 3, 4 };
        var open = periods.Where(p => p > 2);
        periods.Add(5);
        Console.WriteLine(open.Count());                  // prints?
    }

    public void C()
    {
        var list = new List<Money> { new() { Amount = 10 } };
        list[0].Add(5);                                   // compiles? if not, why — and what about an array?
        var arr = new[] { new Money { Amount = 10 } };
        arr[0].Add(5);
        Console.WriteLine(arr[0].Amount);                 // prints?
    }

    public void D(List<string> accounts)
    {
        foreach (var a in accounts)
            if (a.StartsWith("9")) accounts.Remove(a);    // what happens?
    }

    public void E()
    {
        string s1 = "Revenue", s2 = "REVENUE";
        Console.WriteLine(s1.ToUpper() == s2);            // ok? better?
        object o1 = 5m, o2 = 5m;
        Console.WriteLine(o1 == o2);                      // prints?
    }

    public IEnumerable<int> F()
    {
        Console.Write("start ");
        yield return 1;                                   // when does "start" print?
    }

    public async Task G()
    {
        var tasks = Enumerable.Range(1, 12).Select(async m => { await Task.Delay(10); return m; });
        var results = new List<int>();
        foreach (var t in tasks) results.Add(await t);    // sequential or parallel? Better?
    }
}
