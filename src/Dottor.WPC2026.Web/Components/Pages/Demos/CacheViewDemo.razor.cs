using Microsoft.AspNetCore.Components;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class CacheViewDemo
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(20);
    private static readonly (string Label, string Href)[] Links =
    [
        ("hardware", "demo/cacheview?category=hardware"),
        ("software", "demo/cacheview?category=software"),
        ("software + utm", "demo/cacheview?category=software&utm=slide"),
    ];

    private readonly DateTimeOffset requestTime = DateTimeOffset.Now;

    [SupplyParameterFromQuery(Name = "category")]
    public string? Category { get; set; }

    protected override void OnInitialized()
    {
        if (string.IsNullOrEmpty(Category))
        {
            // Un solo URL canonico per categoria mantiene prevedibili le chiavi di VaryByQuery.
            Navigation.NavigateTo("demo/cacheview?category=hardware");
        }
    }

    private bool IsCurrent(string href) =>
        Navigation.ToBaseRelativePath(Navigation.Uri).Equals(href, StringComparison.Ordinal);
}
