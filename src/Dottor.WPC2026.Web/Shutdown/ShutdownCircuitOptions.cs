namespace Dottor.WPC2026.Web.Shutdown;

public sealed class ShutdownCircuitOptions
{
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(15);
}