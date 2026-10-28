namespace Dottor.WPC2026.Web.Services;

/// <summary>Informazioni sull'istanza in esecuzione: cambiano a ogni "deployment".</summary>
public sealed record AppInstanceInfo(string Version, int ProcessId, DateTimeOffset StartedAt)
{
    public static AppInstanceInfo FromConfiguration(IConfiguration configuration) => new(
        configuration["DEMO_VERSION"] ?? "v1",
        Environment.ProcessId,
        DateTimeOffset.Now);
}
