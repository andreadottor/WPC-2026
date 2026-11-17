using Dottor.WPC2026.Web.Shutdown;

namespace Dottor.WPC2026.Web.Components.Pages;

public partial class Maintenance
{
    private IReadOnlyList<ActiveCircuit> _circuits = [];
    private IReadOnlyList<DisconnectedCircuit> _disconnected = [];
    private bool _confirming;
    private bool _stopRequested;

    protected override void OnInitialized()
    {
        _circuits = Shutdown.ActiveCircuits;
        _disconnected = Shutdown.DisconnectedCircuits;
        Shutdown.CircuitsChanged += OnCircuitsChanged;
    }

    private void OnCircuitsChanged() => _ = InvokeAsync(() =>
    {
        _circuits = Shutdown.ActiveCircuits;
        _disconnected = Shutdown.DisconnectedCircuits;
        StateHasChanged();
    });

    private void StopApplication()
    {
        _confirming = false;
        _stopRequested = true;
        Logger.LogWarning("Arresto dell'applicazione richiesto dal pannello di manutenzione (circuito {CircuitId})", Connection.CircuitId);
        ThreadPool.QueueUserWorkItem(_ => Lifetime.StopApplication());
    }

    private static string ShortId(string id) => id.Length > 10 ? id[..10] + "…" : id;

    public void Dispose() => Shutdown.CircuitsChanged -= OnCircuitsChanged;
}
