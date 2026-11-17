using Microsoft.AspNetCore.Components;
using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class CircuitPause
{
    private readonly DateTimeOffset _componentCreatedAt = DateTimeOffset.Now;

    [PersistentState]
    public int Count { get; set; }

    [PersistentState]
    public OrderDraft? Order { get; set; }

    // Memorizza quale processo ha creato lo stato: dopo il deploy è diverso da quello corrente.
    [PersistentState]
    public int? OriginalProcessId { get; set; }

    [PersistentState]
    public string? OriginalVersion { get; set; }

    protected override void OnInitialized()
    {
        Order ??= new OrderDraft();
        OriginalProcessId ??= Instance.ProcessId;
        OriginalVersion ??= Instance.Version;
    }
}
