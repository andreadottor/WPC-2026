namespace Dottor.WPC2026.Web.Models;

using System.ComponentModel.DataAnnotations;
using Dottor.WPC2026.Web.Attributes;

public sealed class RegistrationModel
{
    // Regole sincrone: valutate subito, a ogni modifica.
    [Required(ErrorMessage = "Il nome completo è obbligatorio.")]
    [StringLength(60, MinimumLength = 3, ErrorMessage = "Il nome deve contenere tra 3 e 60 caratteri.")]
    [Display(Name = "Nome completo")]
    public string? FullName { get; set; }

    // Regole sincrone + regola asincrona (UniqueEmail) sullo stesso campo.
    [Required(ErrorMessage = "L'email è obbligatoria.")]
    [EmailAddress(ErrorMessage = "Inserisci un indirizzo email valido.")]
    [UniqueEmail]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Dicci quante pizze mangi al mese (siamo tra amici).")]
    [Range(1, 30, ErrorMessage = "Tra 1 e 30 pizze al mese: oltre serve un medico, non una tessera.")]
    [Display(Name = "Pizze al mese")]
    public int? PizzasPerMonth { get; set; }
}
