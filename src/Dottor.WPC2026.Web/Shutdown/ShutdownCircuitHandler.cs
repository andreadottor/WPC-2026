using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Dottor.WPC2026.Web.Shutdown;

public sealed class ShutdownCircuitHandler(CircuitShutdownService shutdown,  ILogger<ShutdownCircuitHandler> logger) 
    : CircuitHandler
{
    public string? CircuitId { get; private set; }
    public bool IsConnected { get; private set; }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken ct)
    {
        CircuitId = circuit.Id;
        logger.LogInformation("Apertura circuito: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken ct)
    {
        IsConnected = true;
        shutdown.Register(circuit.Id, circuit);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken ct)
    {
        IsConnected = false;
        shutdown.Unregister(circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken ct)
    {
        shutdown.Unregister(circuit.Id);
        logger.LogInformation("Chiusura circuito: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }
}