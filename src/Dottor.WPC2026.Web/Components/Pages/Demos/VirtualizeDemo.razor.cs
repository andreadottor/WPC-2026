using Microsoft.AspNetCore.Components.Web.Virtualization;
using Dottor.WPC2026.Web.Models;
using Dottor.WPC2026.Web.Services;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class VirtualizeDemo
{
    private readonly List<TimelineEvent> events = TimelineEventGenerator.Generate(10_000);
    private readonly HashSet<int> expanded = [];
    private readonly Random random = new(2026);
    private Virtualize<TimelineEvent>? timeline;
    private VirtualizeAnchorMode anchorMode = VirtualizeAnchorMode.Start;
    private int targetIndex = 5000;
    private int? highlightedId;
    private string? status;
    private bool liveFeed;
    private Timer? feedTimer;

    private readonly List<LogLine> logLines = [];
    private bool logRunning;
    private Timer? logTimer;
    private int logCounter;

    private string anchorDescription => anchorMode switch
    {
        VirtualizeAnchorMode.Start => "Start (default): se la lista è all'inizio, i nuovi elementi in cima restano visibili.",
        VirtualizeAnchorMode.End => "End: se la lista è in fondo, resta agganciata alla fine quando arrivano nuovi elementi.",
        _ => "None: la posizione di scorrimento non cambia quando vengono aggiunti elementi."
    };

    protected override void OnInitialized()
    {
        for (var i = 0; i < 40; i++)
        {
            AppendLogLine();
        }
    }

    private void Toggle(int id)
    {
        if (!expanded.Remove(id))
        {
            expanded.Add(id);
        }
    }

    private Task ScrollToStart() => ScrollToIndexAsync(0);

    private Task ScrollToEnd() => ScrollToIndexAsync(events.Count - 1);

    private Task ScrollToTarget() => ScrollToIndexAsync(Math.Clamp(targetIndex, 0, events.Count - 1));

    private async Task ScrollToIndexAsync(int index)
    {
        highlightedId = events[index].Id;
        status = $"Scorrimento all'indice {index} (evento #{highlightedId})...";
        StateHasChanged();
        await timeline!.ScrollToItemAsync(index);
        status = $"Elemento all'indice {index} (evento #{highlightedId}) allineato in cima.";
    }

    private void AddAtTop()
    {
        events.Insert(0, NewEvent());
        status = $"Aggiunto l'evento #{events[0].Id} in cima.";
    }

    private void AddAtBottom()
    {
        events.Add(NewEvent());
        status = $"Aggiunto l'evento #{events[^1].Id} in fondo.";
    }

    private TimelineEvent NewEvent() =>
        TimelineEventGenerator.Create(events.Max(e => e.Id) + 1, DateTime.Now, random) with { Title = "Nuovo evento" };

    private void ToggleLiveFeed()
    {
        liveFeed = !liveFeed;
        feedTimer?.Dispose();
        feedTimer = liveFeed
            ? new Timer(_ => InvokeAsync(() => { AddAtTop(); StateHasChanged(); }), null, 0, 1000)
            : null;
    }

    private void ToggleLog()
    {
        logRunning = !logRunning;
        logTimer?.Dispose();
        logTimer = logRunning
            ? new Timer(_ => InvokeAsync(() => { AppendLogLine(); StateHasChanged(); }), null, 0, 400)
            : null;
    }

    private void AppendLogLine()
    {
        logCounter++;
        var roll = random.Next(10);
        var (level, text) = roll switch
        {
            0 => ("error", $"ERR  Richiesta {logCounter} fallita: TimeoutException\n     at Orders.Process()\n     at Pipeline.Run()"),
            1 or 2 => ("warn", $"WARN Richiesta {logCounter} lenta: {random.Next(800, 3000)} ms"),
            _ => ("info", $"INFO Richiesta {logCounter} completata in {random.Next(5, 200)} ms")
        };
        logLines.Add(new LogLine(logCounter, level, $"{DateTime.Now:HH:mm:ss.fff} {text}"));
    }

    public void Dispose()
    {
        feedTimer?.Dispose();
        logTimer?.Dispose();
    }

    private sealed record LogLine(int Id, string Level, string Text);
}
