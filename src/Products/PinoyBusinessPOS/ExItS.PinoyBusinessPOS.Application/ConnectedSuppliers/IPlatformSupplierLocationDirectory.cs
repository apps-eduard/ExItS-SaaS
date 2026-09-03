using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Lists Active supplier operating locations by PublicOrganizationId (no membership grant).
/// </summary>
public interface IPlatformSupplierLocationDirectory
{
    Task<ApplicationResult<IReadOnlyList<PlatformSupplierLocationDto>>> ListActiveLocationsAsync(
        string publicOrganizationId,
        CancellationToken cancellationToken = default);
}

public sealed record PlatformSupplierLocationDto(
    Guid BranchId,
    string Name,
    string Code,
    bool IsPrimary);
