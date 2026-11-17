using System.Net.Http.Json;
using Dottor.WPC2026.Web.Client.Models;
using Microsoft.Extensions.Hosting;

namespace Dottor.WPC2026.Web.Client.Services;

/// <summary>
/// Servizio in background eseguito nel browser: interroga il server ogni pochi secondi
/// e notifica i componenti interessati. Non dipende da nessuna pagina.
/// </summary>
public sealed class TicketStatsMonitor(HttpClient http, ILogger<TicketStatsMonitor> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    public DateTimeOffset? StartedAt { get; private set; }
    public int Polls { get; private set; }
    public TicketStats? Latest { get; private set; }
    public string? LastError { get; private set; }

    public event Action? Changed;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StartedAt = DateTimeOffset.Now;
        logger.LogInformation("TicketStatsMonitor avviato alle {StartedAt:HH:mm:ss}", StartedAt);

        using var timer = new PeriodicTimer(Interval);
        do
        {
            await RefreshAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            Latest = await http.GetFromJsonAsync<TicketStats>("api/tickets/stats", cancellationToken);
            LastError = null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            LastError = $"Server non raggiungibile alle {DateTimeOffset.Now:HH:mm:ss}: {ex.Message}";
        }

        Polls++;
        Changed?.Invoke();
    }
}