using Microsoft.AspNetCore.Components;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class CacheViewDemo
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(20);
    private static readonly (string Label, string Href)[] Links =
    [
        ("classiche", "demo/cacheview?category=classiche"),
        ("speciali", "demo/cacheview?category=speciali"),
        ("speciali + utm", "demo/cacheview?category=speciali&utm=slide"),
    ];

    private readonly DateTimeOffset _requestTime = DateTimeOffset.Now;

    [SupplyParameterFromQuery(Name = "category")]
    public string? Category { get; set; }

    protected override void OnInitialized()
    {
        if (string.IsNullOrEmpty(Category))
        {
            // Un solo URL canonico per categoria mantiene prevedibili le chiavi di VaryByQuery.
            Navigation.NavigateTo("demo/cacheview?category=classiche");
        }
    }

    private bool IsCurrent(string href) =>
        Navigation.ToBaseRelativePath(Navigation.Uri).Equals(href, StringComparison.Ordinal);
}
