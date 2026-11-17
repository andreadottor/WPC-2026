using System.Net.Http.Json;
using Dottor.WPC2026.Web.Client.Models;
using Microsoft.Extensions.Hosting;

namespace Dottor.WPC2026.Web.Client.Services;

/// <summary>
/// Il "monitor della cucina" eseguito nel browser: interroga il server ogni pochi secondi
/// e notifica i componenti interessati. Non dipende da nessuna pagina.
/// </summary>
public sealed class KitchenMonitor(HttpClient http, ILogger<KitchenMonitor> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    public DateTimeOffset? StartedAt { get; private set; }
    public int Polls { get; private set; }
    public KitchenStats? Latest { get; private set; }
    public string? LastError { get; private set; }

    public event Action? Changed;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StartedAt = DateTimeOffset.Now;
        logger.LogInformation("KitchenMonitor (monitor della cucina) avviato alle {StartedAt:HH:mm:ss}", StartedAt);

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
            Latest = await http.GetFromJsonAsync<KitchenStats>("api/kitchen/stats", cancellationToken);
            LastError = null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            LastError = $"Cucina non raggiungibile alle {DateTimeOffset.Now:HH:mm:ss}: {ex.Message}";
        }

        Polls++;
        Changed?.Invoke();
    }
}