using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Parties;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
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
    string? LinkedBuyerPublicOrganizationId = null,
    string? PartyKind = null);

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
    private readonly IConnectedSupplierRelationshipRepository _relationships;

    public POSCustomerQueryService(
        IPOSCustomerRepository customers,
        PartyBranchAccessService branchAccess,
        IPartyBranchAccessActorAccessor actorAccessor,
        IConnectedSupplierRelationshipRepository relationships)
    {
        _customers = customers;
        _branchAccess = branchAccess;
        _actorAccessor = actorAccessor;
        _relationships = relationships;
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
            CheckoutCustomerSearchItemDto.KindCustomer,
            customer.DisplayName,
            customer.Status.ToString(),
            customer.Id.Value,
            customer.MobileNumber);
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
    /// Narrow Active-only checkout counterparty search for CreateSale (pageSize capped at 20).
    /// Includes POS people and Active B2B Organization relationships (no ViewSuppliers required).
    /// <paramref name="kind"/>: All | Customer | Business (default All).
    /// Blank search is allowed for Business (Active directory) and rejected for Customer/All.
    /// </summary>
    public async Task<ApplicationResult<CheckoutCustomerSearchResult>> SearchForCheckoutAsync(
        Guid organizationId,
        string? search,
        int? page,
        int? pageSize,
        string? kind = null,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(pageSize ?? 20, 1, 20);
        var pageNumber = Math.Max(page ?? 1, 1);
        var skip = (pageNumber - 1) * take;
        var normalizedKind = NormalizeCheckoutSearchKind(kind);
        var term = search?.Trim() ?? string.Empty;
        var hasTerm = term.Length > 0;

        if (!hasTerm
            && normalizedKind is CheckoutCustomerSearchItemDto.KindCustomer or "All")
        {
            return ApplicationResult<CheckoutCustomerSearchResult>.Failure(
                ApplicationErrorCodes.CheckoutCustomerSearchRequired,
                "Checkout customer search requires a non-blank search term.");
        }

        var includePeople = normalizedKind is "All" or CheckoutCustomerSearchItemDto.KindCustomer;
        var includeBusiness = normalizedKind is "All" or CheckoutCustomerSearchItemDto.KindBusiness;

        var merged = new List<CheckoutCustomerSearchItemDto>();
        var peopleTotal = 0;
        var businessTotal = 0;

        if (includePeople && hasTerm)
        {
            var restrict = await _branchAccess
                .FilterCustomerIdsAccessibleAsync(organizationId, Actor, cancellationToken)
                .ConfigureAwait(false);
            // Fetch a page-sized window; merge with businesses then re-page.
            var (items, total) = await _customers
                .ListAsync(
                    PosOrganizationId.From(organizationId),
                    CustomerStatus.Active,
                    term,
                    0,
                    take,
                    restrict,
                    cancellationToken)
                .ConfigureAwait(false);
            peopleTotal = total;
            merged.AddRange(items
                .Where(c => !includeBusiness || !IsCheckoutBusinessParty(c))
                .Select(c => new CheckoutCustomerSearchItemDto(
                    CheckoutCustomerSearchItemDto.KindCustomer,
                    c.DisplayName,
                    c.Status.ToString(),
                    c.Id.Value,
                    c.MobileNumber)));
        }

        if (includeBusiness)
        {
            var supplier = PosOrganizationId.From(organizationId);
            var rows = await _relationships
                .ListAsync(supplier, supplierView: true, cancellationToken)
                .ConfigureAwait(false);
            var businesses = rows
                .Where(r =>
                    r.Status == ConnectedSupplierRelationshipStatus.Active
                    || r.Status == ConnectedSupplierRelationshipStatus.Pending)
                .Where(r => !hasTerm || MatchesBusinessSearch(r, term))
                .OrderBy(r => r.Status == ConnectedSupplierRelationshipStatus.Active ? 0 : 1)
                .ThenBy(r => r.BuyerDisplayNameSnapshot ?? r.BuyerPublicOrganizationIdSnapshot ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
            var connectedBuyerIds = businesses
                .Select(r => r.BuyerOrganizationId.Value)
                .ToHashSet();
            merged.AddRange(businesses.Select(MapBusinessCheckoutItem));

            // Legacy ORG-linked POS Business party rows without an open B2B relationship.
            // Do not treat them as Active B2B; keep for historical Cash attach only when no
            // Active/Pending ConnectedSupplierRelationship exists for that buyer org.
            var restrict = await _branchAccess
                .FilterCustomerIdsAccessibleAsync(organizationId, Actor, cancellationToken)
                .ConfigureAwait(false);
            var (posItems, _) = await _customers
                .ListAsync(
                    supplier,
                    CustomerStatus.Active,
                    hasTerm ? term : null,
                    0,
                    200,
                    restrict,
                    cancellationToken)
                .ConfigureAwait(false);
            var posBusiness = posItems
                .Where(IsCheckoutBusinessParty)
                .Where(c =>
                    c.LinkedBuyerOrganizationId is null
                    || !connectedBuyerIds.Contains(c.LinkedBuyerOrganizationId.Value))
                .Where(c => !hasTerm || MatchesPosBusinessSearch(c, term))
                .OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            businessTotal = businesses.Count + posBusiness.Count;
            merged.AddRange(posBusiness.Select(MapPosBusinessCheckoutItem));
        }

        var ordered = merged
            .OrderBy(x =>
                x.Kind == CheckoutCustomerSearchItemDto.KindBusiness
                && string.Equals(x.Status, "Active", StringComparison.OrdinalIgnoreCase)
                    ? 0
                    : x.Kind == CheckoutCustomerSearchItemDto.KindBusiness
                      && string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                        ? 1
                        : 2)
            .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var pageItems = ordered.Skip(skip).Take(take).ToList();
        var totalCount = includePeople && includeBusiness
            ? peopleTotal + businessTotal
            : includePeople
                ? peopleTotal
                : businessTotal;

        return ApplicationResult<CheckoutCustomerSearchResult>.Success(
            new CheckoutCustomerSearchResult(pageItems, totalCount, pageNumber, take));
    }

    private static string NormalizeCheckoutSearchKind(string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            return "All";
        }

        var trimmed = kind.Trim();
        if (trimmed.Equals(CheckoutCustomerSearchItemDto.KindCustomer, StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("People", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("Person", StringComparison.OrdinalIgnoreCase))
        {
            return CheckoutCustomerSearchItemDto.KindCustomer;
        }

        if (trimmed.Equals(CheckoutCustomerSearchItemDto.KindBusiness, StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("Businesses", StringComparison.OrdinalIgnoreCase))
        {
            return CheckoutCustomerSearchItemDto.KindBusiness;
        }

        return "All";
    }

    private static bool MatchesBusinessSearch(ConnectedSupplierRelationship r, string term)
    {
        var haystack = string.Join(
            ' ',
            r.BuyerDisplayNameSnapshot ?? string.Empty,
            r.BuyerPublicOrganizationIdSnapshot ?? string.Empty);
        return haystack.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCheckoutBusinessParty(POSCustomer customer) =>
        customer.PartyKind == CustomerPartyKind.Business
        || customer.LinkedBuyerOrganizationId is not null;

    private static bool MatchesPosBusinessSearch(POSCustomer customer, string term)
    {
        var haystack = string.Join(
            ' ',
            customer.DisplayName,
            customer.LinkedBuyerPublicOrganizationId ?? string.Empty,
            customer.MobileNumber ?? string.Empty);
        return haystack.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static CheckoutCustomerSearchItemDto MapBusinessCheckoutItem(ConnectedSupplierRelationship r) =>
        new(
            CheckoutCustomerSearchItemDto.KindBusiness,
            string.IsNullOrWhiteSpace(r.BuyerDisplayNameSnapshot)
                ? (r.BuyerPublicOrganizationIdSnapshot ?? "Business")
                : r.BuyerDisplayNameSnapshot!,
            r.Status.ToString(),
            CustomerId: null,
            MobileNumber: null,
            ConnectionId: r.Id.Value,
            BuyerOrganizationId: r.BuyerOrganizationId.Value,
            BuyerPublicOrganizationId: r.BuyerPublicOrganizationIdSnapshot,
            PartyKind: null,
            InitiatedByParty: r.InitiatedByParty.ToString());

    /// <summary>
    /// POS Business party for checkout attach (Cash/GCash via customerId). Not Direct B2B Organization
    /// party — that requires <see cref="MapBusinessCheckoutItem"/> (Active connection).
    /// </summary>
    private static CheckoutCustomerSearchItemDto MapPosBusinessCheckoutItem(POSCustomer customer) =>
        new(
            CheckoutCustomerSearchItemDto.KindCustomer,
            customer.DisplayName,
            customer.Status.ToString(),
            customer.Id.Value,
            customer.MobileNumber,
            ConnectionId: null,
            BuyerOrganizationId: customer.LinkedBuyerOrganizationId,
            BuyerPublicOrganizationId: customer.LinkedBuyerPublicOrganizationId,
            PartyKind: CustomerPartyKind.Business.ToString());

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
            customer.LinkedBuyerPublicOrganizationId,
            customer.PartyKind.ToString());
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
        string? partyKind = null,
        Guid? linkedBuyerOrganizationId = null,
        string? linkedBuyerPublicOrganizationId = null,
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

            if (linkedBuyerOrganizationId is Guid buyerOrgId && buyerOrgId != Guid.Empty)
            {
                var existingBuyer = await _customers
                    .FindByLinkedBuyerOrganizationIdAsync(orgId, buyerOrgId, cancellationToken)
                    .ConfigureAwait(false);
                if (existingBuyer is not null)
                {
                    return ApplicationResult<POSCustomer>.Success(existingBuyer);
                }
            }

            CustomerPartyKind resolvedPartyKind = CustomerPartyKind.Person;
            if (!string.IsNullOrWhiteSpace(partyKind))
            {
                if (!Enum.TryParse(partyKind.Trim(), ignoreCase: true, out resolvedPartyKind)
                    || !Enum.IsDefined(resolvedPartyKind))
                {
                    return ApplicationResult<POSCustomer>.Failure(
                        DomainErrorCodes.InvalidCustomerPartyKind,
                        "Party kind must be Person or Business.");
                }
            }

            if (linkedBuyerOrganizationId is not null
                || !string.IsNullOrWhiteSpace(linkedBuyerPublicOrganizationId))
            {
                resolvedPartyKind = CustomerPartyKind.Business;
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
                    linkedPersonalPublicUserId: linkedPersonalPublicUserId,
                    linkedBuyerOrganizationId: linkedBuyerOrganizationId,
                    linkedBuyerPublicOrganizationId: linkedBuyerPublicOrganizationId,
                    partyKind: resolvedPartyKind)
                : POSCustomer.Create(
                    orgId,
                    displayName,
                    _clock.UtcNow,
                    mobileNumber,
                    address,
                    notes,
                    id: POSCustomerId.From(clientCustomerId.Value),
                    platformBusinessCustomerId: platformBusinessCustomerId,
                    linkedPersonalPublicUserId: linkedPersonalPublicUserId,
                    linkedBuyerOrganizationId: linkedBuyerOrganizationId,
                    linkedBuyerPublicOrganizationId: linkedBuyerPublicOrganizationId,
                    partyKind: resolvedPartyKind);

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
