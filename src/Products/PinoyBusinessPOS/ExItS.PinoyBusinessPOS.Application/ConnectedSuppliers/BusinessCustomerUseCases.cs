using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
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
    bool DisplayNameIsLive = false);

/// <summary>
/// Lists supplier Business Customers = Active (or optionally disconnected) buyer relationships.
/// Catalog aggregates are batch-loaded (no per-row N+1).
/// </summary>
public sealed class ListBusinessCustomers
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly IPosCommercialAccessAccessor _access;

    public ListBusinessCustomers(
        IConnectedSupplierRelationshipRepository relationships,
        IConnectedBuyerProductShareRepository shares,
        IPosCommercialAccessAccessor access)
    {
        _relationships = relationships;
        _shares = shares;
        _access = access;
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
                || (includeDisconnected && r.Status == ConnectedSupplierRelationshipStatus.Disconnected))
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered
                .Where(r => MatchesSearch(r, term))
                .ToList();
        }

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
            .Select(r => Map(
                r,
                eligibleCount,
                stats.GetValueOrDefault(r.Id.Value, new BuyerRelationshipShareStats(0, 0, 0)),
                displayNameIsLive: false))
            .ToList();

        return ApplicationResult<IReadOnlyList<BusinessCustomerDto>>.Success(result);
    }

    private static bool MatchesSearch(ConnectedSupplierRelationship r, string term)
    {
        var haystack = string.Join(
            ' ',
            r.BuyerDisplayNameSnapshot ?? string.Empty,
            r.BuyerPublicOrganizationIdSnapshot ?? string.Empty,
            r.Status.ToString());
        return haystack.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    internal static BusinessCustomerDto Map(
        ConnectedSupplierRelationship r,
        int eligibleCount,
        BuyerRelationshipShareStats stats,
        bool displayNameIsLive,
        string? liveDisplayName = null,
        string? livePublicId = null)
    {
        var sharedCount = r.CatalogSharingMode == CatalogSharingMode.AllEligible
            ? Math.Max(0, eligibleCount - stats.ExcludedCount)
            : stats.ExplicitSharedCount;

        // OverrideCount for SelectedOnly = shared rows with price; for AllEligible also count
        // overrides on still-shared (non-excluded) rows — stats.OverrideCount already filters IsShared.
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
            r.RespondedAtUtc ?? r.CreatedAtUtc,
            r.CreatedAtUtc,
            r.UpdatedAtUtc,
            displayNameIsLive);
    }
}

/// <summary>
/// Supplier Business Customer detail for one connection. Prefers live Platform public identity
/// for display name when the buyer's public ORG id is known (no duplicate Customer entity).
/// </summary>
public sealed class GetBusinessCustomer
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly IPlatformOrganizationPublicResolve _platformOrgs;
    private readonly IPosCommercialAccessAccessor _access;

    public GetBusinessCustomer(
        IConnectedSupplierRelationshipRepository relationships,
        IConnectedBuyerProductShareRepository shares,
        IPlatformOrganizationPublicResolve platformOrgs,
        IPosCommercialAccessAccessor access)
    {
        _relationships = relationships;
        _shares = shares;
        _platformOrgs = platformOrgs;
        _access = access;
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

        var eligibleCount = await _shares.CountEligibleSupplierProductsAsync(supplier, ct)
            .ConfigureAwait(false);
        var statsMap = await _shares.ListShareStatsByRelationshipsAsync([r.Id.Value], ct)
            .ConfigureAwait(false);
        var stats = statsMap.GetValueOrDefault(r.Id.Value, new BuyerRelationshipShareStats(0, 0, 0));

        string? liveName = null;
        string? livePublicId = null;
        var isLive = false;
        if (!string.IsNullOrWhiteSpace(r.BuyerPublicOrganizationIdSnapshot))
        {
            var live = await _platformOrgs
                .ResolveOrganizationForConnectedSupplierAsync(r.BuyerPublicOrganizationIdSnapshot!, ct)
                .ConfigureAwait(false);
            if (live.IsSuccess
                && live.Value is not null
                && live.Value.OrganizationId == r.BuyerOrganizationId.Value)
            {
                liveName = live.Value.DisplayName;
                livePublicId = live.Value.PublicOrganizationId;
                isLive = true;
            }
        }

        return ApplicationResult<BusinessCustomerDto>.Success(
            ListBusinessCustomers.Map(r, eligibleCount, stats, isLive, liveName, livePublicId));
    }
}
