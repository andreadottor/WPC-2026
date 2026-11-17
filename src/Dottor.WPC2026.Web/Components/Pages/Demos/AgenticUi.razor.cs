using Microsoft.AspNetCore.Components.AI;
using Microsoft.Extensions.AI;
using Dottor.WPC2026.Web.Models;
using Dottor.WPC2026.Web.Services.Ai;
using System.ComponentModel;
using System.Text.Json;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class AgenticUi
{
    private UIAgent<DocumentState> agent = default!;
    private IChatClient documentClient = default!;
    private string? publishedInfo;

    protected override void OnInitialized()
    {
        documentClient = new DocumentContextChatClient(ChatClient, () => agent.State.Value);
        agent = CreateAgent();
    }

    private UIAgent<DocumentState> CreateAgent() => new(documentClient, options =>
    {
        options.ChatOptions = new ChatOptions
        {
            Instructions =
                "Sei un assistente che aiuta a scrivere un documento di progetto in italiano. " +
                "Il documento corrente è fornito in JSON in un messaggio di sistema. " +
                "Per QUALSIASI modifica al documento chiama sempre il tool propose_document passando il documento COMPLETO aggiornato " +
                "(titolo, descrizione e tutte le sezioni): l'utente vedrà l'anteprima e deciderà se accettarla. Non riscrivere il documento nel testo della risposta. " +
                "Quando ricevi l'esito di propose_document (ACCEPTED o REJECTED) la richiesta è conclusa: conferma in una frase e NON chiamare di nuovo propose_document. " +
                "Per riassunti o statistiche chiama analyze_document. Per pubblicare chiama publish_document. " +
                "Rispondi in italiano con frasi brevi.",
            Tools =
            [
                AIFunctionFactory.Create(AnalyzeDocument, DocumentTools.Analyze,
                    "Analizza il documento corrente (parole, sezioni, tempo di lettura) senza modificarlo."),
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(PublishDocument, DocumentTools.Publish,
                    "Pubblica il documento corrente. Richiede l'approvazione dell'utente."))
            ]
        };

        // Una chiamata a propose_document diventa stato "predittivo": visibile subito, confermato solo dall'utente.
        options.StateMapper = context =>
        {
            var proposal = context.Update.Contents.OfType<FunctionCallContent>()
                .FirstOrDefault(call => call.Name == DocumentTools.Propose);
            if (proposal?.Arguments?.TryGetValue("document", out var value) == true && ReadDocument(value) is { } document)
            {
                context.SetPredictiveState(document);
            }
        };

        options.RegisterUIAction(AIFunctionFactory.Create(ReviewProposal, DocumentTools.Propose,
            "Propone all'utente una nuova versione completa del documento, che può accettarla o rifiutarla."));

        options.AddGeneratedToolBlocks();
    }, LoggerFactory, DocumentState.CreateSample());

    private string ReviewProposal([Description("Documento completo aggiornato")] DocumentState document, bool? accepted = null)
    {
        var isAccepted = accepted ?? throw new InvalidOperationException("La proposta non è ancora stata valutata dall'utente.");
        if (!agent.State.HasPendingPredictiveState)
        {
            throw new InvalidOperationException("La proposta non è più in attesa.");
        }

        if (isAccepted)
        {
            agent.State.AcceptPredictiveState();
            return "ACCEPTED: l'utente ha accettato la proposta, ora il documento è aggiornato. Richiesta conclusa: conferma brevemente senza chiamare altri tool.";
        }

        agent.State.RejectPredictiveState();
        return "REJECTED: l'utente ha rifiutato la proposta, il documento è invariato. Conferma brevemente senza proporre altro.";
    }

    private DocumentAnalysis AnalyzeDocument()
    {
        var document = agent.State.Value;
        var text = string.Join(' ', [document.Title, document.Description, .. document.Sections.SelectMany(s => new[] { s.Heading, s.Body })]);
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return new DocumentAnalysis
        {
            Words = words,
            Sections = document.Sections.Count,
            ReadingMinutes = Math.Max(1, (int)Math.Ceiling(words / 200.0)),
            Summary = $"\"{document.Title}\": {document.Description} Sezioni: {string.Join(", ", document.Sections.Select(s => s.Heading))}."
        };
    }

    private string PublishDocument([Description("Numero di versione da pubblicare, es. 1.0")] string version)
    {
        publishedInfo = $"Versione {version} pubblicata alle {DateTime.Now:HH:mm:ss}";
        _ = InvokeAsync(StateHasChanged);
        return publishedInfo + ".";
    }

    private static DocumentState? ReadDocument(object? value) => value switch
    {
        DocumentState document => document,
        JsonElement { ValueKind: JsonValueKind.Object } json => json.Deserialize<DocumentState>(JsonSerializerOptions.Web),
        JsonElement { ValueKind: JsonValueKind.String } json => JsonSerializer.Deserialize<DocumentState>(json.GetString()!, JsonSerializerOptions.Web),
        string text => JsonSerializer.Deserialize<DocumentState>(text, JsonSerializerOptions.Web),
        _ => null
    };

    private void Reset()
    {
        var previous = agent;
        publishedInfo = null;
        agent = CreateAgent();
        previous.Dispose();
    }

    public void Dispose() => agent?.Dispose();
}
