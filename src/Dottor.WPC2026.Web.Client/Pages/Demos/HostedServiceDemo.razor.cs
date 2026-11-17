namespace Dottor.WPC2026.Web.Client.Pages.Demos;

public partial class HostedServiceDemo
{
    private readonly DateTimeOffset _componentCreatedAt = DateTimeOffset.Now;

    protected override void OnInitialized() => Monitor.Changed += OnMonitorChanged;

    private void OnMonitorChanged() => _ = InvokeAsync(StateHasChanged);

    public void Dispose() => Monitor.Changed -= OnMonitorChanged;
}
