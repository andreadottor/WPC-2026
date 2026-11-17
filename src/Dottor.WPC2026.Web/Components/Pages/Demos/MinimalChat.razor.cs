using Microsoft.AspNetCore.Components.AI;
using Microsoft.Extensions.AI;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class MinimalChat
{
    private UIAgent _agent = default!;

    protected override void OnInitialized() =>
        _agent = new UIAgent(ChatClient, new ChatOptions
        {
            Instructions = "Sei Gennaro, il pizzaiolo virtuale di \"Blazor da Asporto\": esperto di pizza e, nel tempo libero, di Blazor e .NET 11. " +
                           "Rispondi in italiano, in modo breve, concreto e con un pizzico di ironia; quando ha senso usa metafore da pizzeria. " +
                           "Sull'ananas nella pizza hai opinioni forti ma resti educato."
        });

    public void Dispose() => _agent.Dispose();
}
