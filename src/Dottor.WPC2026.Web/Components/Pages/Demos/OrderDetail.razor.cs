using Microsoft.AspNetCore.Components;
using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class OrderDetail
{
    private Order? order;

    [Parameter]
    public int Id { get; set; }

    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    // Accetta solo URL relativi alla griglia, per evitare open redirect.
    private string BackUrl =>
        ReturnUrl is not null && ReturnUrl.StartsWith("demo/quickgrid", StringComparison.OrdinalIgnoreCase)
            ? ReturnUrl
            : "demo/quickgrid";

    protected override void OnParametersSet() => order = Orders.GetById(Id);
}
