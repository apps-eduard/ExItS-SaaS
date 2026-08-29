using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Durable production setup / recipe. Editing increments <see cref="Revision"/>;
/// historical production runs snapshot revision + expected quantities.
/// </summary>
public sealed class ProductionDefinition
{
    public const int NameMaxLength = 200;

    private readonly List<ProductionComponent> _components;

    public ProductionDefinitionId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string Name { get; private set; }
    public CatalogProductId OutputProductId { get; private set; }
    public ProductUnitId? OutputProductUnitId { get; private set; }
    public decimal OutputQuantityEntered { get; private set; }
    public decimal OutputMultiplierToBase { get; private set; }
    public decimal OutputBaseQuantity { get; private set; }
    public ProductionDefinitionStatus Status { get; private set; }
    public int Revision { get; private set; }
    public Guid CreatedByUserId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid? UpdatedByUserId { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public IReadOnlyList<ProductionComponent> Components => _components;

    public bool IsActive => Status == ProductionDefinitionStatus.Active;

    private ProductionDefinition(
        ProductionDefinitionId id,
        PosOrganizationId organizationId,
        string name,
        CatalogProductId outputProductId,
        ProductUnitId? outputProductUnitId,
        decimal outputQuantityEntered,
        decimal outputMultiplierToBase,
        decimal outputBaseQuantity,
        ProductionDefinitionStatus status,
        int revision,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        Guid? updatedByUserId,
        DateTimeOffset? updatedAtUtc,
        List<ProductionComponent> components)
    {
        Id = id;
        OrganizationId = organizationId;
        Name = name;
        OutputProductId = outputProductId;
        OutputProductUnitId = outputProductUnitId;
        OutputQuantityEntered = outputQuantityEntered;
        OutputMultiplierToBase = outputMultiplierToBase;
        OutputBaseQuantity = outputBaseQuantity;
        Status = status;
        Revision = revision;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = updatedAtUtc;
        _components = components;
    }

    public static ProductionDefinition Create(
        PosOrganizationId organizationId,
        string name,
        CatalogProductId outputProductId,
        decimal outputQuantityEntered,
        decimal outputMultiplierToBase,
        IReadOnlyList<ProductionComponentDraft> components,
        Guid createdByUserId,
        DateTimeOffset utcNow,
        ProductUnitId? outputProductUnitId = null,
        ProductionDefinitionId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(createdByUserId);

        var definitionId = id ?? ProductionDefinitionId.New();
        var (qty, multiplier, baseQty) = NormalizeOutput(outputQuantityEntered, outputMultiplierToBase);
        var built = BuildComponents(definitionId, organizationId, outputProductId, components);

        return new ProductionDefinition(
            definitionId,
            organizationId,
            NormalizeName(name),
            outputProductId,
            outputProductUnitId,
            qty,
            multiplier,
            baseQty,
            ProductionDefinitionStatus.Active,
            revision: 1,
            createdByUserId,
            utcNow,
            updatedByUserId: null,
            updatedAtUtc: null,
            built);
    }

    public void Update(
        string name,
        CatalogProductId outputProductId,
        decimal outputQuantityEntered,
        decimal outputMultiplierToBase,
        IReadOnlyList<ProductionComponentDraft> components,
        Guid actorId,
        DateTimeOffset utcNow,
        ProductUnitId? outputProductUnitId = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);

        var (qty, multiplier, baseQty) = NormalizeOutput(outputQuantityEntered, outputMultiplierToBase);
        var built = BuildComponents(Id, OrganizationId, outputProductId, components);

        Name = NormalizeName(name);
        OutputProductId = outputProductId;
        OutputProductUnitId = outputProductUnitId;
        OutputQuantityEntered = qty;
        OutputMultiplierToBase = multiplier;
        OutputBaseQuantity = baseQty;
        _components.Clear();
        _components.AddRange(built);
        Revision += 1;
        UpdatedByUserId = actorId;
        UpdatedAtUtc = utcNow;
    }

    public void SetActive(bool isActive, Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);
        Status = isActive ? ProductionDefinitionStatus.Active : ProductionDefinitionStatus.Inactive;
        UpdatedByUserId = actorId;
        UpdatedAtUtc = utcNow;
    }

    public static ProductionDefinition Rehydrate(
        ProductionDefinitionId id,
        PosOrganizationId organizationId,
        string name,
        CatalogProductId outputProductId,
        ProductUnitId? outputProductUnitId,
        decimal outputQuantityEntered,
        decimal outputMultiplierToBase,
        decimal outputBaseQuantity,
        ProductionDefinitionStatus status,
        int revision,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        Guid? updatedByUserId,
        DateTimeOffset? updatedAtUtc,
        IReadOnlyList<ProductionComponent> components) =>
        new(
            id,
            organizationId,
            name,
            outputProductId,
            outputProductUnitId,
            outputQuantityEntered,
            outputMultiplierToBase,
            outputBaseQuantity,
            status,
            revision,
            createdByUserId,
            createdAtUtc,
            updatedByUserId,
            updatedAtUtc,
            components.ToList());

    private static List<ProductionComponent> BuildComponents(
        ProductionDefinitionId definitionId,
        PosOrganizationId organizationId,
        CatalogProductId outputProductId,
        IReadOnlyList<ProductionComponentDraft> components)
    {
        if (components is null || components.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.ProductionRequiresComponents,
                "At least one production component is required.");
        }

        var seen = new HashSet<Guid>();
        var built = new List<ProductionComponent>(components.Count);
        var sort = 1;
        foreach (var draft in components)
        {
            if (draft.MaterialProductId == outputProductId)
            {
                throw new DomainException(
                    DomainErrorCodes.ProductionSelfComponentForbidden,
                    "A production definition cannot use its output product as a component.");
            }

            if (!seen.Add(draft.MaterialProductId.Value))
            {
                throw new DomainException(
                    DomainErrorCodes.ProductionDuplicateComponent,
                    "Each material product may appear only once on a production definition.");
            }

            built.Add(ProductionComponent.Create(definitionId, organizationId, sort++, draft));
        }

        return built;
    }

    private static (decimal Qty, decimal Multiplier, decimal BaseQty) NormalizeOutput(
        decimal quantityEntered,
        decimal multiplierToBase)
    {
        if (quantityEntered <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionQuantity,
                "Production output quantity must be greater than zero.");
        }

        ProductUnitConversion.EnsureValidMultiplier(multiplierToBase);
        if (!SaleMoney.HasAtMostDecimals(quantityEntered, SaleMoney.MeasuredQuantityDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionQuantity,
                $"Production output quantity must have at most {SaleMoney.MeasuredQuantityDecimals} decimal places.");
        }

        var baseQuantity = ProductUnitConversion.ToBaseQuantity(quantityEntered, multiplierToBase);
        if (!SaleMoney.HasAtMostDecimals(baseQuantity, SaleMoney.MeasuredQuantityDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionQuantity,
                $"Converted output base quantity must have at most {SaleMoney.MeasuredQuantityDecimals} decimal places.");
        }

        return (quantityEntered, multiplierToBase, baseQuantity);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionDefinitionName,
                "Production definition name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionDefinitionName,
                $"Production definition name must be at most {NameMaxLength} characters.");
        }

        return trimmed;
    }
}
