using Microsoft.JSInterop;
using Dottor.WPC2026.WebWorker;
using System.Diagnostics;
using System.Text.Json;

namespace Dottor.WPC2026.Web.Client.Pages.Demos;

public partial class WebWorkerDemo
{
    private const string AnalyzeMethod = "Dottor.WPC2026.WebWorker.WorkerMethods.AnalyzeOrders";

    private WebWorkerClient? _worker;
    private string _workerStatus = "in attesa dell'interattività";
    private int _rows = 250_000;
    private int _generatedRows;
    private string? _csv;
    private bool _busy;
    private string _status = "pronto";
    private RunResult? _lastRun;
    private CancellationTokenSource? _cts;
    private DateTime _clock = DateTime.Now;
    private readonly CancellationTokenSource _disposed = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _ = RunClockAsync();

        _workerStatus = "avvio del runtime .NET nel worker...";
        StateHasChanged();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _worker = await WebWorkerClient.CreateAsync(JS, cancellationToken: _disposed.Token);
            _workerStatus = $"pronto in {stopwatch.ElapsedMilliseconds} ms";
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSException)
        {
            _workerStatus = $"errore: {ex.Message}";
        }
        StateHasChanged();
    }

    private async Task RunClockAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        try
        {
            while (await timer.WaitForNextTickAsync(_disposed.Token))
            {
                _clock = DateTime.Now;
                StateHasChanged();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task AnalyzeOnUiThread()
    {
        await RunAsync(inWorker: false, data => Task.FromResult(OrderCsvAnalyzer.Analyze(data)));
    }

    private async Task AnalyzeInWorker()
    {
        _cts = new CancellationTokenSource();
        try
        {
            await RunAsync(inWorker: true, async data =>
            {
                var json = await _worker!.InvokeAsync<string>(AnalyzeMethod, [data], cancellationToken: _cts.Token);
                return JsonSerializer.Deserialize(json, WorkerJsonContext.Default.CsvSummary)!;
            });
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
        }
    }

    private async Task RunAsync(bool inWorker, Func<string, Task<CsvSummary>> analyze)
    {
        _busy = true;
        if (_csv is null || _generatedRows != _rows)
        {
            _status = "stampa delle comande (generazione del CSV)...";
            await Task.Delay(50); // lascia al browser il tempo di disegnare lo stato
            _csv = OrderCsvAnalyzer.Generate(_rows);
            _generatedRows = _rows;
        }

        _status = inWorker ? "analisi nel Web Worker..." : "analisi sul thread della UI...";
        await Task.Delay(50);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var summary = await analyze(_csv);
            _lastRun = new RunResult(summary, stopwatch.Elapsed.TotalMilliseconds, inWorker);
            _status = "completato";
        }
        catch (OperationCanceledException)
        {
            _status = "attesa annullata (il worker termina comunque il calcolo in corso)";
        }
        catch (InvalidOperationException ex)
        {
            _status = $"errore: {ex.Message}";
        }
        finally
        {
            _busy = false;
        }
    }

    private void Cancel() => _cts?.Cancel();

    public async ValueTask DisposeAsync()
    {
        _disposed.Cancel();
        if (_worker is not null)
        {
            await _worker.DisposeAsync();
        }
        _disposed.Dispose();
    }

    private sealed record RunResult(CsvSummary Summary, double TotalMs, bool InWorker);
}
