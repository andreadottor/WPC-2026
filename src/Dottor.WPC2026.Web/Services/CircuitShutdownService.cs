using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Dottor.WPC2026.Web.Services;

/// <summary>
/// Coordina lo shutdown controllato: quando l'host si arresta chiede ai circuit connessi
/// di sospendersi (lo stato [PersistentState] viene inviato al browser) e attende che
/// i client si scolleghino, entro un timeout.
/// </summary>
public sealed class CircuitShutdownService
{
    // Deve restare inferiore a HostOptions.ShutdownTimeout (vedi Program.cs).
    public static readonly TimeSpan PauseTimeout = TimeSpan.FromSeconds(10);

    private readonly ConcurrentDictionary<string, Circuit> _circuits = new();
    private readonly TaskCompletionSource _allDisconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<CircuitShutdownService> _logger;
    private volatile bool _isShuttingDown;
    private int _stoppingCallbackRegistered;

    public CircuitShutdownService(IHostApplicationLifetime lifetime, ILogger<CircuitShutdownService> logger)
    {
        _lifetime = lifetime;
        _logger = logger;
    }

    public int ConnectedCircuits => _circuits.Count;

    public void Register(Circuit circuit)
    {
        // I callback di ApplicationStopping vengono eseguiti in ordine inverso (LIFO).
        // SignalR registra il suo (che chiude tutte le connessioni) quando nasce la prima connessione:
        // registrandoci DOPO, alla prima connessione di un circuit, la pausa avviene prima della chiusura.
        if (Interlocked.Exchange(ref _stoppingCallbackRegistered, 1) == 0)
        {
            _lifetime.ApplicationStopping.Register(OnApplicationStopping);
        }

        _circuits.TryAdd(circuit.Id, circuit);
        _logger.LogInformation("Circuit {CircuitId} connesso (attivi: {Count})", circuit.Id, _circuits.Count);
    }

    public void Unregister(Circuit circuit)
    {
        _circuits.TryRemove(circuit.Id, out _);
        _logger.LogInformation("Circuit {CircuitId} disconnesso (attivi: {Count})", circuit.Id, _circuits.Count);

        if (_isShuttingDown && _circuits.IsEmpty)
        {
            _allDisconnected.TrySetResult();
        }
    }

    // I callback di ApplicationStopping sono sincroni: blocchiamo finché i client hanno completato la pausa.
    private void OnApplicationStopping()
    {
        _isShuttingDown = true;

        if (_circuits.IsEmpty)
        {
            _logger.LogInformation("Shutdown: nessun circuit connesso da sospendere");
            return;
        }

        try
        {
            PauseAllCircuitsAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shutdown: errore durante la sospensione dei circuit");
        }
    }

    private async Task PauseAllCircuitsAsync()
    {
        using var timeout = new CancellationTokenSource(PauseTimeout);
        var circuits = _circuits.Values.ToArray();
        _logger.LogWarning("Shutdown: richiesta di pausa per {Count} circuit", circuits.Length);

        await Task.WhenAll(circuits.Select(c => RequestPauseAsync(c, timeout.Token)));

        // true = richiesta accettata, NON "stato già salvato nel browser":
        // attendiamo che ogni client completi la pausa e chiuda la connessione.
        try
        {
            await _allDisconnected.Task.WaitAsync(timeout.Token);
            _logger.LogWarning("Shutdown: tutti i circuit sono stati sospesi, il processo può terminare");
        }
        catch (OperationCanceledException)
        {
            _logger.LogError(
                "Shutdown: timeout di {Timeout}s, {Count} circuit non hanno completato la pausa e perderanno lo stato",
                PauseTimeout.TotalSeconds, _circuits.Count);
        }
    }

    private async Task RequestPauseAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        try
        {
            var accepted = await circuit.RequestCircuitPauseAsync(cancellationToken);
            _logger.LogInformation("Shutdown: pausa del circuit {CircuitId} {Result}",
                circuit.Id, accepted ? "richiesta al client" : "non accettata");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shutdown: impossibile richiedere la pausa del circuit {CircuitId}", circuit.Id);
        }
    }
}

/// <summary>Un'istanza per circuit: registra le connessioni attive e rende disponibile l'Id del circuit.</summary>
public sealed class ShutdownCircuitHandler(CircuitShutdownService shutdownService) : CircuitHandler
{
    public string? CircuitId { get; private set; }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CircuitId = circuit.Id;
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        shutdownService.Register(circuit);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        shutdownService.Unregister(circuit);
        return Task.CompletedTask;
    }
}

