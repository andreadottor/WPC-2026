using Microsoft.AspNetCore.Components;

namespace Dottor.WPC2026.Web.Client.Shared;

public partial class ObserveBox
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
