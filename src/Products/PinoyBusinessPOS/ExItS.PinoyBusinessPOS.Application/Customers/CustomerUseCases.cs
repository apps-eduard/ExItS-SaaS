using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Parties;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Parties;

namespace ExItS.PinoyBusinessPOS.Application.Customers;

public sealed record POSCustomerDto(
    Guid CustomerId,
    Guid OrganizationId,
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes,
    string Status,
    Guid? PlatformBusinessCustomerId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string? LinkedPersonalPublicUserId = null,
    Guid? LinkedBuyerOrganizationId = null,
    string? LinkedBuyerPublicOrganizationId = null);

public sealed record CustomerSyncPageDto(
    List<POSCustomerDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    DateTimeOffset? NextCheckpointUtc);

public sealed class POSCustomerQueryService
{
    private readonly IPOSCustomerRepository _customers;
    private readonly PartyBranchAccessService _branchAccess;
    private readonly IPartyBranchAccessActorAccessor _actorAccessor;

    public POSCustomerQueryService(
        IPOSCustomerRepository customers,
        PartyBranchAccessService branchAccess,
        IPartyBranchAccessActorAccessor actorAccessor)
    {
        _customers = customers;
        _branchAccess = branchAccess;
        _actorAccessor = actorAccessor;
    }

    private PartyBranchAccessActor Actor => _actorAccessor.GetActor();

    public async Task<POSCustomerDto?> GetByIdAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customers
            .GetByIdAsync(PosOrganizationId.From(organizationId), POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return null;
        }

        if (!await _branchAccess.EnsureCanViewCustomerOrNotFoundAsync(
                organizationId,
                customerId,
                Actor,
                cancellationToken)
            .ConfigureAwait(false))
        {
            return null;
        }

        return Map(customer);
    }

    public async Task<POSCustomerDto?> GetByPlatformBusinessCustomerIdAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        CancellationToken cancellationToken = default)
    {
        if (platformBusinessCustomerId == Guid.Empty)
        {
            return null;
        }

        var customer = await _customers
            .FindByPlatformBusinessCustomerIdAsync(
                PosOrganizationId.From(organizationId),
                platformBusinessCustomerId,
                cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return null;
        }

        if (!await _branchAccess.EnsureCanViewCustomerOrNotFoundAsync(
                organizationId,
                customer.Id.Value,
                Actor,
                cancellationToken)
            .ConfigureAwait(false))
        {
            return null;
        }

        return Map(customer);
    }

    /// <summary>
    /// Exact org-scoped lookup for checkout Personal QR/ID selection (Active customers only).
    /// </summary>
    public async Task<CheckoutCustomerSearchItemDto?> GetByLinkedPersonalPublicUserIdForCheckoutAsync(
        Guid organizationId,
        string personalPublicUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(personalPublicUserId))
        {
            return null;
        }

        var customer = await _customers
            .FindByLinkedPersonalPublicUserIdAsync(
                PosOrganizationId.From(organizationId),
                personalPublicUserId,
                cancellationToken)
            .ConfigureAwait(false);
        if (customer is null || customer.Status != CustomerStatus.Active)
        {
            return null;
        }

        if (!await _branchAccess.EnsureCanViewCustomerOrNotFoundAsync(
                organizationId,
                customer.Id.Value,
                Actor,
                cancellationToken)
            .ConfigureAwait(false))
        {
            return null;
        }

        return new CheckoutCustomerSearchItemDto(
            customer.Id.Value,
            customer.DisplayName,
            customer.MobileNumber,
            customer.Status.ToString());
    }

    public async Task<PagedResult<POSCustomerDto>> ListAsync(
        Guid organizationId,
        CustomerStatus? status,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var restrict = await _branchAccess
            .FilterCustomerIdsAccessibleAsync(organizationId, Actor, cancellationToken)
            .ConfigureAwait(false);
        var (items, total) = await _customers
            .ListAsync(PosOrganizationId.From(organizationId), status, search, skip, take, restrict, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<POSCustomerDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    /// <summary>
    /// Narrow Active-only customer search for checkout (CreateSale). pageSize capped at 20.
    /// </summary>
    public async Task<CheckoutCustomerSearchResult> SearchForCheckoutAsync(
        Guid organizationId,
        string search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(pageSize ?? 20, 1, 20);
        var pageNumber = Math.Max(page ?? 1, 1);
        var skip = (pageNumber - 1) * take;
        var restrict = await _branchAccess
            .FilterCustomerIdsAccessibleAsync(organizationId, Actor, cancellationToken)
            .ConfigureAwait(false);
        var (items, total) = await _customers
            .ListAsync(
                PosOrganizationId.From(organizationId),
                CustomerStatus.Active,
                search,
                skip,
                take,
                restrict,
                cancellationToken)
            .ConfigureAwait(false);

        return new CheckoutCustomerSearchResult(
            items.Select(c => new CheckoutCustomerSearchItemDto(
                c.Id.Value,
                c.DisplayName,
                c.MobileNumber,
                c.Status.ToString())).ToList(),
            total,
            pageNumber,
            take);
    }

    public async Task<CustomerSyncPageDto> ListForSyncAsync(
        Guid organizationId,
        DateTimeOffset? sinceUtc,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _customers
            .ListUpdatedSinceAsync(PosOrganizationId.From(organizationId), sinceUtc, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var mapped = items.Select(Map).ToList();
        DateTimeOffset? nextCheckpoint = mapped.Count > 0
            ? mapped.Max(c => c.UpdatedAtUtc)
            : null;

        return new CustomerSyncPageDto(mapped, total, Math.Max(page ?? 1, 1), take, nextCheckpoint);
    }

    public static POSCustomerDto Map(POSCustomer customer) =>
        new(
            customer.Id.Value,
            customer.OrganizationId.Value,
            customer.DisplayName,
            customer.MobileNumber,
            customer.Address,
            customer.Notes,
            customer.Status.ToString(),
            customer.PlatformBusinessCustomerId,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc,
            customer.LinkedPersonalPublicUserId,
            customer.LinkedBuyerOrganizationId,
            customer.LinkedBuyerPublicOrganizationId);
}

public sealed class CreatePOSCustomer
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PartyBranchAccessService _branchAccess;
    private readonly IPartyBranchAccessActorAccessor _actorAccessor;

    public CreatePOSCustomer(
        IPOSCustomerRepository customers,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        PartyBranchAccessService branchAccess,
        IPartyBranchAccessActorAccessor actorAccessor)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _branchAccess = branchAccess;
        _actorAccessor = actorAccessor;
    }

    public async Task<ApplicationResult<POSCustomer>> ExecuteAsync(
        Guid organizationId,
        string displayName,
        string? mobileNumber,
        string? address,
        string? notes,
        Guid? clientCustomerId = null,
        Guid? platformBusinessCustomerId = null,
        string? linkedPersonalPublicUserId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orgId = PosOrganizationId.From(organizationId);

            if (clientCustomerId is not null)
            {
                var existingById = await _customers
                    .GetByIdAsync(orgId, POSCustomerId.From(clientCustomerId.Value), cancellationToken)
                    .ConfigureAwait(false);
                if (existingById is not null)
                {
                    return ApplicationResult<POSCustomer>.Success(existingById);
                }
            }

            var customer = clientCustomerId is null
                ? POSCustomer.Create(
                    orgId,
                    displayName,
                    _clock.UtcNow,
                    mobileNumber,
                    address,
                    notes,
                    platformBusinessCustomerId: platformBusinessCustomerId,
                    linkedPersonalPublicUserId: linkedPersonalPublicUserId)
                : POSCustomer.Create(
                    orgId,
                    displayName,
                    _clock.UtcNow,
                    mobileNumber,
                    address,
                    notes,
                    id: POSCustomerId.From(clientCustomerId.Value),
                    platformBusinessCustomerId: platformBusinessCustomerId,
                    linkedPersonalPublicUserId: linkedPersonalPublicUserId);

            if (customer.PlatformBusinessCustomerId is not null)
            {
                var existingCorrelation = await _customers
                    .FindByPlatformBusinessCustomerIdAsync(
                        orgId,
                        customer.PlatformBusinessCustomerId.Value,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (existingCorrelation is not null)
                {
                    return ApplicationResult<POSCustomer>.Failure(
                        ApplicationErrorCodes.PlatformBusinessCustomerCorrelationConflict,
                        "Another POS customer in this organization is already correlated to that Platform BusinessCustomer.");
                }
            }

            if (!string.IsNullOrWhiteSpace(customer.LinkedPersonalPublicUserId))
            {
                var existingPersonal = await _customers
                    .FindByLinkedPersonalPublicUserIdAsync(
                        orgId,
                        customer.LinkedPersonalPublicUserId,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (existingPersonal is not null)
                {
                    return ApplicationResult<POSCustomer>.Failure(
                        DomainErrorCodes.CustomerExItsIdentityLinkConflict,
                        "Another POS customer in this organization is already linked to that Personal ExItS identity.");
                }

                var notesTag = "exits-id:" + customer.LinkedPersonalPublicUserId;
                var (searchHits, _) = await _customers
                    .ListAsync(orgId, CustomerStatus.Active, customer.LinkedPersonalPublicUserId, 0, 20, null, cancellationToken)
                    .ConfigureAwait(false);
                if (searchHits.Any(c =>
                    string.Equals(
                        c.LinkedPersonalPublicUserId,
                        customer.LinkedPersonalPublicUserId,
                        StringComparison.OrdinalIgnoreCase)
                    || (c.Notes is not null
                        && c.Notes.Contains(notesTag, StringComparison.OrdinalIgnoreCase))))
                {
                    return ApplicationResult<POSCustomer>.Failure(
                        DomainErrorCodes.CustomerExItsIdentityLinkConflict,
                        "Another POS customer in this organization is already linked to that Personal ExItS identity.");
                }
            }

            if (customer.NormalizedMobile is not null)
            {
                var existing = await _customers
                    .FindActiveByNormalizedMobileAsync(orgId, customer.NormalizedMobile, cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null)
                {
                    return ApplicationResult<POSCustomer>.Failure(
                        ApplicationErrorCodes.MobileConflict,
                        "An active customer with this mobile number already exists in this organization.");
                }
            }

            await _customers.AddAsync(customer, cancellationToken).ConfigureAwait(false);

            var actor = _actorAccessor.GetActor();
            if (actor.ActingBranchId is Guid branchId && branchId != Guid.Empty)
            {
                await _branchAccess.GrantCustomerAccessAsync(
                        organizationId,
                        branchId,
                        customer.Id.Value,
                        PartyBranchGrantSource.CreateAtBranch,
                        grantedByActorId: null,
                        cancellationToken,
                        persistChanges: false)
                    .ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ApplicationResult<POSCustomer>.Success(customer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdatePOSCustomer
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdatePOSCustomer(IPOSCustomerRepository customers, IPosUnitOfWork unitOfWork, IClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<POSCustomer>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        string displayName,
        string? mobileNumber,
        string? address,
        string? notes,
        DateTimeOffset? expectedUpdatedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var customer = await _customers
            .GetByIdAsync(orgId, POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<POSCustomer>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        if (expectedUpdatedAtUtc is not null)
        {
            var expected = expectedUpdatedAtUtc.Value.ToUniversalTime();
            var actual = customer.UpdatedAtUtc.ToUniversalTime();
            if (expected.UtcTicks != actual.UtcTicks)
            {
                return ApplicationResult<POSCustomer>.Failure(
                    ApplicationErrorCodes.CustomerConcurrencyConflict,
                    "The customer was updated concurrently. Reload the latest version and try again.");
            }
        }

        try
        {
            var (_, normalizedMobile) = POSCustomer.NormalizeOptionalMobile(mobileNumber);
            if (normalizedMobile is not null
                && !string.Equals(normalizedMobile, customer.NormalizedMobile, StringComparison.Ordinal))
            {
                var existing = await _customers
                    .FindActiveByNormalizedMobileAsync(orgId, normalizedMobile, cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null && existing.Id != customer.Id)
                {
                    return ApplicationResult<POSCustomer>.Failure(
                        ApplicationErrorCodes.MobileConflict,
                        "An active customer with this mobile number already exists in this organization.");
                }
            }

            customer.UpdateProfile(displayName, mobileNumber, address, notes, _clock.UtcNow);
            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<POSCustomer>.Success(customer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeactivatePOSCustomer
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeactivatePOSCustomer(IPOSCustomerRepository customers, IPosUnitOfWork unitOfWork, IClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<POSCustomer>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customers
            .GetByIdAsync(PosOrganizationId.From(organizationId), POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<POSCustomer>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        try
        {
            customer.Deactivate(_clock.UtcNow);
            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<POSCustomer>.Success(customer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ReactivatePOSCustomer
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReactivatePOSCustomer(IPOSCustomerRepository customers, IPosUnitOfWork unitOfWork, IClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<POSCustomer>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var customer = await _customers
            .GetByIdAsync(orgId, POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<POSCustomer>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        try
        {
            if (customer.NormalizedMobile is not null)
            {
                var existing = await _customers
                    .FindActiveByNormalizedMobileAsync(orgId, customer.NormalizedMobile, cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null && existing.Id != customer.Id)
                {
                    return ApplicationResult<POSCustomer>.Failure(
                        ApplicationErrorCodes.MobileConflict,
                        "An active customer with this mobile number already exists in this organization.");
                }
            }

            customer.Reactivate(_clock.UtcNow);
            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<POSCustomer>.Success(customer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CorrelatePOSCustomerToPlatformBusinessCustomer
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CorrelatePOSCustomerToPlatformBusinessCustomer(
        IPOSCustomerRepository customers,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<POSCustomer>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        Guid platformBusinessCustomerId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var customer = await _customers
            .GetByIdAsync(orgId, POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<POSCustomer>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        try
        {
            var existing = await _customers
                .FindByPlatformBusinessCustomerIdAsync(orgId, platformBusinessCustomerId, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null && existing.Id != customer.Id)
            {
                return ApplicationResult<POSCustomer>.Failure(
                    ApplicationErrorCodes.PlatformBusinessCustomerCorrelationConflict,
                    "Another POS customer in this organization is already correlated to that Platform BusinessCustomer.");
            }

            if (customer.PlatformBusinessCustomerId == platformBusinessCustomerId)
            {
                return ApplicationResult<POSCustomer>.Success(customer);
            }

            customer.CorrelateToPlatformBusinessCustomer(platformBusinessCustomerId, _clock.UtcNow);
            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<POSCustomer>.Success(customer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ClearPOSCustomerPlatformCorrelation
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ClearPOSCustomerPlatformCorrelation(
        IPOSCustomerRepository customers,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<POSCustomer>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var customer = await _customers
            .GetByIdAsync(orgId, POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<POSCustomer>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        try
        {
            if (customer.PlatformBusinessCustomerId is null)
            {
                return ApplicationResult<POSCustomer>.Success(customer);
            }

            customer.ClearPlatformBusinessCustomerCorrelation(_clock.UtcNow);
            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<POSCustomer>.Success(customer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class LinkPOSCustomerPersonalExItsIdentity
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public LinkPOSCustomerPersonalExItsIdentity(
        IPOSCustomerRepository customers,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<POSCustomer>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        string personalPublicUserId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var customer = await _customers
            .GetByIdAsync(orgId, POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<POSCustomer>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        try
        {
            var existing = await _customers
                .FindByLinkedPersonalPublicUserIdAsync(orgId, personalPublicUserId, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null && existing.Id != customer.Id)
            {
                return ApplicationResult<POSCustomer>.Failure(
                    DomainErrorCodes.CustomerExItsIdentityLinkConflict,
                    "Another POS customer in this organization is already linked to that Personal ExItS identity.");
            }

            customer.LinkPersonalExItsIdentity(personalPublicUserId, _clock.UtcNow);
            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<POSCustomer>.Success(customer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class LinkPOSCustomerOrganizationExItsIdentity
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public LinkPOSCustomerOrganizationExItsIdentity(
        IPOSCustomerRepository customers,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<POSCustomer>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        Guid buyerOrganizationId,
        string buyerPublicOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var customer = await _customers
            .GetByIdAsync(orgId, POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<POSCustomer>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        try
        {
            var existing = await _customers
                .FindByLinkedBuyerOrganizationIdAsync(orgId, buyerOrganizationId, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null && existing.Id != customer.Id)
            {
                return ApplicationResult<POSCustomer>.Failure(
                    DomainErrorCodes.CustomerExItsIdentityLinkConflict,
                    "Another POS customer in this organization is already linked to that ExItS business identity.");
            }

            customer.LinkOrganizationExItsIdentity(buyerOrganizationId, buyerPublicOrganizationId, _clock.UtcNow);
            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<POSCustomer>.Success(customer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ClearPOSCustomerExItsIdentityLink
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ClearPOSCustomerExItsIdentityLink(
        IPOSCustomerRepository customers,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<POSCustomer>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var customer = await _customers
            .GetByIdAsync(orgId, POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<POSCustomer>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        try
        {
            if (customer.LinkedPersonalPublicUserId is null
                && customer.LinkedBuyerOrganizationId is null
                && customer.LinkedBuyerPublicOrganizationId is null)
            {
                return ApplicationResult<POSCustomer>.Success(customer);
            }

            customer.ClearExItsIdentityLink(_clock.UtcNow);
            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<POSCustomer>.Success(customer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
