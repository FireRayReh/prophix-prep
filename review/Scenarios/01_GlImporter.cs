// SCENARIO 1 — A teammate's PR: "Import GL actuals from the ERP's CSV export."
// Review it out loud. What would you block on, what's a nit, and why?
using System.Globalization;

namespace Prophix.Review.Scenarios;

public class GlImporter
{
    public List<(string Account, decimal Amount)> Rows = new();

    public async void Import(string url)
    {
        var client = new HttpClient();
        var csv = client.GetStringAsync(url).Result;

        foreach (var line in csv.Split('\n'))
        {
            try
            {
                var parts = line.Split(',');
                var amount = decimal.Parse(parts[1]);
                Rows.Add((parts[0], amount));
            }
            catch (Exception)
            {
            }
        }

        var log = new StreamWriter("import.log");
        log.WriteLine($"Imported {Rows.Count} rows at {DateTime.Now}");
    }
}
