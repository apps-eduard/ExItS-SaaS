using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Reference;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Organizations;

public sealed record BranchOperatingHoursDayDto(
    string DayOfWeek,
    bool IsClosed,
    bool IsOpen24Hours,
    string? OpenTime,
    string? CloseTime);

public sealed record BranchFulfillmentReadinessDto(
    Guid BranchId,
    bool CanUseCustomerOrdering,
    bool CanUseDelivery,
    bool CustomerOrderingEnabled,
    bool PickupEnabled,
    bool DeliveryEnabled,
    bool OnlineOrdersPaused,
    string? OnlineOrdersPauseReason,
    bool CustomerOrderingReady,
    bool PickupReady,
    bool DeliveryReady,
    bool CustomerOrderingOperational,
    bool PickupOperational,
    bool DeliveryOperational,
    IReadOnlyList<string> MissingRequirements,
    IReadOnlyList<string> ReasonCodes,
    string? StoreOpenStatus,
    bool StoreIsOpenNow,
    string? StoreStatusMessage,
    bool BranchDetailsComplete = false,
    bool OperatingHoursComplete = false,
    bool DeliveryLocationComplete = false,
    bool DeliveryPolicyComplete = false,
    bool DeliveryAreasComplete = false,
    int PickupSectionsComplete = 0,
    int PickupSectionsTotal = BranchFulfillmentSetupSummary.PickupSectionCount,
    int DeliverySectionsComplete = 0,
    int DeliverySectionsTotal = BranchFulfillmentSetupSummary.DeliverySectionCount);

public sealed record UpsertBranchOperatingHoursCommand(IReadOnlyList<BranchOperatingHoursDayDto> Days);

public sealed record UpdateBranchFulfillmentSettingsCommand(
    bool? CustomerOrderingEnabled,
    bool? PickupEnabled,
    bool? DeliveryEnabled);

public sealed record SetBranchOnlineOrdersPausedCommand(bool Paused, string? Reason);

public sealed class GetBranchOperatingHours
{
    private readonly IOrganizationBranchRepository _branches;
    private readonly IBranchOperatingHoursRepository _hours;

    public GetBranchOperatingHours(IOrganizationBranchRepository branches, IBranchOperatingHoursRepository hours)
    {
        _branches = branches;
        _hours = hours;
    }

    public async Task<ApplicationResult<IReadOnlyList<BranchOperatingHoursDayDto>>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var branch = await _branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<IReadOnlyList<BranchOperatingHoursDayDto>>.Failure(
                ApplicationErrorCodes.BranchNotFound,
                "Branch was not found.");
        }

        var schedule = await _hours.GetByBranchIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<IReadOnlyList<BranchOperatingHoursDayDto>>.Success(
            MapOperatingHours(schedule));
    }

    internal static IReadOnlyList<BranchOperatingHoursDayDto> MapOperatingHours(BranchOperatingHoursSchedule? schedule)
    {
        if (schedule is null)
        {
            return Enum.GetValues<DayOfWeek>()
                .Select(day => new BranchOperatingHoursDayDto(day.ToString(), IsClosed: true, IsOpen24Hours: false, null, null))
                .ToList();
        }

        return schedule.Days
            .Select(day => new BranchOperatingHoursDayDto(
                day.DayOfWeek.ToString(),
                day.IsClosed,
                day.IsOpen24Hours,
                day.OpenTime?.ToString("HH:mm"),
                day.CloseTime?.ToString("HH:mm")))
            .ToList();
    }
}

public sealed class GetBranchFulfillmentReadiness
{
    private readonly IOrganizationBranchRepository _branches;
    private readonly IBranchOperatingHoursRepository _hours;
    private readonly IBranchDeliveryPolicyRepository _policies;
    private readonly IBranchDeliveryServiceAreaRepository _areas;
    private readonly IPhilippineLocalityDirectory _directory;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly EntitlementQueryService _entitlements;
    private readonly IBranchFulfillmentReadinessEvaluator _evaluator;
    private readonly IClock _clock;

    public GetBranchFulfillmentReadiness(
        IOrganizationBranchRepository branches,
        IBranchOperatingHoursRepository hours,
        IBranchDeliveryPolicyRepository policies,
        IBranchDeliveryServiceAreaRepository areas,
        IPhilippineLocalityDirectory directory,
        IPlatformOrganizationRepository organizations,
        EntitlementQueryService entitlements,
        IBranchFulfillmentReadinessEvaluator evaluator,
        IClock clock)
    {
        _branches = branches;
        _hours = hours;
        _policies = policies;
        _areas = areas;
        _directory = directory;
        _organizations = organizations;
        _entitlements = entitlements;
        _evaluator = evaluator;
        _clock = clock;
    }

    public async Task<ApplicationResult<BranchFulfillmentReadinessDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var branch = await _branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                ApplicationErrorCodes.BranchNotFound,
                "Branch was not found.");
        }

        var org = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (org is null)
        {
            return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Organization was not found.");
        }

        var hours = await _hours.GetByBranchIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        var policy = await _policies.GetByBranchIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        var areas = await _areas.ListByBranchAsync(branchId, cancellationToken).ConfigureAwait(false);
        var hasActiveVerifiedArea = areas.Any(a =>
            a.IsActive
            && !string.IsNullOrWhiteSpace(a.PsgcCode)
            && _directory.Contains(a.PsgcCode));
        var caps = await ResolveCapabilitiesAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var result = _evaluator.Evaluate(new BranchFulfillmentReadinessInput(
            branch,
            hours,
            policy,
            org.Profile.TimeZoneId,
            org.Profile.ContactPhone,
            caps,
            _clock.UtcNow,
            hasActiveVerifiedArea));

        return ApplicationResult<BranchFulfillmentReadinessDto>.Success(Map(branch, caps, result));
    }

    private async Task<BranchEntitlementCapabilities> ResolveCapabilitiesAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _entitlements
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

    internal static BranchFulfillmentReadinessDto Map(
        OrganizationBranch branch,
        BranchEntitlementCapabilities caps,
        BranchFulfillmentReadinessResult result) =>
        new(
            branch.Id.Value,
            caps.CanUseCustomerOrdering,
            caps.CanUseDelivery,
            branch.CustomerOrderingEnabled,
            branch.PickupEnabled,
            branch.DeliveryEnabled,
            branch.OnlineOrdersPaused,
            branch.PauseReason?.ToString(),
            result.CustomerOrderingReady,
            result.PickupReady,
            result.DeliveryReady,
            result.CustomerOrderingOperational,
            result.PickupOperational,
            result.DeliveryOperational,
            result.MissingRequirements,
            result.ReasonCodes,
            result.StoreOpenState?.Status.ToString(),
            result.StoreOpenState?.IsOpenNow ?? false,
            BuildStoreStatusMessage(result.StoreOpenState),
            result.SetupSummary.BranchDetailsComplete,
            result.SetupSummary.OperatingHoursComplete,
            result.SetupSummary.DeliveryLocationComplete,
            result.SetupSummary.DeliveryPolicyComplete,
            result.SetupSummary.DeliveryAreasComplete,
            result.SetupSummary.PickupSectionsComplete,
            result.SetupSummary.PickupSectionsTotal,
            result.SetupSummary.DeliverySectionsComplete,
            result.SetupSummary.DeliverySectionsTotal);

    internal static string? BuildStoreStatusMessage(BranchStoreOpenState? state)
    {
        if (state is null)
        {
            return null;
        }

        return state.Status switch
        {
            BranchStoreOpenStatus.Open => "Open",
            BranchStoreOpenStatus.ClosesLater when state.NextCloseTimeLocal is not null =>
                $"Open · Closes {state.NextCloseTimeLocal:HH:mm}",
            BranchStoreOpenStatus.OpensLater when state.NextOpenTimeLocal is not null =>
                $"Closed · Opens {state.NextOpenDayOfWeekLocal} {state.NextOpenTimeLocal:HH:mm}",
            _ => state.IsOpenNow ? "Open" : "Closed"
        };
    }
}

public sealed class UpsertBranchOperatingHours
{
    private readonly IOrganizationBranchRepository _branches;
    private readonly IBranchOperatingHoursRepository _hours;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly GetBranchFulfillmentReadiness _readiness;

    public UpsertBranchOperatingHours(
        IOrganizationBranchRepository branches,
        IBranchOperatingHoursRepository hours,
        IPlatformUnitOfWork unitOfWork,
        GetBranchFulfillmentReadiness readiness)
    {
        _branches = branches;
        _hours = hours;
        _unitOfWork = unitOfWork;
        _readiness = readiness;
    }

    public async Task<ApplicationResult<BranchFulfillmentReadinessDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        UpsertBranchOperatingHoursCommand command,
        CancellationToken cancellationToken = default)
    {
        var branch = await _branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                ApplicationErrorCodes.BranchNotFound,
                "Branch was not found.");
        }

        try
        {
            var days = command.Days
                .Select(d => ParseDay(d))
                .ToList();
            var schedule = BranchOperatingHoursSchedule.Create(branchId, days);
            await _hours.UpsertAsync(schedule, organizationId, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(ex.ErrorCode, ex.Message);
        }

        return await _readiness.ExecuteAsync(organizationId, branchId, cancellationToken).ConfigureAwait(false);
    }

    private static BranchDayOperatingHours ParseDay(BranchOperatingHoursDayDto dto)
    {
        if (!Enum.TryParse<DayOfWeek>(dto.DayOfWeek, true, out var day))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchOperatingHours,
                "Day of week is invalid.");
        }

        if (dto.IsClosed)
        {
            return BranchDayOperatingHours.Closed(day);
        }

        if (dto.IsOpen24Hours)
        {
            return BranchDayOperatingHours.Open24Hours(day);
        }

        if (!TimeOnly.TryParse(dto.OpenTime, out var open) || !TimeOnly.TryParse(dto.CloseTime, out var close))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchOperatingHours,
                "Open and close times are required for operating intervals.");
        }

        return BranchDayOperatingHours.Interval(day, open, close);
    }
}

public sealed class UpdateBranchFulfillmentSettings
{
    private readonly IOrganizationBranchRepository _branches;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly GetBranchFulfillmentReadiness _readiness;

    public UpdateBranchFulfillmentSettings(
        IOrganizationBranchRepository branches,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        GetBranchFulfillmentReadiness readiness)
    {
        _branches = branches;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _readiness = readiness;
    }

    public async Task<ApplicationResult<BranchFulfillmentReadinessDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        UpdateBranchFulfillmentSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        var branch = await _branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                ApplicationErrorCodes.BranchNotFound,
                "Branch was not found.");
        }

        var current = await _readiness.ExecuteAsync(organizationId, branchId, cancellationToken).ConfigureAwait(false);
        if (!current.IsSuccess || current.Value is null)
        {
            return current;
        }

        try
        {
            var utcNow = _clock.UtcNow;
            if (command.CustomerOrderingEnabled is bool ordering)
            {
                if (ordering && !current.Value.CustomerOrderingReady)
                {
                    return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                        DomainErrorCodes.BranchFulfillmentNotReady,
                        "Customer ordering setup is incomplete.");
                }

                branch.SetCustomerOrderingEnabled(ordering, utcNow);
            }

            if (command.PickupEnabled is bool pickup)
            {
                if (pickup && !current.Value.PickupReady)
                {
                    return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                        DomainErrorCodes.BranchFulfillmentNotReady,
                        "Pickup setup is incomplete.");
                }

                branch.SetFulfillmentCapabilities(
                    pickup,
                    branch.DeliveryEnabled,
                    utcNow);
            }

            if (command.DeliveryEnabled is bool delivery)
            {
                if (delivery)
                {
                    var refreshed = await _readiness.ExecuteAsync(organizationId, branchId, cancellationToken)
                        .ConfigureAwait(false);
                    if (!refreshed.IsSuccess || refreshed.Value is null || !refreshed.Value.DeliveryReady)
                    {
                        return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                            DomainErrorCodes.BranchFulfillmentNotReady,
                            "Delivery setup is incomplete.");
                    }
                }

                branch.SetFulfillmentCapabilities(branch.PickupEnabled, delivery, utcNow);
            }
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _branches.UpdateAsync(branch, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await _readiness.ExecuteAsync(organizationId, branchId, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class SetBranchOnlineOrdersPaused
{
    private readonly IOrganizationBranchRepository _branches;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly GetBranchFulfillmentReadiness _readiness;

    public SetBranchOnlineOrdersPaused(
        IOrganizationBranchRepository branches,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        GetBranchFulfillmentReadiness readiness)
    {
        _branches = branches;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _readiness = readiness;
    }

    public async Task<ApplicationResult<BranchFulfillmentReadinessDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        SetBranchOnlineOrdersPausedCommand command,
        CancellationToken cancellationToken = default)
    {
        var branch = await _branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                ApplicationErrorCodes.BranchNotFound,
                "Branch was not found.");
        }

        OnlineOrdersPauseReason reason = OnlineOrdersPauseReason.Other;
        if (command.Paused)
        {
            if (!Enum.TryParse(command.Reason ?? nameof(OnlineOrdersPauseReason.Other), true, out reason))
            {
                reason = OnlineOrdersPauseReason.Other;
            }
        }

        branch.SetOnlineOrdersPaused(command.Paused, command.Paused ? reason : null, _clock.UtcNow);
        await _branches.UpdateAsync(branch, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await _readiness.ExecuteAsync(organizationId, branchId, cancellationToken).ConfigureAwait(false);
    }
}
