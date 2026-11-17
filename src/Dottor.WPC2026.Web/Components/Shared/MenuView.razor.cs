using Microsoft.AspNetCore.Components;
using Dottor.WPC2026.Web.Services;

namespace Dottor.WPC2026.Web.Components.Shared;

public partial class MenuView
{
    private static readonly System.Globalization.CultureInfo Italian = System.Globalization.CultureInfo.GetCultureInfo("it-IT");
    private MenuReport? report;

    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    [Parameter, EditorRequired]
    public string Category { get; set; } = default!;

    [Parameter, EditorRequired]
    public string Portion { get; set; } = default!;

    [Parameter, EditorRequired]
    public string Heading { get; set; } = default!;

    [Parameter]
    public TimeSpan? ExpiresAfter { get; set; }

    protected override async Task OnInitializedAsync() =>
        report = await Menu.GenerateAsync(Category, Portion, HttpContext?.RequestAborted ?? CancellationToken.None);
}
