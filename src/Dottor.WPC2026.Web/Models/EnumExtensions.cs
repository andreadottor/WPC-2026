using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Dottor.WPC2026.Web.Models;

public static class EnumExtensions
{
    /// <summary>Restituisce il testo di [Display(Name)] (in italiano) o il nome del valore.</summary>
    public static string ToDisplayName(this Enum value) =>
        value.GetType().GetField(value.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName()
        ?? value.ToString();
}
