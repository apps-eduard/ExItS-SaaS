using ExItS.PinoyBusinessPOS.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ExItS.PinoyBusinessPOS.Application.Auth;

/// <summary>
/// Structured local security events only. Never logs passwords, tokens, or authorization headers.
/// Platform audit remains the system-of-record when production auth exists.
/// </summary>
public sealed class LoggingAuthEventSink(ILogger<LoggingAuthEventSink> logger) : IAuthEventSink
{
    private static readonly HashSet<string> ForbiddenKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "token", "authorization", "credential", "secret", "marker",
        "pin", "hash", "verifier", "salt"
    };

    public void Record(string eventName, IReadOnlyDictionary<string, string?> safeProperties)
    {
        var filtered = safeProperties
            .Where(kv => !ForbiddenKeys.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

        logger.LogInformation("POS auth event {EventName} {@Properties}", eventName, filtered);
    }
}
