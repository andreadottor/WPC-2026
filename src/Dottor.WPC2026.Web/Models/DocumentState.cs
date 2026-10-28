using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Dottor.WPC2026.Web.Models;

/// <summary>Stato condiviso tra UI e agente (UIAgent&lt;DocumentState&gt;).</summary>
public sealed class DocumentState
{
    [Description("Titolo del documento")]
    public string Title { get; set; } = "";

    [Description("Breve descrizione del documento")]
    public string Description { get; set; } = "";

    [Description("Sezioni del documento, in ordine")]
    public List<DocumentSection> Sections { get; set; } = [];

    public DocumentState Clone() => new()
    {
        Title = Title,
        Description = Description,
        Sections = [.. Sections.Select(s => new DocumentSection { Heading = s.Heading, Body = s.Body })]
    };

    public static DocumentState CreateSample() => new()
    {
        Title = "Portale prenotazione sale riunioni",
        Description = "Il progetto ha l'obiettivo di realizzare un portale che consenta ai dipendenti di effettuare la prenotazione delle sale riunioni in modo semplice.",
        Sections =
        [
            new()
            {
                Heading = "Obiettivi",
                Body = "Al fine di ridurre i conflitti di prenotazione, il portale deve mostrare in tempo reale la disponibilità delle sale. Nel caso in cui una sala risulti essere occupata, il sistema propone delle alternative."
            },
            new()
            {
                Heading = "Architettura",
                Body = "L'applicazione è una Blazor Web App .NET 11 che effettua l'accesso ai dati tramite API REST. Le notifiche vengono inviate in modo da avvisare gli utenti delle modifiche."
            },
            new()
            {
                Heading = "Rilascio",
                Body = "Il rilascio avviene in modo graduale al fine di raccogliere il feedback dei primi utenti."
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
