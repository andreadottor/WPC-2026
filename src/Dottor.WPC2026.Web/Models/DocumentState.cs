using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Dottor.WPC2026.Web.Models;

/// <summary>Stato condiviso tra UI e agente (UIAgent&lt;DocumentState&gt;): la scheda della pizza del mese.</summary>
public sealed class DocumentState
{
    [Description("Nome della pizza")]
    public string Title { get; set; } = "";

    [Description("Breve descrizione della pizza")]
    public string Description { get; set; } = "";

    [Description("Sezioni della scheda (impasto, ingredienti, ...), in ordine")]
    public List<DocumentSection> Sections { get; set; } = [];

    public DocumentState Clone() => new()
    {
        Title = Title,
        Description = Description,
        Sections = [.. Sections.Select(s => new DocumentSection { Heading = s.Heading, Body = s.Body })]
    };

    public static DocumentState CreateSample() => new()
    {
        Title = "La Blazor: la pizza del mese",
        Description = "La pizza ha l'obiettivo di soddisfare gli sviluppatori che hanno fretta di rilasciare, in modo che possano mangiare senza staccarsi dalla tastiera.",
        Sections =
        [
            new()
            {
                Heading = "Impasto",
                Body = "Al fine di ottenere un impasto leggero, la lievitazione avviene in modo lento per 48 ore. Nel caso in cui l'impasto risulti essere troppo idratato, il pizzaiolo provvede ad aggiungere della farina."
            },
            new()
            {
                Heading = "Ingredienti",
                Body = "La pizza viene farcita con pomodoro San Marzano, fior di latte e basilico fresco. In uscita dal forno vengono aggiunti dei pomodorini confit in modo da dare colore. L'ananas non è previsto."
            },
            new()
            {
                Heading = "Cottura e consegna",
                Body = "La cottura avviene in forno a legna per 90 secondi al fine di mantenere il cornicione morbido. La consegna avviene tramite rider in modo da arrivare ancora calda."
            }
        ]
    };
}

public sealed class DocumentSection
{
    [Description("Titolo della sezione")]
    public string Heading { get; set; } = "";

    [Description("Testo della sezione")]
    public string Body { get; set; } = "";
}

/// <summary>Risultato del tool server-side "analyze_document" (non distruttivo).</summary>
public sealed class DocumentAnalysis
{
    [JsonPropertyName("words")]
    public int Words { get; set; }
    [JsonPropertyName("sections")]
    public int Sections { get; set; }
    [JsonPropertyName("readingMinutes")]
    public int ReadingMinutes { get; set; }
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = "";
}
