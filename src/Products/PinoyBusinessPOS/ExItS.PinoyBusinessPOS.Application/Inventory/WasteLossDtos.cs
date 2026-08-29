using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed record WasteLossLineDto(
    Guid LineId,
    Guid ProductId,
    Guid? ProductUnitId,
    Guid? InventoryLotId,
    int LineNumber,
    decimal QuantityEntered,
    decimal MultiplierToBase,
    decimal BaseQuantity,
    string NameSnapshot,
    string UnitLabelSnapshot,
    decimal? UnitCostSnapshot,
    decimal? LineCostSnapshot,
    Guid? InventoryMovementId);

public sealed record WasteLossDto(
    Guid WasteLossId,
    Guid OrganizationId,
    Guid? BranchId,
    string WasteLossNumber,
    string? ReferenceNumber,
    DateTimeOffset OccurredAtUtc,
    string Reason,
    string? Notes,
    string Status,
    string CostStatus,
    decimal? TotalCostSnapshot,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    Guid? VoidedByUserId,
    DateTimeOffset? VoidedAtUtc,
    IReadOnlyList<WasteLossLineDto> Lines);

public sealed record WasteLossListItemDto(
    Guid WasteLossId,
    string WasteLossNumber,
    Guid? BranchId,
    string? ReferenceNumber,
    DateTimeOffset OccurredAtUtc,
    string Reason,
    string Status,
    string CostStatus,
    int LineCount,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateWasteLossLineRequest(
    Guid ProductId,
    decimal Quantity,
    Guid? ProductUnitId = null,
    Guid? InventoryLotId = null);

public sealed record CreateWasteLossRequest(
    string Reason,
    IReadOnlyList<CreateWasteLossLineRequest> Lines,
    Guid? BranchId = null,
    string? ReferenceNumber = null,
    string? Notes = null,
    DateTimeOffset? OccurredAtUtc = null,
    Guid? WasteLossId = null,
    string? IdempotencyKey = null);

public sealed record WasteLossFilter(
    DateTimeOffset? FromOccurredAtUtc = null,
    DateTimeOffset? ToOccurredAtUtc = null,
    string? Reason = null,
    string? Status = null,
    Guid? BranchId = null,
    string? ReferenceNumber = null);

public static class WasteLossMapper
{
    public static WasteLossDto Map(WasteLoss wasteLoss) =>
        new(
            wasteLoss.Id.Value,
            wasteLoss.OrganizationId.Value,
            wasteLoss.BranchId?.Value,
            wasteLoss.WasteLossNumber,
            wasteLoss.ReferenceNumber,
            wasteLoss.OccurredAtUtc,
            WasteLossReasons.ToCode(wasteLoss.Reason),
            wasteLoss.Notes,
            WasteLossStatuses.ToCode(wasteLoss.Status),
            ProductionCostStatuses.ToCode(wasteLoss.CostStatus),
            wasteLoss.TotalCostSnapshot,
            wasteLoss.CreatedByUserId,
            wasteLoss.CreatedAtUtc,
            wasteLoss.VoidedByUserId,
            wasteLoss.VoidedAtUtc,
            wasteLoss.Lines.Select(MapLine).ToList());

    public static WasteLossListItemDto MapListItem(WasteLoss wasteLoss) =>
        new(
            wasteLoss.Id.Value,
            wasteLoss.WasteLossNumber,
            wasteLoss.BranchId?.Value,
            wasteLoss.ReferenceNumber,
            wasteLoss.OccurredAtUtc,
            WasteLossReasons.ToCode(wasteLoss.Reason),
            WasteLossStatuses.ToCode(wasteLoss.Status),
            ProductionCostStatuses.ToCode(wasteLoss.CostStatus),
            wasteLoss.Lines.Count,
            wasteLoss.CreatedAtUtc);

    public static WasteLossLineDto MapLine(WasteLossLine line) =>
        new(
            line.Id.Value,
            line.ProductId.Value,
            line.ProductUnitId?.Value,
            line.InventoryLotId?.Value,
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
