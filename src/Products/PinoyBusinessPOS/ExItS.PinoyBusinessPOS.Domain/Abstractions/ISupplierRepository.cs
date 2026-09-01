using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Domain.Abstractions;

public sealed record SupplierFilter(
    string? SupplierCode = null,
    string? Name = null,
    string? ContactPerson = null,
    string? Email = null,
    string? Mobile = null,
    string? TaxOrRegistrationNumber = null,
    SupplierStatus? Status = null);

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(
        PosOrganizationId organizationId,
        SupplierId supplierId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Supplier> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        SupplierFilter filter,
        int skip,
        int take,
        IReadOnlyCollection<Guid>? restrictToSupplierIds = null,
        CancellationToken cancellationToken = default);

    Task<Supplier?> FindActiveByNormalizedNameAsync(
        PosOrganizationId organizationId,
        string normalizedName,
        CancellationToken cancellationToken = default);

    Task<Supplier?> FindActiveByNormalizedEmailAsync(
        PosOrganizationId organizationId,
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task<Supplier?> FindActiveByNormalizedMobileAsync(
        PosOrganizationId organizationId,
        string normalizedMobile,
        CancellationToken cancellationToken = default);

    Task<Supplier?> FindActiveByNormalizedTaxAsync(
        PosOrganizationId organizationId,
        string normalizedTax,
        CancellationToken cancellationToken = default);

    Task<string> AllocateNextSupplierCodeAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);

    /// <summary>Batch display-name lookup (avoids N+1 when mapping payables/report rows).</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesByIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<Guid> supplierIds,
        CancellationToken cancellationToken = default);
}
