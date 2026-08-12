namespace ExItS.PinoyBusinessPOS.Application.Statements;

/// <summary>POS-side free recent-history window for linked-customer statements (WP06/WP10).</summary>
public sealed class PersonalStatementsOptions
{
    public const string SectionName = "PersonalStatements";

    /// <summary>
    /// Inclusive calendar months of free history: current UTC month plus (N-1) prior months.
    /// Default 3 ⇒ current month + previous 2 months. Configuration-driven; not pricing.
    /// </summary>
    public int FreeRecentMonths { get; set; } = 3;
}

public static class PersonalHistoryWindows
{
    /// <summary>
    /// Start of the free-history window (inclusive) in UTC calendar months.
    /// Not rolling days: with N=3 as of mid-August → 1 June 00:00:00Z.
    /// </summary>
    public static DateTimeOffset ComputeFreeWindowStart(DateTimeOffset asOfUtc, int freeRecentMonths)
    {
        if (asOfUtc.Offset != TimeSpan.Zero)
        {
            asOfUtc = asOfUtc.ToUniversalTime();
        }

        var months = Math.Clamp(freeRecentMonths, 1, 120);
        var monthStart = new DateTimeOffset(asOfUtc.Year, asOfUtc.Month, 1, 0, 0, 0, TimeSpan.Zero);
        return monthStart.AddMonths(-(months - 1));
    }
}

/// <summary>Result of settled-history / receipt detail gating after ownership is established.</summary>
public enum PersonalHistoryDetailAccessDecision
{
    Allowed = 0,
    ExtendedHistoryRequired = 1
}

/// <summary>
/// Shared Personal settled-history policy (WP06/WP10). Controllers must not own this logic.
/// Ownership/privacy (404) is evaluated by callers <em>before</em> invoking these helpers.
/// </summary>
public static class PersonalSettledHistoryPolicy
{
    public const string ExtendedFeatureCode = PersonalDigitalRecordsFeatureCodes.Extended;

    /// <summary>
    /// Detail/receipt ordering after ownership:
    /// free window → specific open-debt exception → entitlement → premium denial.
    /// </summary>
    public static PersonalHistoryDetailAccessDecision EvaluateDetailAccess(
        DateTimeOffset recordOccurredAtUtc,
        DateTimeOffset freeHistoryStartsAtUtc,
        bool openDebtExceptionApplies,
        bool hasExtendedEntitlement)
    {
        if (recordOccurredAtUtc >= freeHistoryStartsAtUtc)
        {
            return PersonalHistoryDetailAccessDecision.Allowed;
        }

        if (openDebtExceptionApplies)
        {
            return PersonalHistoryDetailAccessDecision.Allowed;
        }

        if (hasExtendedEntitlement)
        {
            return PersonalHistoryDetailAccessDecision.Allowed;
        }

        return PersonalHistoryDetailAccessDecision.ExtendedHistoryRequired;
    }

    /// <summary>
    /// Open-debt exception for a receipt: outstanding must be genuinely &gt; 0 and the sale's
    /// linked credit must still be Active. Settling to zero removes the exception.
    /// Does not unlock unrelated old settled receipts.
    /// </summary>
    public static bool OpenDebtReceiptExceptionApplies(
        decimal outstandingBalance,
        bool isUtangSale,
        bool hasLinkedCredit,
        bool linkedCreditIsActive) =>
        outstandingBalance > 0m
        && isUtangSale
        && hasLinkedCredit
        && linkedCreditIsActive;
}

/// <summary>Platform-owned Personal feature check used by POS history APIs.</summary>
public interface IPersonalFeatureEntitlementClient
{
    /// <summary>
    /// Returns true when the authenticated Personal session has an active entitlement.
    /// Fail-closed (false) on unreachable Platform / errors — never invents entitlement.
    /// </summary>
    Task<bool> HasActiveEntitlementAsync(
        string featureCode,
        CancellationToken cancellationToken = default);
}

public static class PersonalDigitalRecordsFeatureCodes
{
    public const string Extended = "personal-digital-records-extended";
}
