using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed class ProductionDefinitionQueryService
{
    private readonly IProductionDefinitionRepository _definitions;

    public ProductionDefinitionQueryService(IProductionDefinitionRepository definitions) =>
        _definitions = definitions;

    public async Task<ProductionDefinitionDto?> GetByIdAsync(
        Guid organizationId,
        Guid definitionId,
        CancellationToken cancellationToken = default)
    {
        var definition = await _definitions
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                ProductionDefinitionId.From(definitionId),
                cancellationToken)
            .ConfigureAwait(false);
        return definition is null ? null : ProductionMapper.Map(definition);
    }

    public async Task<PagedResult<ProductionDefinitionListItemDto>> ListAsync(
        Guid organizationId,
        ProductionDefinitionFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _definitions
            .ListAsync(PosOrganizationId.From(organizationId), filter, skip, take, cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<ProductionDefinitionListItemDto>(
            items.Select(ProductionMapper.MapListItem).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }
}

public sealed class CreateProductionDefinition
{
    private readonly IProductionDefinitionRepository _definitions;
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateProductionDefinition(
        IProductionDefinitionRepository definitions,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _definitions = definitions;
        _products = products;
        _units = units;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProductionDefinitionDto>> ExecuteAsync(
        Guid organizationId,
        CreateProductionDefinitionRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<ProductionDefinitionDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to create a production definition.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(
                    async ct =>
                    {
                        if (request.ProductionDefinitionId is Guid clientId && clientId != Guid.Empty)
                        {
                            var existing = await _definitions
                                .GetByIdAsync(orgId, ProductionDefinitionId.From(clientId), ct)
                                .ConfigureAwait(false);
                            if (existing is not null)
                            {
                                return ApplicationResult<ProductionDefinitionDto>.Success(
                                    ProductionMapper.Map(existing));
                            }
                        }

                        var resolved = await ResolveComponentsAsync(orgId, request, ct).ConfigureAwait(false);
                        if (!resolved.IsSuccess)
                        {
                            return ApplicationResult<ProductionDefinitionDto>.Failure(
                                resolved.ErrorCode!,
                                resolved.ErrorMessage!);
                        }

                        var cycleError = await ValidateNoCycleAsync(
                                orgId,
                                resolved.Value!.OutputProductId,
                                resolved.Value.ComponentDrafts.Select(c => c.MaterialProductId).ToList(),
                                excludingDefinitionId: null,
                                ct)
                            .ConfigureAwait(false);
                        if (cycleError is not null)
                        {
                            return cycleError;
                        }

                        var utcNow = _clock.UtcNow;
                        ProductionDefinitionId? clientDefinitionId =
                            request.ProductionDefinitionId is Guid sid && sid != Guid.Empty
                                ? ProductionDefinitionId.From(sid)
                                : null;

                        var definition = ProductionDefinition.Create(
                            orgId,
                            request.Name,
                            resolved.Value.OutputProductId,
                            resolved.Value.OutputQuantity,
                            resolved.Value.OutputMultiplier,
                            resolved.Value.ComponentDrafts,
                            actorId,
                            utcNow,
                            resolved.Value.OutputUnitId,
                            clientDefinitionId);

                        await _definitions.AddAsync(definition, ct).ConfigureAwait(false);
                        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                        return ApplicationResult<ProductionDefinitionDto>.Success(ProductionMapper.Map(definition));
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductionDefinitionDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<ApplicationResult<ResolvedDefinition>> ResolveComponentsAsync(
        PosOrganizationId orgId,
        CreateProductionDefinitionRequest request,
        CancellationToken ct) =>
        await ResolveComponentsCoreAsync(
                orgId,
                request.OutputProductId,
                request.OutputQuantity,
                request.OutputProductUnitId,
                request.Components,
                ct)
            .ConfigureAwait(false);

    internal async Task<ApplicationResult<ResolvedDefinition>> ResolveComponentsCoreAsync(
        PosOrganizationId orgId,
        Guid outputProductId,
        decimal outputQuantity,
        Guid? outputProductUnitId,
        IReadOnlyList<CreateProductionComponentRequest> components,
        CancellationToken ct)
    {
        if (components is null || components.Count == 0)
        {
            return ApplicationResult<ResolvedDefinition>.Failure(
                DomainErrorCodes.ProductionRequiresComponents,
                "At least one production component is required.");
        }

        var output = await _products
            .GetByIdAsync(orgId, CatalogProductId.From(outputProductId), ct)
            .ConfigureAwait(false);
        if (output is null)
        {
            return ApplicationResult<ResolvedDefinition>.Failure(
                ApplicationErrorCodes.SaleProductNotFound,
                "Output product was not found in this organization.");
        }

        if (output.Status != CatalogProductStatus.Active)
        {
            return ApplicationResult<ResolvedDefinition>.Failure(
                ApplicationErrorCodes.SaleProductNotActive,
                "Only active catalog products can be production outputs.");
        }

        if (!output.IsProduced)
        {
            return ApplicationResult<ResolvedDefinition>.Failure(
                DomainErrorCodes.ProductionOutputNotEligible,
                "Output product must have IsProduced capability.");
        }

        decimal outputMultiplier = 1m;
        ProductUnitId? outputUnitId = null;
        if (outputProductUnitId is Guid ouid && ouid != Guid.Empty)
        {
            var unit = await _units.GetByIdAsync(orgId, ProductUnitId.From(ouid), ct).ConfigureAwait(false);
            if (unit is null || !unit.IsActive || unit.ProductId != output.Id)
            {
                return ApplicationResult<ResolvedDefinition>.Failure(
                    DomainErrorCodes.InvalidProductUnitId,
                    "Output product unit was not found for this product.");
            }

            outputMultiplier = unit.MultiplierToBase;
            outputUnitId = unit.Id;
        }

        var drafts = new List<ProductionComponentDraft>(components.Count);
        foreach (var component in components)
        {
            var material = await _products
                .GetByIdAsync(orgId, CatalogProductId.From(component.MaterialProductId), ct)
                .ConfigureAwait(false);
            if (material is null)
            {
                return ApplicationResult<ResolvedDefinition>.Failure(
                    ApplicationErrorCodes.SaleProductNotFound,
                    "One or more material products were not found in this organization.");
            }

            if (material.Status != CatalogProductStatus.Active)
            {
                return ApplicationResult<ResolvedDefinition>.Failure(
                    ApplicationErrorCodes.SaleProductNotActive,
                    "Only active catalog products can be production components.");
            }

            if (material.Id == output.Id)
            {
                return ApplicationResult<ResolvedDefinition>.Failure(
                    DomainErrorCodes.ProductionSelfComponentForbidden,
                    "A production definition cannot use its output product as a component.");
            }

            if (!material.CanBeUsedAsIngredient)
            {
                return ApplicationResult<ResolvedDefinition>.Failure(
                    DomainErrorCodes.ProductionComponentNotEligible,
                    $"Product '{material.Name}' is not eligible as a production material (CanBeUsedAsIngredient required).");
            }

            decimal multiplier = 1m;
            ProductUnitId? unitId = null;
            if (component.ProductUnitId is Guid uid && uid != Guid.Empty)
            {
                var unit = await _units.GetByIdAsync(orgId, ProductUnitId.From(uid), ct).ConfigureAwait(false);
                if (unit is null || !unit.IsActive || unit.ProductId != material.Id)
                {
                    return ApplicationResult<ResolvedDefinition>.Failure(
                        DomainErrorCodes.InvalidProductUnitId,
                        "Product unit was not found for this material.");
                }

                multiplier = unit.MultiplierToBase;
                unitId = unit.Id;
            }

            drafts.Add(new ProductionComponentDraft(
                material.Id,
                component.Quantity,
                multiplier,
                unitId,
                component.SortOrder));
        }

        return ApplicationResult<ResolvedDefinition>.Success(
            new ResolvedDefinition(output.Id, outputQuantity, outputMultiplier, outputUnitId, drafts));
    }

    internal async Task<ApplicationResult<ProductionDefinitionDto>?> ValidateNoCycleAsync(
        PosOrganizationId orgId,
        CatalogProductId outputProductId,
        IReadOnlyList<CatalogProductId> componentProductIds,
        ProductionDefinitionId? excludingDefinitionId,
        CancellationToken ct)
    {
        var all = await _definitions.ListAllForCycleValidationAsync(orgId, ct).ConfigureAwait(false);
        var edges = new Dictionary<Guid, HashSet<Guid>>();

        void AddEdge(Guid from, Guid to)
        {
            if (!edges.TryGetValue(from, out var set))
            {
                set = [];
                edges[from] = set;
            }

            set.Add(to);
        }

        foreach (var definition in all)
        {
            if (excludingDefinitionId is ProductionDefinitionId exclude && definition.Id == exclude)
            {
                continue;
            }

            foreach (var component in definition.Components)
            {
                AddEdge(definition.OutputProductId.Value, component.MaterialProductId.Value);
            }
        }

        foreach (var componentId in componentProductIds)
        {
            AddEdge(outputProductId.Value, componentId.Value);
        }

        if (HasPathTo(edges, outputProductId.Value, outputProductId.Value, requireHop: true))
        {
            return ApplicationResult<ProductionDefinitionDto>.Failure(
                DomainErrorCodes.ProductionCycleDetected,
                "Production definition would create a material cycle.");
        }

        return null;
    }

    private static bool HasPathTo(
        Dictionary<Guid, HashSet<Guid>> edges,
        Guid start,
        Guid target,
        bool requireHop)
    {
        var visited = new HashSet<Guid>();
        var stack = new Stack<(Guid Node, int Depth)>();
        stack.Push((start, 0));
        while (stack.Count > 0)
        {
            var (node, depth) = stack.Pop();
            if (depth > 0 && node == target)
            {
                return true;
            }

            if (!visited.Add(node) || !edges.TryGetValue(node, out var next))
            {
                continue;
            }

            foreach (var child in next)
            {
                stack.Push((child, depth + 1));
            }
        }

        return !requireHop && start == target;
    }

    internal sealed record ResolvedDefinition(
        CatalogProductId OutputProductId,
        decimal OutputQuantity,
        decimal OutputMultiplier,
        ProductUnitId? OutputUnitId,
        List<ProductionComponentDraft> ComponentDrafts);
}

public sealed class UpdateProductionDefinition
{
    private readonly IProductionDefinitionRepository _definitions;
    private readonly CreateProductionDefinition _createHelper;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateProductionDefinition(
        IProductionDefinitionRepository definitions,
        CreateProductionDefinition createHelper,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _definitions = definitions;
        _createHelper = createHelper;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProductionDefinitionDto>> ExecuteAsync(
        Guid organizationId,
        Guid definitionId,
        UpdateProductionDefinitionRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<ProductionDefinitionDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to update a production definition.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var id = ProductionDefinitionId.From(definitionId);
        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(
                    async ct =>
                    {
                        var definition = await _definitions.GetByIdAsync(orgId, id, ct).ConfigureAwait(false);
                        if (definition is null)
                        {
                            return ApplicationResult<ProductionDefinitionDto>.Failure(
                                ApplicationErrorCodes.ProductionDefinitionNotFound,
                                "Production definition was not found.");
                        }

                        var resolved = await _createHelper
                            .ResolveComponentsCoreAsync(
                                orgId,
                                request.OutputProductId,
                                request.OutputQuantity,
                                request.OutputProductUnitId,
                                request.Components,
                                ct)
                            .ConfigureAwait(false);
                        if (!resolved.IsSuccess)
                        {
                            return ApplicationResult<ProductionDefinitionDto>.Failure(
                                resolved.ErrorCode!,
                                resolved.ErrorMessage!);
                        }

                        var cycleError = await _createHelper
                            .ValidateNoCycleAsync(
                                orgId,
                                resolved.Value!.OutputProductId,
                                resolved.Value.ComponentDrafts.Select(c => c.MaterialProductId).ToList(),
                                excludingDefinitionId: id,
                                ct)
                            .ConfigureAwait(false);
                        if (cycleError is not null)
                        {
                            return cycleError;
                        }

                        definition.Update(
                            request.Name,
                            resolved.Value.OutputProductId,
                            resolved.Value.OutputQuantity,
                            resolved.Value.OutputMultiplier,
                            resolved.Value.ComponentDrafts,
                            actorId,
                            _clock.UtcNow,
                            resolved.Value.OutputUnitId);

                        await _definitions.UpdateAsync(definition, ct).ConfigureAwait(false);
                        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                        return ApplicationResult<ProductionDefinitionDto>.Success(ProductionMapper.Map(definition));
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductionDefinitionDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class SetProductionDefinitionActive
{
    private readonly IProductionDefinitionRepository _definitions;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SetProductionDefinitionActive(
        IProductionDefinitionRepository definitions,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _definitions = definitions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProductionDefinitionDto>> ExecuteAsync(
        Guid organizationId,
        Guid definitionId,
        bool isActive,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<ProductionDefinitionDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to update a production definition.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var id = ProductionDefinitionId.From(definitionId);
        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(
                    async ct =>
                    {
                        var definition = await _definitions.GetByIdAsync(orgId, id, ct).ConfigureAwait(false);
                        if (definition is null)
                        {
                            return ApplicationResult<ProductionDefinitionDto>.Failure(
                                ApplicationErrorCodes.ProductionDefinitionNotFound,
                                "Production definition was not found.");
                        }

                        definition.SetActive(isActive, actorId, _clock.UtcNow);
                        await _definitions.UpdateAsync(definition, ct).ConfigureAwait(false);
                        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                        return ApplicationResult<ProductionDefinitionDto>.Success(ProductionMapper.Map(definition));
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductionDefinitionDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
