using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.AI;
using Dottor.WPC2026.Web.Models;
using Dottor.WPC2026.Web.Services.Ai;

namespace Dottor.WPC2026.Web.Components.Shared;

public partial class AgenticWorkspace
{
    private static readonly string[] Suggestions =
    [
        "Rendi la descrizione più golosa",
        "Aggiungi una sezione sugli allergeni",
        "Proponi un nome più simpatico",
        "Migliora la chiarezza del testo",
        "Riassumi la scheda",
        "Aggiungi la pizza al menu a 9,50 €"
    ];

    private IDisposable? _stateRegistration;
    private IDisposable? _statusRegistration;
    private DocumentState _baseline = new();
    private ConversationStatus _lastStatus;

    [CascadingParameter]
    public AgentContext AgentContext { get; set; } = default!;

    [Inject]
    private ILogger<AgenticWorkspace> Logger { get; set; } = default!;

    [Parameter, EditorRequired]
    public UIAgent<DocumentState> Agent { get; set; } = default!;

    [Parameter, EditorRequired]
    public AiOptions Ai { get; set; } = default!;

    [Parameter]
    public string? PublishedInfo { get; set; }

    [Parameter]
    public EventCallback ResetRequested { get; set; }

    private DocumentState Document => Agent.State.Value;

    private bool IsRunning => AgentContext.Status is ConversationStatus.Streaming or ConversationStatus.AwaitingInput;

    protected override void OnInitialized()
    {
        _baseline = Document.Clone();
        _lastStatus = AgentContext.Status;
        _stateRegistration = Agent.State.OnChanged(() => _ = InvokeAsync(StateHasChanged));
        _statusRegistration = AgentContext.RegisterOnStatusChanged(status => _ = InvokeAsync(() =>
        {
            // Fotografa il documento all'inizio di ogni richiesta: è la base del diff.
            if (status == ConversationStatus.Streaming && _lastStatus is not (ConversationStatus.Streaming or ConversationStatus.AwaitingInput)
                && !Agent.State.HasPendingPredictiveState)
            {
                _baseline = Document.Clone();
            }
            _lastStatus = status;
            if (status == ConversationStatus.Error)
            {
                Logger.LogError(AgentContext.Error, "Demo 10: errore durante il turno dell'agente");
            }
            StateHasChanged();
        }));
    }

    private Task SendAsync(string prompt) => AgentContext.SendMessageAsync(prompt, CancellationToken.None);

    private Task ResetAsync() => ResetRequested.InvokeAsync();

    public void Dispose()
    {
        _stateRegistration?.Dispose();
        _statusRegistration?.Dispose();
    }
}
