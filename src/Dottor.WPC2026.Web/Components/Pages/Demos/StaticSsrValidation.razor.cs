using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class StaticSsrValidation
{
    private static int postCount;
    private string? flashMessage;
    private int? flashTicketId;

    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    [SupplyParameterFromForm]
    public NewTicketModel? Input { get; set; }

    // Novità .NET 11: TempData in Static SSR, ideale per i messaggi dopo un redirect.
    [SupplyParameterFromTempData]
    public string? Message { get; set; }

    [SupplyParameterFromTempData]
    public int? CreatedTicketId { get; set; }

    // Novità .NET 11: valore letto dalla Session e riscritto a fine richiesta (resta fino alla fine della sessione).
    [SupplyParameterFromSession(Name = "MyTicketIds")]
    public string? MyTicketIds { get; set; }

    private List<Ticket> sessionTickets = [];

    protected override void OnInitialized()
    {
        Input ??= new NewTicketModel();

        // Il valore della proprietà viene riscritto in TempData a fine richiesta:
        // lo copiamo e lo azzeriamo, così il messaggio compare una sola volta.
        (flashMessage, flashTicketId) = (Message, CreatedTicketId);
        (Message, CreatedTicketId) = (null, null);

        sessionTickets = (MyTicketIds ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => int.TryParse(id, out var value) ? Tickets.GetById(value) : null)
            .OfType<Ticket>()
            .ToList();

        if (HttpMethods.IsPost(HttpContext?.Request.Method ?? ""))
        {
            Interlocked.Increment(ref postCount);
        }
    }

    private void CreateTicket()
    {
        var ticket = Tickets.Create(Input!);
        Message = $"Ticket #{ticket.Id} \"{ticket.Title}\" creato con priorità {ticket.Priority.ToDisplayName()}.";
        CreatedTicketId = ticket.Id;
        MyTicketIds = string.IsNullOrEmpty(MyTicketIds) ? $"{ticket.Id}" : $"{MyTicketIds},{ticket.Id}";
        Navigation.NavigateTo("demo/static-ssr-validation");
    }
}
