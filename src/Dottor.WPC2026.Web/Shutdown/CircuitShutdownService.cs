using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Dottor.WPC2026.Web.Shutdown;

public sealed class CircuitShutdownService
{
    private readonly ConcurrentDictionary<string, Circuit> _circuits = new();
    private readonly ShutdownCircuitOptions _options;
    private bool _isShuttingDown;
    private TaskCompletionSource _shutdownTcs = new();

    public bool IsStopping { get { return _isShuttingDown; } }

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
                            .Select(c => c.RequestCircuitPauseAsync())
                            .Append(_shutdownTcs.Task);

        Task.WhenAll(pauseTasks).Wait(_options.ShutdownTimeout);

        _shutdownTcs.Task.Wait(_options.ShutdownTimeout);
    }

    public void Register(string circuitId, Circuit circuit)
    {
        _circuits.TryAdd(circuitId, circuit);
    }

    public void Unregister(string circuitId)
    {
        _circuits.TryRemove(circuitId, out _);

        if (_isShuttingDown && _circuits.IsEmpty)
        {
            _shutdownTcs.TrySetResult();
        }
    }
}