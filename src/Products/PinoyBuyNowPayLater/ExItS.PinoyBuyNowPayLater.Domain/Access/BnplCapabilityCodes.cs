namespace ExItS.PinoyBuyNowPayLater.Domain.Access;

/// <summary>
/// Stable BNPL capability identifiers. Authorization checks capabilities, never role/preset names.
/// Implemented in BNPL-02; extended in BNPL-03 with customer capabilities (BNPL-D-00-18).
/// </summary>
public static class BnplCapabilityCodes
{
    public const string Config = "bnpl.config";
    public const string CustomerRead = "bnpl.customer.read";
    public const string CustomerManage = "bnpl.customer.manage";
    public const string ApplicationCreate = "bnpl.application.create";
    public const string ApplicationApprove = "bnpl.application.approve";
    public const string PlanRead = "bnpl.plan.read";
    public const string RepaymentCreate = "bnpl.repayment.create";
    public const string CollectionsManage = "bnpl.collections.manage";
    public const string SettlementManage = "bnpl.settlement.manage";
    public const string AuditRead = "bnpl.audit.read";
    public const string ReportsRead = "bnpl.reports.read";

    public static IReadOnlyList<string> All { get; } =
    [
        Config,
        CustomerRead,
        CustomerManage,
        ApplicationCreate,
        ApplicationApprove,
        PlanRead,
        RepaymentCreate,
        CollectionsManage,
        SettlementManage,
        AuditRead,
        ReportsRead
    ];

    public static bool IsKnown(string? capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
        {
            return false;
        }

        var normalized = capability.Trim();
        return All.Any(c => string.Equals(c, normalized, StringComparison.Ordinal));
    }
}
