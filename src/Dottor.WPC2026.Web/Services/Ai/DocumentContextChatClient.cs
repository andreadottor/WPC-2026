using System.Text.Json;
using Dottor.WPC2026.Web.Models;
using Microsoft.Extensions.AI;

namespace Dottor.WPC2026.Web.Services.Ai;

/// <summary>
/// Aggiunge a ogni richiesta un messaggio di sistema con il documento corrente,
/// così l'agente (reale o simulato) lavora sempre sullo stato condiviso aggiornato.
/// </summary>
public sealed class DocumentContextChatClient(IChatClient innerClient, Func<DocumentState> getDocument)
    : DelegatingChatClient(innerClient)
{
    private const string DocumentKey = "wpc.document";

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        base.GetResponseAsync(WithDocument(messages), options, cancellationToken);

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        base.GetStreamingResponseAsync(WithDocument(messages), options, cancellationToken);

    public static DocumentState? FindDocument(IEnumerable<ChatMessage> messages) =>
        messages.Select(m => m.AdditionalProperties?.GetValueOrDefault(DocumentKey)).OfType<DocumentState>().FirstOrDefault();

    private List<ChatMessage> WithDocument(IEnumerable<ChatMessage> messages)
    {
        var document = getDocument().Clone();
        var json = JsonSerializer.Serialize(document, JsonSerializerOptions.Web);
        var context = new ChatMessage(ChatRole.System, $"Documento corrente (JSON):\n{json}")
        {
            AdditionalProperties = new() { [DocumentKey] = document }
        };
        return [context, .. messages];
    }
}
