using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class ProductionEntityMapper
{
    public static ProductionDefinition ToDomain(
        ProductionDefinitionRecord record,
        IReadOnlyList<ProductionComponentRecord> components) =>
        ProductionDefinition.Rehydrate(
            ProductionDefinitionId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.Name,
            CatalogProductId.From(record.OutputProductId),
            record.OutputProductUnitId is null ? null : ProductUnitId.From(record.OutputProductUnitId.Value),
            record.OutputQuantityEntered,
            record.OutputMultiplierToBase,
            record.OutputBaseQuantity,
            ProductionDefinitionStatuses.Parse(record.Status),
            record.Revision,
            record.CreatedByUserId,
            record.CreatedAtUtc,
            record.UpdatedByUserId,
            record.UpdatedAtUtc,
            components.OrderBy(c => c.SortOrder).Select(ToDomain).ToList());

    public static ProductionComponent ToDomain(ProductionComponentRecord record) =>
        ProductionComponent.Rehydrate(
            ProductionComponentId.From(record.Id),
            ProductionDefinitionId.From(record.ProductionDefinitionId),
            PosOrganizationId.From(record.OrganizationId),
            CatalogProductId.From(record.MaterialProductId),
            record.ProductUnitId is null ? null : ProductUnitId.From(record.ProductUnitId.Value),
            record.SortOrder,
            record.QuantityEntered,
            record.MultiplierToBase,
            record.BaseQuantity);

    public static ProductionDefinitionRecord ToRecord(ProductionDefinition definition) =>
        new()
        {
            Id = definition.Id.Value,
            OrganizationId = definition.OrganizationId.Value,
            Name = definition.Name,
            OutputProductId = definition.OutputProductId.Value,
            OutputProductUnitId = definition.OutputProductUnitId?.Value,
            OutputQuantityEntered = definition.OutputQuantityEntered,
            OutputMultiplierToBase = definition.OutputMultiplierToBase,
            OutputBaseQuantity = definition.OutputBaseQuantity,
            Status = ProductionDefinitionStatuses.ToCode(definition.Status),
            Revision = definition.Revision,
            CreatedByUserId = definition.CreatedByUserId,
            CreatedAtUtc = definition.CreatedAtUtc,
            UpdatedByUserId = definition.UpdatedByUserId,
            UpdatedAtUtc = definition.UpdatedAtUtc
        };

    public static ProductionComponentRecord ToRecord(ProductionComponent component) =>
        new()
        {
            Id = component.Id.Value,
            ProductionDefinitionId = component.ProductionDefinitionId.Value,
            OrganizationId = component.OrganizationId.Value,
            MaterialProductId = component.MaterialProductId.Value,
            ProductUnitId = component.ProductUnitId?.Value,
            SortOrder = component.SortOrder,
            QuantityEntered = component.QuantityEntered,
            MultiplierToBase = component.MultiplierToBase,
            BaseQuantity = component.BaseQuantity
        };

    public static void Apply(ProductionDefinition definition, ProductionDefinitionRecord record)
    {
        record.Name = definition.Name;
        record.OutputProductId = definition.OutputProductId.Value;
        record.OutputProductUnitId = definition.OutputProductUnitId?.Value;
        record.OutputQuantityEntered = definition.OutputQuantityEntered;
        record.OutputMultiplierToBase = definition.OutputMultiplierToBase;
        record.OutputBaseQuantity = definition.OutputBaseQuantity;
        record.Status = ProductionDefinitionStatuses.ToCode(definition.Status);
        record.Revision = definition.Revision;
        record.UpdatedByUserId = definition.UpdatedByUserId;
        record.UpdatedAtUtc = definition.UpdatedAtUtc;
    }

    public static ProductionRun ToDomain(
        ProductionRunRecord record,
        IReadOnlyList<ProductionRunMaterialRecord> materials) =>
        ProductionRun.Rehydrate(
            ProductionRunId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.BranchId is null ? null : PosBranchId.From(record.BranchId.Value),
            record.ProductionNumber,
            record.ReferenceNumber,
            ProductionDefinitionId.From(record.ProductionDefinitionId),
            record.ProductionDefinitionRevision,
            record.ProductionDefinitionNameSnapshot,
            CatalogProductId.From(record.OutputProductId),
            record.OutputProductUnitId is null ? null : ProductUnitId.From(record.OutputProductUnitId.Value),
            record.OutputQuantityEntered,
            record.OutputMultiplierToBase,
            record.OutputBaseQuantity,
            record.OutputNameSnapshot,
            record.OutputUnitLabelSnapshot,
            record.ProducedAtUtc,
            record.OutputExpirationDate,
            record.OutputLotNumber,
            ProductionRunStatuses.Parse(record.Status),
            ProductionCostStatuses.Parse(record.CostStatus),
            record.TotalMaterialCost,
            record.OutputBaseUnitCost,
            record.Notes,
            record.CreatedByUserId,
            record.CreatedAtUtc,
            record.VoidedByUserId,
            record.VoidedAtUtc,
            record.IdempotencyKey,
            record.OutputInventoryMovementId,
            materials.OrderBy(m => m.LineNumber).Select(ToDomain).ToList());

    public static ProductionRunMaterial ToDomain(ProductionRunMaterialRecord record) =>
        ProductionRunMaterial.Rehydrate(
            ProductionRunMaterialId.From(record.Id),
            ProductionRunId.From(record.ProductionRunId),
            PosOrganizationId.From(record.OrganizationId),
            CatalogProductId.From(record.MaterialProductId),
            record.ProductUnitId is null ? null : ProductUnitId.From(record.ProductUnitId.Value),
            record.LineNumber,
            record.ExpectedQuantityEntered,
            record.ActualQuantityEntered,
            record.MultiplierToBase,
            record.ExpectedBaseQuantity,
            record.ActualBaseQuantity,
            record.NameSnapshot,
            record.UnitLabelSnapshot,
            record.UnitCostSnapshot,
            record.LineCostSnapshot,
            record.InventoryMovementId);

    public static ProductionRunRecord ToRecord(ProductionRun run) =>
        new()
        {
            Id = run.Id.Value,
            OrganizationId = run.OrganizationId.Value,
            BranchId = run.BranchId?.Value,
            ProductionNumber = run.ProductionNumber,
            ReferenceNumber = run.ReferenceNumber,
            ProductionDefinitionId = run.ProductionDefinitionId.Value,
            ProductionDefinitionRevision = run.ProductionDefinitionRevision,
            ProductionDefinitionNameSnapshot = run.ProductionDefinitionNameSnapshot,
            OutputProductId = run.OutputProductId.Value,
            OutputProductUnitId = run.OutputProductUnitId?.Value,
            OutputQuantityEntered = run.OutputQuantityEntered,
            OutputMultiplierToBase = run.OutputMultiplierToBase,
            OutputBaseQuantity = run.OutputBaseQuantity,
            OutputNameSnapshot = run.OutputNameSnapshot,
            OutputUnitLabelSnapshot = run.OutputUnitLabelSnapshot,
            ProducedAtUtc = run.ProducedAtUtc,
            OutputExpirationDate = run.OutputExpirationDate,
            OutputLotNumber = run.OutputLotNumber,
            Status = ProductionRunStatuses.ToCode(run.Status),
            CostStatus = ProductionCostStatuses.ToCode(run.CostStatus),
            TotalMaterialCost = run.TotalMaterialCost,
            OutputBaseUnitCost = run.OutputBaseUnitCost,
            Notes = run.Notes,
            CreatedByUserId = run.CreatedByUserId,
            CreatedAtUtc = run.CreatedAtUtc,
            VoidedByUserId = run.VoidedByUserId,
            VoidedAtUtc = run.VoidedAtUtc,
            IdempotencyKey = run.IdempotencyKey,
            OutputInventoryMovementId = run.OutputInventoryMovementId
        };

    public static ProductionRunMaterialRecord ToRecord(ProductionRunMaterial material) =>
        new()
        {
            Id = material.Id.Value,
            ProductionRunId = material.ProductionRunId.Value,
            OrganizationId = material.OrganizationId.Value,
            MaterialProductId = material.MaterialProductId.Value,
            ProductUnitId = material.ProductUnitId?.Value,
            LineNumber = material.LineNumber,
            ExpectedQuantityEntered = material.ExpectedQuantityEntered,
            ActualQuantityEntered = material.ActualQuantityEntered,
            MultiplierToBase = material.MultiplierToBase,
            ExpectedBaseQuantity = material.ExpectedBaseQuantity,
            ActualBaseQuantity = material.ActualBaseQuantity,
            NameSnapshot = material.NameSnapshot,
            UnitLabelSnapshot = material.UnitLabelSnapshot,
            UnitCostSnapshot = material.UnitCostSnapshot,
            LineCostSnapshot = material.LineCostSnapshot,
            InventoryMovementId = material.InventoryMovementId
        };

    public static void Apply(ProductionRun run, ProductionRunRecord record)
    {
        record.Status = ProductionRunStatuses.ToCode(run.Status);
        record.VoidedByUserId = run.VoidedByUserId;
        record.VoidedAtUtc = run.VoidedAtUtc;
        record.OutputInventoryMovementId = run.OutputInventoryMovementId;
    }
}
