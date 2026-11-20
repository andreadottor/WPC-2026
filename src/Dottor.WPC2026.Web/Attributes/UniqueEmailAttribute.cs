namespace Dottor.WPC2026.Web.Attributes;

using Dottor.WPC2026.Web.Services;
using System.ComponentModel.DataAnnotations;


/// <summary>
/// Validazione asincrona nativa di .NET 11: il framework passa il CancellationToken
/// e lo annulla se il valore cambia prima che il controllo termini.
/// </summary>
public sealed class UniqueEmailAttribute() : AsyncValidationAttribute("L'indirizzo {0} ha già la tessera del Pizza Club.")
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

