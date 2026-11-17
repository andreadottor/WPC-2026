using System.Collections.Concurrent;

namespace Dottor.WPC2026.Web.Services;

/// <summary>
/// Menu della pizzeria con le vendite del giorno, generato in memoria per la demo CacheView. I contatori vivono in questo
/// processo: si azzerano al riavvio e non sono condivisi tra i nodi.
/// </summary>
public sealed class MenuService
{
    public static readonly TimeSpan SimulatedWork = TimeSpan.FromMilliseconds(300);
    public static IReadOnlyList<string> Categories { get; } = ["classiche", "speciali"];

    private static readonly Dictionary<string, (string Product, int Units, decimal UnitPrice)[]> Catalog = new()
    {
        ["classiche"] =
        [
            ("Margherita", 142, 7m),
            ("Marinara", 58, 6m),
            ("Diavola", 97, 8.5m),
            ("Capricciosa", 76, 9m),
        ],
        ["speciali"] =
        [
            ("Bufalina con pomodorini", 64, 11m),
            ("Mortadella e pistacchio", 88, 12m),
            ("La Blazor (tutto in un componente)", 42, 13m),
            ("Hawaiana 🍍 (su richiesta scritta)", 3, 15m),
        ],
    };
    private readonly ConcurrentDictionary<string, int> _generations = new();

    public static bool IsKnown(string? category) => category is not null && Catalog.ContainsKey(category);

    public async Task<MenuReport> GenerateAsync(string category, string portion, CancellationToken cancellationToken)
    {
        if (!Catalog.TryGetValue(category, out var products))
        {
            throw new ArgumentOutOfRangeException(nameof(category), category, "Categoria non prevista dal menu.");
        }

        // Simula una query costosa (il pizzaiolo che conta le pizze) senza bloccare un thread del pool.
        await Task.Delay(SimulatedWork, cancellationToken);

        var generation = _generations.AddOrUpdate($"{portion}:{category}", 1, (_, current) => current + 1);
        var rows = products.Select(p => new MenuRow(p.Product, p.Units, p.Units * p.UnitPrice)).ToArray();
        return new MenuReport(category, rows, DateTimeOffset.Now, generation);
    }
}

public sealed record MenuRow(string Product, int Units, decimal Revenue);

public sealed record MenuReport(string Category, IReadOnlyList<MenuRow> Rows, DateTimeOffset GeneratedAt, int Generation)
{
    public decimal Total => Rows.Sum(r => r.Revenue);
}
