using Microsoft.AspNetCore.Components.AI;
using Microsoft.Extensions.AI;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class MinimalChat
{
    private UIAgent agent = default!;

    protected override void OnInitialized() =>
        agent = new UIAgent(ChatClient, new ChatOptions
        {
            Instructions = "Sei un esperto di Blazor e .NET 11. Rispondi in italiano, in modo breve e concreto."
        });

    public void Dispose() => agent.Dispose();
}
