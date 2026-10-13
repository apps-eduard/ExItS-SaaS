using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Organizations;

public sealed record StaffPersonalSharedDto(
    string? FirstName,
    string? MiddleName,
    string? LastName,
    string? DateOfBirth,
    string? Gender,
    string? Nationality,
    string? ProfilePhotoUrl,
    string? MobileNumber,
    string? Email,
    string? Country,
    string? AddressLine1,
    string? AddressLine2,
    string? Barangay,
    string? CityMunicipality,
    string? ProvinceState,
    string? PostalCode);

public sealed record MembershipBusinessProfileDto(
    Guid MembershipId,
    Guid OrganizationId,
    Guid UserId,
    string DisplayName,
    string Role,
    string? RoleDisplay,
    string? Department,
    string? JobTitle,
    string? WorkPhone,
    string? WorkEmail,
    bool IsBusinessContact,
    string? StaffId,
    string? Country,
    string? AddressLine1,
    string? AddressLine2,
    string? Barangay,
    string? CityMunicipality,
    string? ProvinceState,
    string? PostalCode,
    bool ProfileDetailsCaptured,
    string? FirstName,
    string? MiddleName,
    string? LastName,
    string? DateOfBirth,
    string? Gender,
    string? Nationality,
    string? ProfilePhotoUrl,
    string? MobileNumber,
    string? Email,
    string? StaffDisplayName,
    StaffPersonalSharedDto? Personal,
    DateTimeOffset UpdatedAtUtc);

public sealed record OrganizationStaffIdSettingsDto(
    Guid OrganizationId,
    string Prefix,
    int NextNumber,
    int PadDigits);

public sealed record UpdateOwnMembershipBusinessContactCommand(
    string? WorkPhone,
    string? WorkEmail,
    bool IsBusinessContact);

public sealed record UpdateManagedMembershipBusinessProfileCommand(
    string? Department,
    string? JobTitle,
    string? WorkPhone,
    string? WorkEmail,
    bool IsBusinessContact,
    string? StaffId = null,
    string? Country = null,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? Barangay = null,
    string? CityMunicipality = null,
    string? ProvinceState = null,
    string? PostalCode = null,
    bool AssignStaffId = false,
    string? FirstName = null,
    string? MiddleName = null,
    string? LastName = null,
    string? DateOfBirth = null,
    string? Gender = null,
    string? Nationality = null,
    string? ProfilePhotoUrl = null,
    string? MobileNumber = null,
    string? Email = null,
    string? StaffDisplayName = null);

/// <summary>
/// Organization-specific business profile for a membership.
/// Never mutates PlatformUser personal identity or organization role/permissions.
/// </summary>
public sealed class MembershipBusinessProfileUseCases
{
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformUserRepository _users;
    private readonly IPersonalUserProfileRepository _personalProfiles;
    private readonly IPersonalAddressRepository _addresses;
    private readonly IOrganizationStaffIdSettingsRepository _staffIds;
    private readonly IPersonalProfilePhotoStore _photos;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public MembershipBusinessProfileUseCases(
        IOrganizationMembershipRepository memberships,
        IPlatformUserRepository users,
        IPersonalUserProfileRepository personalProfiles,
        IPersonalAddressRepository addresses,
        IOrganizationStaffIdSettingsRepository staffIds,
        IPersonalProfilePhotoStore photos,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _memberships = memberships;
        _users = users;
        _personalProfiles = personalProfiles;
        _addresses = addresses;
        _staffIds = staffIds;
        _photos = photos;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<MembershipBusinessProfileDto>> GetAsync(
        Guid organizationId,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        var membership = await LoadMembershipAsync(organizationId, membershipId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<MembershipBusinessProfileDto>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "Organization membership was not found.");
        }

        if (membership.StaffId is null
            && membership.Role is not OrganizationRole.OrganizationOwner
            && membership.Status is MembershipStatus.Active)
        {
            var settings = await LoadOrCreateSettingsAsync(membership.OrganizationId, cancellationToken)
                .ConfigureAwait(false);
            membership.SetStaffWorkplace(
                settings.AllocateNext(_clock.UtcNow),
                membership.Country,
                membership.AddressLine1,
                membership.AddressLine2,
                membership.Barangay,
                membership.CityMunicipality,
                membership.ProvinceState,
                membership.PostalCode,
                _clock.UtcNow);
            await _staffIds.UpdateAsync(settings, cancellationToken).ConfigureAwait(false);
            await _memberships.UpdateAsync(membership, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return ApplicationResult<MembershipBusinessProfileDto>.Success(
            await MapAsync(membership, cancellationToken).ConfigureAwait(false));
    }

    public async Task<ApplicationResult<MembershipBusinessProfileDto>> UpdateOwnAsync(
        Guid organizationId,
        Guid membershipId,
        PlatformUserId actorUserId,
        UpdateOwnMembershipBusinessContactCommand command,
        CancellationToken cancellationToken = default)
    {
        var membership = await LoadMembershipAsync(organizationId, membershipId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<MembershipBusinessProfileDto>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "Organization membership was not found.");
        }

        if (membership.UserId != actorUserId)
        {
            return ApplicationResult<MembershipBusinessProfileDto>.Failure(
                DomainErrorCodes.MembershipBusinessProfileSelfEditDenied,
                "You can only edit your own business contact fields.");
        }

        try
        {
            membership.UpdateOwnBusinessContact(
                command.WorkPhone,
                command.WorkEmail,
                command.IsBusinessContact,
                _clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<MembershipBusinessProfileDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _memberships.UpdateAsync(membership, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<MembershipBusinessProfileDto>.Success(
            await MapAsync(membership, cancellationToken).ConfigureAwait(false));
    }

    public async Task<ApplicationResult<MembershipBusinessProfileDto>> UpdateManagedAsync(
        Guid organizationId,
        Guid membershipId,
        UpdateManagedMembershipBusinessProfileCommand command,
        string? actorReference = null,
        CancellationToken cancellationToken = default)
    {
        var membership = await LoadMembershipAsync(organizationId, membershipId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<MembershipBusinessProfileDto>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "Organization membership was not found.");
        }

        try
        {
            var staffId = await ResolveStaffIdAsync(membership, command, cancellationToken).ConfigureAwait(false);
            membership.UpdateManagedBusinessProfile(
                command.Department,
                command.JobTitle,
                command.WorkPhone,
                command.WorkEmail,
                command.IsBusinessContact,
                _clock.UtcNow,
                actorReference);
            membership.SetStaffWorkplace(
                staffId,
                command.Country,
                command.AddressLine1,
                command.AddressLine2,
                command.Barangay,
                command.CityMunicipality,
                command.ProvinceState,
                command.PostalCode,
                _clock.UtcNow);
            var staffUser = await _users.GetByIdAsync(membership.UserId, cancellationToken).ConfigureAwait(false);
            var personalUser = await ResolvePersonalUserAsync(staffUser, cancellationToken).ConfigureAwait(false);
            var photoUrl = await SnapshotPhotoAsync(membership, personalUser, command.ProfilePhotoUrl, cancellationToken)
                .ConfigureAwait(false);
            membership.SetStaffIdentity(
                command.FirstName,
                command.MiddleName,
                command.LastName,
                command.DateOfBirth,
                command.Gender,
                command.Nationality,
                photoUrl,
                command.MobileNumber,
                command.Email,
                command.StaffDisplayName,
                _clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<MembershipBusinessProfileDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _memberships.UpdateAsync(membership, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<MembershipBusinessProfileDto>.Success(
            await MapAsync(membership, cancellationToken).ConfigureAwait(false));
    }

    private async Task<OrganizationMembership?> LoadMembershipAsync(
        Guid organizationId,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        var membership = await _memberships
            .GetByIdAsync(OrganizationMembershipId.From(membershipId), cancellationToken)
            .ConfigureAwait(false);
        if (membership is null || membership.OrganizationId.Value != organizationId)
        {
            return null;
        }

        return membership;
    }

    private async Task<MembershipBusinessProfileDto> MapAsync(
        OrganizationMembership membership,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(membership.UserId, cancellationToken).ConfigureAwait(false);
        var personal = await LoadPersonalAsync(membership, user, cancellationToken).ConfigureAwait(false);
        return new MembershipBusinessProfileDto(
            membership.Id.Value,
            membership.OrganizationId.Value,
            membership.UserId.Value,
            user?.DisplayName ?? string.Empty,
            membership.Role.ToString(),
            OrganizationRoleDisplay.ToDisplayLabel(membership.Role),
            membership.Department,
            membership.JobTitle,
            membership.WorkPhone,
            membership.WorkEmail,
            membership.IsBusinessContact,
            membership.StaffId,
            membership.Country,
            membership.AddressLine1,
            membership.AddressLine2,
            membership.Barangay,
            membership.CityMunicipality,
            membership.ProvinceState,
            membership.PostalCode,
            membership.ProfileDetailsCaptured,
            membership.ProfileFirstName,
            membership.ProfileMiddleName,
            membership.ProfileLastName,
            membership.ProfileDateOfBirth?.ToString("yyyy-MM-dd"),
            membership.ProfileGender,
            membership.ProfileNationality,
            membership.ProfilePhotoUrl,
            membership.ProfileMobile,
            membership.ProfileEmail,
            membership.ProfileDisplayName,
            personal,
            membership.UpdatedAtUtc);
    }

    public async Task<ApplicationResult<OrganizationStaffIdSettingsDto>> GetStaffIdSettingsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var settings = await LoadOrCreateSettingsAsync(PlatformOrganizationId.From(organizationId), cancellationToken)
            .ConfigureAwait(false);
        return ApplicationResult<OrganizationStaffIdSettingsDto>.Success(ToSettingsDto(settings));
    }

    public async Task<ApplicationResult<OrganizationStaffIdSettingsDto>> UpdateStaffIdSettingsAsync(
        Guid organizationId,
        string prefix,
        int nextNumber,
        int padDigits,
        CancellationToken cancellationToken = default)
    {
        var orgId = PlatformOrganizationId.From(organizationId);
        var settings = await LoadOrCreateSettingsAsync(orgId, cancellationToken).ConfigureAwait(false);
        try
        {
            settings.Configure(prefix, nextNumber, padDigits, _clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationStaffIdSettingsDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _staffIds.UpdateAsync(settings, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<OrganizationStaffIdSettingsDto>.Success(ToSettingsDto(settings));
    }

    private async Task<string?> ResolveStaffIdAsync(
        OrganizationMembership membership,
        UpdateManagedMembershipBusinessProfileCommand command,
        CancellationToken cancellationToken)
    {
        var requested = string.IsNullOrWhiteSpace(command.StaffId) ? null : command.StaffId.Trim();
        if (requested is null && command.AssignStaffId && membership.StaffId is null
            && membership.Role is not OrganizationRole.OrganizationOwner)
        {
            var settings = await LoadOrCreateSettingsAsync(membership.OrganizationId, cancellationToken)
                .ConfigureAwait(false);
            requested = settings.AllocateNext(_clock.UtcNow);
            await _staffIds.UpdateAsync(settings, cancellationToken).ConfigureAwait(false);
        }

        requested ??= membership.StaffId;
        if (requested is not null
            && !string.Equals(requested, membership.StaffId, StringComparison.Ordinal)
            && await _memberships.StaffIdInUseAsync(
                membership.OrganizationId,
                requested,
                membership.Id,
                cancellationToken).ConfigureAwait(false))
        {
            throw new DomainException(DomainErrorCodes.InvalidStaffNumber, "That staff ID is already used in this organization.");
        }

        return requested;
    }

    private async Task<OrganizationStaffIdSettings> LoadOrCreateSettingsAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var settings = await _staffIds.GetAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (settings is not null)
        {
            return settings;
        }

        settings = OrganizationStaffIdSettings.CreateDefault(organizationId, _clock.UtcNow);
        await _staffIds.AddAsync(settings, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return settings;
    }

    private async Task<string?> SnapshotPhotoAsync(
        OrganizationMembership membership,
        PlatformUser? personalUser,
        string? requestedUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestedUrl))
        {
            return membership.ProfilePhotoUrl;
        }

        var trimmed = requestedUrl.Trim();
        if (StaffProfilePhoto.IsStaffPhotoPath(trimmed))
        {
            return membership.ProfilePhotoUrl ?? trimmed;
        }

        if (StaffProfilePhoto.IsPersonalPhotoPath(trimmed) && personalUser is not null)
        {
            var file = await _photos.ReadAsync(personalUser.Id.Value, cancellationToken).ConfigureAwait(false);
            if (file is null)
            {
                return membership.ProfilePhotoUrl ?? trimmed;
            }

            await _photos.SaveForKeyAsync(
                StaffProfilePhoto.StorageKey(membership.Id.Value),
                file.ContentType,
                file.Content,
                cancellationToken).ConfigureAwait(false);
            return StaffProfilePhoto.PublicPath(
                membership.OrganizationId.Value,
                membership.Id.Value,
                _clock.UtcNow.ToUnixTimeMilliseconds());
        }

        return trimmed;
    }

    public async Task<PersonalProfilePhotoFile?> OpenLinkedPersonalPhotoAsync(
        Guid organizationId,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        var membership = await LoadMembershipAsync(organizationId, membershipId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return null;
        }

        var staffUser = await _users.GetByIdAsync(membership.UserId, cancellationToken).ConfigureAwait(false);
        var personalUser = await ResolvePersonalUserAsync(staffUser, cancellationToken).ConfigureAwait(false);
        if (personalUser is null)
        {
            return null;
        }

        return await _photos.ReadAsync(personalUser.Id.Value, cancellationToken).ConfigureAwait(false);
    }

    private async Task<PlatformUser?> ResolvePersonalUserAsync(
        PlatformUser? staffUser,
        CancellationToken cancellationToken)
    {
        if (staffUser is null)
        {
            return null;
        }

        if (staffUser.LinkedPersonalUserId is not null)
        {
            return await _users.GetByIdAsync(staffUser.LinkedPersonalUserId, cancellationToken).ConfigureAwait(false);
        }

        return staffUser.HomeOrganizationId is null ? staffUser : null;
    }

    private async Task<StaffPersonalSharedDto?> LoadPersonalAsync(
        OrganizationMembership membership,
        PlatformUser? staffUser,
        CancellationToken cancellationToken)
    {
        if (staffUser is null)
        {
            return null;
        }

        var personalUser = await ResolvePersonalUserAsync(staffUser, cancellationToken).ConfigureAwait(false);
        if (personalUser is null)
        {
            return new StaffPersonalSharedDto(
                staffUser.FirstName,
                null,
                staffUser.LastName,
                null,
                null,
                null,
                null,
                staffUser.Phone,
                staffUser.NormalizedContactEmail,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        var profile = await _personalProfiles.GetByUserAsync(personalUser.Id, cancellationToken).ConfigureAwait(false);
        var addresses = await _addresses.ListByUserAsync(personalUser.Id, cancellationToken).ConfigureAwait(false);
        var primary = addresses.FirstOrDefault(address => address.IsPrimary) ?? addresses.FirstOrDefault();
        return new StaffPersonalSharedDto(
            personalUser.FirstName,
            profile?.MiddleName,
            personalUser.LastName,
            profile?.DateOfBirth?.ToString("yyyy-MM-dd"),
            profile?.Gender,
            profile?.Nationality,
            profile?.ProfilePhotoUrl is null
                ? null
                : IsRemoteProfilePhoto(profile.ProfilePhotoUrl)
                    ? profile.ProfilePhotoUrl
                    : StaffProfilePhoto.LinkedPersonalPublicPath(
                        membership.OrganizationId.Value,
                        membership.Id.Value,
                        profile.UpdatedAtUtc.ToUnixTimeMilliseconds()),
            personalUser.Phone,
            personalUser.NormalizedEmail,
            primary?.Country,
            primary?.AddressLine1,
            primary?.AddressLine2,
            primary?.Barangay,
            primary?.CityMunicipality,
            primary?.ProvinceState,
            primary?.PostalCode);
    }

    private static bool IsRemoteProfilePhoto(string url) =>
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static OrganizationStaffIdSettingsDto ToSettingsDto(OrganizationStaffIdSettings settings) =>
        new(settings.OrganizationId.Value, settings.Prefix, settings.NextNumber, settings.PadDigits);
}
