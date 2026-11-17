using Microsoft.JSInterop;
using Dottor.WPC2026.WebWorker;
using System.Diagnostics;
using System.Text.Json;

namespace Dottor.WPC2026.Web.Client.Pages.Demos;

public partial class WebWorkerDemo
{
    private const string AnalyzeMethod = "Dottor.WPC2026.WebWorker.WorkerMethods.AnalyzeTickets";

    private WebWorkerClient? worker;
    private string workerStatus = "in attesa dell'interattività";
    private int rows = 250_000;
    private int generatedRows;
    private string? csv;
    private bool busy;
    private string status = "pronto";
    private RunResult? lastRun;
    private CancellationTokenSource? cts;
    private DateTime clock = DateTime.Now;
    private readonly CancellationTokenSource disposed = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _ = RunClockAsync();

        workerStatus = "avvio del runtime .NET nel worker...";
        StateHasChanged();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            worker = await WebWorkerClient.CreateAsync(JS, cancellationToken: disposed.Token);
            workerStatus = $"pronto in {stopwatch.ElapsedMilliseconds} ms";
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSException)
        {
            workerStatus = $"errore: {ex.Message}";
        }
        StateHasChanged();
    }

    private async Task RunClockAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        try
        {
            while (await timer.WaitForNextTickAsync(disposed.Token))
            {
                clock = DateTime.Now;
                StateHasChanged();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task AnalyzeOnUiThread()
    {
        await RunAsync(inWorker: false, data => Task.FromResult(TicketCsvAnalyzer.Analyze(data)));
    }

    private async Task AnalyzeInWorker()
    {
        cts = new CancellationTokenSource();
        try
        {
            await RunAsync(inWorker: true, async data =>
            {
                var json = await worker!.InvokeAsync<string>(AnalyzeMethod, [data], cancellationToken: cts.Token);
                return JsonSerializer.Deserialize(json, WorkerJsonContext.Default.CsvSummary)!;
            });
        }
        finally
        {
            cts.Dispose();
            cts = null;
        }
    }

    private async Task RunAsync(bool inWorker, Func<string, Task<CsvSummary>> analyze)
    {
        busy = true;
        if (csv is null || generatedRows != rows)
        {
            status = "generazione del CSV...";
            await Task.Delay(50); // lascia al browser il tempo di disegnare lo stato
            csv = TicketCsvAnalyzer.Generate(rows);
            generatedRows = rows;
        }

        status = inWorker ? "analisi nel Web Worker..." : "analisi sul thread della UI...";
        await Task.Delay(50);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var summary = await analyze(csv);
            lastRun = new RunResult(summary, stopwatch.Elapsed.TotalMilliseconds, inWorker);
            status = "completato";
        }
        catch (OperationCanceledException)
        {
            status = "attesa annullata (il worker termina comunque il calcolo in corso)";
        }
        catch (InvalidOperationException ex)
        {
            status = $"errore: {ex.Message}";
        }
        finally
        {
            busy = false;
        }
    }

    private void Cancel() => cts?.Cancel();

    public async ValueTask DisposeAsync()
    {
        disposed.Cancel();
        if (worker is not null)
        {
            await worker.DisposeAsync();
        }
        disposed.Dispose();
    }

    private sealed record RunResult(CsvSummary Summary, double TotalMs, bool InWorker);
}
