namespace Dottor.WPC2026.Web.Services;

public sealed class DemoInstance(IConfiguration configuration)
{
    public string Version { get; } = configuration["DEMO_VERSION"] ?? "v1";
    public int ProcessId { get; } = Environment.ProcessId;
}