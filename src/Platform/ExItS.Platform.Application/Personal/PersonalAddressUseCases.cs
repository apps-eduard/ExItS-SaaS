using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public sealed record SavePersonalAddressRequest(
    string AddressType,
    string? Country,
    string? AddressLine1,
    string? AddressLine2,
    string? Barangay,
    string? CityMunicipality,
    string? ProvinceState,
    string? PostalCode,
    bool IsPrimary);

public sealed class ManagePersonalAddresses
{
    private readonly IPersonalAddressRepository _addresses;
    private readonly GetPersonalProfile _getProfile;
    private readonly IClock _clock;

    public ManagePersonalAddresses(
        IPersonalAddressRepository addresses,
        GetPersonalProfile getProfile,
        IClock clock)
    {
        _addresses = addresses;
        _getProfile = getProfile;
        _clock = clock;
    }

    public Task<ApplicationResult<PersonalProfileDto>> AddAsync(
        PlatformUserId userIdentityId,
        AccountProfileId accountProfileId,
        SavePersonalAddressRequest request,
        CancellationToken cancellationToken = default) =>
        SaveAsync(userIdentityId, accountProfileId, existing: null, request, cancellationToken);

    public async Task<ApplicationResult<PersonalProfileDto>> UpdateAsync(
        PlatformUserId userIdentityId,
        AccountProfileId accountProfileId,
        PersonalAddressId addressId,
        SavePersonalAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _addresses
            .GetByIdForUserAsync(addressId, userIdentityId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return NotFound();
        }

        return await SaveAsync(userIdentityId, accountProfileId, existing, request, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ApplicationResult<PersonalProfileDto>> DeleteAsync(
        PlatformUserId userIdentityId,
        AccountProfileId accountProfileId,
        PersonalAddressId addressId,
        CancellationToken cancellationToken = default)
    {
        var removed = await _addresses
            .DeleteAsync(addressId, userIdentityId, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        if (!removed)
        {
            return NotFound();
        }

        return await _getProfile.ExecuteAsync(userIdentityId, accountProfileId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ApplicationResult<PersonalProfileDto>> SetPrimaryAsync(
        PlatformUserId userIdentityId,
        AccountProfileId accountProfileId,
        PersonalAddressId addressId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _addresses
            .GetByIdForUserAsync(addressId, userIdentityId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return NotFound();
        }

        await _addresses
            .SaveEnsuringSinglePrimaryAsync(existing, makePrimary: true, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        return await _getProfile.ExecuteAsync(userIdentityId, accountProfileId, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ApplicationResult<PersonalProfileDto>> SaveAsync(
        PlatformUserId userIdentityId,
        AccountProfileId accountProfileId,
        PersonalAddress? existing,
        SavePersonalAddressRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var draft = new PersonalAddressDraft(
                PersonalAddress.ParseType(request.AddressType),
                request.Country,
                request.AddressLine1,
                request.AddressLine2,
                request.Barangay,
                request.CityMunicipality,
                request.ProvinceState,
                request.PostalCode);
            var address = existing ?? PersonalAddress.Create(userIdentityId, draft, isPrimary: false, _clock.UtcNow);
            if (existing is not null)
            {
                existing.Update(draft, _clock.UtcNow);
            }

            await _addresses
                .SaveEnsuringSinglePrimaryAsync(address, request.IsPrimary, _clock.UtcNow, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalProfileDto>.Failure(ex.ErrorCode, ex.Message);
        }

        return await _getProfile.ExecuteAsync(userIdentityId, accountProfileId, cancellationToken)
            .ConfigureAwait(false);
    }

    private static ApplicationResult<PersonalProfileDto> NotFound() =>
        ApplicationResult<PersonalProfileDto>.Failure(
            ApplicationErrorCodes.PersonalAddressNotFound,
            "Address was not found.");
}
