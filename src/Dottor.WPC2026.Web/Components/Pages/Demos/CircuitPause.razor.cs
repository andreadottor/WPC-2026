using Microsoft.AspNetCore.Components;
using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class CircuitPause
{
    private readonly DateTimeOffset componentCreatedAt = DateTimeOffset.Now;

    [PersistentState]
    public int Count { get; set; }

    [PersistentState]
    public TicketDraft? Ticket { get; set; }

    // Memorizza quale processo ha creato lo stato: dopo il deploy è diverso da quello corrente.
    [PersistentState]
    public int? OriginalProcessId { get; set; }

    [PersistentState]
    public string? OriginalVersion { get; set; }

    protected override void OnInitialized()
    {
        Ticket ??= new TicketDraft();
        OriginalProcessId ??= Instance.ProcessId;
        OriginalVersion ??= Instance.Version;
    }
}
