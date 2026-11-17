namespace Dottor.WPC2026.Web.Services;

/// <summary>
/// Demo settings for the opt-in auto-pause package. The package default delay is two minutes;
/// the demo uses a much shorter value so the pause is observable on stage.
/// </summary>
public sealed class AutoPauseDemoOptions
{
    public static readonly TimeSpan PackageDefaultHiddenDelay = TimeSpan.FromMinutes(2);

    public bool Enabled { get; set; } = true;
    public TimeSpan HiddenDelay { get; set; } = TimeSpan.FromSeconds(10);
}
