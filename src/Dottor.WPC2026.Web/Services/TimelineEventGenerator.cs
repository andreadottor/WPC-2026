using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Services;

/// <summary>Genera eventi con descrizioni di lunghezza molto diversa (altezze variabili).</summary>
public static class TimelineEventGenerator
{
    private static readonly string[] Titles =
    [
        "Deploy completato", "Utente autenticato", "Timeout database", "Cache invalidata",
        "Ordine creato", "Pagamento rifiutato", "Job notturno avviato", "Certificato in scadenza",
        "Memoria oltre soglia", "Nuovo commento", "Backup completato", "Errore di validazione"
    ];

    private const string LoremText =
        "Il servizio ha registrato un'attività significativa durante l'elaborazione della richiesta. " +
        "Sono stati coinvolti più componenti dell'infrastruttura e i dettagli sono stati salvati nel log applicativo. " +
        "Il team operativo può consultare la traccia distribuita per analizzare tempi di risposta e dipendenze. " +
        "Non sono richieste azioni immediate, ma è consigliato monitorare l'andamento nelle prossime ore. ";

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
