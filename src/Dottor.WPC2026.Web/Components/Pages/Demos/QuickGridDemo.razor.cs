using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.QuickGrid;
using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class QuickGridDemo
{
    private IQueryable<Ticket> tickets = default!;
    private readonly PaginationState pagination = new() { ItemsPerPage = 15 };
    private readonly GridSort<Ticket> sortByPriority = GridSort<Ticket>.ByAscending(t => t.Priority);
    private readonly GridSort<Ticket> sortByStatus = GridSort<Ticket>.ByAscending(t => t.Status);
    private string currentUrl = "";

    protected override void OnInitialized()
    {
        tickets = Tickets.GetAll();
        currentUrl = Navigation.Uri;
        Navigation.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        currentUrl = e.Location;
        InvokeAsync(StateHasChanged);
    }

    private void OpenTicket(Ticket ticket)
    {
        var returnUrl = Navigation.ToBaseRelativePath(Navigation.Uri);
        Navigation.NavigateTo($"demo/quickgrid/{ticket.Id}?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    private static string PriorityClass(TicketPriority priority) => priority switch
    {
        TicketPriority.Critical => "text-bg-danger",
        TicketPriority.High => "text-bg-warning",
        TicketPriority.Medium => "text-bg-info",
        _ => "text-bg-light border"
    };

    public void Dispose() => Navigation.LocationChanged -= OnLocationChanged;
}
