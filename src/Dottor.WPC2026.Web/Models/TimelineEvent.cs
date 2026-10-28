using System.ComponentModel.DataAnnotations;

namespace Dottor.WPC2026.Web.Models;

public enum EventSeverity
{
    [Display(Name = "Info")] Info,
    [Display(Name = "Avviso")] Warning,
    [Display(Name = "Errore")] Error,
    [Display(Name = "Critico")] Critical
}

public sealed record TimelineEvent(
    int Id,
    DateTime Timestamp,
    string Title,
    string Description,
    EventSeverity Severity);
