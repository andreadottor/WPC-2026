using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.AI;

namespace Dottor.WPC2026.Web.Components.Shared;

public partial class ProposalCard
{
    private bool _busy;
    private bool _answered;
    private bool _accepted;

    [Parameter, EditorRequired]
    public UIActionBlock Block { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (Block.IsComplete && Block.Result?.Result is { } result)
        {
            _accepted = result.ToString()?.Contains("ACCEPTED", StringComparison.Ordinal) == true;
        }
    }

    private async Task RespondAsync(bool accept)
    {
        if (_busy || _answered)
        {
            return;
        }

        _busy = true;
        try
        {
            Block.Call.Arguments ??= new Dictionary<string, object?>();
            Block.Call.Arguments["accepted"] = accept;
            _accepted = accept;
            _answered = true;
            await Block.InvokeAsync(CancellationToken.None);
        }
        finally
        {
            _busy = false;
        }
    }
}
