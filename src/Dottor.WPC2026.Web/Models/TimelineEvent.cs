using System.ComponentModel.DataAnnotations;

namespace Dottor.WPC2026.Web.Models;

public enum EventSeverity
{
    [Display(Name = "Tutto ok")] Info,
    [Display(Name = "Attenzione")] Warning,
    [Display(Name = "Problema")] Error,
    [Display(Name = "Allarme rosso")] Critical
}

public sealed record TimelineEvent(
    int Id,
    DateTime Timestamp,
    string Title,
    string Description,
    EventSeverity Severity);
