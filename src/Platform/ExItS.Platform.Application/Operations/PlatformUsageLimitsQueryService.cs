using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Operations;

public sealed class PlatformUsageLimitsQueryService
{
    private readonly SubscriptionQueryService _subscriptions;
    private readonly EntitlementQueryService _entitlements;
    private readonly IOrganizationProductUsageReader _usageReader;
    private readonly IPosDeviceRepository _devices;
    private readonly IOrganizationBusinessTypeEntitlementResolver _businessTypes;

    public PlatformUsageLimitsQueryService(
        SubscriptionQueryService subscriptions,
        EntitlementQueryService entitlements,
        IOrganizationProductUsageReader usageReader,
        IPosDeviceRepository devices,
        IOrganizationBusinessTypeEntitlementResolver businessTypes)
    {
        _subscriptions = subscriptions;
        _entitlements = entitlements;
        _usageReader = usageReader;
        _devices = devices;
        _businessTypes = businessTypes;
    }

    public async Task<PagedResult<UsageLimitRowDto>> ListAsync(
        Guid? organizationId,
        string? productCode,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await _subscriptions
            .ListAsync(
                organizationId,
                productCode,
                status: null,
                search: null,
                isTrial: null,
                planId: null,
                SubscriptionListSortBy.UpdatedAtUtc,
                sortDescending: true,
                page,
                pageSize,
                cancellationToken)
            .ConfigureAwait(false);

        var rows = new List<UsageLimitRowDto>();
        foreach (var subscription in subscriptions.Items)
        {
            var snapshot = await _entitlements
                .GetLatestAsync(subscription.OrganizationId, subscription.ProductCode, cancellationToken)
                .ConfigureAwait(false);
            if (snapshot is null || snapshot.Grants.Count == 0)
            {
                continue;
            }

            var orgId = PlatformOrganizationId.From(subscription.OrganizationId);
            var code = ProductCode.Create(subscription.ProductCode);
            var usage = await _usageReader.GetUsageAsync(orgId, code, cancellationToken).ConfigureAwait(false);
            var activeDevices = await _devices.CountActiveAsync(orgId, cancellationToken).ConfigureAwait(false);
            int? activeBusinessTypes = null;
            var businessTypes = await _businessTypes.ResolveAsync(orgId, code, cancellationToken).ConfigureAwait(false);
            if (businessTypes.IsSuccess && businessTypes.Value is not null)
            {
                activeBusinessTypes = businessTypes.Value.EffectiveBusinessTypeIds.Count;
            }

            foreach (var grant in snapshot.Grants)
            {
                rows.Add(MapRow(subscription, snapshot, grant, usage, activeDevices, activeBusinessTypes));
            }
        }

        return new PagedResult<UsageLimitRowDto>(
            rows,
            subscriptions.TotalCount,
            subscriptions.Page,
            subscriptions.PageSize);
    }

    private static UsageLimitRowDto MapRow(
        SubscriptionDto subscription,
        EntitlementSnapshotDto snapshot,
        EntitlementGrantDto grant,
        OrganizationProductUsageSnapshot usage,
        int activeDeviceCount,
        int? activeBusinessTypeCount)
    {
        var (measuredUsage, usageStatus) = ResolveUsage(grant.FeatureCode, usage, activeDeviceCount, activeBusinessTypeCount);
        var isQuantityLimit = IsQuantityLimitFeature(grant.FeatureCode);
        var unlimited = grant.Enabled && grant.NumericLimit is null && isQuantityLimit;
        decimal? usagePercent = null;
        if (usageStatus == UsageLimitUsageStatuses.Measured
            && measuredUsage.HasValue
            && grant.NumericLimit is > 0)
        {
            usagePercent = Math.Round(measuredUsage.Value * 100m / grant.NumericLimit.Value, 1);
        }

        return new UsageLimitRowDto(
            subscription.OrganizationId,
            subscription.OrganizationDisplayName ?? subscription.OrganizationId.ToString("D"),
            subscription.ProductCode,
            subscription.ProductDisplayName,
            subscription.Id,
            subscription.Status,
            subscription.PlanDisplayName,
            subscription.PlanKey,
            grant.FeatureCode,
            grant.Enabled,
            grant.NumericLimit,
            unlimited,
            measuredUsage,
            usageStatus,
            usagePercent);
    }

    private static (int? Usage, string UsageStatus) ResolveUsage(
        string featureCode,
        OrganizationProductUsageSnapshot usage,
        int activeDeviceCount,
        int? activeBusinessTypeCount) =>
        featureCode switch
        {
            FeatureCode.PlanMaxBranches when usage.BranchCountAvailable && usage.ActiveBranchCount.HasValue =>
                (usage.ActiveBranchCount.Value, UsageLimitUsageStatuses.Measured),
            FeatureCode.PlanMaxBranches =>
                (null, UsageLimitUsageStatuses.Unavailable),
            FeatureCode.PlanMaxActiveStaff =>
                (usage.ActiveStaffCount, UsageLimitUsageStatuses.Measured),
            FeatureCode.PlanMaxActivePosDevices =>
                (activeDeviceCount, UsageLimitUsageStatuses.Measured),
            FeatureCode.PlanMaxActiveBusinessTypes when activeBusinessTypeCount.HasValue =>
                (activeBusinessTypeCount.Value, UsageLimitUsageStatuses.Measured),
            FeatureCode.PlanMaxActiveBusinessTypes =>
                (null, UsageLimitUsageStatuses.Unavailable),
            _ when featureCode.StartsWith("plan-max-", StringComparison.Ordinal) =>
                (null, UsageLimitUsageStatuses.Unavailable),
            _ => (null, UsageLimitUsageStatuses.NotInstrumented),
        };

    private static bool IsQuantityLimitFeature(string featureCode) =>
        featureCode is FeatureCode.PlanMaxBranches
            or FeatureCode.PlanMaxActiveStaff
            or FeatureCode.PlanMaxActivePosDevices
            or FeatureCode.PlanMaxActiveBusinessTypes;
}
