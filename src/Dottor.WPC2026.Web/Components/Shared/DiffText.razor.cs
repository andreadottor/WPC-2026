using Microsoft.AspNetCore.Components;

namespace Dottor.WPC2026.Web.Components.Shared;

/// <summary>Mostra le differenze tra due testi evidenziando aggiunte (ins) e rimozioni (del).</summary>
public partial class DiffText
{
    [Parameter] public string? OldText { get; set; }

    [Parameter] public string? NewText { get; set; }
}
