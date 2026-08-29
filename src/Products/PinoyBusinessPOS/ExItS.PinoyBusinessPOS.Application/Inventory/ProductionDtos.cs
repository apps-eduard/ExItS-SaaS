using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed record ProductionComponentDto(
    Guid ComponentId,
    Guid MaterialProductId,
    Guid? ProductUnitId,
    int SortOrder,
    decimal QuantityEntered,
    decimal MultiplierToBase,
    decimal BaseQuantity);

public sealed record ProductionDefinitionDto(
    Guid ProductionDefinitionId,
    Guid OrganizationId,
    string Name,
    Guid OutputProductId,
    Guid? OutputProductUnitId,
    decimal OutputQuantityEntered,
    decimal OutputMultiplierToBase,
    decimal OutputBaseQuantity,
    string Status,
    bool IsActive,
    int Revision,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    Guid? UpdatedByUserId,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyList<ProductionComponentDto> Components);

public sealed record ProductionDefinitionListItemDto(
    Guid ProductionDefinitionId,
    string Name,
    Guid OutputProductId,
    decimal OutputQuantityEntered,
    decimal OutputBaseQuantity,
    string Status,
    bool IsActive,
    int ComponentCount,
    int Revision,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateProductionComponentRequest(
    Guid MaterialProductId,
    decimal Quantity,
    Guid? ProductUnitId = null,
    int? SortOrder = null);

public sealed record CreateProductionDefinitionRequest(
    string Name,
    Guid OutputProductId,
    decimal OutputQuantity,
    IReadOnlyList<CreateProductionComponentRequest> Components,
    Guid? OutputProductUnitId = null,
    Guid? ProductionDefinitionId = null);

public sealed record UpdateProductionDefinitionRequest(
    string Name,
    Guid OutputProductId,
    decimal OutputQuantity,
    IReadOnlyList<CreateProductionComponentRequest> Components,
    Guid? OutputProductUnitId = null);

public sealed record SetProductionDefinitionActiveRequest(bool IsActive);

public sealed record ProductionRunMaterialDto(
    Guid MaterialId,
    Guid MaterialProductId,
    Guid? ProductUnitId,
    int LineNumber,
    decimal ExpectedQuantityEntered,
    decimal ActualQuantityEntered,
    decimal MultiplierToBase,
    decimal ExpectedBaseQuantity,
    decimal ActualBaseQuantity,
    string NameSnapshot,
    string UnitLabelSnapshot,
    decimal? UnitCostSnapshot,
    decimal? LineCostSnapshot,
    Guid? InventoryMovementId);

public sealed record ProductionRunDto(
    Guid ProductionRunId,
    Guid OrganizationId,
    Guid? BranchId,
    string ProductionNumber,
    string? ReferenceNumber,
    Guid ProductionDefinitionId,
    int ProductionDefinitionRevision,
    string ProductionDefinitionNameSnapshot,
    Guid OutputProductId,
    Guid? OutputProductUnitId,
    decimal OutputQuantityEntered,
    decimal OutputMultiplierToBase,
    decimal OutputBaseQuantity,
    string OutputNameSnapshot,
    string OutputUnitLabelSnapshot,
    DateTimeOffset ProducedAtUtc,
    DateOnly? OutputExpirationDate,
    string? OutputLotNumber,
    string Status,
    string CostStatus,
    decimal? TotalMaterialCost,
    decimal? OutputBaseUnitCost,
    string? Notes,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    Guid? VoidedByUserId,
    DateTimeOffset? VoidedAtUtc,
    Guid? OutputInventoryMovementId,
    IReadOnlyList<ProductionRunMaterialDto> Materials);

public sealed record ProductionRunListItemDto(
    Guid ProductionRunId,
    string ProductionNumber,
    Guid? BranchId,
    Guid OutputProductId,
    string OutputNameSnapshot,
    decimal OutputBaseQuantity,
    string Status,
    string CostStatus,
    decimal? TotalMaterialCost,
    decimal? OutputBaseUnitCost,
    DateTimeOffset ProducedAtUtc,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateProductionRunMaterialOverrideRequest(
    Guid MaterialProductId,
    decimal ActualQuantity,
    Guid? ProductUnitId = null);

public sealed record CreateProductionRunRequest(
    Guid ProductionDefinitionId,
    decimal OutputQuantity,
    Guid? OutputProductUnitId = null,
    Guid? BranchId = null,
    string? ReferenceNumber = null,
    string? Notes = null,
    DateTimeOffset? ProducedAtUtc = null,
    DateOnly? OutputExpirationDate = null,
    string? OutputLotNumber = null,
    IReadOnlyList<CreateProductionRunMaterialOverrideRequest>? MaterialOverrides = null,
    Guid? ProductionRunId = null,
    string? IdempotencyKey = null);

public static class ProductionMapper
{
    public static ProductionDefinitionDto Map(ProductionDefinition definition) =>
        new(
            definition.Id.Value,
            definition.OrganizationId.Value,
            definition.Name,
            definition.OutputProductId.Value,
            definition.OutputProductUnitId?.Value,
            definition.OutputQuantityEntered,
            definition.OutputMultiplierToBase,
            definition.OutputBaseQuantity,
            ProductionDefinitionStatuses.ToCode(definition.Status),
            definition.IsActive,
            definition.Revision,
            definition.CreatedByUserId,
            definition.CreatedAtUtc,
            definition.UpdatedByUserId,
            definition.UpdatedAtUtc,
            definition.Components.Select(MapComponent).ToList());

    public static ProductionDefinitionListItemDto MapListItem(ProductionDefinition definition) =>
        new(
            definition.Id.Value,
            definition.Name,
            definition.OutputProductId.Value,
            definition.OutputQuantityEntered,
            definition.OutputBaseQuantity,
            ProductionDefinitionStatuses.ToCode(definition.Status),
            definition.IsActive,
            definition.Components.Count,
            definition.Revision,
            definition.CreatedAtUtc);

    public static ProductionComponentDto MapComponent(ProductionComponent component) =>
        new(
            component.Id.Value,
            component.MaterialProductId.Value,
            component.ProductUnitId?.Value,
            component.SortOrder,
            component.QuantityEntered,
            component.MultiplierToBase,
            component.BaseQuantity);

    public static ProductionRunDto Map(ProductionRun run) =>
        new(
            run.Id.Value,
            run.OrganizationId.Value,
            run.BranchId?.Value,
            run.ProductionNumber,
            run.ReferenceNumber,
            run.ProductionDefinitionId.Value,
            run.ProductionDefinitionRevision,
            run.ProductionDefinitionNameSnapshot,
            run.OutputProductId.Value,
            run.OutputProductUnitId?.Value,
            run.OutputQuantityEntered,
            run.OutputMultiplierToBase,
            run.OutputBaseQuantity,
            run.OutputNameSnapshot,
            run.OutputUnitLabelSnapshot,
            run.ProducedAtUtc,
            run.OutputExpirationDate,
            run.OutputLotNumber,
            ProductionRunStatuses.ToCode(run.Status),
            ProductionCostStatuses.ToCode(run.CostStatus),
            run.TotalMaterialCost,
            run.OutputBaseUnitCost,
            run.Notes,
            run.CreatedByUserId,
            run.CreatedAtUtc,
            run.VoidedByUserId,
            run.VoidedAtUtc,
            run.OutputInventoryMovementId,
            run.Materials.Select(MapMaterial).ToList());

    public static ProductionRunListItemDto MapListItem(ProductionRun run) =>
        new(
            run.Id.Value,
            run.ProductionNumber,
            run.BranchId?.Value,
            run.OutputProductId.Value,
            run.OutputNameSnapshot,
            run.OutputBaseQuantity,
            ProductionRunStatuses.ToCode(run.Status),
            ProductionCostStatuses.ToCode(run.CostStatus),
            run.TotalMaterialCost,
            run.OutputBaseUnitCost,
            run.ProducedAtUtc,
            run.CreatedAtUtc);

    public static ProductionRunMaterialDto MapMaterial(ProductionRunMaterial material) =>
        new(
            material.Id.Value,
            material.MaterialProductId.Value,
            material.ProductUnitId?.Value,
            material.LineNumber,
            material.ExpectedQuantityEntered,
            material.ActualQuantityEntered,
            material.MultiplierToBase,
            material.ExpectedBaseQuantity,
            material.ActualBaseQuantity,
            material.NameSnapshot,
            material.UnitLabelSnapshot,
            material.UnitCostSnapshot,
            material.LineCostSnapshot,
            material.InventoryMovementId);
}
