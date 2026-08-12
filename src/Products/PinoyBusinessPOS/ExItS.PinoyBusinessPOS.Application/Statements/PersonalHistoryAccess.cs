namespace ExItS.PinoyBusinessPOS.Application.Statements;

/// <summary>POS-side free recent-history window for linked-customer statements (WP06).</summary>
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
