using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class StaticSsrValidation
{
    private static int _postCount;
    private string? _flashMessage;
    private int? _flashOrderId;

    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    [SupplyParameterFromForm]
    public NewOrderModel? Input { get; set; }

    // Novità .NET 11: TempData in Static SSR, ideale per i messaggi dopo un redirect.
    [SupplyParameterFromTempData]
    public string? Message { get; set; }

    [SupplyParameterFromTempData]
    public int? CreatedOrderId { get; set; }

    // Novità .NET 11: valore letto dalla Session e riscritto a fine richiesta (resta fino alla fine della sessione).
    [SupplyParameterFromSession(Name = "MyOrderIds")]
    public string? MyOrderIds { get; set; }

    private List<Order> _sessionOrders = [];

    protected override void OnInitialized()
    {
        Input ??= new NewOrderModel();

        // Il valore della proprietà viene riscritto in TempData a fine richiesta:
        // lo copiamo e lo azzeriamo, così il messaggio compare una sola volta.
        (_flashMessage, _flashOrderId) = (Message, CreatedOrderId);
        (Message, CreatedOrderId) = (null, null);

        _sessionOrders = (MyOrderIds ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => int.TryParse(id, out var value) ? Orders.GetById(value) : null)
            .OfType<Order>()
            .ToList();

        if (HttpMethods.IsPost(HttpContext?.Request.Method ?? ""))
        {
            Interlocked.Increment(ref _postCount);
        }
    }

    private void PlaceOrder()
    {
        var order = Orders.Create(Input!);
        Message = $"Ordine #{order.Id} ricevuto: una {order.Pizza} sta per entrare in forno 🔥 (livello di fame: {order.Hunger.ToDisplayName()}).";
        CreatedOrderId = order.Id;
        MyOrderIds = string.IsNullOrEmpty(MyOrderIds) ? $"{order.Id}" : $"{MyOrderIds},{order.Id}";
        Navigation.NavigateTo("demo/static-ssr-validation");
    }
}
