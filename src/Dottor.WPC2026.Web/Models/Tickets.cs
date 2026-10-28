using System.ComponentModel.DataAnnotations;

namespace Dottor.WPC2026.Web.Models;

public enum TicketPriority
{
    [Display(Name = "Bassa")] Low,
    [Display(Name = "Media")] Medium,
    [Display(Name = "Alta")] High,
    [Display(Name = "Critica")] Critical
}

public enum TicketStatus
{
    [Display(Name = "Aperto")] Open,
    [Display(Name = "In lavorazione")] InProgress,
    [Display(Name = "In attesa")] Waiting,
    [Display(Name = "Chiuso")] Closed
}

public sealed record Ticket(
    int Id,
    string Title,
    string Description,
    TicketPriority Priority,
    TicketStatus Status,
    DateTime CreatedAt,
    string AssignedTo,
    string? ReporterEmail = null);

/// <summary>Bozza di ticket della demo Circuit Pause: deve essere serializzabile per [PersistentState].</summary>
public sealed class TicketDraft
{
    public string? Title { get; set; }
    public string? Description { get; set; }
}

/// <summary>Modello del form Static SSR: usa solo attributi supportati dalla validazione client-side.</summary>
public sealed class NewTicketModel
{
    [Required(ErrorMessage = "Il titolo è obbligatorio.")]
    [StringLength(80, MinimumLength = 5, ErrorMessage = "Il titolo deve contenere tra 5 e 80 caratteri.")]
    [Display(Name = "Titolo")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "La descrizione è obbligatoria.")]
    [MinLength(20, ErrorMessage = "La descrizione deve contenere almeno 20 caratteri.")]
    [Display(Name = "Descrizione")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "L'email è obbligatoria.")]
    [EmailAddress(ErrorMessage = "Inserisci un indirizzo email valido.")]
    [Display(Name = "Email di contatto")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Seleziona una priorità.")]
    [Display(Name = "Priorità")]
    public TicketPriority? Priority { get; set; }
}
