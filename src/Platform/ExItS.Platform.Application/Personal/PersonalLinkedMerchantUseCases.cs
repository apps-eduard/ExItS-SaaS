using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Personal;

public sealed record LinkedMerchantDto(
    Guid LinkedCustomerId,
    Guid BusinessCustomerId,
    Guid OrganizationId,
    string OrganizationDisplayName,
    string CustomerDisplayName,
    string LinkStatus,
    DateTimeOffset LinkedAtUtc,
    bool CanCustomerOrder = false,
    bool CanCustomerDelivery = false);

/// <summary>Authoritative seller ordering capability for a Personal linked merchant.</summary>
public sealed record LinkedMerchantOrderingCapabilityDto(
    Guid OrganizationId,
    bool CanCustomerOrder,
    bool CanCustomerDelivery,
    string OrganizationDisplayName = "");

public sealed class ListLinkedMerchantsForPersonalUser
{
    private readonly ILinkedCustomerAppUserRepository _links;
    private readonly IBusinessCustomerRepository _customers;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IEntitlementSnapshotRepository _entitlements;

    public ListLinkedMerchantsForPersonalUser(
        ILinkedCustomerAppUserRepository links,
        IBusinessCustomerRepository customers,
        IPlatformOrganizationRepository organizations,
        IEntitlementSnapshotRepository entitlements)
    {
        _links = links;
        _customers = customers;
        _organizations = organizations;
        _entitlements = entitlements;
    }

    public async Task<PagedResult<LinkedMerchantDto>> ExecuteAsync(
        PlatformUserId userIdentityId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _links
            .ListActiveByUserAsync(userIdentityId, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var customerIds = items.Select(i => i.BusinessCustomerId).Distinct().ToList();
        var customers = (await _customers.ListByIdsAsync(customerIds, cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id.Value);

        var orgIds = items.Select(i => i.OrganizationId).Distinct().ToList();
        var orgNames = new Dictionary<Guid, string>();
        var orderingByOrg = new Dictionary<Guid, (bool Order, bool Delivery)>();
        foreach (var orgId in orgIds)
        {
            var org = await _organizations.GetByIdAsync(orgId, cancellationToken).ConfigureAwait(false);
            if (org is not null)
            {
                orgNames[org.Id.Value] = org.DisplayName;
            }

            orderingByOrg[orgId.Value] = await ResolveOrderingCapabilityAsync(orgId, cancellationToken)
                .ConfigureAwait(false);
        }

        var mapped = new List<LinkedMerchantDto>(items.Count);
        foreach (var link in items)
        {
            if (!customers.TryGetValue(link.BusinessCustomerId.Value, out var customer)
                || customer.OrganizationId != link.OrganizationId)
            {
                continue;
            }

            var (canOrder, canDelivery) = orderingByOrg.GetValueOrDefault(link.OrganizationId.Value);
            mapped.Add(new LinkedMerchantDto(
                link.Id.Value,
                link.BusinessCustomerId.Value,
                link.OrganizationId.Value,
                orgNames.GetValueOrDefault(link.OrganizationId.Value, string.Empty),
                customer.DisplayName,
                link.Status.ToString(),
                link.LinkedAtUtc,
                canOrder,
                canDelivery));
        }

        return new PagedResult<LinkedMerchantDto>(
            mapped,
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    private async Task<(bool CanOrder, bool CanDelivery)> ResolveOrderingCapabilityAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _entitlements
            .GetLatestForOrganizationProductAsync(
                organizationId,
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                cancellationToken)
            .ConfigureAwait(false);
        return LinkedMerchantOrderingCapability.FromSnapshot(snapshot);
    }
}

public sealed class GetLinkedMerchantOrderingCapability
{
    private readonly ILinkedCustomerAppUserRepository _links;
    private readonly IEntitlementSnapshotRepository _entitlements;
    private readonly IPlatformOrganizationRepository _organizations;

    public GetLinkedMerchantOrderingCapability(
        ILinkedCustomerAppUserRepository links,
        IEntitlementSnapshotRepository entitlements,
        IPlatformOrganizationRepository organizations)
    {
        _links = links;
        _entitlements = entitlements;
        _organizations = organizations;
    }

    public async Task<ApplicationResult<LinkedMerchantOrderingCapabilityDto>> ExecuteAsync(
        PlatformUserId userIdentityId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            return ApplicationResult<LinkedMerchantOrderingCapabilityDto>.Failure(
                ApplicationErrorCodes.LinkedCustomerAppUserNotFound,
                "Organization id is required.");
        }

        var org = PlatformOrganizationId.From(organizationId);
        var (links, _) = await _links
            .ListActiveByUserAsync(userIdentityId, skip: 0, take: 200, cancellationToken)
            .ConfigureAwait(false);
        if (!links.Any(l => l.OrganizationId == org))
        {
            return ApplicationResult<LinkedMerchantOrderingCapabilityDto>.Failure(
                ApplicationErrorCodes.LinkedCustomerAppUserNotFound,
                "No active linked merchant was found for this organization.");
        }

        var snapshot = await _entitlements
            .GetLatestForOrganizationProductAsync(
                org,
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                cancellationToken)
            .ConfigureAwait(false);
        var (canOrder, canDelivery) = LinkedMerchantOrderingCapability.FromSnapshot(snapshot);
        var organization = await _organizations.GetByIdAsync(org, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<LinkedMerchantOrderingCapabilityDto>.Success(
            new LinkedMerchantOrderingCapabilityDto(
                organizationId,
                canOrder,
                canDelivery,
                organization?.DisplayName ?? string.Empty));
    }
}

/// <summary>
/// Public fulfillment branch snapshots for a Personal user who is actively linked to the merchant.
/// </summary>
public sealed class ListLinkedMerchantFulfillmentBranches
{
    private readonly ILinkedCustomerAppUserRepository _links;
    private readonly ListBranches _listBranches;

    public ListLinkedMerchantFulfillmentBranches(
        ILinkedCustomerAppUserRepository links,
        ListBranches listBranches)
    {
        _links = links;
        _listBranches = listBranches;
    }

    public async Task<ApplicationResult<IReadOnlyList<OrganizationBranchDto>>> ExecuteAsync(
        PlatformUserId userIdentityId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            return ApplicationResult<IReadOnlyList<OrganizationBranchDto>>.Failure(
                ApplicationErrorCodes.LinkedCustomerAppUserNotFound,
                "Organization id is required.");
        }

        var org = PlatformOrganizationId.From(organizationId);
        var (links, _) = await _links
            .ListActiveByUserAsync(userIdentityId, skip: 0, take: 200, cancellationToken)
            .ConfigureAwait(false);
        if (!links.Any(l => l.OrganizationId == org))
        {
            return ApplicationResult<IReadOnlyList<OrganizationBranchDto>>.Failure(
                ApplicationErrorCodes.LinkedCustomerAppUserNotFound,
                "No active linked merchant was found for this organization.");
        }

        var branches = await _listBranches
            .ExecuteForLinkedCustomerAsync(org, cancellationToken)
            .ConfigureAwait(false);
        return ApplicationResult<IReadOnlyList<OrganizationBranchDto>>.Success(branches);
    }
}

internal static class LinkedMerchantOrderingCapability
{
    public static (bool CanOrder, bool CanDelivery) FromSnapshot(EntitlementSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return (false, false);
        }

        if (!ProductAccessEligibility.CanEnterPinoyBusinessPos(snapshot.SubscriptionStatus, snapshot.Grants))
        {
            return (false, false);
        }

        var canOrder = HasEnabledFeature(snapshot.Grants, FeatureCode.StoreCustomerOrdering);
        var canDelivery = canOrder && HasEnabledFeature(snapshot.Grants, FeatureCode.StoreDeliveryOrders);
        return (canOrder, canDelivery);
    }

    private static bool HasEnabledFeature(IEnumerable<EntitlementGrant> grants, string featureCode) =>
        grants.Any(g =>
            g.Enabled
            && string.Equals(g.FeatureCode.Value, featureCode, StringComparison.Ordinal));
}
