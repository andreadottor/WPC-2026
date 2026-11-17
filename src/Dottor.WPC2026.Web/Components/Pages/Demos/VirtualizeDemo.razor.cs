using Microsoft.AspNetCore.Components.Web.Virtualization;
using Dottor.WPC2026.Web.Models;
using Dottor.WPC2026.Web.Services;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class VirtualizeDemo
{
    private readonly List<TimelineEvent> _events = TimelineEventGenerator.Generate(10_000);
    private readonly HashSet<int> _expanded = [];
    private readonly Random _random = new(2026);
    private Virtualize<TimelineEvent>? _timeline;
    private VirtualizeAnchorMode _anchorMode = VirtualizeAnchorMode.Start;
    private int _targetIndex = 5000;
    private int? _highlightedId;
    private string? _status;
    private bool _liveFeed;
    private Timer? _feedTimer;

    private readonly List<LogLine> _logLines = [];
    private bool _logRunning;
    private Timer? _logTimer;
    private int _logCounter;

    private string anchorDescription => _anchorMode switch
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
        if (!_expanded.Remove(id))
        {
            _expanded.Add(id);
        }
    }

    private Task ScrollToStart() => ScrollToIndexAsync(0);

    private Task ScrollToEnd() => ScrollToIndexAsync(_events.Count - 1);

    private Task ScrollToTarget() => ScrollToIndexAsync(Math.Clamp(_targetIndex, 0, _events.Count - 1));

    private async Task ScrollToIndexAsync(int index)
    {
        _highlightedId = _events[index].Id;
        _status = $"Scorrimento all'indice {index} (evento #{_highlightedId})...";
        StateHasChanged();
        await _timeline!.ScrollToItemAsync(index);
        _status = $"Elemento all'indice {index} (evento #{_highlightedId}) allineato in cima.";
    }

    private void AddAtTop()
    {
        _events.Insert(0, NewEvent());
        _status = $"Aggiunto l'evento #{_events[0].Id} in cima.";
    }

    private void AddAtBottom()
    {
        _events.Add(NewEvent());
        _status = $"Aggiunto l'evento #{_events[^1].Id} in fondo.";
    }

    private TimelineEvent NewEvent() =>
        TimelineEventGenerator.Create(_events.Max(e => e.Id) + 1, DateTime.Now, _random) with { Title = "Nuovo ordine in arrivo 🍕" };

    private void ToggleLiveFeed()
    {
        _liveFeed = !_liveFeed;
        _feedTimer?.Dispose();
        _feedTimer = _liveFeed
            ? new Timer(_ => InvokeAsync(() => { AddAtTop(); StateHasChanged(); }), null, 0, 1000)
            : null;
    }

    private void ToggleLog()
    {
        _logRunning = !_logRunning;
        _logTimer?.Dispose();
        _logTimer = _logRunning
            ? new Timer(_ => InvokeAsync(() => { AppendLogLine(); StateHasChanged(); }), null, 0, 400)
            : null;
    }

    private void AppendLogLine()
    {
        _logCounter++;
        var roll = _random.Next(10);
        var (level, text) = roll switch
        {
            0 => ("error", $"ERR  Consegna {_logCounter} in ritardo: RiderLostException\n     at Navigator.FindAddress()\n     at Delivery.Run()"),
            1 or 2 => ("warn", $"WARN Consegna {_logCounter}: traffico, +{_random.Next(3, 15)} minuti"),
            _ => ("info", $"INFO Consegna {_logCounter} completata in {_random.Next(8, 30)} minuti 🍕")
        };
        _logLines.Add(new LogLine(_logCounter, level, $"{DateTime.Now:HH:mm:ss.fff} {text}"));
    }

    public void Dispose()
    {
        _feedTimer?.Dispose();
        _logTimer?.Dispose();
    }

    private sealed record LogLine(int Id, string Level, string Text);
}
