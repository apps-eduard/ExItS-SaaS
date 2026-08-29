using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Immutable posted production run. Materials decrease and output increases atomically on create.
/// Correction is void-only (compensating restoration / output reversal movements).
/// PRODUCTION_COST_SCOPE=MATERIAL_ONLY — never SellingPrice.
/// </summary>
public sealed class ProductionRun
{
    public const int ReferenceNumberMaxLength = 128;
    public const int NotesMaxLength = 512;
    public const int IdempotencyKeyMaxLength = 128;
    public const int DefinitionNameMaxLength = ProductionDefinition.NameMaxLength;

    private readonly List<ProductionRunMaterial> _materials;

    public ProductionRunId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId? BranchId { get; }
    public string ProductionNumber { get; }
    public string? ReferenceNumber { get; }
    public ProductionDefinitionId ProductionDefinitionId { get; }
    public int ProductionDefinitionRevision { get; }
    public string ProductionDefinitionNameSnapshot { get; }
    public CatalogProductId OutputProductId { get; }
    public ProductUnitId? OutputProductUnitId { get; }
    public decimal OutputQuantityEntered { get; }
    public decimal OutputMultiplierToBase { get; }
    public decimal OutputBaseQuantity { get; }
    public string OutputNameSnapshot { get; }
    public string OutputUnitLabelSnapshot { get; }
    public DateTimeOffset ProducedAtUtc { get; }
    public DateOnly? OutputExpirationDate { get; }
    public string? OutputLotNumber { get; }
    public ProductionRunStatus Status { get; private set; }
    public ProductionCostStatus CostStatus { get; }
    public decimal? TotalMaterialCost { get; }
    public decimal? OutputBaseUnitCost { get; }
    public string? Notes { get; }
    public Guid CreatedByUserId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid? VoidedByUserId { get; private set; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public string? IdempotencyKey { get; }
    public Guid? OutputInventoryMovementId { get; private set; }

    public IReadOnlyList<ProductionRunMaterial> Materials => _materials;

    private ProductionRun(
        ProductionRunId id,
        PosOrganizationId organizationId,
        PosBranchId? branchId,
        string productionNumber,
        string? referenceNumber,
        ProductionDefinitionId productionDefinitionId,
        int productionDefinitionRevision,
        string productionDefinitionNameSnapshot,
        CatalogProductId outputProductId,
        ProductUnitId? outputProductUnitId,
        decimal outputQuantityEntered,
        decimal outputMultiplierToBase,
        decimal outputBaseQuantity,
        string outputNameSnapshot,
        string outputUnitLabelSnapshot,
        DateTimeOffset producedAtUtc,
        DateOnly? outputExpirationDate,
        string? outputLotNumber,
        ProductionRunStatus status,
        ProductionCostStatus costStatus,
        decimal? totalMaterialCost,
        decimal? outputBaseUnitCost,
        string? notes,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        Guid? voidedByUserId,
        DateTimeOffset? voidedAtUtc,
        string? idempotencyKey,
        Guid? outputInventoryMovementId,
        List<ProductionRunMaterial> materials)
    {
        Id = id;
        OrganizationId = organizationId;
        BranchId = branchId;
        ProductionNumber = productionNumber;
        ReferenceNumber = referenceNumber;
        ProductionDefinitionId = productionDefinitionId;
        ProductionDefinitionRevision = productionDefinitionRevision;
        ProductionDefinitionNameSnapshot = productionDefinitionNameSnapshot;
        OutputProductId = outputProductId;
        OutputProductUnitId = outputProductUnitId;
        OutputQuantityEntered = outputQuantityEntered;
        OutputMultiplierToBase = outputMultiplierToBase;
        OutputBaseQuantity = outputBaseQuantity;
        OutputNameSnapshot = outputNameSnapshot;
        OutputUnitLabelSnapshot = outputUnitLabelSnapshot;
        ProducedAtUtc = producedAtUtc;
        OutputExpirationDate = outputExpirationDate;
        OutputLotNumber = outputLotNumber;
        Status = status;
        CostStatus = costStatus;
        TotalMaterialCost = totalMaterialCost;
        OutputBaseUnitCost = outputBaseUnitCost;
        Notes = notes;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        VoidedByUserId = voidedByUserId;
        VoidedAtUtc = voidedAtUtc;
        IdempotencyKey = idempotencyKey;
        OutputInventoryMovementId = outputInventoryMovementId;
        _materials = materials;
    }

    public static ProductionRun Create(
        PosOrganizationId organizationId,
        string productionNumber,
        ProductionDefinitionId productionDefinitionId,
        int productionDefinitionRevision,
        string productionDefinitionNameSnapshot,
        CatalogProductId outputProductId,
        decimal outputQuantityEntered,
        decimal outputMultiplierToBase,
        string outputNameSnapshot,
        string outputUnitLabelSnapshot,
        IReadOnlyList<ProductionRunMaterialDraft> materials,
        Guid createdByUserId,
        DateTimeOffset utcNow,
        DateTimeOffset? producedAtUtc = null,
        PosBranchId? branchId = null,
        ProductUnitId? outputProductUnitId = null,
        DateOnly? outputExpirationDate = null,
        string? outputLotNumber = null,
        string? referenceNumber = null,
        string? notes = null,
        string? idempotencyKey = null,
        ProductionRunId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(createdByUserId);

        var produced = producedAtUtc ?? utcNow;
        SaleMoney.EnsureUtc(produced);

        if (materials is null || materials.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.ProductionRequiresComponents,
                "At least one production material line is required.");
        }

        if (outputQuantityEntered <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionQuantity,
                "Production output quantity must be greater than zero.");
        }

        ProductUnitConversion.EnsureValidMultiplier(outputMultiplierToBase);
        if (!SaleMoney.HasAtMostDecimals(outputQuantityEntered, SaleMoney.MeasuredQuantityDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionQuantity,
                $"Production output quantity must have at most {SaleMoney.MeasuredQuantityDecimals} decimal places.");
        }

        var outputBase = ProductUnitConversion.ToBaseQuantity(outputQuantityEntered, outputMultiplierToBase);
        if (!SaleMoney.HasAtMostDecimals(outputBase, SaleMoney.MeasuredQuantityDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionQuantity,
                $"Converted output base quantity must have at most {SaleMoney.MeasuredQuantityDecimals} decimal places.");
        }

        if (productionDefinitionRevision < 1)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionDefinitionRevision,
                "Production definition revision must be at least 1.");
        }

        var runId = id ?? ProductionRunId.New();
        var built = new List<ProductionRunMaterial>(materials.Count);
        var lineNumber = 1;
        foreach (var draft in materials)
        {
            built.Add(ProductionRunMaterial.Create(runId, organizationId, lineNumber++, draft));
        }

        var costStatus = ProductionCostStatuses.FromMaterialCosts(built.Select(m => m.UnitCostSnapshot).ToList());
        decimal? totalMaterialCost = null;
        decimal? outputUnitCost = null;
        if (costStatus == ProductionCostStatus.Complete)
        {
            totalMaterialCost = SaleMoney.RoundMoney(built.Sum(m => m.LineCostSnapshot!.Value));
            outputUnitCost = SaleMoney.RoundMoney(totalMaterialCost.Value / outputBase);
        }
        else if (costStatus == ProductionCostStatus.Partial)
        {
            var known = built.Where(m => m.LineCostSnapshot is not null).Sum(m => m.LineCostSnapshot!.Value);
            totalMaterialCost = SaleMoney.RoundMoney(known);
        }

        var (_, normalizedLot) = InventoryLot.NormalizeLotNumber(outputLotNumber);

        return new ProductionRun(
            runId,
            organizationId,
            branchId,
            ProductionNumbers.Normalize(productionNumber),
            NormalizeOptional(referenceNumber, ReferenceNumberMaxLength, DomainErrorCodes.InvalidProductionReference, "Reference number"),
            productionDefinitionId,
            productionDefinitionRevision,
            NormalizeDefinitionName(productionDefinitionNameSnapshot),
            outputProductId,
            outputProductUnitId,
            outputQuantityEntered,
            outputMultiplierToBase,
            outputBase,
            NormalizeName(outputNameSnapshot),
            NormalizeUnitLabel(outputUnitLabelSnapshot),
            produced,
            outputExpirationDate,
            normalizedLot,
            ProductionRunStatus.Posted,
            costStatus,
            totalMaterialCost,
            outputUnitCost,
            NormalizeOptional(notes, NotesMaxLength, DomainErrorCodes.InvalidProductionNotes, "Notes"),
            createdByUserId,
            utcNow,
            voidedByUserId: null,
            voidedAtUtc: null,
            NormalizeIdempotencyKey(idempotencyKey),
            outputInventoryMovementId: null,
            built);
    }

    public void AttachOutputInventoryMovement(StockMovementId movementId)
    {
        if (OutputInventoryMovementId is not null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionRun,
                "Output inventory movement is already linked to this production run.");
        }

        OutputInventoryMovementId = movementId.Value;
    }

    /// <summary>
    /// Marks the document voided. Inventory restoration / output reversal is applied by the use case.
    /// </summary>
    public void Void(DateTimeOffset utcNow, Guid actorId)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);

        if (Status == ProductionRunStatus.Voided)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionRunStatusTransition,
                "Production run is already voided.");
        }

        if (Status != ProductionRunStatus.Posted)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionRunStatusTransition,
                "Only a posted production run can be voided.");
        }

        Status = ProductionRunStatus.Voided;
        VoidedAtUtc = utcNow;
        VoidedByUserId = actorId;
    }

    public static ProductionRun Rehydrate(
        ProductionRunId id,
        PosOrganizationId organizationId,
        PosBranchId? branchId,
        string productionNumber,
        string? referenceNumber,
        ProductionDefinitionId productionDefinitionId,
        int productionDefinitionRevision,
        string productionDefinitionNameSnapshot,
        CatalogProductId outputProductId,
        ProductUnitId? outputProductUnitId,
        decimal outputQuantityEntered,
        decimal outputMultiplierToBase,
        decimal outputBaseQuantity,
        string outputNameSnapshot,
        string outputUnitLabelSnapshot,
        DateTimeOffset producedAtUtc,
        DateOnly? outputExpirationDate,
        string? outputLotNumber,
        ProductionRunStatus status,
        ProductionCostStatus costStatus,
        decimal? totalMaterialCost,
        decimal? outputBaseUnitCost,
        string? notes,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        Guid? voidedByUserId,
        DateTimeOffset? voidedAtUtc,
        string? idempotencyKey,
        Guid? outputInventoryMovementId,
        IReadOnlyList<ProductionRunMaterial> materials) =>
        new(
            id,
            organizationId,
            branchId,
            productionNumber,
            referenceNumber,
            productionDefinitionId,
            productionDefinitionRevision,
            productionDefinitionNameSnapshot,
            outputProductId,
            outputProductUnitId,
            outputQuantityEntered,
            outputMultiplierToBase,
            outputBaseQuantity,
            outputNameSnapshot,
            outputUnitLabelSnapshot,
            producedAtUtc,
            outputExpirationDate,
            outputLotNumber,
            status,
            costStatus,
            totalMaterialCost,
            outputBaseUnitCost,
            notes,
            createdByUserId,
            createdAtUtc,
            voidedByUserId,
            voidedAtUtc,
            idempotencyKey,
            outputInventoryMovementId,
            materials.ToList());

    private static string NormalizeDefinitionName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionDefinitionName,
                "Production definition name snapshot is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > DefinitionNameMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionDefinitionName,
                $"Production definition name snapshot must be at most {DefinitionNameMaxLength} characters.");
        }

        return trimmed;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionRun,
                "Output product name snapshot is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > PurchaseOrderLine.NameSnapshotMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionRun,
                $"Output product name snapshot must be at most {PurchaseOrderLine.NameSnapshotMaxLength} characters.");
        }

        return trimmed;
    }

    private static string NormalizeUnitLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionRun,
                "Output unit label snapshot is required.");
        }

        var trimmed = label.Trim();
        if (trimmed.Length > ProductionRunMaterial.UnitLabelMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionRun,
                $"Output unit label snapshot must be at most {ProductionRunMaterial.UnitLabelMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string errorCode, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"{label} must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var trimmed = key.Trim();
        if (trimmed.Length > IdempotencyKeyMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionIdempotencyKey,
                $"Idempotency key must be at most {IdempotencyKeyMaxLength} characters.");
        }

        return trimmed;
    }
}
