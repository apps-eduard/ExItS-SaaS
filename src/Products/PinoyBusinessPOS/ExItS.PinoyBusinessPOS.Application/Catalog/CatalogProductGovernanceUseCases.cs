using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public sealed record BranchProductAvailabilityDto(
    Guid OrganizationId,
    Guid BranchId,
    Guid ProductId,
    bool IsOffered,
    string Reason,
    bool HasExplicitOverride);

/// <summary>Owner/Admin branch assortment override for OrganizationStandard products.</summary>
public sealed class SetBranchProductAvailability
{
    private readonly ICatalogProductRepository _products;
    private readonly IBranchProductAvailabilityRepository _availability;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly CatalogProductGovernanceAuthority _governance;
    private readonly ICatalogGovernanceActorAccessor _actorAccessor;

    public SetBranchProductAvailability(
        ICatalogProductRepository products,
        IBranchProductAvailabilityRepository availability,
        IOrganizationBranchDirectory branches,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        CatalogProductGovernanceAuthority governance,
        ICatalogGovernanceActorAccessor actorAccessor)
    {
        _products = products;
        _availability = availability;
        _branches = branches;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _governance = governance;
        _actorAccessor = actorAccessor;
    }

    public async Task<ApplicationResult<BranchProductAvailabilityDto>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        Guid branchId,
        bool isOffered,
        CancellationToken cancellationToken = default)
    {
        var actor = _actorAccessor.GetActor();
        if (!_governance.CanManageStandardAvailability(actor))
        {
            return ApplicationResult<BranchProductAvailabilityDto>.Failure(
                ApplicationErrorCodes.ProductAvailabilityForbidden,
                "Only organization Owner/Administrator may configure OrganizationStandard branch availability.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var branch = PosBranchId.From(branchId);
        var active = await _branches
            .IsActiveInOrganizationAsync(organizationId, branchId, cancellationToken)
            .ConfigureAwait(false);
        if (!active)
        {
            return ApplicationResult<BranchProductAvailabilityDto>.Failure(
                ApplicationErrorCodes.ProductBranchInvalid,
                "Branch is not an active branch in this organization.");
        }

        var product = await _products
            .GetByIdAsync(orgId, CatalogProductId.From(productId), cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<BranchProductAvailabilityDto>.Failure(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        if (product.Scope != CatalogProductScope.OrganizationStandard)
        {
            return ApplicationResult<BranchProductAvailabilityDto>.Failure(
                ApplicationErrorCodes.ProductAvailabilityForbidden,
                "Branch availability overrides apply to OrganizationStandard products only.");
        }

        try
        {
            var now = _clock.UtcNow;
            var existing = await _availability
                .GetAsync(orgId, branch, product.Id, cancellationToken)
                .ConfigureAwait(false);

            if (isOffered)
            {
                // Restore default offered: remove sparse false override.
                if (existing is not null)
                {
                    await _availability.DeleteAsync(orgId, branch, product.Id, cancellationToken).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                return ApplicationResult<BranchProductAvailabilityDto>.Success(
                    new BranchProductAvailabilityDto(
                        organizationId,
                        branchId,
                        productId,
                        true,
                        nameof(CatalogProductOfferingReason.DefaultOrganizationStandard),
                        HasExplicitOverride: false));
            }

            if (existing is null)
            {
                await _availability
                    .AddAsync(BranchProductAvailability.Create(orgId, branch, product.Id, false, now), cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (existing.IsOffered)
            {
                existing.SetOffered(false, now);
                await _availability.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<BranchProductAvailabilityDto>.Success(
                new BranchProductAvailabilityDto(
                    organizationId,
                    branchId,
                    productId,
                    false,
                    nameof(CatalogProductOfferingReason.ExplicitlyNotOffered),
                    HasExplicitOverride: true));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BranchProductAvailabilityDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<BranchProductAvailabilityDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>Promotes BranchLocal → OrganizationStandard (same ProductId; price preserved).</summary>
public sealed class PromoteCatalogProductToOrganizationStandard
{
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly CatalogProductGovernanceAuthority _governance;
    private readonly ICatalogGovernanceActorAccessor _actorAccessor;
    private readonly CatalogProductQueryService _queries;

    public PromoteCatalogProductToOrganizationStandard(
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        CatalogProductGovernanceAuthority governance,
        ICatalogGovernanceActorAccessor actorAccessor,
        CatalogProductQueryService queries)
    {
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _governance = governance;
        _actorAccessor = actorAccessor;
        _queries = queries;
    }

    public async Task<ApplicationResult<PosCatalogProductDto>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var actor = _actorAccessor.GetActor();
        if (!_governance.CanPromote(actor))
        {
            return ApplicationResult<PosCatalogProductDto>.Failure(
                ApplicationErrorCodes.ProductPromotionForbidden,
                "Only organization Owner/Administrator may promote BranchLocal products.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var product = await _products
            .GetByIdAsync(orgId, CatalogProductId.From(productId), cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<PosCatalogProductDto>.Failure(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        try
        {
            product.PromoteToOrganizationStandard(_clock.UtcNow);
            await _products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var dto = await _queries
                .GetByIdAsync(organizationId, product.Id.Value, cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<PosCatalogProductDto>.Success(dto!);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosCatalogProductDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosCatalogProductDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Advisory duplicate-name check for React. Server create/update remain authoritative.
/// Foreign BranchLocal may be reported as duplicate without revealing metadata.
/// </summary>
public sealed class QueryCatalogProductNameConflict
{
    private readonly ICatalogProductRepository _products;
    private readonly CatalogProductQueryService _queries;
    private readonly CatalogProductGovernanceAuthority _governance;
    private readonly ICatalogGovernanceActorAccessor _actorAccessor;
    private readonly ICatalogProductAvailabilityResolver _availability;

    public QueryCatalogProductNameConflict(
        ICatalogProductRepository products,
        CatalogProductQueryService queries,
        CatalogProductGovernanceAuthority governance,
        ICatalogGovernanceActorAccessor actorAccessor,
        ICatalogProductAvailabilityResolver availability)
    {
        _products = products;
        _queries = queries;
        _governance = governance;
        _actorAccessor = actorAccessor;
        _availability = availability;
    }

    public async Task<ApplicationResult<CatalogProductNameConflictDto>> ExecuteAsync(
        Guid organizationId,
        string name,
        Guid? excludeProductId,
        CancellationToken cancellationToken = default)
    {
        string normalized;
        try
        {
            (_, normalized) = CatalogProduct.NormalizeProductName(name);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CatalogProductNameConflictDto>.Failure(ex.ErrorCode, ex.Message);
        }

        var orgId = PosOrganizationId.From(organizationId);
        CatalogProductId? exclude = excludeProductId is Guid eid && eid != Guid.Empty
            ? CatalogProductId.From(eid)
            : null;

        var existing = await CatalogAssignment
            .FindExistingByNormalizedNameAsync(_products, orgId, normalized, exclude, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return ApplicationResult<CatalogProductNameConflictDto>.Success(
                new CatalogProductNameConflictDto(false, false));
        }

        var actor = _actorAccessor.GetActor();
        var canReveal = existing.Scope != CatalogProductScope.BranchLocal
            || _governance.CanViewBranchLocalInManagement(actor, existing.OriginBranchId);

        if (!canReveal)
        {
            return ApplicationResult<CatalogProductNameConflictDto>.Success(
                new CatalogProductNameConflictDto(true, false));
        }

        var dto = await _queries
            .GetByIdAsync(organizationId, existing.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (dto is null)
        {
            return ApplicationResult<CatalogProductNameConflictDto>.Success(
                new CatalogProductNameConflictDto(true, false));
        }

        if (actor.ActingBranchId is Guid branch && branch != Guid.Empty)
        {
            var offering = await _availability
                .ResolveForBranchAsync(
                    orgId,
                    PosBranchId.From(branch),
                    [existing],
                    cancellationToken)
                .ConfigureAwait(false);
            if (offering.TryGetValue(existing.Id.Value, out var offer))
            {
                dto = dto with { IsOfferedAtBranch = offer.IsOffered };
            }
        }

        return ApplicationResult<CatalogProductNameConflictDto>.Success(
            new CatalogProductNameConflictDto(true, true, dto));
    }
}

/// <summary>
/// Bulk read of sparse branch offering overrides for one product (no N+1).
/// OrganizationStandard: ExplicitRows only; missing branch = offered by default.
/// BranchLocal: returns origin-only synthetic offered row (no cross-branch sharing).
/// </summary>
public sealed class QueryProductBranchAvailability
{
    private readonly ICatalogProductRepository _products;
    private readonly IBranchProductAvailabilityRepository _availability;
    private readonly CatalogProductGovernanceAuthority _governance;
    private readonly ICatalogGovernanceActorAccessor _actorAccessor;

    public QueryProductBranchAvailability(
        ICatalogProductRepository products,
        IBranchProductAvailabilityRepository availability,
        CatalogProductGovernanceAuthority governance,
        ICatalogGovernanceActorAccessor actorAccessor)
    {
        _products = products;
        _availability = availability;
        _governance = governance;
        _actorAccessor = actorAccessor;
    }

    public async Task<ApplicationResult<ProductBranchAvailabilityReadDto>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var product = await _products
            .GetByIdAsync(orgId, CatalogProductId.From(productId), cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<ProductBranchAvailabilityReadDto>.Failure(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        var actor = _actorAccessor.GetActor();
        if (product.Scope == CatalogProductScope.BranchLocal
            && !_governance.CanViewBranchLocalInManagement(actor, product.OriginBranchId))
        {
            return ApplicationResult<ProductBranchAvailabilityReadDto>.Failure(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        var scopeCode = CatalogProductScopes.ToCode(product.Scope);
        if (product.Scope == CatalogProductScope.BranchLocal)
        {
            if (product.OriginBranchId is null)
            {
                return ApplicationResult<ProductBranchAvailabilityReadDto>.Success(
                    new ProductBranchAvailabilityReadDto(
                        product.Id.Value,
                        scopeCode,
                        null,
                        []));
            }

            return ApplicationResult<ProductBranchAvailabilityReadDto>.Success(
                new ProductBranchAvailabilityReadDto(
                    product.Id.Value,
                    scopeCode,
                    product.OriginBranchId.Value,
                    [
                        new ProductBranchOfferingItemDto(
                            product.OriginBranchId.Value,
                            true,
                            nameof(CatalogProductOfferingReason.BranchLocalOrigin),
                            HasExplicitOverride: false)
                    ]));
        }

        var rows = await _availability
            .ListByProductAsync(orgId, product.Id, cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(r => new ProductBranchOfferingItemDto(
                r.BranchId.Value,
                r.IsOffered,
                r.IsOffered
                    ? nameof(CatalogProductOfferingReason.ExplicitlyOffered)
                    : nameof(CatalogProductOfferingReason.ExplicitlyNotOffered),
                HasExplicitOverride: true))
            .OrderBy(i => i.BranchId)
            .ToList();

        return ApplicationResult<ProductBranchAvailabilityReadDto>.Success(
            new ProductBranchAvailabilityReadDto(
                product.Id.Value,
                scopeCode,
                product.OriginBranchId?.Value,
                items));
    }
}

/// <summary>Shared commercial offering gate for Sell/checkout/storefront/orders.</summary>
public static class CatalogProductCommercialOfferingGate
{
    public static async Task<ApplicationResult> EnsureOfferedAsync(
        ICatalogProductAvailabilityResolver resolver,
        PosOrganizationId organizationId,
        Guid branchId,
        IReadOnlyList<CatalogProduct> products,
        CancellationToken cancellationToken)
    {
        if (products.Count == 0)
        {
            return ApplicationResult.Success();
        }

        var branch = PosBranchId.From(branchId);
        var offered = await resolver
            .ResolveForBranchAsync(organizationId, branch, products, cancellationToken)
            .ConfigureAwait(false);

        foreach (var product in products)
        {
            if (!offered.TryGetValue(product.Id.Value, out var result) || !result.IsOffered)
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.ProductNotOfferedAtBranch,
                    "One or more products are not offered at this branch.");
            }
        }

        return ApplicationResult.Success();
    }
}
