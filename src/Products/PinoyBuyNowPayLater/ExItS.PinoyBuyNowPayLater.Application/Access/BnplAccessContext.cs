namespace ExItS.PinoyBuyNowPayLater.Application.Access;

/// <summary>
/// Transport-neutral trusted BNPL operational access facts.
/// Does not include role names, cookies, headers, or commercial plan pricing.
/// Capability and branch facts are supplied by an approved trusted transport (D-P12-03).
/// </summary>
public sealed class BnplAccessContext
{
    public BnplAccessContext(
        Guid actorId,
        Guid organizationId,
        string productCode,
        bool hasTrustedOrganizationMembership,
        bool hasTrustedOrganizationEntitlement,
        bool hasTrustedProductAssignment,
        BnplBranchScope branchScope,
        IEnumerable<string> capabilities)
    {
        ActorId = actorId;
        OrganizationId = organizationId;
        ProductCode = productCode?.Trim() ?? string.Empty;
        HasTrustedOrganizationMembership = hasTrustedOrganizationMembership;
        HasTrustedOrganizationEntitlement = hasTrustedOrganizationEntitlement;
        HasTrustedProductAssignment = hasTrustedProductAssignment;
        BranchScope = branchScope ?? BnplBranchScope.None;
        Capabilities = new HashSet<string>(
            (capabilities ?? Array.Empty<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim()),
            StringComparer.Ordinal);
    }

    public Guid ActorId { get; }

    public Guid OrganizationId { get; }

    public string ProductCode { get; }

    public bool HasTrustedOrganizationMembership { get; }

    public bool HasTrustedOrganizationEntitlement { get; }

    public bool HasTrustedProductAssignment { get; }

    public BnplBranchScope BranchScope { get; }

    public IReadOnlySet<string> Capabilities { get; }

    public bool HasCapability(string capability) =>
        !string.IsNullOrWhiteSpace(capability)
        && Capabilities.Contains(capability.Trim());
}
