using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Resolves a Platform organization from a public organization ID or Business QR payload.
/// POS Application must not call Platform DB directly — implementations call Platform HTTP APIs.
/// </summary>
public interface IPlatformOrganizationPublicResolve
{
    Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> ResolveOrganizationForConnectedSupplierAsync(
        string publicOrganizationIdOrQrPayload,
        CancellationToken cancellationToken = default);
}

public sealed record PlatformOrganizationPublicResolveResult(
    Guid OrganizationId,
    string PublicOrganizationId,
    string DisplayName);
