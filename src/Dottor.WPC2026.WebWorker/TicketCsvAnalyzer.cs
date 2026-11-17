using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

namespace Dottor.WPC2026.WebWorker;

/// <summary>
/// Elaborazione CPU-bound volutamente "pesante": la stessa funzione viene eseguita
/// sul thread della UI oppure nel Web Worker, per confrontare la reattività della pagina.
/// </summary>
public static class TicketCsvAnalyzer
{
    private static readonly string[] Categories = ["Hardware", "Software", "Rete", "Accessi", "Stampanti", "Email"];
    private static readonly string[] Priorities = ["Bassa", "Media", "Alta", "Critica"];

    public static string Generate(int rows)
    {
        var random = new Random(2026);
        var builder = new StringBuilder(rows * 40);
        builder.AppendLine("id;categoria;priorita;minuti_risoluzione;riaperto");
        for (var i = 1; i <= rows; i++)
        {
            builder.Append(i).Append(';')
                   .Append(Categories[random.Next(Categories.Length)]).Append(';')
                   .Append(Priorities[random.Next(Priorities.Length)]).Append(';')
                   .Append(random.Next(5, 2_880)).Append(';')
                   .Append(random.Next(10) == 0 ? "si" : "no").Append('\n');
        }
        return builder.ToString();
    }

    public static CsvSummary Analyze(string csv)
    {
        var stopwatch = Stopwatch.StartNew();
        var minutesByCategory = new Dictionary<string, List<int>>();
        var reopenedByCategory = new Dictionary<string, int>();
        var rows = 0;

        foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            var fields = line.Split(';');
            var category = fields[1];
            var minutes = int.Parse(fields[3], CultureInfo.InvariantCulture);

            if (!minutesByCategory.TryGetValue(category, out var values))
            {
                minutesByCategory[category] = values = [];
                reopenedByCategory[category] = 0;
            }
            values.Add(minutes);
            if (fields[4] == "si")
            {
                reopenedByCategory[category]++;
            }
            rows++;
        }

        var summary = minutesByCategory
            .Select(pair =>
            {
                var sorted = pair.Value.Order().ToArray();
                return new CategorySummary(
                    pair.Key,
                    sorted.Length,
                    Math.Round(sorted.Average() / 60, 1),
                    Math.Round(sorted[(int)(sorted.Length * 0.95)] / 60.0, 1),
                    reopenedByCategory[pair.Key]);
            })
            .OrderByDescending(c => c.Tickets)
            .ToList();

        return new CsvSummary(rows, summary, stopwatch.Elapsed.TotalMilliseconds);
    }
}

public sealed record CategorySummary(string Category, int Tickets, double AverageHours, double P95Hours, int Reopened);

public sealed record CsvSummary(int Rows, IReadOnlyList<CategorySummary> Categories, double ElapsedMs);

// Serializzazione generata a compile time: funziona anche con il trimming di WebAssembly.
[JsonSerializable(typeof(CsvSummary))]
public sealed partial class WorkerJsonContext : JsonSerializerContext;