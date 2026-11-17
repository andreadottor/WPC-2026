namespace Dottor.WPC2026.Web.Client.Models;

/// <summary>Risposta di /api/kitchen/stats, condivisa tra server e client.</summary>
public sealed record KitchenStats(
    int Total,
    int InOven,
    int Starving,
    string? LastOrder,
    string ServerVersion,
    DateTimeOffset GeneratedAt);
