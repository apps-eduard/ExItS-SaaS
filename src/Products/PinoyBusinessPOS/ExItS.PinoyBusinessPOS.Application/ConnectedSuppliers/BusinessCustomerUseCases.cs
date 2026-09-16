using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Parties;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Supplier-side projection of an accepted buyer OrganizationConnection.
/// Does not create a duplicate Organization or POSCustomer identity.
/// </summary>
public sealed record BusinessCustomerDto(
    Guid ConnectionId,
    Guid SupplierOrganizationId,
    Guid BuyerOrganizationId,
    string OrganizationDisplayName,
    string? OrganizationPublicId,
    string RelationshipStatus,
    string CatalogSharingMode,
    decimal? CustomerDiscountPercent,
    int EligibleCount,
    int SharedCount,
    int ExcludedCount,
    int OverrideCount,
    DateTimeOffset? ConnectedSinceUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    bool DisplayNameIsLive = false,
    string InitiatedByParty = "Buyer",
    bool ActionRequired = false,
    Guid? SupplierBranchId = null,
    string? SupplierBranchName = null,
    string ContactSource = "Custom",
    Guid? OrganizationMemberId = null,
    bool? OrganizationMemberAvailable = null,
    string? ContactPersonName = null,
    string? ContactDepartment = null,
    string? ContactRole = null,
    string? ContactPhone = null,
    string? ContactEmail = null,
    string? PreferredContactMethod = null,
    string? DeliveryInstructions = null,
    string? BillingContactNotes = null,
    string? InternalNotes = null,
    /// <summary>inherit | allow | block</summary>
    string CustomerDeliveryOverride = "inherit",
    bool OrgOfferDelivery = false,
    bool EffectiveDeliveryAllowed = false);

/// <summary>Seller-owned relationship contact update (does not modify buyer Organization identity).</summary>
public sealed record UpdateBusinessCustomerRelationshipContactRequest(
    string ContactSource,
    Guid? OrganizationMemberId,
    string? ContactPersonName,
    string? ContactDepartment,
    string? ContactRole,
    string? ContactPhone,
    string? ContactEmail,
    string? PreferredContactMethod,
    string? DeliveryInstructions,
    string? BillingContactNotes,
    string? InternalNotes,
    DateTimeOffset ExpectedUpdatedAtUtc);

/// <summary>
/// Identity display policy for Business Customer list and detail.
/// Platform has no batch public-organization resolver; per-row live resolve would N+1.
/// Therefore both surfaces use relationship snapshot as the primary identity.
/// Live list enrichment is deferred until a safe batch Platform mechanism exists.
/// </summary>
public static class BusinessCustomerIdentityDisplay
{
    public const string Policy = "SNAPSHOT_CONSISTENT";
    public const string LiveListIdentity = "DEFERRED_NO_BATCH_RESOLVER";
}

/// <summary>
/// Lists supplier Business Customers = Active and Pending buyer relationships
/// (optionally Disconnected/Declined history). Catalog aggregates are batch-loaded.
/// Primary identity = relationship buyer snapshot (same as detail).
/// Default visibility = home supplier branch only (Main does not auto-share).
/// </summary>
public sealed class ListBusinessCustomers
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IPartyBranchAccessActorAccessor? _actorAccessor;

    public ListBusinessCustomers(
        IConnectedSupplierRelationshipRepository relationships,
        IConnectedBuyerProductShareRepository shares,
        IPosCommercialAccessAccessor access,
        IPartyBranchAccessActorAccessor? actorAccessor = null)
    {
        _relationships = relationships;
        _shares = shares;
        _access = access;
        _actorAccessor = actorAccessor;
    }

    public async Task<ApplicationResult<IReadOnlyList<BusinessCustomerDto>>> ExecuteAsync(
        Guid orgId,
        string? search = null,
        bool includeDisconnected = false,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<BusinessCustomerDto>>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        var supplier = PosOrganizationId.From(orgId);
        var rows = await _relationships.ListAsync(supplier, supplierView: true, ct).ConfigureAwait(false);
        var filtered = rows
            .Where(r =>
                r.Status == ConnectedSupplierRelationshipStatus.Active
                || r.Status == ConnectedSupplierRelationshipStatus.Pending
                || (includeDisconnected
                    && (r.Status == ConnectedSupplierRelationshipStatus.Disconnected
                        || r.Status == ConnectedSupplierRelationshipStatus.Declined)))
            .ToList();

        filtered = ApplyHomeBranchVisibility(filtered);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered
                .Where(r => MatchesSearch(r, term))
                .ToList();
        }

        filtered = filtered
            .OrderBy(r => r.Status == ConnectedSupplierRelationshipStatus.Active ? 0
                : r.Status == ConnectedSupplierRelationshipStatus.Pending ? 1 : 2)
            .ThenBy(r => r.BuyerDisplayNameSnapshot ?? r.BuyerPublicOrganizationIdSnapshot ?? string.Empty)
            .ToList();

        var eligibleCount = filtered.Count == 0
            ? 0
            : await _shares.CountEligibleSupplierProductsAsync(supplier, ct).ConfigureAwait(false);

        var stats = filtered.Count == 0
            ? new Dictionary<Guid, BuyerRelationshipShareStats>()
            : await _shares.ListShareStatsByRelationshipsAsync(
                    filtered.Select(r => r.Id.Value).ToList(),
                    ct)
                .ConfigureAwait(false);

        var result = filtered
            .Select(r => MapFromSnapshot(
                r,
                eligibleCount,
                stats.GetValueOrDefault(r.Id.Value, new BuyerRelationshipShareStats(0, 0, 0))))
            .ToList();

        return ApplicationResult<IReadOnlyList<BusinessCustomerDto>>.Success(result);
    }

    private List<ConnectedSupplierRelationship> ApplyHomeBranchVisibility(
        List<ConnectedSupplierRelationship> rows)
    {
        if (_actorAccessor is null)
        {
            return rows;
        }

        var actor = _actorAccessor.GetActor();
        var organizationWide =
            actor.IsOrganizationGovernance
            && (actor.ActingBranchId is null || actor.ActingBranchId == Guid.Empty);

        return rows
            .Where(r => SupplierConnectionBranchRouting.IsVisibleAtSupplierBranch(
                r.SupplierBranchId,
                r.SharedSupplierBranchIds,
                actor.ActingBranchId,
                organizationWide))
            .ToList();
    }

    /// <summary>
    /// Search matches snapshot display name and public ORG id — the identity users see.
    /// </summary>
    private static bool MatchesSearch(ConnectedSupplierRelationship r, string term)
    {
        var haystack = string.Join(
            ' ',
            r.BuyerDisplayNameSnapshot ?? string.Empty,
            r.BuyerPublicOrganizationIdSnapshot ?? string.Empty,
            r.Status.ToString());
        return haystack.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Maps a relationship to the Business Customer DTO using snapshot identity only.
    /// Does not call Platform; DisplayNameIsLive is always false under SNAPSHOT_CONSISTENT.
    /// </summary>
    internal static BusinessCustomerDto MapFromSnapshot(
        ConnectedSupplierRelationship r,
        int eligibleCount,
        BuyerRelationshipShareStats stats) =>
        Map(r, eligibleCount, stats, displayNameIsLive: false);

    /// <summary>
    /// Shared mapper. Live name/public id parameters remain for unit-test isolation of Map
    /// semantics; production list/detail always pass null / DisplayNameIsLive=false.
    /// </summary>
    internal static BusinessCustomerDto Map(
        ConnectedSupplierRelationship r,
        int eligibleCount,
        BuyerRelationshipShareStats stats,
        bool displayNameIsLive,
        string? liveDisplayName = null,
        string? livePublicId = null,
        bool? organizationMemberAvailable = null) =>
        MapCore(r, eligibleCount, stats, displayNameIsLive, liveDisplayName, livePublicId, organizationMemberAvailable);

    private static BusinessCustomerDto MapCore(
        ConnectedSupplierRelationship r,
        int eligibleCount,
        BuyerRelationshipShareStats stats,
        bool displayNameIsLive,
        string? liveDisplayName,
        string? livePublicId,
        bool? organizationMemberAvailable)
    {
        var sharedCount = r.CatalogSharingMode == CatalogSharingMode.AllEligible
            ? Math.Max(0, eligibleCount - stats.ExcludedCount)
            : stats.ExplicitSharedCount;

        var actionRequired = r.Status == ConnectedSupplierRelationshipStatus.Pending
            && r.IsRecipient(r.SupplierOrganizationId);

        // ConnectedSince is only meaningful after acceptance — RespondedAtUtc only (never CreatedAtUtc).
        DateTimeOffset? connectedSinceUtc = r.Status == ConnectedSupplierRelationshipStatus.Active
            ? r.RespondedAtUtc
            : null;

        return new BusinessCustomerDto(
            r.Id.Value,
            r.SupplierOrganizationId.Value,
            r.BuyerOrganizationId.Value,
            !string.IsNullOrWhiteSpace(liveDisplayName)
                ? liveDisplayName.Trim()
                : (string.IsNullOrWhiteSpace(r.BuyerDisplayNameSnapshot)
                    ? (r.BuyerPublicOrganizationIdSnapshot ?? string.Empty)
                    : r.BuyerDisplayNameSnapshot!),
            livePublicId ?? r.BuyerPublicOrganizationIdSnapshot,
            r.Status.ToString(),
            r.CatalogSharingMode.ToString(),
            r.CustomerDiscountPercent,
            eligibleCount,
            sharedCount,
            stats.ExcludedCount,
            stats.OverrideCount,
            connectedSinceUtc,
            r.CreatedAtUtc,
            r.UpdatedAtUtc,
            displayNameIsLive,
            r.InitiatedByParty.ToString(),
            actionRequired,
            r.SupplierBranchId,
            r.SupplierBranchNameSnapshot,
            r.ContactSource.ToString(),
            r.OrganizationMemberId,
            organizationMemberAvailable,
            r.ContactPersonName,
            r.ContactDepartment,
            r.ContactRole,
            r.ContactPhone,
            r.ContactEmail,
            r.PreferredContactMethod,
            r.DeliveryInstructions,
            r.BillingContactNotes,
            r.InternalNotes,
            CustomerDeliveryOverride: r.CustomerDeliveryOverride switch
            {
                CustomerDeliveryOverride.Allow => "allow",
                CustomerDeliveryOverride.Block => "block",
                _ => "inherit",
            });
    }
}

/// <summary>
/// Supplier Business Customer detail for one connection.
/// Uses the same snapshot identity policy as <see cref="ListBusinessCustomers"/> —
/// no per-detail Platform live resolve (avoids list/detail asymmetry and N+1 if applied to lists).
/// </summary>
public sealed class GetBusinessCustomer
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IPartyBranchAccessActorAccessor? _actorAccessor;
    private readonly IConnectedBuyerBusinessContactDirectory? _contacts;
    private readonly ConnectedSupplierCommerceReadinessService? _commerceReadiness;
    private readonly IOrganizationFulfillmentSettingsRepository? _fulfillmentSettings;

    public GetBusinessCustomer(
        IConnectedSupplierRelationshipRepository relationships,
        IConnectedBuyerProductShareRepository shares,
        IPosCommercialAccessAccessor access,
        IPartyBranchAccessActorAccessor? actorAccessor = null,
        IConnectedBuyerBusinessContactDirectory? contacts = null,
        ConnectedSupplierCommerceReadinessService? commerceReadiness = null,
        IOrganizationFulfillmentSettingsRepository? fulfillmentSettings = null)
    {
        _relationships = relationships;
        _shares = shares;
        _access = access;
        _actorAccessor = actorAccessor;
        _contacts = contacts;
        _commerceReadiness = commerceReadiness;
        _fulfillmentSettings = fulfillmentSettings;
    }

    public async Task<ApplicationResult<BusinessCustomerDto>> ExecuteAsync(
        Guid orgId,
        Guid connectionId,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        var r = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(connectionId), ct)
            .ConfigureAwait(false);
        var supplier = PosOrganizationId.From(orgId);
        if (r is null || r.SupplierOrganizationId != supplier)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(
                ConnectedSupplierErrorCodes.NotFound,
                "Business customer relationship was not found.");
        }

        if (_actorAccessor is not null)
        {
            var actor = _actorAccessor.GetActor();
            var organizationWide =
                actor.IsOrganizationGovernance
                && (actor.ActingBranchId is null || actor.ActingBranchId == Guid.Empty);
            if (!SupplierConnectionBranchRouting.IsVisibleAtSupplierBranch(
                    r.SupplierBranchId,
                    r.SharedSupplierBranchIds,
                    actor.ActingBranchId,
                    organizationWide))
            {
                return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(
                    ConnectedSupplierErrorCodes.NotFound,
                    "Business customer relationship was not found.");
            }
        }

        var eligibleCount = await _shares.CountEligibleSupplierProductsAsync(supplier, ct)
            .ConfigureAwait(false);
        var statsMap = await _shares.ListShareStatsByRelationshipsAsync([r.Id.Value], ct)
            .ConfigureAwait(false);
        var stats = statsMap.GetValueOrDefault(r.Id.Value, new BuyerRelationshipShareStats(0, 0, 0));

        bool? memberAvailable = null;
        if (r.ContactSource == RelationshipContactSource.OrganizationMember
            && r.OrganizationMemberId is Guid memberId
            && r.Status == ConnectedSupplierRelationshipStatus.Active
            && _contacts is not null)
        {
            var live = await _contacts
                .GetAsync(r.BuyerOrganizationId.Value, supplier.Value, memberId, ct)
                .ConfigureAwait(false);
            memberAvailable = live.IsSuccess && live.Value is not null;
            if (memberAvailable == true && live.Value is { } contact)
            {
                // Refresh authoritative display snapshots for active linked members (do not persist here).
                var refreshed = ListBusinessCustomers.Map(
                        r,
                        eligibleCount,
                        stats,
                        displayNameIsLive: false,
                        organizationMemberAvailable: true)
                    with
                    {
                        ContactPersonName = contact.DisplayName,
                        ContactDepartment = contact.Department,
                        ContactRole = contact.RoleTitle,
                        ContactPhone = contact.Phone,
                        ContactEmail = contact.Email
                    };
                return ApplicationResult<BusinessCustomerDto>.Success(
                    await EnrichDeliveryAsync(r, refreshed, ct).ConfigureAwait(false));
            }
        }
        else if (r.ContactSource == RelationshipContactSource.OrganizationMember)
        {
            memberAvailable = false;
        }

        var dto = ListBusinessCustomers.Map(
            r,
            eligibleCount,
            stats,
            displayNameIsLive: false,
            organizationMemberAvailable: memberAvailable);

        return ApplicationResult<BusinessCustomerDto>.Success(
            await EnrichDeliveryAsync(r, dto, ct).ConfigureAwait(false));
    }

    private async Task<BusinessCustomerDto> EnrichDeliveryAsync(
        ConnectedSupplierRelationship r,
        BusinessCustomerDto dto,
        CancellationToken ct)
    {
        var orgOffer = false;
        if (_fulfillmentSettings is not null)
        {
            var settings = await _fulfillmentSettings
                .GetAsync(r.SupplierOrganizationId, ct)
                .ConfigureAwait(false);
            orgOffer = settings?.OfferDelivery == true;
        }

        var readyBranch = false;
        if (_commerceReadiness is not null)
        {
            var evaluated = await _commerceReadiness.EvaluateAsync(r, ct).ConfigureAwait(false);
            readyBranch = evaluated.SupportedFulfillmentMethods.Contains(
                ConnectedSupplierCommerceReadiness.FulfillmentDelivery,
                StringComparer.OrdinalIgnoreCase);
            if (!readyBranch && orgOffer)
            {
                readyBranch = evaluated.Requirements.Any(req =>
                    req.Code == ConnectedSupplierCommerceReadiness.DeliveryConfig
                    && req.Status == ConnectedSupplierCommerceReadiness.StatusComplete);
            }
        }

        var effective = EffectiveDeliveryAllowance.IsAllowed(
            orgOffer,
            readyBranch,
            r.CustomerDeliveryOverride);

        return dto with
        {
            OrgOfferDelivery = orgOffer,
            EffectiveDeliveryAllowed = effective,
        };
    }
}

/// <summary>
/// Lists privacy-safe buyer Organization contacts for Connected relationships only.
/// Pending relationships are refused (Custom contact only).
/// </summary>
public sealed class ListBusinessCustomerOrganizationContacts
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IConnectedBuyerBusinessContactDirectory _contacts;

    public ListBusinessCustomerOrganizationContacts(
        IConnectedSupplierRelationshipRepository relationships,
        IPosCommercialAccessAccessor access,
        IConnectedBuyerBusinessContactDirectory contacts)
    {
        _relationships = relationships;
        _access = access;
        _contacts = contacts;
    }

    public async Task<ApplicationResult<IReadOnlyList<BuyerOrganizationBusinessContactDto>>> ExecuteAsync(
        Guid orgId,
        Guid connectionId,
        string? search = null,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<BuyerOrganizationBusinessContactDto>>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        var r = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(connectionId), ct)
            .ConfigureAwait(false);
        var supplier = PosOrganizationId.From(orgId);
        if (r is null || r.SupplierOrganizationId != supplier)
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<BuyerOrganizationBusinessContactDto>>(
                ConnectedSupplierErrorCodes.NotFound,
                "Business customer relationship was not found.");
        }

        if (r.Status != ConnectedSupplierRelationshipStatus.Active)
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<BuyerOrganizationBusinessContactDto>>(
                ConnectedSupplierErrorCodes.OrganizationContactNotConnected,
                "Organization staff contacts are only available after the connection is accepted.");
        }

        return await _contacts
            .ListAsync(r.BuyerOrganizationId.Value, supplier.Value, search, ct)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Updates seller-owned relationship contact fields only.
/// Never modifies buyer display name / public organization id snapshots,
/// credit approval, relationship status, or branch access.
/// </summary>
public sealed class UpdateBusinessCustomerRelationshipContact
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IConnectedBuyerBusinessContactDirectory? _contacts;
    private readonly TimeProvider _clock;

    public UpdateBusinessCustomerRelationshipContact(
        IConnectedSupplierRelationshipRepository relationships,
        IConnectedBuyerProductShareRepository shares,
        IPosUnitOfWork uow,
        IPosCommercialAccessAccessor access,
        IConnectedBuyerBusinessContactDirectory? contacts = null,
        TimeProvider? clock = null)
    {
        _relationships = relationships;
        _shares = shares;
        _uow = uow;
        _access = access;
        _contacts = contacts;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<BusinessCustomerDto>> ExecuteAsync(
        Guid orgId,
        Guid connectionId,
        UpdateBusinessCustomerRelationshipContactRequest request,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        var r = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(connectionId), ct)
            .ConfigureAwait(false);
        var supplier = PosOrganizationId.From(orgId);
        if (r is null || r.SupplierOrganizationId != supplier)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(
                ConnectedSupplierErrorCodes.NotFound,
                "Business customer relationship was not found.");
        }

        if (r.Status is not (ConnectedSupplierRelationshipStatus.Pending or ConnectedSupplierRelationshipStatus.Active))
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(
                ConnectedSupplierErrorCodes.RelationshipInactive,
                "Relationship contact can only be edited while Pending or Active.");
        }

        if (r.UpdatedAtUtc != request.ExpectedUpdatedAtUtc)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(
                ConnectedSupplierErrorCodes.ConcurrencyConflict,
                "Business customer was modified by another request. Refresh and retry.");
        }

        if (!TryParseContactSource(request.ContactSource, out var source))
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(
                ConnectedSupplierErrorCodes.OrganizationContactInvalid,
                "Contact source must be Custom or OrganizationMember.");
        }

        string? person = request.ContactPersonName;
        string? department = request.ContactDepartment;
        string? role = request.ContactRole;
        string? phone = request.ContactPhone;
        string? email = request.ContactEmail;
        Guid? memberId = request.OrganizationMemberId;

        if (source == RelationshipContactSource.OrganizationMember)
        {
            if (r.Status != ConnectedSupplierRelationshipStatus.Active)
            {
                return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(
                    ConnectedSupplierErrorCodes.OrganizationContactNotConnected,
                    "Organization staff contacts are only available after the connection is accepted.");
            }

            if (memberId is null || memberId == Guid.Empty || _contacts is null)
            {
                return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(
                    ConnectedSupplierErrorCodes.OrganizationContactInvalid,
                    "Select an organization contact from the connected buyer.");
            }

            var live = await _contacts
                .GetAsync(r.BuyerOrganizationId.Value, supplier.Value, memberId.Value, ct)
                .ConfigureAwait(false);
            if (!live.IsSuccess)
            {
                return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(
                    live.ErrorCode ?? ConnectedSupplierErrorCodes.OrganizationContactInvalid,
                    live.ErrorMessage ?? "Could not validate organization contact.");
            }

            if (live.Value is null)
            {
                return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(
                    ConnectedSupplierErrorCodes.OrganizationContactInvalid,
                    "Selected organization contact is not eligible or does not belong to the buyer organization.");
            }

            // Authoritative identity fields — ignore client-supplied person/role/phone/email for org members.
            person = live.Value.DisplayName;
            department = live.Value.Department;
            role = live.Value.RoleTitle;
            phone = live.Value.Phone;
            email = live.Value.Email;
        }
        else
        {
            memberId = null;
        }

        try
        {
            r.UpdateRelationshipContact(
                source,
                memberId,
                person,
                department,
                role,
                phone,
                email,
                request.PreferredContactMethod,
                request.DeliveryInstructions,
                request.BillingContactNotes,
                request.InternalNotes,
                _clock.GetUtcNow());
            await _relationships.UpdateAsync(r, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(ex.ErrorCode, ex.Message);
        }

        var eligibleCount = await _shares.CountEligibleSupplierProductsAsync(supplier, ct)
            .ConfigureAwait(false);
        var statsMap = await _shares.ListShareStatsByRelationshipsAsync([r.Id.Value], ct)
            .ConfigureAwait(false);
        var stats = statsMap.GetValueOrDefault(r.Id.Value, new BuyerRelationshipShareStats(0, 0, 0));

        return ApplicationResult<BusinessCustomerDto>.Success(
            ListBusinessCustomers.Map(
                r,
                eligibleCount,
                stats,
                displayNameIsLive: false,
                organizationMemberAvailable: source == RelationshipContactSource.OrganizationMember
                    ? true
                    : null));
    }

    private static bool TryParseContactSource(string? raw, out RelationshipContactSource source)
    {
        if (string.IsNullOrWhiteSpace(raw)
            || string.Equals(raw, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            source = RelationshipContactSource.Custom;
            return true;
        }

        if (string.Equals(raw, "OrganizationMember", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "OrganizationStaff", StringComparison.OrdinalIgnoreCase))
        {
            source = RelationshipContactSource.OrganizationMember;
            return true;
        }

        source = RelationshipContactSource.Custom;
        return false;
    }
}
