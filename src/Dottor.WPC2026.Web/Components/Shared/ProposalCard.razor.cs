using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.AI;

namespace Dottor.WPC2026.Web.Components.Shared;

public partial class ProposalCard
{
    private bool busy;
    private bool answered;
    private bool accepted;

    [Parameter, EditorRequired]
    public UIActionBlock Block { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (Block.IsComplete && Block.Result?.Result is { } result)
        {
            accepted = result.ToString()?.Contains("ACCEPTED", StringComparison.Ordinal) == true;
        }
    }

    private async Task RespondAsync(bool accept)
    {
        if (busy || answered)
        {
            return;
        }

        busy = true;
        try
        {
            Block.Call.Arguments ??= new Dictionary<string, object?>();
            Block.Call.Arguments["accepted"] = accept;
            accepted = accept;
            answered = true;
            await Block.InvokeAsync(CancellationToken.None);
        }
        finally
        {
            busy = false;
        }
    }
}
