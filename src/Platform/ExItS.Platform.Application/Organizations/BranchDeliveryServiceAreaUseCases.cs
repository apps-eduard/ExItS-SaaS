using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed record BranchDeliveryServiceAreaDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string CountryCode,
    string? RegionOrProvinceName,
    string CityMunicipalityName,
    string NormalizedCityMunicipalityName,
    string? ExternalAreaCode,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AddBranchDeliveryServiceAreaCommand(
    string CountryCode,
    string CityMunicipalityName,
    string? RegionOrProvinceName = null,
    string? ExternalAreaCode = null);

public sealed class ListBranchDeliveryServiceAreas
{
    private readonly IOrganizationBranchRepository _branches;
    private readonly IBranchDeliveryServiceAreaRepository _areas;

    public ListBranchDeliveryServiceAreas(
        IOrganizationBranchRepository branches,
        IBranchDeliveryServiceAreaRepository areas)
    {
        _branches = branches;
        _areas = areas;
    }

    public async Task<ApplicationResult<IReadOnlyList<BranchDeliveryServiceAreaDto>>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var branch = await _branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<IReadOnlyList<BranchDeliveryServiceAreaDto>>.Failure(
                ApplicationErrorCodes.BranchNotFound,
                "Branch was not found.");
        }

        var areas = await _areas.ListByBranchAsync(branchId, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<IReadOnlyList<BranchDeliveryServiceAreaDto>>.Success(
            areas.Select(ToDto).ToList());
    }

    internal static BranchDeliveryServiceAreaDto ToDto(BranchDeliveryServiceArea area) =>
        new(
            area.Id.Value,
            area.OrganizationId.Value,
            area.BranchId.Value,
            area.CountryCode,
            area.RegionOrProvinceName,
            area.CityMunicipalityName,
            area.NormalizedCityMunicipalityName,
            area.ExternalAreaCode,
            area.IsActive,
            area.CreatedAtUtc,
            area.UpdatedAtUtc);
}

public sealed class AddBranchDeliveryServiceArea
{
    private readonly IOrganizationBranchRepository _branches;
    private readonly IBranchDeliveryServiceAreaRepository _areas;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly GetBranchFulfillmentReadiness _readiness;

    public AddBranchDeliveryServiceArea(
        IOrganizationBranchRepository branches,
        IBranchDeliveryServiceAreaRepository areas,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        GetBranchFulfillmentReadiness readiness)
    {
        _branches = branches;
        _areas = areas;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _readiness = readiness;
    }

    public async Task<ApplicationResult<BranchFulfillmentReadinessDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        AddBranchDeliveryServiceAreaCommand command,
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
            var existing = await _areas.ListByBranchAsync(branchId, cancellationToken).ConfigureAwait(false);
            var active = existing.Where(a => a.IsActive).ToList();
            var area = BranchDeliveryServiceArea.Create(
                organizationId,
                branchId,
                command.CountryCode,
                command.CityMunicipalityName,
                _clock.UtcNow,
                command.RegionOrProvinceName,
                command.ExternalAreaCode,
                active);
            await _areas.AddAsync(area, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(ex.ErrorCode, ex.Message);
        }

        return await _readiness.ExecuteAsync(organizationId, branchId, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class DeactivateBranchDeliveryServiceArea
{
    private readonly IOrganizationBranchRepository _branches;
    private readonly IBranchDeliveryServiceAreaRepository _areas;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly GetBranchFulfillmentReadiness _readiness;

    public DeactivateBranchDeliveryServiceArea(
        IOrganizationBranchRepository branches,
        IBranchDeliveryServiceAreaRepository areas,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        GetBranchFulfillmentReadiness readiness)
    {
        _branches = branches;
        _areas = areas;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _readiness = readiness;
    }

    public async Task<ApplicationResult<BranchFulfillmentReadinessDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        BranchDeliveryServiceAreaId areaId,
        CancellationToken cancellationToken = default)
    {
        var branch = await _branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                ApplicationErrorCodes.BranchNotFound,
                "Branch was not found.");
        }

        var area = await _areas.GetByIdAsync(areaId, cancellationToken).ConfigureAwait(false);
        if (area is null || area.BranchId != branchId || area.OrganizationId != organizationId)
        {
            return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                DomainErrorCodes.BranchDeliveryServiceAreaNotFound,
                "Delivery service area was not found.");
        }

        area.Deactivate(_clock.UtcNow);
        await _areas.UpdateAsync(area, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await _readiness.ExecuteAsync(organizationId, branchId, cancellationToken).ConfigureAwait(false);
    }
}
