namespace ExItS.PinoyBuyNowPayLater.Domain.Access;

/// <summary>
/// Convenience grant bundles for Local Validation / admin UX. Never authorize by preset name.
/// </summary>
public static class BnplCapabilityPresets
{
    public const string Owner = "Owner";
    public const string Manager = "Manager";
    public const string Approver = "BnplApprover";
    public const string Sales = "Sales";
    public const string Collector = "Collector";
    public const string Reporting = "Reporting";

    public static IReadOnlySet<string> CapabilitiesFor(string? presetLabel)
    {
        if (string.IsNullOrWhiteSpace(presetLabel))
        {
            return Empty;
        }

        return presetLabel.Trim() switch
        {
            Owner => OwnerCapabilities,
            Manager => ManagerCapabilities,
            Approver => ApproverCapabilities,
            Sales => SalesCapabilities,
            Collector => CollectorCapabilities,
            Reporting => ReportingCapabilities,
            _ => Empty
        };
    }

    public static IReadOnlySet<string> Empty { get; } = new HashSet<string>(StringComparer.Ordinal);

    public static IReadOnlySet<string> OwnerCapabilities { get; } = new HashSet<string>(BnplCapabilityCodes.All, StringComparer.Ordinal);

    public static IReadOnlySet<string> ManagerCapabilities { get; } = new HashSet<string>(
    [
        BnplCapabilityCodes.CustomerRead,
        BnplCapabilityCodes.CustomerManage,
        BnplCapabilityCodes.ApplicationRead,
        BnplCapabilityCodes.ApplicationCreate,
        BnplCapabilityCodes.ApplicationApprove,
        BnplCapabilityCodes.PlanRead,
        BnplCapabilityCodes.PlanManage,
        BnplCapabilityCodes.RepaymentCreate,
        BnplCapabilityCodes.CollectionsManage,
        BnplCapabilityCodes.AuditRead,
        BnplCapabilityCodes.ReportsRead
    ], StringComparer.Ordinal);

    public static IReadOnlySet<string> ApproverCapabilities { get; } = new HashSet<string>(
    [
        BnplCapabilityCodes.CustomerRead,
        BnplCapabilityCodes.ApplicationRead,
        BnplCapabilityCodes.ApplicationApprove,
        BnplCapabilityCodes.PlanRead
    ], StringComparer.Ordinal);

    public static IReadOnlySet<string> SalesCapabilities { get; } = new HashSet<string>(
    [
        BnplCapabilityCodes.CustomerRead,
        BnplCapabilityCodes.CustomerManage,
        BnplCapabilityCodes.ApplicationRead,
        BnplCapabilityCodes.ApplicationCreate,
        BnplCapabilityCodes.PlanRead,
        BnplCapabilityCodes.PlanManage
    ], StringComparer.Ordinal);

    public static IReadOnlySet<string> CollectorCapabilities { get; } = new HashSet<string>(
    [
        BnplCapabilityCodes.CustomerRead,
        BnplCapabilityCodes.ApplicationRead,
        BnplCapabilityCodes.PlanRead,
        BnplCapabilityCodes.RepaymentCreate,
        BnplCapabilityCodes.CollectionsManage
    ], StringComparer.Ordinal);

    public static IReadOnlySet<string> ReportingCapabilities { get; } = new HashSet<string>(
    [
        BnplCapabilityCodes.CustomerRead,
        BnplCapabilityCodes.ApplicationRead,
        BnplCapabilityCodes.PlanRead,
        BnplCapabilityCodes.ReportsRead
    ], StringComparer.Ordinal);
}
