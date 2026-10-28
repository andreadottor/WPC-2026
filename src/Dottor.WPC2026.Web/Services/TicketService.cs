using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Services;

/// <summary>Ticket demo generati in memoria (seed fisso: stessi dati a ogni avvio).</summary>
public sealed class TicketService
{
    private static readonly string[] Subjects =
    [
        "Errore di accesso al portale", "Stampante non raggiungibile", "Richiesta nuova licenza",
        "Rallentamento dell'applicazione", "Email non sincronizzate", "VPN disconnessa",
        "Aggiornamento driver", "Report mensile non generato", "Reset password",
        "Configurazione nuovo laptop", "Errore 500 in fase di checkout", "Backup non completato"
    ];

    private static readonly string[] Areas = ["Milano", "Roma", "Torino", "Bologna", "Napoli", "remoto"];

    private static readonly string[] Agents =
    [
        "Andrea Dottor", "Chiara Ferri", "Luca Moretti", "Sara Galli", "Paolo Conti", "Elena Riva"
    ];

    private readonly Lock _lock = new();
    private readonly List<Ticket> _tickets;

    public TicketService()
    {
        var random = new Random(2026);
        var start = new DateTime(2026, 1, 1, 8, 0, 0);

        _tickets = [.. Enumerable.Range(1, 240).Select(id => new Ticket(
            Id: id,
            Title: $"{Subjects[random.Next(Subjects.Length)]} ({Areas[random.Next(Areas.Length)]})",
            Description: "Ticket generato automaticamente per la demo QuickGrid.",
            Priority: (TicketPriority)random.Next(4),
            Status: (TicketStatus)random.Next(4),
            CreatedAt: start.AddHours(random.Next(0, 24 * 150)),
            AssignedTo: Agents[random.Next(Agents.Length)]))];
    }

    public IQueryable<Ticket> GetAll()
    {
        lock (_lock)
        {
            return _tickets.ToArray().AsQueryable();
        }
    }

    public Ticket? GetById(int id)
    {
        lock (_lock)
        {
            return _tickets.FirstOrDefault(t => t.Id == id);
        }
    }

    public Ticket Create(NewTicketModel model)
    {
        lock (_lock)
        {
            var ticket = new Ticket(
                Id: _tickets.Max(t => t.Id) + 1,
                Title: model.Title!,
                Description: model.Description!,
                Priority: model.Priority!.Value,
                Status: TicketStatus.Open,
                CreatedAt: DateTime.Now,
                AssignedTo: "Da assegnare",
                ReporterEmail: model.Email);
            _tickets.Add(ticket);
            return ticket;
        }
    }
}
