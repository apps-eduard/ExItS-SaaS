using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Reference;
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
    string? PsgcCode,
    string? LocalityType,
    string? RegionCode,
    string? RegionName,
    string? ProvinceCode,
    string? ProvinceName,
    string DisplayLabel,
    bool IsActive,
    bool IsVerified,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AddBranchDeliveryServiceAreaCommand(string PsgcCode);

public sealed class ListBranchDeliveryServiceAreas
{
    private readonly IOrganizationBranchRepository _branches;
    private readonly IBranchDeliveryServiceAreaRepository _areas;
    private readonly IPhilippineLocalityDirectory _directory;

    public ListBranchDeliveryServiceAreas(
        IOrganizationBranchRepository branches,
        IBranchDeliveryServiceAreaRepository areas,
        IPhilippineLocalityDirectory directory)
    {
        _branches = branches;
        _areas = areas;
        _directory = directory;
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
            areas.Select(a => ToDto(a, _directory)).ToList());
    }

    internal static BranchDeliveryServiceAreaDto ToDto(
        BranchDeliveryServiceArea area,
        IPhilippineLocalityDirectory directory)
    {
        PhilippineLocality? locality = null;
        if (!string.IsNullOrWhiteSpace(area.PsgcCode))
        {
            locality = directory.GetByPsgcCode(area.PsgcCode);
        }

        var isVerified = locality is not null;
        var displayName = locality is not null
            ? PhilippineLocality.FriendlyName(locality.Name)
            : area.CityMunicipalityName;
        var regionOrProvince = locality?.ProvinceName
            ?? locality?.RegionName
            ?? area.RegionOrProvinceName;
        var displayLabel = locality?.DisplayLabel
            ?? (string.IsNullOrWhiteSpace(regionOrProvince)
                ? displayName
                : $"{displayName} · {regionOrProvince}");

        return new(
            area.Id.Value,
            area.OrganizationId.Value,
            area.BranchId.Value,
            area.CountryCode,
            regionOrProvince,
            locality?.Name ?? area.CityMunicipalityName,
            area.NormalizedCityMunicipalityName,
            area.PsgcCode,
            locality?.Type.ToString(),
            locality?.RegionCode,
            locality?.RegionName,
            locality?.ProvinceCode,
            locality?.ProvinceName,
            displayLabel,
            area.IsActive,
            isVerified,
            area.CreatedAtUtc,
            area.UpdatedAtUtc);
    }
}

public sealed class AddBranchDeliveryServiceArea
{
    private readonly IOrganizationBranchRepository _branches;
    private readonly IBranchDeliveryServiceAreaRepository _areas;
    private readonly IPhilippineLocalityDirectory _directory;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly GetBranchFulfillmentReadiness _readiness;

    public AddBranchDeliveryServiceArea(
        IOrganizationBranchRepository branches,
        IBranchDeliveryServiceAreaRepository areas,
        IPhilippineLocalityDirectory directory,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        GetBranchFulfillmentReadiness readiness)
    {
        _branches = branches;
        _areas = areas;
        _directory = directory;
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

        PhilippineLocality locality;
        try
        {
            var normalizedCode = BranchDeliveryServiceArea.NormalizePsgcCode(command.PsgcCode);
            locality = _directory.GetByPsgcCode(normalizedCode)
                ?? throw new DomainException(
                    DomainErrorCodes.InvalidBranchDeliveryServiceArea,
                    "Unknown or unsupported PSGC locality code.");

            if (locality.Type is not (PhilippineLocalityType.City or PhilippineLocalityType.Municipality))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidBranchDeliveryServiceArea,
                    "Only City or Municipality PSGC localities can be delivery service areas.");
            }

            var regionOrProvince = locality.ProvinceName ?? locality.RegionName;
            var existing = await _areas.ListByBranchAsync(branchId, cancellationToken).ConfigureAwait(false);
            var active = existing.Where(a => a.IsActive).ToList();
            var area = BranchDeliveryServiceArea.CreateFromPsgc(
                organizationId,
                branchId,
                locality.PsgcCode,
                locality.Name,
                _clock.UtcNow,
                regionOrProvince,
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
