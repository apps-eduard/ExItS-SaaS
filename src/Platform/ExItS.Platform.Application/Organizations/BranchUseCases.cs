using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
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
    bool PickupEnabled = true,
    bool DeliveryEnabled = false,
    bool CanOfferPickup = false,
    bool CanOfferDeliveryLocation = false,
    BranchDeliveryPolicyDto? DeliveryPolicy = null);

public sealed record BranchCapacityDto(int Used, int Allowed);

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
    bool PickupEnabled = true,
    bool DeliveryEnabled = false);

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
    bool? PickupEnabled = null,
    bool? DeliveryEnabled = null);

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

public sealed class ListBranches(IOrganizationBranchRepository branches, IBranchDeliveryPolicyRepository policies)
{
    public async Task<IReadOnlyList<OrganizationBranchDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var list = await branches.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var policyList = await policies.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var policiesByBranchId = policyList.ToDictionary(p => p.BranchId.Value);
        var result = new List<OrganizationBranchDto>(list.Count);
        foreach (var branch in list)
        {
            policiesByBranchId.TryGetValue(branch.Id.Value, out var policy);
            result.Add(BranchMapper.ToDto(branch, policy));
        }

        return result;
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
                command.DeliveryEnabled);
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
            branch.UpdateAddress(
                command.AddressLine1,
                command.AddressLine2,
                command.City,
                command.Region,
                command.PostalCode,
                command.CountryCode,
                clock.UtcNow);

            if (command.ClearCoordinates == true)
            {
                branch.UpdateCoordinates(null, null, clock.UtcNow);
            }
            else if (command.Latitude is not null || command.Longitude is not null)
            {
                branch.UpdateCoordinates(command.Latitude, command.Longitude, clock.UtcNow);
            }

            if (command.PickupEnabled is not null || command.DeliveryEnabled is not null)
            {
                branch.SetFulfillmentCapabilities(
                    command.PickupEnabled ?? branch.PickupEnabled,
                    command.DeliveryEnabled ?? branch.DeliveryEnabled,
                    clock.UtcNow);
            }

            if (command.Status == OrganizationBranchStatus.Active)
            {
                branch.Activate(clock.UtcNow);
            }

            if (command.Status == OrganizationBranchStatus.Inactive)
            {
                branch.Deactivate(clock.UtcNow);
            }

            if (command.Status == OrganizationBranchStatus.Archived)
            {
                branch.Archive(clock.UtcNow);
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

internal sealed record PosPlanLimits(int MaxBranches, int MaxActivePosDevices);

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
            : ApplicationResult<PosPlanLimits>.Success(new(plan.MaxBranches, plan.MaxActivePosDevices));
    }
}

internal static class BranchMapper
{
    public static OrganizationBranchDto ToDto(OrganizationBranch x, BranchDeliveryPolicy? policy = null) =>
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
            x.CanOfferPickup,
            x.CanOfferDeliveryLocation,
            policy is null ? null : ToDto(policy));

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
