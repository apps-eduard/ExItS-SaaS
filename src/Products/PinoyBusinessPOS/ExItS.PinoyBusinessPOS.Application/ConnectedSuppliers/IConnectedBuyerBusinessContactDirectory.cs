using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Privacy-safe buyer-organization business contacts for Connected B2B relationship contact picker.
/// Implemented by POS API via Platform <c>/b2b-business-contacts</c> (not the private members directory).
/// </summary>
public interface IConnectedBuyerBusinessContactDirectory
{
    Task<ApplicationResult<IReadOnlyList<BuyerOrganizationBusinessContactDto>>> ListAsync(
        Guid buyerOrganizationId,
        Guid requesterSupplierOrganizationId,
        string? search = null,
        CancellationToken ct = default);

    Task<ApplicationResult<BuyerOrganizationBusinessContactDto?>> GetAsync(
        Guid buyerOrganizationId,
        Guid requesterSupplierOrganizationId,
        Guid organizationMemberId,
        CancellationToken ct = default);
}

public sealed record BuyerOrganizationBusinessContactDto(
    Guid OrganizationMemberId,
    Guid UserId,
    string DisplayName,
    string RoleTitle,
    bool IsOwner,
    string? Department,
    string? Phone,
    string? Email,
    string? EmployeeCode);
