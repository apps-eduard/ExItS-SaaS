using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public sealed class SetBranchProductPriceOverride
{
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IBranchProductPriceOverrideRepository _overrides;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly CatalogProductGovernanceAuthority _governance;
    private readonly ICatalogGovernanceActorAccessor _actorAccessor;

    public SetBranchProductPriceOverride(
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IBranchProductPriceOverrideRepository overrides,
        IOrganizationBranchDirectory branches,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        CatalogProductGovernanceAuthority governance,
        ICatalogGovernanceActorAccessor actorAccessor)
    {
        _products = products;
        _units = units;
        _overrides = overrides;
        _branches = branches;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _governance = governance;
        _actorAccessor = actorAccessor;
    }

    public async Task<ApplicationResult<BranchProductPriceOverrideDto>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        SetBranchProductPriceOverrideRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = _actorAccessor.GetActor();
        if (!_governance.CanMutateOrganizationStandardPrice(actor))
        {
            return ApplicationResult<BranchProductPriceOverrideDto>.Failure(
                ApplicationErrorCodes.ProductBranchPriceForbidden,
                "Only organization Owner/Administrator may configure branch price overrides.");
        }

        if (request.SellingPrice < 0m)
        {
            return ApplicationResult<BranchProductPriceOverrideDto>.Failure(
                ApplicationErrorCodes.ProductBranchPriceInvalid,
                "Branch price override must be greater than or equal to zero.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var branch = PosBranchId.From(request.BranchId);
        var active = await _branches
            .IsActiveInOrganizationAsync(organizationId, request.BranchId, cancellationToken)
            .ConfigureAwait(false);
        if (!active)
        {
            return ApplicationResult<BranchProductPriceOverrideDto>.Failure(
                ApplicationErrorCodes.ProductBranchInvalid,
                "Branch is not an active branch in this organization.");
        }

        var product = await _products
            .GetByIdAsync(orgId, CatalogProductId.From(productId), cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<BranchProductPriceOverrideDto>.Failure(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        if (product.Scope != CatalogProductScope.OrganizationStandard)
        {
            return ApplicationResult<BranchProductPriceOverrideDto>.Failure(
                ApplicationErrorCodes.ProductBranchPriceForbidden,
                "Branch price overrides apply to OrganizationStandard products only.");
        }

        var unitKey = ResolveUnitKey(request.ProductUnitId);
        if (unitKey != BranchProductPriceOverride.BaseProductUnitKey)
        {
            var unit = await _units
                .GetByIdAsync(orgId, ProductUnitId.From(unitKey), cancellationToken)
                .ConfigureAwait(false);
            if (unit is null || unit.ProductId != product.Id || unit.Kind != ProductUnitKind.Sell)
            {
                return ApplicationResult<BranchProductPriceOverrideDto>.Failure(
                    ApplicationErrorCodes.ProductBranchPriceInvalid,
                    "Selling unit was not found for this product.");
            }
        }

        try
        {
            var now = _clock.UtcNow;
            var existing = await _overrides
                .GetAsync(orgId, branch, product.Id, unitKey, cancellationToken)
                .ConfigureAwait(false);

            if (existing is null)
            {
                await _overrides
                    .AddAsync(
                        BranchProductPriceOverride.Create(
                            orgId,
                            branch,
                            product.Id,
                            unitKey,
                            request.SellingPrice,
                            now),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                existing.SetSellingPrice(request.SellingPrice, now);
                await _overrides.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<BranchProductPriceOverrideDto>.Success(
                new BranchProductPriceOverrideDto(
                    organizationId,
                    request.BranchId,
                    productId,
                    unitKey == BranchProductPriceOverride.BaseProductUnitKey ? null : unitKey,
                    request.SellingPrice,
                    HasExplicitOverride: true));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BranchProductPriceOverrideDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<BranchProductPriceOverrideDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private static Guid ResolveUnitKey(Guid? productUnitId) =>
        productUnitId is null || productUnitId == Guid.Empty
            ? BranchProductPriceOverride.BaseProductUnitKey
            : productUnitId.Value;
}

public sealed class RemoveBranchProductPriceOverride
{
    private readonly ICatalogProductRepository _products;
    private readonly IBranchProductPriceOverrideRepository _overrides;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly CatalogProductGovernanceAuthority _governance;
    private readonly ICatalogGovernanceActorAccessor _actorAccessor;

    public RemoveBranchProductPriceOverride(
        ICatalogProductRepository products,
        IBranchProductPriceOverrideRepository overrides,
        IOrganizationBranchDirectory branches,
        IPosUnitOfWork unitOfWork,
        CatalogProductGovernanceAuthority governance,
        ICatalogGovernanceActorAccessor actorAccessor)
    {
        _products = products;
        _overrides = overrides;
        _branches = branches;
        _unitOfWork = unitOfWork;
        _governance = governance;
        _actorAccessor = actorAccessor;
    }

    public async Task<ApplicationResult> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        Guid branchId,
        Guid? productUnitId = null,
        CancellationToken cancellationToken = default)
    {
        var actor = _actorAccessor.GetActor();
        if (!_governance.CanMutateOrganizationStandardPrice(actor))
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.ProductBranchPriceForbidden,
                "Only organization Owner/Administrator may remove branch price overrides.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var branch = PosBranchId.From(branchId);
        var active = await _branches
            .IsActiveInOrganizationAsync(organizationId, branchId, cancellationToken)
            .ConfigureAwait(false);
        if (!active)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.ProductBranchInvalid,
                "Branch is not an active branch in this organization.");
        }

        var product = await _products
            .GetByIdAsync(orgId, CatalogProductId.From(productId), cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        if (product.Scope != CatalogProductScope.OrganizationStandard)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.ProductBranchPriceForbidden,
                "Branch price overrides apply to OrganizationStandard products only.");
        }

        var unitKey = productUnitId is null || productUnitId == Guid.Empty
            ? BranchProductPriceOverride.BaseProductUnitKey
            : productUnitId.Value;

        await _overrides
            .DeleteAsync(orgId, branch, product.Id, unitKey, cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult.Success();
    }
}

public sealed class GetBranchProductPricing
{
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IEffectivePriceResolver _effectivePrices;
    private readonly IOrganizationBranchDirectory _branches;

    public GetBranchProductPricing(
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IEffectivePriceResolver effectivePrices,
        IOrganizationBranchDirectory branches)
    {
        _products = products;
        _units = units;
        _effectivePrices = effectivePrices;
        _branches = branches;
    }

    public async Task<ApplicationResult<BranchProductPricingDto>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var branch = PosBranchId.From(branchId);
        var exists = await _branches
            .ExistsInOrganizationAsync(organizationId, branchId, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
        {
            return ApplicationResult<BranchProductPricingDto>.Failure(
                ApplicationErrorCodes.ProductBranchInvalid,
                "Branch is not part of this organization.");
        }

        var product = await _products
            .GetByIdAsync(orgId, CatalogProductId.From(productId), cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<BranchProductPricingDto>.Failure(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        var units = await _units.ListByProductAsync(orgId, product.Id, cancellationToken).ConfigureAwait(false);
        var sellUnits = units.Where(u => u.IsActive && u.Kind == ProductUnitKind.Sell).ToList();
        var unitsByProduct = new Dictionary<CatalogProductId, IReadOnlyList<CatalogProductUnit>>
        {
            [product.Id] = sellUnits
        };
        var resolved = await _effectivePrices
            .ResolveAsync(orgId, branch, [product], unitsByProduct, cancellationToken)
            .ConfigureAwait(false);

        var baseKey = EffectivePriceKeys.ForBaseProduct(productId);
        if (!resolved.TryGetValue(baseKey, out var basePrice))
        {
            return ApplicationResult<BranchProductPricingDto>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "Effective base price could not be resolved.");
        }

        var unitPrices = sellUnits
            .Select(u =>
            {
                var key = EffectivePriceKeys.ForSellUnit(productId, u.Id.Value);
                var entry = resolved[key];
                return new BranchProductPricingItemDto(
                    u.Id.Value,
                    entry.OrganizationDefaultPrice,
                    entry.BranchOverridePrice,
                    entry.EffectivePrice,
                    entry.HasBranchPriceOverride);
            })
            .ToList();

        return ApplicationResult<BranchProductPricingDto>.Success(
            new BranchProductPricingDto(
                productId,
                branchId,
                new BranchProductPricingItemDto(
                    ProductUnitId: null,
                    basePrice.OrganizationDefaultPrice,
                    basePrice.BranchOverridePrice,
                    basePrice.EffectivePrice,
                    basePrice.HasBranchPriceOverride),
                unitPrices));
    }
}
