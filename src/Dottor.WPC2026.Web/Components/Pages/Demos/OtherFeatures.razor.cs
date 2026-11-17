using Microsoft.AspNetCore.Components;
using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class OtherFeatures
{
    private readonly NewTicketModel sample = new();
    private BrowserOptions? browserOptions;

    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    protected override void OnInitialized()
    {
        if (HttpContext is not null)
        {
            browserOptions = BrowserOptions.GetBrowserOptions(HttpContext);
        }
    }
}
