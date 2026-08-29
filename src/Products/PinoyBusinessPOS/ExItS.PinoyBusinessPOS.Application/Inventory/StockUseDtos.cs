using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed record StockUseLineDto(
    Guid LineId,
    Guid ProductId,
    Guid? ProductUnitId,
    int LineNumber,
    decimal QuantityEntered,
    decimal MultiplierToBase,
    decimal BaseQuantity,
    string NameSnapshot,
    string UnitLabelSnapshot,
    decimal? UnitCostSnapshot,
    decimal? LineCostSnapshot,
    Guid? InventoryMovementId);

public sealed record StockUseDto(
    Guid StockUseId,
    Guid OrganizationId,
    Guid? BranchId,
    string StockUseNumber,
    string? ReferenceNumber,
    DateTimeOffset OccurredAtUtc,
    string Reason,
    string? Notes,
    string Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    Guid? VoidedByUserId,
    DateTimeOffset? VoidedAtUtc,
    IReadOnlyList<StockUseLineDto> Lines);

public sealed record StockUseListItemDto(
    Guid StockUseId,
    string StockUseNumber,
    Guid? BranchId,
    string? ReferenceNumber,
    DateTimeOffset OccurredAtUtc,
    string Reason,
    string Status,
    int LineCount,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateStockUseLineRequest(
    Guid ProductId,
    decimal Quantity,
    Guid? ProductUnitId = null);

public sealed record CreateStockUseRequest(
    string Reason,
    IReadOnlyList<CreateStockUseLineRequest> Lines,
    Guid? BranchId = null,
    string? ReferenceNumber = null,
    string? Notes = null,
    DateTimeOffset? OccurredAtUtc = null,
    Guid? StockUseId = null,
    string? IdempotencyKey = null);

public sealed record StockUseFilter(
    DateTimeOffset? FromOccurredAtUtc = null,
    DateTimeOffset? ToOccurredAtUtc = null,
    string? Reason = null,
    string? Status = null,
    Guid? BranchId = null,
    string? ReferenceNumber = null);

public static class StockUseMapper
{
    public static StockUseDto Map(StockUse stockUse) =>
        new(
            stockUse.Id.Value,
            stockUse.OrganizationId.Value,
            stockUse.BranchId?.Value,
            stockUse.StockUseNumber,
            stockUse.ReferenceNumber,
            stockUse.OccurredAtUtc,
            StockUseReasons.ToCode(stockUse.Reason),
            stockUse.Notes,
            StockUseStatuses.ToCode(stockUse.Status),
            stockUse.CreatedByUserId,
            stockUse.CreatedAtUtc,
            stockUse.VoidedByUserId,
            stockUse.VoidedAtUtc,
            stockUse.Lines.Select(MapLine).ToList());

    public static StockUseListItemDto MapListItem(StockUse stockUse) =>
        new(
            stockUse.Id.Value,
            stockUse.StockUseNumber,
            stockUse.BranchId?.Value,
            stockUse.ReferenceNumber,
            stockUse.OccurredAtUtc,
            StockUseReasons.ToCode(stockUse.Reason),
            StockUseStatuses.ToCode(stockUse.Status),
            stockUse.Lines.Count,
            stockUse.CreatedAtUtc);

    public static StockUseLineDto MapLine(StockUseLine line) =>
        new(
            line.Id.Value,
            line.ProductId.Value,
            line.ProductUnitId?.Value,
            line.LineNumber,
            line.QuantityEntered,
            line.MultiplierToBase,
            line.BaseQuantity,
            line.NameSnapshot,
            line.UnitLabelSnapshot,
            line.UnitCostSnapshot,
            line.LineCostSnapshot,
            line.InventoryMovementId);
}
