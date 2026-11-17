using System.Collections.Concurrent;

namespace Dottor.WPC2026.Web.Services;

/// <summary>
/// Synthetic, in-memory report generator for the CacheView demo. Generation counters live in this
/// process only: they reset on restart and are not shared between nodes.
/// </summary>
public sealed class CategoryReportService
{
    public static readonly TimeSpan SimulatedWork = TimeSpan.FromMilliseconds(300);
    public static IReadOnlyList<string> Categories { get; } = ["hardware", "software"];

    private static readonly Dictionary<string, (string Product, int Units, decimal UnitPrice)[]> Catalog = new()
    {
        ["hardware"] =
        [
            ("Notebook 14\"", 42, 1_190m),
            ("Monitor 27\"", 75, 289m),
            ("Docking station USB-C", 64, 159m),
            ("Tastiera meccanica", 120, 89m),
        ],
        ["software"] =
        [
            ("Licenza IDE annuale", 38, 499m),
            ("Pipeline CI (minuti extra)", 210, 12m),
            ("Suite produttività", 95, 129m),
            ("Monitoraggio applicativo", 27, 340m),
        ],
    };

    private readonly ConcurrentDictionary<string, int> generations = new();

    public static bool IsKnown(string? category) => category is not null && Catalog.ContainsKey(category);

    public async Task<CategoryReport> GenerateAsync(string category, string portion, CancellationToken cancellationToken)
    {
        if (!Catalog.TryGetValue(category, out var products))
        {
            throw new ArgumentOutOfRangeException(nameof(category), category, "Categoria non prevista dalla demo.");
        }

        // Simulates an expensive query without blocking a thread-pool thread.
        await Task.Delay(SimulatedWork, cancellationToken);

        var generation = generations.AddOrUpdate($"{portion}:{category}", 1, (_, current) => current + 1);
        var rows = products.Select(p => new CategoryReportRow(p.Product, p.Units, p.Units * p.UnitPrice)).ToArray();
        return new CategoryReport(category, rows, DateTimeOffset.Now, generation);
    }
}

public sealed record CategoryReportRow(string Product, int Units, decimal Revenue);

public sealed record CategoryReport(string Category, IReadOnlyList<CategoryReportRow> Rows, DateTimeOffset GeneratedAt, int Generation)
{
    public decimal Total => Rows.Sum(r => r.Revenue);
}
