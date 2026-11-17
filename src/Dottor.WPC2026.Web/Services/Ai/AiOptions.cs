using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace Dottor.WPC2026.Web.Services.Ai;

/// <summary>Sezione "AI" della configurazione (appsettings, User Secrets, variabili d'ambiente AI__*).</summary>
public sealed class AiOptions
{
    /// <summary>Endpoint OpenAI v1 della risorsa Microsoft Foundry.</summary>
    public string Endpoint { get; set; } = "https://blazor-ai-resource.openai.azure.com/openai/v1";

    /// <summary>Nome del deployment del modello su Foundry.</summary>
    public string Model { get; set; } = "gpt-6.1-sol";

    /// <summary>Chiave API: solo nei User Secrets (AI:ApiKey) o in AI__ApiKey, mai nel repository.</summary>
    public string? ApiKey { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public string Description => IsConfigured
        ? $"{Model} · Microsoft Foundry"
        : "Chiave API mancante (AI:ApiKey)";
}

public static class AiServiceCollectionExtensions
{
    /// <summary>
    /// Registra l'IChatClient (Microsoft.Extensions.AI) usato dalle demo AI:
    /// client OpenAI puntato sull'endpoint v1 di Microsoft Foundry.
    /// </summary>
    public static IServiceCollection AddFoundryChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("AI").Get<AiOptions>() ?? new AiOptions();
        services.AddSingleton(options);

        IChatClient innerClient = options.IsConfigured
            ? new OpenAIClient(new ApiKeyCredential(options.ApiKey!), new OpenAIClientOptions { Endpoint = new Uri(options.Endpoint) })
                .GetChatClient(options.Model)
                .AsIChatClient()
            : new MissingApiKeyChatClient();

        // UseFunctionInvocation esegue i tool server-side e gestisce le richieste di approvazione
        // (ApprovalRequiredAIFunction). Le UI action registrate su UIAgent sono solo dichiarazioni
        // e tornano al browser, dove le esegue il componente.
        services.AddChatClient(innerClient)
            .UseFunctionInvocation();

        return services;
    }

    /// <summary>Senza chiave l'app parte comunque: la chat mostra un errore che spiega come configurarla.</summary>
    private sealed class MissingApiKeyChatClient : IChatClient
    {
        private const string Message =
            "Chiave API non configurata: esegui dotnet user-secrets --project src/Dottor.WPC2026.Web set \"AI:ApiKey\" \"<chiave>\" e riavvia l'app.";

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(Message);

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(Message);

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
