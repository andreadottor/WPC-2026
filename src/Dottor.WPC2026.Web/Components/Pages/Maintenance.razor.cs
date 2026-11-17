using Dottor.WPC2026.Web.Shutdown;

namespace Dottor.WPC2026.Web.Components.Pages;

public partial class Maintenance
{
    private IReadOnlyList<ActiveCircuit> circuits = [];
    private IReadOnlyList<DisconnectedCircuit> disconnected = [];
    private bool confirming;
    private bool stopRequested;

    protected override void OnInitialized()
    {
        circuits = Shutdown.ActiveCircuits;
        disconnected = Shutdown.DisconnectedCircuits;
        Shutdown.CircuitsChanged += OnCircuitsChanged;
    }

    private void OnCircuitsChanged() => _ = InvokeAsync(() =>
    {
        circuits = Shutdown.ActiveCircuits;
        disconnected = Shutdown.DisconnectedCircuits;
        StateHasChanged();
    });

    private void StopApplication()
    {
        confirming = false;
        stopRequested = true;
        Logger.LogWarning("Arresto dell'applicazione richiesto dal pannello di manutenzione (circuito {CircuitId})", Connection.CircuitId);
        ThreadPool.QueueUserWorkItem(_ => Lifetime.StopApplication());
    }

    private static string ShortId(string id) => id.Length > 10 ? id[..10] + "…" : id;

    public void Dispose() => Shutdown.CircuitsChanged -= OnCircuitsChanged;
}
