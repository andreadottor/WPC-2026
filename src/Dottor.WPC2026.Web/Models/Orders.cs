using System.ComponentModel.DataAnnotations;

namespace Dottor.WPC2026.Web.Models;

/// <summary>Quanto ha fame il cliente: la "priorità" dell'ordine.</summary>
public enum HungerLevel
{
    [Display(Name = "Languorino")] Peckish,
    [Display(Name = "Fame")] Hungry,
    [Display(Name = "Tanta fame")] VeryHungry,
    [Display(Name = "Fame da lupi 🐺")] Starving
}

public enum OrderStatus
{
    [Display(Name = "Ricevuto")] Received,
    [Display(Name = "In forno")] InOven,
    [Display(Name = "In consegna")] OutForDelivery,
    [Display(Name = "Consegnato")] Delivered
}

public sealed record Order(
    int Id,
    string Pizza,
    string Address,
    HungerLevel Hunger,
    OrderStatus Status,
    DateTime CreatedAt,
    string Rider,
    string? CustomerEmail = null);

/// <summary>Ordine in compilazione della demo Circuit Pause: deve essere serializzabile per [PersistentState].</summary>
public sealed class OrderDraft
{
    public string? Address { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Modello del form Static SSR: usa solo attributi supportati dalla validazione client-side.</summary>
public sealed class NewOrderModel
{
    [Required(ErrorMessage = "Scegli una pizza.")]
    [StringLength(60, MinimumLength = 5, ErrorMessage = "Il nome della pizza deve contenere tra 5 e 60 caratteri.")]
    [Display(Name = "Pizza")]
    public string? Pizza { get; set; }

    [Required(ErrorMessage = "L'indirizzo di consegna è obbligatorio.")]
    [MinLength(10, ErrorMessage = "L'indirizzo deve contenere almeno 10 caratteri: il rider non è un indovino.")]
    [Display(Name = "Indirizzo di consegna")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "L'email è obbligatoria.")]
    [EmailAddress(ErrorMessage = "Inserisci un indirizzo email valido.")]
    [Display(Name = "Email per la conferma")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Dicci quanta fame hai.")]
    [Display(Name = "Livello di fame")]
    public HungerLevel? Hunger { get; set; }
}
