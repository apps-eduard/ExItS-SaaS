using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Parties;
using ExItS.PinoyBusinessPOS.Application.Suppliers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Branches;

public interface IBranchSetupProgressRepository
{
    Task<BranchSetupProgressDto?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default);

    Task UpsertVisitAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        string? lastVisitedStep,
        DateTimeOffset utcNow,
        bool markCompleted,
        CancellationToken cancellationToken = default);
}

public interface IBranchReadinessMetricsRepository
{
    Task<BranchReadinessMetrics> GetMetricsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default);
}

public sealed record BranchReadinessMetrics(
    int ActiveCatalogProducts,
    int OfferedProducts,
    int PriceOverrides,
    int ProductsWithStock,
    int CustomerAccessGrants,
    int SupplierAccessGrants);

public sealed class BranchReadinessQueryService
{
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IBranchReadinessMetricsRepository _metrics;
    private readonly IBranchSetupProgressRepository _progress;

    public BranchReadinessQueryService(
        IOrganizationBranchDirectory branches,
        IBranchReadinessMetricsRepository metrics,
        IBranchSetupProgressRepository progress)
    {
        _branches = branches;
        _metrics = metrics;
        _progress = progress;
    }

    public async Task<ApplicationResult<BranchReadinessDto>> GetAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            return ApplicationResult<BranchReadinessDto>.Failure(
                DomainErrorCodes.InvalidBranchId,
                "BranchId is required.");
        }

        var exists = await _branches
            .ExistsInOrganizationAsync(organizationId, branchId, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
        {
            return ApplicationResult<BranchReadinessDto>.Failure(
                ApplicationErrorCodes.CustomerOrderBranchNotFound,
                "Branch was not found in this organization.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var branch = PosBranchId.From(branchId);
        var metrics = await _metrics.GetMetricsAsync(orgId, branch, cancellationToken).ConfigureAwait(false);
        var setup = await _progress.GetAsync(orgId, branch, cancellationToken).ConfigureAwait(false);

        var sections = BuildSections(branchId, metrics);
        var overall = DeriveOverall(sections);

        return ApplicationResult<BranchReadinessDto>.Success(
            new BranchReadinessDto(organizationId, branchId, overall, sections, setup));
    }

    private static IReadOnlyList<BranchReadinessSectionDto> BuildSections(Guid branchId, BranchReadinessMetrics metrics)
    {
        var branchPath = branchId.ToString("D");
        return
        [
            new("Details", BranchReadinessSectionStatus.Optional, null, null, $"/org/branches/{branchPath}?tab=details"),
            new(
                "Staff",
                BranchReadinessSectionStatus.NeedsAttention,
                "Assign staff explicitly during setup.",
                null,
                $"/org/branches/{branchPath}?tab=staff"),
            new(
                "Products",
                metrics.OfferedProducts > 0 ? BranchReadinessSectionStatus.Complete : BranchReadinessSectionStatus.NeedsAttention,
                metrics.OfferedProducts > 0 ? $"{metrics.OfferedProducts} offered" : "No products offered yet",
                metrics.OfferedProducts,
                "/org/catalog"),
            new(
                "Pricing",
                metrics.PriceOverrides > 0 || metrics.OfferedProducts > 0
                    ? BranchReadinessSectionStatus.Complete
                    : BranchReadinessSectionStatus.NeedsAttention,
                metrics.PriceOverrides > 0 ? $"{metrics.PriceOverrides} overrides" : "Using organization defaults",
                metrics.PriceOverrides,
                $"/org/branches/{branchPath}/pricing"),
            new(
                "Inventory",
                metrics.ProductsWithStock > 0
                    ? BranchReadinessSectionStatus.Complete
                    : BranchReadinessSectionStatus.NeedsAttention,
                metrics.ProductsWithStock > 0 ? $"{metrics.ProductsWithStock} stocked products" : "Starting stock is zero",
                metrics.ProductsWithStock,
                "/org/inventory"),
            new(
                "Parties",
                metrics.CustomerAccessGrants + metrics.SupplierAccessGrants > 0
                    ? BranchReadinessSectionStatus.Complete
                    : BranchReadinessSectionStatus.NeedsAttention,
                $"{metrics.CustomerAccessGrants} customers, {metrics.SupplierAccessGrants} suppliers",
                metrics.CustomerAccessGrants + metrics.SupplierAccessGrants,
                $"/org/branches/{branchPath}/setup"),
            new(
                "Fulfillment",
                BranchReadinessSectionStatus.Optional,
                "Configure after core branch setup.",
                null,
                $"/org/branches/{branchPath}/fulfillment"),
            new(
                "Device",
                BranchReadinessSectionStatus.NotApplicable,
                "Register devices separately.",
                null,
                $"/org/branches/{branchPath}?tab=devices"),
        ];
    }

    private static BranchReadinessOverallStatus DeriveOverall(IReadOnlyList<BranchReadinessSectionDto> sections)
    {
        var required = sections.Where(s => s.Status is not BranchReadinessSectionStatus.Optional
            and not BranchReadinessSectionStatus.NotApplicable).ToList();
        if (required.Count == 0)
        {
            return BranchReadinessOverallStatus.NotStarted;
        }

        if (required.All(s => s.Status == BranchReadinessSectionStatus.Complete))
        {
            return BranchReadinessOverallStatus.Ready;
        }

        if (required.All(s => s.Status == BranchReadinessSectionStatus.NeedsAttention))
        {
            return BranchReadinessOverallStatus.NotStarted;
        }

        return BranchReadinessOverallStatus.NeedsAttention;
    }
}

public sealed class BranchSetupProgressService
{
    private readonly IBranchSetupProgressRepository _progress;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public BranchSetupProgressService(
        IBranchSetupProgressRepository progress,
        IOrganizationBranchDirectory branches,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _progress = progress;
        _branches = branches;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<BranchSetupProgressDto>> UpsertAsync(
        Guid organizationId,
        Guid branchId,
        UpsertBranchSetupProgressRequest request,
        CancellationToken cancellationToken = default)
    {
        var exists = await _branches
            .ExistsInOrganizationAsync(organizationId, branchId, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
        {
            return ApplicationResult<BranchSetupProgressDto>.Failure(
                ApplicationErrorCodes.CustomerOrderBranchNotFound,
                "Branch was not found in this organization.");
        }

        await _progress
            .UpsertVisitAsync(
                PosOrganizationId.From(organizationId),
                PosBranchId.From(branchId),
                request.LastVisitedStep,
                _clock.UtcNow,
                request.MarkCompleted,
                cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var current = await _progress
            .GetAsync(PosOrganizationId.From(organizationId), PosBranchId.From(branchId), cancellationToken)
            .ConfigureAwait(false);
        return ApplicationResult<BranchSetupProgressDto>.Success(
            current ?? new BranchSetupProgressDto(null, null, null, null));
    }
}

public sealed class PartyBranchExplicitAssignService
{
    private readonly PartyBranchAccessService _access;
    private readonly PartyBranchAccessGovernanceAuthority _governance;
    private readonly IPartyBranchAccessActorAccessor _actorAccessor;
    private readonly IPOSCustomerRepository _customers;
    private readonly ISupplierRepository _suppliers;
    private readonly IPosUnitOfWork _unitOfWork;

    public PartyBranchExplicitAssignService(
        PartyBranchAccessService access,
        PartyBranchAccessGovernanceAuthority governance,
        IPartyBranchAccessActorAccessor actorAccessor,
        IPOSCustomerRepository customers,
        ISupplierRepository suppliers,
        IPosUnitOfWork unitOfWork)
    {
        _access = access;
        _governance = governance;
        _actorAccessor = actorAccessor;
        _customers = customers;
        _suppliers = suppliers;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplicationResult> GrantCustomerAsync(
        Guid organizationId,
        Guid customerId,
        GrantPartyBranchAccessRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var gate = RequireGovernance();
        if (!gate.IsSuccess)
        {
            return gate;
        }

        if (request.BranchId == Guid.Empty)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.InvalidBranchId,
                "BranchId is required.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var customer = await _customers
            .GetByIdAsync(orgId, POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        await _access
            .GrantCustomerExplicitAssignAsync(organizationId, request.BranchId, customerId, actorId, cancellationToken)
            .ConfigureAwait(false);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> RevokeCustomerAsync(
        Guid organizationId,
        Guid customerId,
        GrantPartyBranchAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = RequireGovernance();
        if (!gate.IsSuccess)
        {
            return gate;
        }

        var orgId = PosOrganizationId.From(organizationId);
        var customer = await _customers
            .GetByIdAsync(orgId, POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        await _access
            .RevokeCustomerExplicitAssignAsync(organizationId, request.BranchId, customerId, cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> GrantSupplierAsync(
        Guid organizationId,
        Guid supplierId,
        GrantPartyBranchAccessRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var gate = RequireGovernance();
        if (!gate.IsSuccess)
        {
            return gate;
        }

        var orgId = PosOrganizationId.From(organizationId);
        var supplier = await _suppliers
            .GetByIdAsync(orgId, Domain.Suppliers.SupplierId.From(supplierId), cancellationToken)
            .ConfigureAwait(false);
        if (supplier is null)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.SupplierNotFound,
                "Supplier was not found.");
        }

        await _access
            .GrantSupplierExplicitAssignAsync(organizationId, request.BranchId, supplierId, actorId, cancellationToken)
            .ConfigureAwait(false);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> RevokeSupplierAsync(
        Guid organizationId,
        Guid supplierId,
        GrantPartyBranchAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = RequireGovernance();
        if (!gate.IsSuccess)
        {
            return gate;
        }

        var orgId = PosOrganizationId.From(organizationId);
        var supplier = await _suppliers
            .GetByIdAsync(orgId, Domain.Suppliers.SupplierId.From(supplierId), cancellationToken)
            .ConfigureAwait(false);
        if (supplier is null)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.SupplierNotFound,
                "Supplier was not found.");
        }

        await _access
            .RevokeSupplierExplicitAssignAsync(organizationId, request.BranchId, supplierId, cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult.Success();
    }

    private ApplicationResult RequireGovernance()
    {
        var actor = _actorAccessor.GetActor();
        if (!_governance.CanBypassBranchFilter(actor))
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.CustomerBranchAccessForbidden,
                "Owner or Admin governance is required for explicit branch access assignment.");
        }

        return ApplicationResult.Success();
    }
}
