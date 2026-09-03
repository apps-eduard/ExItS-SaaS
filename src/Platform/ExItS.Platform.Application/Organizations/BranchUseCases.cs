using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Reference;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Organizations;

public sealed record BranchDeliveryPolicyDto(
    Guid BranchId,
    Guid OrganizationId,
    decimal MinimumOrderAmount,
    decimal BaseDeliveryFee,
    decimal IncludedDistanceKm,
    decimal AdditionalFeePerKm,
    decimal MaximumDeliveryDistanceKm,
    decimal? FreeDeliveryThreshold,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record OrganizationPrimaryBranchDto(Guid BranchId);

public sealed record OrganizationBranchDto(
    Guid Id,
    Guid OrganizationId,
    string Code,
    string Name,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    bool IsPrimary,
    OrganizationBranchStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    decimal? Latitude = null,
    decimal? Longitude = null,
    bool PickupEnabled = false,
    bool DeliveryEnabled = false,
    bool CustomerOrderingEnabled = false,
    bool OnlineOrdersPaused = false,
    string? OnlineOrdersPauseReason = null,
    string? ContactPhone = null,
    string? TimeZoneId = null,
    bool CanOfferPickup = false,
    bool CanOfferDeliveryLocation = false,
    bool CustomerOrderingReady = false,
    bool PickupReady = false,
    bool DeliveryReady = false,
    bool CustomerOrderingOperational = false,
    bool PickupOperational = false,
    bool DeliveryOperational = false,
    bool CanUseCustomerOrdering = false,
    bool CanUseDelivery = false,
    string? StoreStatusMessage = null,
    BranchDeliveryPolicyDto? DeliveryPolicy = null,
    DateTimeOffset? SuspendedAtUtc = null,
    Guid? SuspendedByUserId = null,
    string? SuspensionReason = null,
    IReadOnlyList<string>? MissingRequirements = null,
    bool BranchDetailsComplete = false,
    bool OperatingHoursComplete = false,
    bool DeliveryLocationComplete = false,
    bool DeliveryPolicyComplete = false,
    bool DeliveryAreasComplete = false,
    int PickupSectionsComplete = 0,
    int PickupSectionsTotal = BranchFulfillmentSetupSummary.PickupSectionCount,
    int DeliverySectionsComplete = 0,
    int DeliverySectionsTotal = BranchFulfillmentSetupSummary.DeliverySectionCount,
    IReadOnlyList<BranchDeliveryServiceAreaPublicDto>? ActiveDeliveryServiceAreas = null,
    Guid? AreaId = null);

public sealed record BranchDeliveryServiceAreaPublicDto(
    Guid Id,
    string CityMunicipalityName,
    string? RegionOrProvinceName,
    string? PsgcCode = null,
    bool IsVerified = false);

public sealed record BranchCapacityDto(int Used, int Allowed);

public sealed record BranchManagementSummaryItemDto(
    Guid Id,
    Guid OrganizationId,
    string Code,
    string Name,
    bool IsPrimary,
    OrganizationBranchStatus Status,
    string? City,
    string? Region,
    string? AddressLine1,
    bool PickupEnabled,
    bool DeliveryEnabled,
    bool CustomerOrderingEnabled,
    int AssignedStaffCount,
    int ActiveDeviceCount,
    int PickupSectionsComplete,
    int PickupSectionsTotal,
    int DeliverySectionsComplete,
    int DeliverySectionsTotal,
    Guid? AreaId = null,
    string? AreaName = null);

public sealed record BranchStaffAccessItemDto(
    Guid MembershipId,
    Guid UserId,
    string DisplayName,
    string MembershipRole,
    string MembershipStatus,
    string? PosRoleCode,
    string? PosRoleDisplay,
    bool HasExplicitAccess,
    bool HasOrganizationWideAccess);

public sealed record SetPrimaryBranchCommand(string? Reason = null);

public sealed record CreateBranchCommand(
    string Code,
    string Name,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? City = null,
    string? Region = null,
    string? PostalCode = null,
    string? CountryCode = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    bool PickupEnabled = false,
    bool DeliveryEnabled = false,
    bool CustomerOrderingEnabled = false,
    string? ContactPhone = null,
    string? TimeZoneId = null);

public sealed record UpdateBranchCommand(
    string Name,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? City = null,
    string? Region = null,
    string? PostalCode = null,
    string? CountryCode = null,
    OrganizationBranchStatus? Status = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    bool? ClearCoordinates = null,
    string? ContactPhone = null,
    string? TimeZoneId = null);

public sealed record UpsertBranchDeliveryPolicyCommand(
    decimal MinimumOrderAmount,
    decimal BaseDeliveryFee,
    decimal IncludedDistanceKm,
    decimal AdditionalFeePerKm,
    decimal MaximumDeliveryDistanceKm,
    decimal? FreeDeliveryThreshold = null);

public sealed record DeliveryFeePreviewRequest(
    decimal MerchandiseSubtotal,
    decimal DistanceKm);

public sealed record DeliveryFeePreviewDto(
    decimal DistanceKm,
    decimal ExtraDistanceKm,
    decimal DistanceCharge,
    decimal DeliveryFee,
    bool FreeDeliveryApplied,
    bool Available,
    string? UnavailableReason = null);

/// <summary>
/// Returns the organization's structural primary branch id for inventory/reconciliation metadata.
/// Does not apply staff branch-assignment filtering (MB2-02A-H1).
/// </summary>
public sealed class GetOrganizationPrimaryBranch(IOrganizationBranchRepository branches)
{
    public async Task<ApplicationResult<OrganizationPrimaryBranchDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var primary = await branches
            .GetPrimaryAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (primary is null)
        {
            return ApplicationResult<OrganizationPrimaryBranchDto>.Failure(
                ApplicationErrorCodes.BranchNotFound,
                "The organization has no primary branch configured.");
        }

        return ApplicationResult<OrganizationPrimaryBranchDto>.Success(
            new OrganizationPrimaryBranchDto(primary.Id.Value));
    }
}

public sealed class ListBranches(
    IOrganizationBranchRepository branches,
    IBranchDeliveryPolicyRepository policies,
    IBranchOperatingHoursRepository hours,
    IBranchDeliveryServiceAreaRepository areas,
    IPhilippineLocalityDirectory localityDirectory,
    IPlatformOrganizationRepository organizations,
    EntitlementQueryService entitlements,
    IBranchFulfillmentReadinessEvaluator readinessEvaluator,
    IOrganizationBranchAccessService branchAccess,
    IClock clock)
{
    public async Task<IReadOnlyList<OrganizationBranchDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId actorUserId,
        CancellationToken cancellationToken = default)
    {
        var list = await branches.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var accessible = await branchAccess
            .ResolveAccessibleActiveBranchIdsAsync(actorUserId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (accessible is not null)
        {
            list = list.Where(b => accessible.Contains(b.Id.Value)).ToList();
        }

        return await MapListAsync(organizationId, list, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Linked Personal customers need Active branch fulfillment snapshots without organization membership.
    /// Skips staff branch-access filtering; still returns only org-owned branches for the seller.
    /// </summary>
    public Task<IReadOnlyList<OrganizationBranchDto>> ExecuteForLinkedCustomerAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        MapListFromOrganizationAsync(organizationId, cancellationToken);

    private async Task<IReadOnlyList<OrganizationBranchDto>> MapListFromOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var list = await branches.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        return await MapListAsync(organizationId, list, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<OrganizationBranchDto>> MapListAsync(
        PlatformOrganizationId organizationId,
        IReadOnlyList<OrganizationBranch> list,
        CancellationToken cancellationToken)
    {
        var policyList = await policies.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var policiesByBranchId = policyList.ToDictionary(p => p.BranchId.Value);
        var hoursByBranchId = await hours.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var activeAreaCounts = await areas
            .CountActiveByBranchIdsAsync(organizationId, list.Select(b => b.Id).ToList(), cancellationToken)
            .ConfigureAwait(false);
        var orgAreas = await areas.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var activeAreasByBranch = orgAreas
            .Where(a => a.IsActive
                        && !string.IsNullOrWhiteSpace(a.PsgcCode)
                        && localityDirectory.Contains(a.PsgcCode!))
            .GroupBy(a => a.BranchId.Value)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<BranchDeliveryServiceAreaPublicDto>)g
                    .Select(a =>
                    {
                        var locality = localityDirectory.GetByPsgcCode(a.PsgcCode!);
                        return new BranchDeliveryServiceAreaPublicDto(
                            a.Id.Value,
                            locality is null
                                ? a.CityMunicipalityName
                                : PhilippineLocality.FriendlyName(locality.Name),
                            locality?.ProvinceName ?? locality?.RegionName ?? a.RegionOrProvinceName,
                            a.PsgcCode,
                            IsVerified: locality is not null);
                    })
                    .ToList());
        var org = await organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var caps = await ResolveCapabilitiesAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var utcNow = clock.UtcNow;
        var result = new List<OrganizationBranchDto>(list.Count);
        foreach (var branch in list)
        {
            policiesByBranchId.TryGetValue(branch.Id.Value, out var policy);
            hoursByBranchId.TryGetValue(branch.Id.Value, out var schedule);
            var hasActiveVerifiedArea = activeAreaCounts.TryGetValue(branch.Id.Value, out var count) && count > 0;
            // CountActive already requires non-null PSGC; still require directory match for readiness.
            if (hasActiveVerifiedArea)
            {
                hasActiveVerifiedArea = orgAreas.Any(a =>
                    a.IsActive
                    && a.BranchId == branch.Id
                    && !string.IsNullOrWhiteSpace(a.PsgcCode)
                    && localityDirectory.Contains(a.PsgcCode!));
            }

            var readiness = readinessEvaluator.Evaluate(new BranchFulfillmentReadinessInput(
                branch,
                schedule,
                policy,
                org?.Profile.TimeZoneId,
                org?.Profile.ContactPhone,
                caps,
                utcNow,
                hasActiveVerifiedArea));
            activeAreasByBranch.TryGetValue(branch.Id.Value, out var branchAreas);
            result.Add(BranchMapper.ToDto(branch, policy, readiness, caps, branchAreas));
        }

        return result;
    }

    private async Task<BranchEntitlementCapabilities> ResolveCapabilitiesAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var snapshot = await entitlements
            .GetLatestAsync(organizationId.Value, ProductCode.PinoyBusinessPos, cancellationToken)
            .ConfigureAwait(false);
        if (snapshot is null)
        {
            return new BranchEntitlementCapabilities(false, false);
        }

        var canOrder = snapshot.Grants.Any(g =>
            g.Enabled
            && string.Equals(g.FeatureCode, FeatureCode.StoreCustomerOrdering, StringComparison.Ordinal));
        var canDelivery = canOrder && snapshot.Grants.Any(g =>
            g.Enabled
            && string.Equals(g.FeatureCode, FeatureCode.StoreDeliveryOrders, StringComparison.Ordinal));
        return new BranchEntitlementCapabilities(canOrder, canDelivery);
    }
}

public sealed class CreateBranch(
    IOrganizationBranchRepository branches,
    IBranchDeliveryPolicyRepository policies,
    ISubscriptionRepository subscriptions,
    IPlanRepository plans,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<OrganizationBranchDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CreateBranchCommand command,
        CancellationToken cancellationToken = default)
    {
        var limit = await PosOrganizationPlanLimits.ResolveAsync(organizationId, subscriptions, plans, cancellationToken).ConfigureAwait(false);
        if (!limit.IsSuccess || limit.Value is null)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(limit.ErrorCode!, limit.ErrorMessage!);
        }

        var active = await branches.CountActiveAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (active >= limit.Value.MaxBranches)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(
                ApplicationErrorCodes.BranchCapacityExceeded,
                "The active POS plan branch limit has been reached.");
        }

        OrganizationBranch branch;
        BranchDeliveryPolicy? policy = null;
        try
        {
            var code = OrganizationBranch.NormalizeCode(command.Code);
            var existing = await branches.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
            if (existing.Any(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)))
            {
                return ApplicationResult<OrganizationBranchDto>.Failure(
                    ApplicationErrorCodes.BranchCodeConflict,
                    "A branch with this code already exists.");
            }

            branch = OrganizationBranch.Create(
                organizationId,
                code,
                command.Name,
                clock.UtcNow,
                command.AddressLine1,
                command.AddressLine2,
                command.City,
                command.Region,
                command.PostalCode,
                command.CountryCode,
                command.Latitude,
                command.Longitude,
                command.PickupEnabled,
                command.DeliveryEnabled,
                command.CustomerOrderingEnabled);
            if (!string.IsNullOrWhiteSpace(command.ContactPhone))
            {
                branch.UpdateContactPhone(command.ContactPhone, clock.UtcNow);
            }

            if (!string.IsNullOrWhiteSpace(command.TimeZoneId))
            {
                branch.UpdateTimeZone(command.TimeZoneId, clock.UtcNow);
            }

            policy = BranchDeliveryPolicy.CreateDefault(branch.Id, organizationId, clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await branches.AddAsync(branch, cancellationToken).ConfigureAwait(false);
        await policies.AddAsync(policy, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<OrganizationBranchDto>.Success(BranchMapper.ToDto(branch, policy));
    }
}

public sealed class UpdateBranch(
    IOrganizationBranchRepository branches,
    IBranchDeliveryPolicyRepository policies,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<OrganizationBranchDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        UpdateBranchCommand command,
        CancellationToken cancellationToken = default)
    {
        var branch = await branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(ApplicationErrorCodes.BranchNotFound, "Branch was not found.");
        }

        try
        {
            branch.Rename(command.Name, clock.UtcNow);
            // Null address fields mean "omit" (preserve existing). Empty string clears.
            // Prevents coordinate-only updates from wiping structured address / readiness.
            branch.UpdateAddress(
                command.AddressLine1 ?? branch.AddressLine1,
                command.AddressLine2 ?? branch.AddressLine2,
                command.City ?? branch.City,
                command.Region ?? branch.Region,
                command.PostalCode ?? branch.PostalCode,
                command.CountryCode ?? branch.CountryCode,
                clock.UtcNow);

            if (command.ClearCoordinates == true)
            {
                branch.UpdateCoordinates(null, null, clock.UtcNow);
            }
            else if (command.Latitude is not null || command.Longitude is not null)
            {
                branch.UpdateCoordinates(command.Latitude, command.Longitude, clock.UtcNow);
            }

            if (command.ContactPhone is not null)
            {
                branch.UpdateContactPhone(command.ContactPhone, clock.UtcNow);
            }

            if (command.TimeZoneId is not null)
            {
                branch.UpdateTimeZone(command.TimeZoneId, clock.UtcNow);
            }

            if (command.Status is not null)
            {
                return ApplicationResult<OrganizationBranchDto>.Failure(
                    ApplicationErrorCodes.StepUpRequired,
                    "Branch status changes require a dedicated governance action with password step-up.");
            }
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await branches.UpdateAsync(branch, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var policy = await policies.GetByBranchIdAsync(branch.Id, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<OrganizationBranchDto>.Success(BranchMapper.ToDto(branch, policy));
    }
}

public sealed class ArchiveBranch(IOrganizationBranchRepository branches, IPlatformUnitOfWork unitOfWork, IClock clock)
{
    public async Task<ApplicationResult<OrganizationBranchDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var branch = await branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(ApplicationErrorCodes.BranchNotFound, "Branch was not found.");
        }

        if (branch.IsPrimary)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "The primary branch cannot be archived.");
        }

        try
        {
            branch.Archive(clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await branches.UpdateAsync(branch, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<OrganizationBranchDto>.Success(BranchMapper.ToDto(branch));
    }
}

public sealed class SuspendBranch(
    IOrganizationBranchRepository branches,
    IPosDeviceRepository devices,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<OrganizationBranchDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        PlatformUserId actorUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var branch = await branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(ApplicationErrorCodes.BranchNotFound, "Branch was not found.");
        }

        var activeDevices = (await devices
            .ListByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false))
            .Count(d => d.BranchId == branchId && d.Status == PosDeviceStatus.Active);
        if (activeDevices > 0)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(
                DomainErrorCodes.OrganizationBranchSuspendBlockedActiveDevices,
                "Revoke or reassign active POS devices on this branch before suspending it.");
        }

        try
        {
            branch.Suspend(actorUserId, reason, clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await branches.UpdateAsync(branch, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<OrganizationBranchDto>.Success(BranchMapper.ToDto(branch));
    }
}

public sealed class ReactivateBranch(
    IOrganizationBranchRepository branches,
    IBranchDeliveryPolicyRepository policies,
    ISubscriptionRepository subscriptions,
    IPlanRepository plans,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<OrganizationBranchDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var branch = await branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(ApplicationErrorCodes.BranchNotFound, "Branch was not found.");
        }

        if (branch.Status == OrganizationBranchStatus.Archived)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(
                DomainErrorCodes.InvalidOrganizationBranchStatusTransition,
                "An archived branch cannot be reactivated.");
        }

        if (branch.Status != OrganizationBranchStatus.Active)
        {
            var limit = await PosOrganizationPlanLimits.ResolveAsync(organizationId, subscriptions, plans, cancellationToken).ConfigureAwait(false);
            if (!limit.IsSuccess || limit.Value is null)
            {
                return ApplicationResult<OrganizationBranchDto>.Failure(limit.ErrorCode!, limit.ErrorMessage!);
            }

            var active = await branches.CountActiveAsync(organizationId, cancellationToken).ConfigureAwait(false);
            if (active >= limit.Value.MaxBranches)
            {
                return ApplicationResult<OrganizationBranchDto>.Failure(
                    ApplicationErrorCodes.BranchCapacityExceeded,
                    "The active POS plan branch limit has been reached.");
            }
        }

        try
        {
            branch.Activate(clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await branches.UpdateAsync(branch, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var policy = await policies.GetByBranchIdAsync(branch.Id, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<OrganizationBranchDto>.Success(BranchMapper.ToDto(branch, policy));
    }
}

public sealed class SetPrimaryBranch(
    IOrganizationBranchRepository branches,
    IBranchDeliveryPolicyRepository policies,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<OrganizationBranchDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var target = await branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (target is null || target.OrganizationId != organizationId)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(ApplicationErrorCodes.BranchNotFound, "Branch was not found.");
        }

        if (target.IsPrimary)
        {
            var policySame = await policies.GetByBranchIdAsync(target.Id, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<OrganizationBranchDto>.Success(BranchMapper.ToDto(target, policySame));
        }

        if (target.Status != OrganizationBranchStatus.Active)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(
                DomainErrorCodes.OrganizationBranchPrimaryChangeInvalid,
                "Only an active branch can become the primary branch.");
        }

        var currentPrimary = await branches.GetPrimaryAsync(organizationId, cancellationToken).ConfigureAwait(false);
        try
        {
            currentPrimary?.DemoteFromPrimary(clock.UtcNow);
            target.PromoteToPrimary(clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationBranchDto>.Failure(ex.ErrorCode, ex.Message);
        }

        if (currentPrimary is not null)
        {
            await branches.UpdateAsync(currentPrimary, cancellationToken).ConfigureAwait(false);
        }

        await branches.UpdateAsync(target, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var policy = await policies.GetByBranchIdAsync(target.Id, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<OrganizationBranchDto>.Success(BranchMapper.ToDto(target, policy));
    }
}

public sealed class ListBranchManagementSummaries(
    ListBranches listBranches,
    IOrganizationMembershipBranchAssignmentRepository assignments,
    IPosDeviceRepository devices,
    IOrganizationMembershipRepository memberships,
    IOrganizationAreaRepository areas)
{
    public async Task<ApplicationResult<IReadOnlyList<BranchManagementSummaryItemDto>>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId actorUserId,
        CancellationToken cancellationToken = default)
    {
        var branches = await listBranches.ExecuteAsync(organizationId, actorUserId, cancellationToken).ConfigureAwait(false);

        var membershipPage = await memberships
            .ListByOrganizationAsync(organizationId, MembershipStatus.Active, skip: 0, take: 500, cancellationToken)
            .ConfigureAwait(false);
        var normalStaffMembershipIds = membershipPage.Items
            .Where(m => m.Role == OrganizationRole.OrganizationMember)
            .Select(m => m.Id.Value)
            .ToHashSet();

        var assignmentRows = await assignments.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var staffCounts = assignmentRows
            .Where(a => normalStaffMembershipIds.Contains(a.MembershipId.Value))
            .GroupBy(a => a.BranchId.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var deviceList = await devices.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var deviceCounts = deviceList
            .Where(d => d.Status == PosDeviceStatus.Active)
            .GroupBy(d => d.BranchId.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var areaNames = (await areas.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false))
            .ToDictionary(a => a.Id.Value, a => a.Name);

        var items = branches
            .OrderByDescending(b => b.IsPrimary)
            .ThenBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
            .Select(b => new BranchManagementSummaryItemDto(
                b.Id,
                b.OrganizationId,
                b.Code,
                b.Name,
                b.IsPrimary,
                b.Status,
                b.City,
                b.Region,
                b.AddressLine1,
                b.PickupEnabled,
                b.DeliveryEnabled,
                b.CustomerOrderingEnabled,
                staffCounts.GetValueOrDefault(b.Id, 0),
                deviceCounts.GetValueOrDefault(b.Id, 0),
                b.PickupSectionsComplete,
                b.PickupSectionsTotal,
                b.DeliverySectionsComplete,
                b.DeliverySectionsTotal,
                b.AreaId,
                b.AreaId is Guid areaId ? areaNames.GetValueOrDefault(areaId) : null))
            .ToList();

        return ApplicationResult<IReadOnlyList<BranchManagementSummaryItemDto>>.Success(items);
    }
}

public sealed class UpsertBranchDeliveryPolicy(
    IOrganizationBranchRepository branches,
    IBranchDeliveryPolicyRepository policies,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<BranchDeliveryPolicyDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        UpsertBranchDeliveryPolicyCommand command,
        CancellationToken cancellationToken = default)
    {
        var branch = await branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<BranchDeliveryPolicyDto>.Failure(ApplicationErrorCodes.BranchNotFound, "Branch was not found.");
        }

        if (branch.Status == OrganizationBranchStatus.Archived)
        {
            return ApplicationResult<BranchDeliveryPolicyDto>.Failure(
                DomainErrorCodes.InvalidOrganizationBranchStatusTransition,
                "An archived branch cannot be changed.");
        }

        try
        {
            var existing = await policies.GetByBranchIdAsync(branchId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                existing = BranchDeliveryPolicy.Create(
                    branchId,
                    organizationId,
                    command.MinimumOrderAmount,
                    command.BaseDeliveryFee,
                    command.IncludedDistanceKm,
                    command.AdditionalFeePerKm,
                    command.MaximumDeliveryDistanceKm,
                    command.FreeDeliveryThreshold,
                    clock.UtcNow);
                await policies.AddAsync(existing, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                if (existing.OrganizationId != organizationId)
                {
                    return ApplicationResult<BranchDeliveryPolicyDto>.Failure(
                        ApplicationErrorCodes.BranchNotFound,
                        "Branch was not found.");
                }

                existing.Update(
                    command.MinimumOrderAmount,
                    command.BaseDeliveryFee,
                    command.IncludedDistanceKm,
                    command.AdditionalFeePerKm,
                    command.MaximumDeliveryDistanceKm,
                    command.FreeDeliveryThreshold,
                    clock.UtcNow);
                await policies.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<BranchDeliveryPolicyDto>.Success(BranchMapper.ToDto(existing));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BranchDeliveryPolicyDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class PreviewBranchDeliveryFee(
    IOrganizationBranchRepository branches,
    IBranchDeliveryPolicyRepository policies)
{
    public async Task<ApplicationResult<DeliveryFeePreviewDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        DeliveryFeePreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var branch = await branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<DeliveryFeePreviewDto>.Failure(ApplicationErrorCodes.BranchNotFound, "Branch was not found.");
        }

        if (!branch.CanOfferDeliveryLocation)
        {
            return ApplicationResult<DeliveryFeePreviewDto>.Success(
                new DeliveryFeePreviewDto(0, 0, 0, 0, false, false, "Delivery is not available for this branch."));
        }

        var policy = await policies.GetByBranchIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (policy is null || !policy.IsCompleteForPublicDelivery)
        {
            return ApplicationResult<DeliveryFeePreviewDto>.Success(
                new DeliveryFeePreviewDto(0, 0, 0, 0, false, false, "Delivery policy is incomplete."));
        }

        try
        {
            var quote = policy.CalculateFee(request.MerchandiseSubtotal, request.DistanceKm);
            return ApplicationResult<DeliveryFeePreviewDto>.Success(
                new DeliveryFeePreviewDto(
                    quote.DistanceKm,
                    quote.ExtraDistanceKm,
                    quote.DistanceCharge,
                    quote.DeliveryFee,
                    quote.FreeDeliveryApplied,
                    true));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<DeliveryFeePreviewDto>.Success(
                new DeliveryFeePreviewDto(
                    BranchDeliveryPolicy.RoundDistance(request.DistanceKm),
                    0,
                    0,
                    0,
                    false,
                    false,
                    ex.Message));
        }
    }
}

public sealed class GetBranchCapacity(IOrganizationBranchRepository branches, ISubscriptionRepository subscriptions, IPlanRepository plans)
{
    public async Task<ApplicationResult<BranchCapacityDto>> ExecuteAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default)
    {
        var limit = await PosOrganizationPlanLimits.ResolveAsync(organizationId, subscriptions, plans, cancellationToken).ConfigureAwait(false);
        if (!limit.IsSuccess || limit.Value is null)
        {
            return ApplicationResult<BranchCapacityDto>.Failure(limit.ErrorCode!, limit.ErrorMessage!);
        }

        return ApplicationResult<BranchCapacityDto>.Success(
            new(await branches.CountActiveAsync(organizationId, cancellationToken).ConfigureAwait(false), limit.Value.MaxBranches));
    }
}

public sealed class EnsureMainBranchExists(
    IOrganizationBranchRepository branches,
    IBranchDeliveryPolicyRepository policies,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<OrganizationBranch> ExecuteAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default)
    {
        var primary = await branches.GetPrimaryAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (primary is not null)
        {
            return primary;
        }

        var main = OrganizationBranch.CreateMainBranch(organizationId, clock.UtcNow);
        await branches.AddAsync(main, cancellationToken).ConfigureAwait(false);
        await policies.AddAsync(BranchDeliveryPolicy.CreateDefault(main.Id, organizationId, clock.UtcNow), cancellationToken)
            .ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return main;
    }
}

internal sealed record PosPlanLimits(int MaxBranches, int MaxActivePosDevices, int MaxAreas);

internal static class PosOrganizationPlanLimits
{
    public static async Task<ApplicationResult<PosPlanLimits>> ResolveAsync(
        PlatformOrganizationId organizationId,
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptions
            .GetCurrentForOrganizationProductAsync(organizationId, ProductCode.Create(ProductCode.PinoyBusinessPos), cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null || !ExItS.Platform.Domain.Subscriptions.Subscription.IsActiveLike(subscription.Status))
        {
            return ApplicationResult<PosPlanLimits>.Failure(
                ApplicationErrorCodes.SubscriptionNotFound,
                "An active POS subscription is required.");
        }

        var plan = await plans.GetByIdAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        return plan is null
            ? ApplicationResult<PosPlanLimits>.Failure(ApplicationErrorCodes.PlanNotFound, "The active POS subscription plan was not found.")
            : ApplicationResult<PosPlanLimits>.Success(new(plan.MaxBranches, plan.MaxActivePosDevices, plan.MaxAreas));
    }
}

internal static class BranchMapper
{
    public static OrganizationBranchDto ToDto(
        OrganizationBranch x,
        BranchDeliveryPolicy? policy = null,
        BranchFulfillmentReadinessResult? readiness = null,
        BranchEntitlementCapabilities? entitlements = null,
        IReadOnlyList<BranchDeliveryServiceAreaPublicDto>? activeDeliveryServiceAreas = null) =>
        new(
            x.Id.Value,
            x.OrganizationId.Value,
            x.Code,
            x.Name,
            x.AddressLine1,
            x.AddressLine2,
            x.City,
            x.Region,
            x.PostalCode,
            x.CountryCode,
            x.IsPrimary,
            x.Status,
            x.CreatedAtUtc,
            x.UpdatedAtUtc,
            x.Latitude,
            x.Longitude,
            x.PickupEnabled,
            x.DeliveryEnabled,
            x.CustomerOrderingEnabled,
            x.OnlineOrdersPaused,
            x.PauseReason?.ToString(),
            x.ContactPhone,
            x.TimeZoneId,
            x.CanOfferPickup,
            x.CanOfferDeliveryLocation,
            readiness?.CustomerOrderingReady ?? false,
            readiness?.PickupReady ?? false,
            readiness?.DeliveryReady ?? false,
            readiness?.CustomerOrderingOperational ?? false,
            readiness?.PickupOperational ?? false,
            readiness?.DeliveryOperational ?? false,
            entitlements?.CanUseCustomerOrdering ?? false,
            entitlements?.CanUseDelivery ?? false,
            GetBranchFulfillmentReadiness.BuildStoreStatusMessage(readiness?.StoreOpenState),
            policy is null ? null : ToDto(policy),
            x.SuspendedAtUtc,
            x.SuspendedByUserId?.Value,
            x.SuspensionReason,
            readiness?.MissingRequirements,
            readiness?.SetupSummary.BranchDetailsComplete ?? false,
            readiness?.SetupSummary.OperatingHoursComplete ?? false,
            readiness?.SetupSummary.DeliveryLocationComplete ?? false,
            readiness?.SetupSummary.DeliveryPolicyComplete ?? false,
            readiness?.SetupSummary.DeliveryAreasComplete ?? false,
            readiness?.SetupSummary.PickupSectionsComplete ?? 0,
            readiness?.SetupSummary.PickupSectionsTotal ?? BranchFulfillmentSetupSummary.PickupSectionCount,
            readiness?.SetupSummary.DeliverySectionsComplete ?? 0,
            readiness?.SetupSummary.DeliverySectionsTotal ?? BranchFulfillmentSetupSummary.DeliverySectionCount,
            activeDeliveryServiceAreas,
            x.AreaId?.Value);

    public static BranchDeliveryPolicyDto ToDto(BranchDeliveryPolicy x) =>
        new(
            x.BranchId.Value,
            x.OrganizationId.Value,
            x.MinimumOrderAmount,
            x.BaseDeliveryFee,
            x.IncludedDistanceKm,
            x.AdditionalFeePerKm,
            x.MaximumDeliveryDistanceKm,
            x.FreeDeliveryThreshold,
            x.CreatedAtUtc,
            x.UpdatedAtUtc);
}
