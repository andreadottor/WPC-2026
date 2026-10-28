using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace Dottor.WPC2026.Web.Services.Ai;

/// <summary>Sezione "AI" della configurazione (appsettings, User Secrets, variabili d'ambiente AI__*).</summary>
public sealed class AiOptions
{
    /// <summary>"Simulated" (default) oppure "OpenAI" (qualsiasi endpoint compatibile: OpenAI, Azure OpenAI v1, GitHub Models...).</summary>
    public string Provider { get; set; } = "Simulated";
    public string Model { get; set; } = "gpt-4.1-mini";
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }

    public bool IsSimulated =>
        !string.Equals(Provider, "OpenAI", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(ApiKey);

    public string Description => IsSimulated
        ? "Modalità dimostrativa: risposte simulate e deterministiche, nessuna chiamata esterna"
        : $"Provider reale: {Model} ({Endpoint ?? "api.openai.com"})";
}

public static class AiServiceCollectionExtensions
{
    /// <summary>
    /// Registra l'IChatClient usato dalla demo Agentic UI.
    /// La pipeline è la stessa in modalità reale e simulata: cambia solo il client "interno".
    /// </summary>
    public static IServiceCollection AddDocumentAssistant(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("AI").Get<AiOptions>() ?? new AiOptions();
        services.AddSingleton(options);

        IChatClient innerClient = options.IsSimulated
            ? new SimulatedDocumentChatClient()
            : CreateOpenAIClient(options);

        // UseFunctionInvocation esegue i tool server-side e gestisce le richieste di approvazione
        // (ApprovalRequiredAIFunction). Le UI action registrate su UIAgent sono solo dichiarazioni
        // e tornano al browser, dove le esegue il componente.
        services.AddChatClient(innerClient)
            .UseFunctionInvocation();

        return services;
    }

    private static IChatClient CreateOpenAIClient(AiOptions options)
    {
        var clientOptions = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            clientOptions.Endpoint = new Uri(options.Endpoint);
        }

        return new OpenAIClient(new ApiKeyCredential(options.ApiKey!), clientOptions)
            .GetChatClient(options.Model)
            .AsIChatClient();
    }
}
