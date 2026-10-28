using System.ComponentModel.DataAnnotations;
using Dottor.WPC2026.Web.Services;

namespace Dottor.WPC2026.Web.Models;

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

    [Required(ErrorMessage = "L'età è obbligatoria.")]
    [Range(18, 120, ErrorMessage = "L'età deve essere compresa tra 18 e 120 anni.")]
    [Display(Name = "Età")]
    public int? Age { get; set; }
}

/// <summary>
/// Validazione asincrona nativa di .NET 11: il framework passa il CancellationToken
/// e lo annulla se il valore cambia prima che il controllo termini.
/// </summary>
public sealed class UniqueEmailAttribute() : AsyncValidationAttribute("L'indirizzo {0} è già registrato.")
{
    // Il controllo richiede I/O: come indicato dalla documentazione, il percorso sincrono non lo supporta.
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) =>
        value is not string email || string.IsNullOrWhiteSpace(email)
            ? ValidationResult.Success
            : throw new InvalidOperationException("UniqueEmail richiede la validazione asincrona (Validator.TryValidateObjectAsync / EditContext.ValidateAsync).");

    protected override async Task<ValidationResult?> IsValidAsync(
        object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        if (value is not string email || string.IsNullOrWhiteSpace(email))
        {
            return ValidationResult.Success; // Ci pensa [Required]
        }

        var users = validationContext.GetRequiredService<UserService>();
        var exists = await users.EmailExistsAsync(email, cancellationToken);

        return exists
            ? new ValidationResult(FormatErrorMessage(email), [validationContext.MemberName!])
            : ValidationResult.Success;
    }
}
