// SCENARIO 4 — "Cache FX rates so consolidation doesn't hammer the rates API." Registered as a Singleton.
namespace Prophix.Review.Scenarios;

public interface IFxRateApi { Task<decimal> GetRateAsync(string from, string to, DateOnly date); }

public class FxRateCache(IFxRateApi api)
{
    private static Dictionary<string, decimal> _cache = new();

    public decimal GetRate(string from, string to)
    {
        var key = from + to;
        if (!_cache.ContainsKey(key))
        {
            lock (this)
            {
                _cache[key] = api.GetRateAsync(from, to, DateOnly.FromDateTime(DateTime.Now)).GetAwaiter().GetResult();
            }
        }
        return _cache[key];
    }

    public decimal Convert(decimal amount, string from, string to) =>
        from.ToLower() == to.ToLower() ? amount : amount * GetRate(from, to);
}
