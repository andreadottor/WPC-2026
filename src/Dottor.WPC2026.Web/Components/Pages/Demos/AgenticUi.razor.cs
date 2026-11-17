using Microsoft.AspNetCore.Components.AI;
using Microsoft.Extensions.AI;
using Dottor.WPC2026.Web.Models;
using Dottor.WPC2026.Web.Services.Ai;
using System.ComponentModel;
using System.Text.Json;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class AgenticUi
{
    private UIAgent<DocumentState> _agent = default!;
    private IChatClient _documentClient = default!;
    private string? _publishedInfo;

    protected override void OnInitialized()
    {
        _documentClient = new DocumentContextChatClient(ChatClient, () => _agent.State.Value);
        _agent = CreateAgent();
    }

    private UIAgent<DocumentState> CreateAgent() => new(_documentClient, options =>
    {
        options.ChatOptions = new ChatOptions
        {
            Instructions =
                "Sei l'assistente del pizzaiolo di \"Blazor da Asporto\" e aiuti a scrivere, in italiano, la scheda della pizza del mese (il documento). " +
                "Il documento corrente è fornito in JSON in un messaggio di sistema. " +
                "Per QUALSIASI modifica al documento chiama sempre il tool propose_document passando il documento COMPLETO aggiornato " +
                "(titolo, descrizione e tutte le sezioni): l'utente vedrà l'anteprima e deciderà se accettarla. Non riscrivere il documento nel testo della risposta. " +
                "Quando ricevi l'esito di propose_document (ACCEPTED o REJECTED) la richiesta è conclusa: conferma in una frase e NON chiamare di nuovo propose_document. " +
                "Per riassunti o statistiche chiama analyze_document. " +
                "Per aggiungere la pizza al menu chiama publish_document. Rispondi in italiano con frasi brevi e un tocco di ironia.",
            Tools =
            [
                AIFunctionFactory.Create(AnalyzeDocument, DocumentTools.Analyze,
                    "Analizza la scheda corrente della pizza (parole, sezioni, tempo di lettura) senza modificarla."),
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(PublishDocument, DocumentTools.Publish,
                    "Aggiunge al menu la pizza descritta nella scheda corrente, al prezzo indicato. Richiede l'approvazione dell'utente."))
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
            "Propone all'utente una nuova versione completa della scheda della pizza, che può accettarla o rifiutarla."));

        options.AddGeneratedToolBlocks();
    }, LoggerFactory, DocumentState.CreateSample());

    private string ReviewProposal([Description("Scheda della pizza completa e aggiornata")] DocumentState document, bool? accepted = null)
    {
        var isAccepted = accepted ?? throw new InvalidOperationException("La proposta non è ancora stata valutata dall'utente.");
        if (!_agent.State.HasPendingPredictiveState)
        {
            throw new InvalidOperationException("La proposta non è più in attesa.");
        }

        if (isAccepted)
        {
            _agent.State.AcceptPredictiveState();
            return "ACCEPTED: l'utente ha accettato la proposta, ora la scheda è aggiornata. Richiesta conclusa: conferma brevemente senza chiamare altri tool.";
        }

        _agent.State.RejectPredictiveState();
        return "REJECTED: l'utente ha rifiutato la proposta, la scheda è invariata. Conferma brevemente senza proporre altro.";
    }

    private DocumentAnalysis AnalyzeDocument()
    {
        var document = _agent.State.Value;
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

    private string PublishDocument([Description("Prezzo di vendita in euro, es. 9,50")] string price)
    {
        _publishedInfo = $"🍕 \"{_agent.State.Value.Title}\" aggiunta al menu a {price} € alle {DateTime.Now:HH:mm:ss}";
        _ = InvokeAsync(StateHasChanged);
        return _publishedInfo + ".";
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
        var previous = _agent;
        _publishedInfo = null;
        _agent = CreateAgent();
        previous.Dispose();
    }

    public void Dispose() => _agent?.Dispose();
}
