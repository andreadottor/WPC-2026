using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Dottor.WPC2026.Web.Shutdown;

public sealed class CircuitShutdownService
{
    private readonly ConcurrentDictionary<string, ActiveCircuit> _circuits = new();
    private readonly ConcurrentDictionary<string, DisconnectedCircuit> _disconnected = new();
    private readonly ShutdownCircuitOptions _options;
    private bool _isShuttingDown;
    private TaskCompletionSource _shutdownTcs = new();

    public bool IsStopping { get { return _isShuttingDown; } }

    public int ActiveCircuitCount => _circuits.Count;

    public IReadOnlyList<ActiveCircuit> ActiveCircuits => _circuits.Values.OrderBy(c => c.ConnectedAt).ToList();

    // Circuiti con la connessione caduta ma ancora tenuti in memoria dal server (DisconnectedCircuitRetentionPeriod).
    public IReadOnlyList<DisconnectedCircuit> DisconnectedCircuits => _disconnected.Values.OrderBy(c => c.DisconnectedAt).ToList();

    public event Action? CircuitsChanged;

    public CircuitShutdownService(IHostApplicationLifetime appLifetime, IOptions<ShutdownCircuitOptions> options)
    {
        _options = options.Value;
        appLifetime.ApplicationStopping.Register(OnApplicationStopping);
    }

    private void OnApplicationStopping()
    {
        _isShuttingDown = true;

        if (_circuits.IsEmpty)
            return;

        var pauseTasks = _circuits.Values
                            .Select(c => c.Circuit.RequestCircuitPauseAsync())
                            .Append(_shutdownTcs.Task);

        Task.WhenAll(pauseTasks).Wait(_options.ShutdownTimeout);

        _shutdownTcs.Task.Wait(_options.ShutdownTimeout);
    }

    public void Register(string circuitId, Circuit circuit)
    {
        _disconnected.TryRemove(circuitId, out _);
        _circuits.TryAdd(circuitId, new ActiveCircuit(circuitId, circuit, DateTimeOffset.Now));
        CircuitsChanged?.Invoke();
    }

    public void Unregister(string circuitId, bool circuitClosed = false)
    {
        var wasConnected = _circuits.TryRemove(circuitId, out var circuit);
        if (circuitClosed)
        {
            _disconnected.TryRemove(circuitId, out _);
        }
        else if (wasConnected)
        {
            _disconnected[circuitId] = new DisconnectedCircuit(circuitId, circuit!.ConnectedAt, DateTimeOffset.Now);
        }
        CircuitsChanged?.Invoke();

        if (_isShuttingDown && _circuits.IsEmpty)
        {
            _shutdownTcs.TrySetResult();
        }
    }
}

public sealed record ActiveCircuit(string Id, Circuit Circuit, DateTimeOffset ConnectedAt);

public sealed record DisconnectedCircuit(string Id, DateTimeOffset ConnectedAt, DateTimeOffset DisconnectedAt);
