using Microsoft.AspNetCore.Components;
using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class TicketDetail
{
    private Ticket? ticket;

    [Parameter]
    public int Id { get; set; }

    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    // Accetta solo URL relativi alla griglia, per evitare open redirect.
    private string BackUrl =>
        ReturnUrl is not null && ReturnUrl.StartsWith("demo/quickgrid", StringComparison.OrdinalIgnoreCase)
            ? ReturnUrl
            : "demo/quickgrid";

    protected override void OnParametersSet() => ticket = Tickets.GetById(Id);
}
