using Microsoft.AspNetCore.Components;
using System.Diagnostics;

namespace Dottor.WPC2026.Web.Components.Pages;

public partial class Error
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    // Il template .NET 11 usa [PersistentState], ma su questa pagina statica (nessun render mode)
    // la persistenza dello stato fallisce con un 500: qui non c'è nulla da trasferire al client.
    private string? RequestId { get; set; }

    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    protected override void OnInitialized()
    {
        RequestId ??= Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
    }
}
