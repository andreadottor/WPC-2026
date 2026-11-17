namespace Dottor.WPC2026.Web.Client.Models;

/// <summary>Risposta di /api/tickets/stats, condivisa tra server e client.</summary>
public sealed record TicketStats(
    int Total,
    int Open,
    int Critical,
    string? LastTicketTitle,
    string ServerVersion,
    DateTimeOffset GeneratedAt);