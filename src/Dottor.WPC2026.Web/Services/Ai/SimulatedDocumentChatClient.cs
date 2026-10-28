using System.Runtime.CompilerServices;
using System.Text.Json;
using Dottor.WPC2026.Web.Models;
using Microsoft.Extensions.AI;

namespace Dottor.WPC2026.Web.Services.Ai;

/// <summary>
/// IChatClient deterministico usato in "modalità dimostrativa" (nessuna chiave, nessuna rete).
/// Riconosce poche parole chiave in italiano e produce le stesse forme di contenuto di un modello reale:
/// testo in streaming, chiamate a UI action (propose_document) e a tool server-side (analyze_document, publish_document).
/// </summary>
public sealed class SimulatedDocumentChatClient : IChatClient
{
    public const string ProposeDocumentTool = "propose_document";
    public const string AnalyzeDocumentTool = "analyze_document";
    public const string PublishDocumentTool = "publish_document";

    private static readonly (string From, string To)[] ClarityReplacements =
    [
        ("al fine di", "per"),
        ("Al fine di", "Per"),
        ("nel caso in cui", "se"),
        ("Nel caso in cui", "Se"),
        ("risulti essere", "sia"),
        ("risulta essere", "è"),
        ("di effettuare la prenotazione delle", "di prenotare le"),
        ("effettua l'accesso ai dati", "accede ai dati"),
        ("vengono inviate in modo da avvisare", "avvisano"),
        ("Il progetto ha l'obiettivo di realizzare", "Il progetto realizza"),
    ];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken);

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var history = messages.ToList();
        var document = DocumentContextChatClient.FindDocument(history) ?? DocumentState.CreateSample();
        var messageId = Guid.NewGuid().ToString("N");
        var last = history.LastOrDefault(m => m.Role != ChatRole.System);

        // Dopo l'esecuzione di un tool o di una UI action il modello "commenta" il risultato.
        if (last?.Role == ChatRole.Tool)
        {
            var text = DescribeToolResults(history, last);
            await foreach (var update in StreamText(text, messageId, cancellationToken))
            {
                yield return update;
            }
            yield break;
        }

        var prompt = last?.Text ?? string.Empty;
        var lower = prompt.ToLowerInvariant();

        if (ContainsAny(lower, "pubblica"))
        {
            await foreach (var update in StreamText("Preparo la pubblicazione: serve la tua approvazione.", messageId, cancellationToken))
            {
                yield return update;
            }
            yield return FunctionCall(messageId, PublishDocumentTool, new() { ["version"] = "1.0" });
            yield break;
        }

        if (ContainsAny(lower, "riassumi", "riepilogo", "analizza", "statistiche"))
        {
            yield return FunctionCall(messageId, AnalyzeDocumentTool, []);
            yield break;
        }

        var proposal = CreateProposal(document, lower);
        if (proposal is not null)
        {
            await foreach (var update in StreamText(proposal.Value.Intro, messageId, cancellationToken))
            {
                yield return update;
            }
            yield return FunctionCall(messageId, ProposeDocumentTool, new()
            {
                ["document"] = JsonSerializer.SerializeToElement(proposal.Value.Document, JsonSerializerOptions.Web)
            });
            yield break;
        }

        const string help =
            "Sono in **modalità dimostrativa**: riconosco alcune richieste. Prova con:\n\n" +
            "- *Rendi la descrizione più tecnica*\n" +
            "- *Aggiungi una sezione sulla sicurezza*\n" +
            "- *Proponi un titolo alternativo*\n" +
            "- *Migliora la chiarezza del testo*\n" +
            "- *Riassumi il documento*\n" +
            "- *Pubblica il documento*";
        await foreach (var update in StreamText(help, messageId, cancellationToken))
        {
            yield return update;
        }
    }

    private static (string Intro, DocumentState Document)? CreateProposal(DocumentState current, string prompt)
    {
        var document = current.Clone();

        if (ContainsAny(prompt, "tecnic"))
        {
            document.Description =
                "Applicazione web Blazor (.NET 11, render mode Interactive Server) che espone il catalogo delle sale, " +
                "la disponibilità in tempo reale e la gestione delle prenotazioni tramite API REST autenticate con Microsoft Entra ID.";
            return ("Ecco una descrizione più tecnica. La vedi in anteprima a sinistra: confermala o rifiutala.", document);
        }

        if (ContainsAny(prompt, "sicurezza"))
        {
            document.Sections.Add(new DocumentSection
            {
                Heading = "Sicurezza",
                Body = "L'accesso richiede autenticazione aziendale. Ogni prenotazione è tracciata con utente e orario; " +
                       "i dati personali sono conservati per 12 mesi e cifrati a riposo."
            });
            return ("Propongo una nuova sezione \"Sicurezza\" in fondo al documento.", document);
        }

        if (ContainsAny(prompt, "titolo"))
        {
            document.Title = "RoomBook – prenotazione intelligente delle sale riunioni";
            return ("Che ne dici di questo titolo?", document);
        }

        if (ContainsAny(prompt, "chiarezza", "chiaro", "migliora", "semplifica"))
        {
            document.Description = Simplify(document.Description);
            foreach (var section in document.Sections)
            {
                section.Body = Simplify(section.Body);
            }
            return ("Ho semplificato le frasi più prolisse. Le differenze sono evidenziate.", document);
        }

        return null;
    }

    private static string Simplify(string text)
    {
        foreach (var (from, to) in ClarityReplacements)
        {
            text = text.Replace(from, to, StringComparison.Ordinal);
        }
        return text;
    }

    private static string DescribeToolResults(List<ChatMessage> history, ChatMessage toolMessage)
    {
        var calls = history.SelectMany(m => m.Contents).OfType<FunctionCallContent>()
            .GroupBy(c => c.CallId).ToDictionary(g => g.Key, g => g.Last().Name);

        var parts = new List<string>();
        foreach (var result in toolMessage.Contents.OfType<FunctionResultContent>())
        {
            var name = calls.GetValueOrDefault(result.CallId);
            var value = result.Result switch
            {
                JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
                JsonElement json => json.GetRawText(),
                null => string.Empty,
                var other => other.ToString()
            } ?? string.Empty;
            var rejected = value.Contains("reject", StringComparison.OrdinalIgnoreCase)
                           || value.Contains("rifiut", StringComparison.OrdinalIgnoreCase);

            parts.Add(name switch
            {
                ProposeDocumentTool when rejected => "Va bene, ho scartato la proposta: il documento è rimasto invariato.",
                ProposeDocumentTool => "Perfetto, la modifica è stata applicata al documento.",
                PublishDocumentTool when rejected => "Pubblicazione annullata: nessuna azione è stata eseguita.",
                PublishDocumentTool => $"Documento pubblicato. {value}",
                AnalyzeDocumentTool => "Ecco l'analisi del documento (vedi la scheda qui sopra). " +
                                       "Il testo è breve: puoi chiedermi di migliorarne la chiarezza.",
                _ => $"Ho ricevuto il risultato di {name}."
            });
        }

        return parts.Count > 0 ? string.Join(" ", parts) : "Fatto.";
    }

    private static ChatResponseUpdate FunctionCall(string messageId, string name, Dictionary<string, object?> arguments) =>
        new(ChatRole.Assistant, [new FunctionCallContent($"call_{Guid.NewGuid():N}", name, arguments)])
        {
            MessageId = messageId
        };

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamText(
        string text, string messageId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var words = text.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            await Task.Delay(35, cancellationToken);
            yield return new ChatResponseUpdate(ChatRole.Assistant, i == 0 ? words[i] : " " + words[i])
            {
                MessageId = messageId
            };
        }
    }

    private static bool ContainsAny(string text, params string[] values) =>
        values.Any(v => text.Contains(v, StringComparison.Ordinal));

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
    }
}
