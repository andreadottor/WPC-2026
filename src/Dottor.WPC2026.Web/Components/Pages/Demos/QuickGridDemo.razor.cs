using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.QuickGrid;
using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class QuickGridDemo
{
    private IQueryable<Order> orders = default!;
    private readonly PaginationState pagination = new() { ItemsPerPage = 15 };
    private readonly GridSort<Order> sortByPizza = GridSort<Order>.ByAscending(o => o.Pizza);
    private readonly GridSort<Order> sortByHunger = GridSort<Order>.ByAscending(o => o.Hunger);
    private readonly GridSort<Order> sortByStatus = GridSort<Order>.ByAscending(o => o.Status);
    private string currentUrl = "";

    protected override void OnInitialized()
    {
        orders = Orders.GetAll();
        currentUrl = Navigation.Uri;
        Navigation.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        currentUrl = e.Location;
        InvokeAsync(StateHasChanged);
    }

    private void OpenOrder(Order order)
    {
        var returnUrl = Navigation.ToBaseRelativePath(Navigation.Uri);
        Navigation.NavigateTo($"demo/quickgrid/{order.Id}?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    private static string HungerClass(HungerLevel hunger) => hunger switch
    {
        HungerLevel.Starving => "text-bg-danger",
        HungerLevel.VeryHungry => "text-bg-warning",
        HungerLevel.Hungry => "text-bg-info",
        _ => "text-bg-light border"
    };

    private static string StatusIcon(OrderStatus status) => status switch
    {
        OrderStatus.Received => "📝",
        OrderStatus.InOven => "🔥",
        OrderStatus.OutForDelivery => "🛵",
        _ => "✅"
    };

    public void Dispose() => Navigation.LocationChanged -= OnLocationChanged;
}
