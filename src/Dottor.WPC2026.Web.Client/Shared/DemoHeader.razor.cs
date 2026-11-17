using Microsoft.AspNetCore.Components;

namespace Dottor.WPC2026.Web.Client.Shared;

public partial class DemoHeader
{
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter] public int? Number { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
