namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Read-only physical and reservation consistency audit (MB2-02C). Never mutates inventory state.
/// </summary>
public interface IInventoryPhysicalAudit
{
    Task<InventoryPhysicalAuditResult> AuditAsync(
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);
}

public sealed record InventoryPhysicalAuditResult(
    int OrganizationsScanned,
    int ProductsScanned,
    int BranchBalancesScanned,
    int OrgOnHandMismatchCount,
    int OrgReservedMismatchCount,
    int ReservationMismatchCount,
    int NegativeOnHandCount,
    int NegativeReservedCount,
    int OverReservedCount,
    int LotMismatchCount,
    int MovementBranchIssueCount,
    int UnresolvedLegacyCount,
    bool IsClean);

public sealed record PosInventoryBranchBreakdownDto(
    Guid BranchId,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity);

public sealed record PosOrganizationInventoryProductDto(
    Guid ProductId,
    string ProductName,
    string UnitOfMeasure,
    decimal OrganizationOnHandQuantity,
    decimal OrganizationReservedQuantity,
    decimal OrganizationAvailableQuantity,
    IReadOnlyList<PosInventoryBranchBreakdownDto> Branches);
