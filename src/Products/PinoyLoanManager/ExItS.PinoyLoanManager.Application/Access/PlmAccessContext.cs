namespace ExItS.PinoyLoanManager.Application.Access;

/// <summary>
/// Transport-neutral trusted PLM operational access facts.
/// Does not include roles, grants, cookies, headers, or commercial plan details.
/// </summary>
public sealed class PlmAccessContext
{
    public PlmAccessContext(
        Guid actorId,
        Guid organizationId,
        string productCode,
        bool hasTrustedProductAccess)
    {
        ActorId = actorId;
        OrganizationId = organizationId;
        ProductCode = productCode?.Trim() ?? string.Empty;
        HasTrustedProductAccess = hasTrustedProductAccess;
    }

    public Guid ActorId { get; }

    public Guid OrganizationId { get; }

    public string ProductCode { get; }

    public bool HasTrustedProductAccess { get; }
}
