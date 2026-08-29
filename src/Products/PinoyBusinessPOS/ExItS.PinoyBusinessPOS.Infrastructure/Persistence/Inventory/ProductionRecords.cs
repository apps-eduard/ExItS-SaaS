namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

internal sealed class ProductionDefinitionRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OutputProductId { get; set; }
    public Guid? OutputProductUnitId { get; set; }
    public decimal OutputQuantityEntered { get; set; }
    public decimal OutputMultiplierToBase { get; set; }
    public decimal OutputBaseQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Revision { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}

internal sealed class ProductionComponentRecord
{
    public Guid Id { get; set; }
    public Guid ProductionDefinitionId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MaterialProductId { get; set; }
    public Guid? ProductUnitId { get; set; }
    public int SortOrder { get; set; }
    public decimal QuantityEntered { get; set; }
    public decimal MultiplierToBase { get; set; }
    public decimal BaseQuantity { get; set; }
}

internal sealed class ProductionRunRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public string ProductionNumber { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public Guid ProductionDefinitionId { get; set; }
    public int ProductionDefinitionRevision { get; set; }
    public string ProductionDefinitionNameSnapshot { get; set; } = string.Empty;
    public Guid OutputProductId { get; set; }
    public Guid? OutputProductUnitId { get; set; }
    public decimal OutputQuantityEntered { get; set; }
    public decimal OutputMultiplierToBase { get; set; }
    public decimal OutputBaseQuantity { get; set; }
    public string OutputNameSnapshot { get; set; } = string.Empty;
    public string OutputUnitLabelSnapshot { get; set; } = string.Empty;
    public DateTimeOffset ProducedAtUtc { get; set; }
    public DateOnly? OutputExpirationDate { get; set; }
    public string? OutputLotNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CostStatus { get; set; } = string.Empty;
    public decimal? TotalMaterialCost { get; set; }
    public decimal? OutputBaseUnitCost { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? VoidedByUserId { get; set; }
    public DateTimeOffset? VoidedAtUtc { get; set; }
    public string? IdempotencyKey { get; set; }
    public Guid? OutputInventoryMovementId { get; set; }
}

internal sealed class ProductionRunMaterialRecord
{
    public Guid Id { get; set; }
    public Guid ProductionRunId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MaterialProductId { get; set; }
    public Guid? ProductUnitId { get; set; }
    public int LineNumber { get; set; }
    public decimal ExpectedQuantityEntered { get; set; }
    public decimal ActualQuantityEntered { get; set; }
    public decimal MultiplierToBase { get; set; }
    public decimal ExpectedBaseQuantity { get; set; }
    public decimal ActualBaseQuantity { get; set; }
    public string NameSnapshot { get; set; } = string.Empty;
    public string UnitLabelSnapshot { get; set; } = string.Empty;
    public decimal? UnitCostSnapshot { get; set; }
    public decimal? LineCostSnapshot { get; set; }
    public Guid? InventoryMovementId { get; set; }
}

internal sealed class ProductionRunNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}
