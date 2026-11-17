using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Services;

/// <summary>Genera il diario della pizzeria (cucina e rider) con descrizioni di lunghezza molto diversa (altezze variabili).</summary>
public static class TimelineEventGenerator
{
    private static readonly string[] Titles =
    [
        "Forno a 450 °C", "Rider partito 🛵", "Mozzarella in esaurimento", "Impasto lievitato",
        "Nuovo ordine", "Pagamento rifiutato", "Pizza consegnata", "Citofono che non risponde",
        "Recensione a 5 stelle", "Rider sotto la pioggia", "Basilico finito", "🍍 Ananas rilevato"
    ];

    private const string LoremText =
        "La cucina ha registrato un'attività significativa durante la preparazione dell'ordine. " +
        "Sono stati coinvolti il banco impasti, il forno a legna e il reparto farciture, e i dettagli sono finiti sul blocchetto delle comande. " +
        "Il pizzaiolo può consultare la comanda per analizzare tempi di cottura, ingredienti e il percorso del rider. " +
        "Non sono richieste azioni immediate, ma è consigliato tenere d'occhio il forno nei prossimi minuti. ";

    public static List<TimelineEvent> Generate(int count)
    {
        var random = new Random(11);
        var start = new DateTime(2026, 5, 1, 0, 0, 0);
        return [.. Enumerable.Range(0, count).Select(i => Create(i + 1, start.AddMinutes(i * 3), random))];
    }

    public static TimelineEvent Create(int id, DateTime timestamp, Random random)
    {
        // Da 1 a 6 ripetizioni del paragrafo: ogni elemento ha un'altezza diversa.
        var length = random.Next(40, LoremText.Length * 6);
        var description = string.Concat(Enumerable.Repeat(LoremText, 6))[..length].Trim() + ".";
        return new TimelineEvent(
            id,
            timestamp,
            Titles[random.Next(Titles.Length)],
            description,
            (EventSeverity)random.Next(4));
    }
}
