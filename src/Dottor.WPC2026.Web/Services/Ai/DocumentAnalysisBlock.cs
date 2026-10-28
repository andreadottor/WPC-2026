using Dottor.WPC2026.Web.Models;
using Microsoft.AspNetCore.Components.AI;

namespace Dottor.WPC2026.Web.Services.Ai;

/// <summary>
/// Blocco tipizzato generato dal source generator di Microsoft.AspNetCore.Components.AI:
/// trasforma la chiamata al tool server-side "analyze_document" in un oggetto con il risultato deserializzato.
/// </summary>
[ToolBlock(SimulatedDocumentChatClient.AnalyzeDocumentTool)]
public partial class DocumentAnalysisBlock : FunctionInvocationContentBlock
{
    [ToolResult]
    public DocumentAnalysis? Analysis { get; set; }
}
