using System.Collections.Concurrent;

namespace Dottor.WPC2026.Web.Services;

/// <summary>Soci del Pizza Club in memoria, con latenza simulata.</summary>
public sealed class UserService(ILogger<UserService> logger)
{
    private static readonly TimeSpan SimulatedLatency = TimeSpan.FromMilliseconds(800);

    private readonly ConcurrentDictionary<string, string> _users = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mario.rossi@example.com"] = "Mario Rossi",
        ["giulia.bianchi@example.com"] = "Giulia Bianchi",
        ["andrea@example.com"] = "Andrea Verdi"
    };

    public IReadOnlyCollection<string> RegisteredEmails => [.. _users.Keys.Order()];

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        logger.LogInformation("Verifica email {Email} avviata", email);
        try
        {
            await Task.Delay(SimulatedLatency, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Verifica email {Email} annullata", email);
            throw;
        }

        // Permette di mostrare lo stato "faulted" durante la demo.
        if (email.StartsWith("errore@", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Servizio di verifica non disponibile: il server è in pausa pranzo.");
        }

        var exists = _users.ContainsKey(email);
        logger.LogInformation("Verifica email {Email} completata: {Result}", email, exists ? "già registrata" : "disponibile");
        return exists;
    }

    public async Task RegisterAsync(string email, string fullName, CancellationToken cancellationToken = default)
    {
        await Task.Delay(300, cancellationToken);
        _users[email] = fullName;
    }
}
