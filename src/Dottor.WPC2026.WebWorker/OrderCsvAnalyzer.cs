using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

namespace Dottor.WPC2026.WebWorker;

/// <summary>
/// Elaborazione CPU-bound volutamente "pesante": la stessa funzione viene eseguita
/// sul thread della UI oppure nel Web Worker, per confrontare la reattività della pagina.
/// </summary>
public static class OrderCsvAnalyzer
{
    private static readonly string[] Pizzas = ["Margherita", "Diavola", "Capricciosa", "Quattro formaggi", "Marinara", "Bufalina"];
    private static readonly string[] Zones = ["Centro", "Stazione", "Fiera", "Periferia"];

    public static string Generate(int rows)
    {
        var random = new Random(2026);
        var builder = new StringBuilder(rows * 40);
        builder.AppendLine("id;pizza;zona;minuti_consegna;reclamo");
        for (var i = 1; i <= rows; i++)
        {
            builder.Append(i).Append(';')
                   .Append(Pizzas[random.Next(Pizzas.Length)]).Append(';')
                   .Append(Zones[random.Next(Zones.Length)]).Append(';')
                   .Append(random.Next(10, 75)).Append(';')
                   .Append(random.Next(20) == 0 ? "si" : "no").Append('\n');
        }
        return builder.ToString();
    }

    public static CsvSummary Analyze(string csv)
    {
        var stopwatch = Stopwatch.StartNew();
        var minutesByPizza = new Dictionary<string, List<int>>();
        var complaintsByPizza = new Dictionary<string, int>();
        var rows = 0;

        foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            var fields = line.Split(';');
            var pizza = fields[1];
            var minutes = int.Parse(fields[3], CultureInfo.InvariantCulture);

            if (!minutesByPizza.TryGetValue(pizza, out var values))
            {
                minutesByPizza[pizza] = values = [];
                complaintsByPizza[pizza] = 0;
            }
            values.Add(minutes);
            if (fields[4] == "si")
            {
                complaintsByPizza[pizza]++;
            }
            rows++;
        }

        var summary = minutesByPizza
            .Select(pair =>
            {
                var sorted = pair.Value.Order().ToArray();
                return new PizzaSummary(
                    pair.Key,
                    sorted.Length,
                    Math.Round(sorted.Average(), 1),
                    sorted[(int)(sorted.Length * 0.95)],
                    complaintsByPizza[pair.Key]);
            })
            .OrderByDescending(p => p.Orders)
            .ToList();

        return new CsvSummary(rows, summary, stopwatch.Elapsed.TotalMilliseconds);
    }
}

public sealed record PizzaSummary(string Pizza, int Orders, double AverageMinutes, int P95Minutes, int Complaints);

public sealed record CsvSummary(int Rows, IReadOnlyList<PizzaSummary> Pizzas, double ElapsedMs);

// Serializzazione generata a compile time: funziona anche con il trimming di WebAssembly.
[JsonSerializable(typeof(CsvSummary))]
public sealed partial class WorkerJsonContext : JsonSerializerContext;