using Microsoft.AspNetCore.Components;
using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class OtherFeatures
{
    private readonly NewOrderModel _sample = new();
    private BrowserOptions? _browserOptions;

    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    protected override void OnInitialized()
    {
        if (HttpContext is not null)
        {
            _browserOptions = BrowserOptions.GetBrowserOptions(HttpContext);
        }
    }
}
