namespace ExItS.PinoyBusinessPOS.Domain.Payments;

/// <summary>
/// Check clearing lifecycle for Utang repayments.
/// Received/PendingClearing does not settle outstanding; Cleared settles exactly once.
/// </summary>
public enum UtangCheckClearingStatus
{
    /// <summary>Non-check methods (Cash / ManualGCash).</summary>
    None = 0,
    PendingClearing = 1,
    Cleared = 2,
    Bounced = 3,
    Cancelled = 4
}

public static class UtangCheckClearingStatuses
{
    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(UtangCheckClearingStatus.None),
        nameof(UtangCheckClearingStatus.PendingClearing),
        nameof(UtangCheckClearingStatus.Cleared),
        nameof(UtangCheckClearingStatus.Bounced),
        nameof(UtangCheckClearingStatus.Cancelled)
    ];

    public static bool TryParse(string? code, out UtangCheckClearingStatus status)
    {
        status = UtangCheckClearingStatus.None;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var match = Codes.FirstOrDefault(c =>
            string.Equals(c, code.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        status = Enum.Parse<UtangCheckClearingStatus>(match, ignoreCase: false);
        return true;
    }
}
