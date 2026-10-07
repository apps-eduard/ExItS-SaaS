using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Geography;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public static class PersonalProfileMapper
{
    public static PersonalAddress? Primary(IReadOnlyList<PersonalAddress> addresses) =>
        addresses.FirstOrDefault(address => address.IsPrimary) ?? addresses.FirstOrDefault();

    public static PersonalAddress? StaffAddress(IReadOnlyList<PersonalAddress> addresses) =>
        addresses.FirstOrDefault(address => address.IsPrimary && address.IsComplete())
        ?? addresses.FirstOrDefault(address => address.IsComplete())
        ?? Primary(addresses);

    public static PersonalAddress? CustomerAddress(IReadOnlyList<PersonalAddress> addresses) =>
        addresses.FirstOrDefault(address => address.IsPrimary && HasLocation(address))
        ?? addresses.FirstOrDefault(HasLocation)
        ?? Primary(addresses);

    private static bool HasLocation(PersonalAddress address) =>
        !string.IsNullOrWhiteSpace(address.CityMunicipality)
        && !string.IsNullOrWhiteSpace(address.ProvinceState)
        && !string.IsNullOrWhiteSpace(address.Country);

    public static PersonalProfileFacts Facts(PlatformUser user, IReadOnlyList<PersonalAddress> addresses)
    {
        var primary = Primary(addresses);
        var staff = StaffAddress(addresses);
        var hasCustomerLocation = addresses.Any(address =>
            !string.IsNullOrWhiteSpace(address.CityMunicipality)
            && !string.IsNullOrWhiteSpace(address.ProvinceState)
            && !string.IsNullOrWhiteSpace(address.Country));
        return new PersonalProfileFacts(
            user.FirstName,
            user.LastName,
            user.DisplayName,
            user.Phone,
            user.NormalizedEmail,
            primary?.Country,
            staff?.AddressLine1,
            staff?.Barangay,
            staff?.CityMunicipality,
            staff?.ProvinceState,
            addresses.Any(address => address.IsComplete()),
            hasCustomerLocation);
    }

    public static PersonalAddressDto ToAddressDto(PersonalAddress address) =>
        new(
            address.Id.Value,
            address.AddressType.ToString(),
            address.Country,
            address.AddressLine1,
            address.AddressLine2,
            address.Barangay,
            address.CityMunicipality,
            address.ProvinceState,
            address.PostalCode,
            address.IsPrimary,
            CountryCatalog.Find(address.Country)?.Code);

    public static PersonalProfileDto ToDto(
        PlatformUser user,
        AccountProfile profile,
        PersonalUserProfile? personal,
        IReadOnlyList<PersonalAddress> addresses,
        string? publicUserId,
        string? qrPayload)
    {
        var facts = Facts(user, addresses);
        var primary = Primary(addresses);
        return new PersonalProfileDto(
            user.Id.Value,
            profile.Id.Value,
            user.Username,
            user.DisplayName,
            user.NormalizedEmail,
            profile.AccountClass.ToString(),
            profile.Status,
            publicUserId,
            qrPayload,
            user.Phone,
            user.FirstName,
            personal?.MiddleName,
            user.LastName,
            personal?.DateOfBirth?.ToString("yyyy-MM-dd"),
            personal?.Gender,
            personal?.Nationality,
            personal?.ProfilePhotoUrl,
            personal?.AlternativeMobile,
            primary?.Country,
            primary?.AddressLine1,
            primary?.AddressLine2,
            primary?.Barangay,
            primary?.CityMunicipality,
            primary?.ProvinceState,
            primary?.PostalCode,
            primary?.IsPrimary ?? false,
            (personal?.ShowProfilePhoto ?? PersonalFieldVisibility.Private).ToString(),
            (personal?.ShowDisplayName ?? PersonalFieldVisibility.Connections).ToString(),
            (personal?.ShowCity ?? PersonalFieldVisibility.Private).ToString(),
            (personal?.ShowMobile ?? PersonalFieldVisibility.Private).ToString(),
            (personal?.ShowEmail ?? PersonalFieldVisibility.Private).ToString(),
            PersonalProfileRules.BaseCompletionPercent(facts),
            PersonalProfileRules.MissingForBase(facts),
            PersonalProfileRules.MissingForStaff(facts),
            PersonalProfileRules.MissingForCustomer(facts),
            addresses.Select(ToAddressDto).ToArray());
    }

    public static PersonalUserProfileDraft ToDraft(UpdatePersonalProfileRequest request, PersonalUserProfile existing) =>
        new(
            request.MiddleName ?? existing.MiddleName,
            request.ClearDateOfBirth ? null : request.DateOfBirth ?? existing.DateOfBirth,
            request.Gender ?? existing.Gender,
            request.Nationality ?? existing.Nationality,
            string.IsNullOrWhiteSpace(request.ProfilePhotoUrl)
                ? existing.ProfilePhotoUrl
                : request.ProfilePhotoUrl,
            request.AlternativeMobile ?? existing.AlternativeMobile,
            PersonalUserProfile.ParseVisibility(request.ShowProfilePhoto, existing.ShowProfilePhoto),
            PersonalUserProfile.ParseVisibility(request.ShowDisplayName, existing.ShowDisplayName),
            PersonalUserProfile.ParseVisibility(request.ShowCity, existing.ShowCity),
            PersonalUserProfile.ParseVisibility(request.ShowMobile, existing.ShowMobile),
            PersonalUserProfile.ParseVisibility(request.ShowEmail, existing.ShowEmail));

    public static bool IncludesAddress(UpdatePersonalProfileRequest request) =>
        request.Country is not null
        || request.AddressLine1 is not null
        || request.AddressLine2 is not null
        || request.Barangay is not null
        || request.CityMunicipality is not null
        || request.Province is not null
        || request.ProvinceState is not null
        || request.PostalCode is not null;
}

/// <summary>
/// What another Personal user may see. Residential address, date of birth, staff, and customer data are never included.
/// </summary>
public sealed record PersonalConnectionProfileDto(
    Guid UserIdentityId,
    string? PublicUserId,
    string? Username,
    string? DisplayName,
    string? ProfilePhotoUrl,
    string? CityMunicipality,
    string? MobileNumber,
    string? Email);

public sealed record StaffVisiblePersonalProfileDto(
    Guid UserIdentityId,
    string? FirstName,
    string? LastName,
    string? MobileNumber,
    string? Email,
    string? AddressLine1,
    string? Barangay,
    string? CityMunicipality,
    string? ProvinceState,
    string? Country);

public sealed record CustomerVisiblePersonalProfileDto(
    Guid UserIdentityId,
    string? FirstName,
    string? LastName,
    string? MobileNumber,
    string? Email,
    string? CityMunicipality,
    string? ProvinceState,
    string? Country,
    string? DisplayName,
    string? AddressLine1,
    string? AddressLine2,
    string? Barangay,
    string? PostalCode,
    string? ProfilePhotoUrl,
    string? Gender);

public static class PersonalProfileProjection
{
    public static PersonalConnectionProfileDto ForAudience(
        PlatformUser user,
        PersonalUserProfile? profile,
        PersonalProfileAudience audience,
        IReadOnlyList<PersonalAddress>? addresses = null)
    {
        var photo = profile?.ShowProfilePhoto ?? PersonalFieldVisibility.Private;
        var name = profile?.ShowDisplayName ?? PersonalFieldVisibility.Connections;
        var city = profile?.ShowCity ?? PersonalFieldVisibility.Private;
        var mobile = profile?.ShowMobile ?? PersonalFieldVisibility.Private;
        var email = profile?.ShowEmail ?? PersonalFieldVisibility.Private;
        var primaryCity = addresses is null ? null : PersonalProfileMapper.Primary(addresses)?.CityMunicipality;
        return new PersonalConnectionProfileDto(
            user.Id.Value,
            user.PublicUserId,
            user.Username,
            PersonalProfileRules.Allows(name, audience) ? user.DisplayName : null,
            PersonalProfileRules.Allows(photo, audience) ? profile?.ProfilePhotoUrl : null,
            PersonalProfileRules.Allows(city, audience) ? primaryCity : null,
            PersonalProfileRules.Allows(mobile, audience) ? user.Phone : null,
            PersonalProfileRules.Allows(email, audience) ? user.NormalizedEmail : null);
    }

    public static StaffVisiblePersonalProfileDto ForStaffRelationship(PlatformUser user, PersonalAddress? address) =>
        new(
            user.Id.Value,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.NormalizedEmail,
            address?.AddressLine1,
            address?.Barangay,
            address?.CityMunicipality,
            address?.ProvinceState,
            address?.Country);

    public static CustomerVisiblePersonalProfileDto ForCustomerRelationship(
        PlatformUser user,
        PersonalAddress? address,
        string? profilePhotoUrl = null,
        string? gender = null) =>
        new(
            user.Id.Value,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.NormalizedEmail,
            address?.CityMunicipality,
            address?.ProvinceState,
            address?.Country,
            user.DisplayName,
            address?.AddressLine1,
            address?.AddressLine2,
            address?.Barangay,
            address?.PostalCode,
            profilePhotoUrl,
            gender);

    public static string? CustomerVisiblePhotoUrl(Guid organizationId, Guid userId, PersonalUserProfile? profile)
    {
        var url = profile?.ProfilePhotoUrl?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (url.Contains("/personal/profile/photo", StringComparison.OrdinalIgnoreCase))
        {
            return $"/api/v1/platform/organizations/{organizationId:D}/personal-profiles/{userId:D}/customer-photo?v={profile!.UpdatedAtUtc.ToUnixTimeMilliseconds()}";
        }

        return null;
    }
}

public sealed class PersonalProfileAcceptanceGate
{
    public const string IncompleteMessagePrefix = "Complete your profile before accepting. Missing: ";

    private readonly IPersonalAddressRepository _addresses;

    public PersonalProfileAcceptanceGate(IPersonalAddressRepository addresses) => _addresses = addresses;

    public async Task<string?> MissingStaffFieldsAsync(PlatformUser user, CancellationToken cancellationToken)
    {
        var addresses = await _addresses.ListByUserAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var missing = PersonalProfileRules.MissingForStaff(PersonalProfileMapper.Facts(user, addresses));
        return missing.Count == 0 ? null : IncompleteMessagePrefix + string.Join(", ", missing);
    }

    public async Task<string?> MissingCustomerFieldsAsync(PlatformUser user, CancellationToken cancellationToken)
    {
        var addresses = await _addresses.ListByUserAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var missing = PersonalProfileRules.MissingForCustomer(PersonalProfileMapper.Facts(user, addresses));
        return missing.Count == 0 ? null : IncompleteMessagePrefix + string.Join(", ", missing);
    }
}

public sealed class GetPersonalConnectionProfile
{
    private readonly IPlatformUserRepository _users;
    private readonly IPersonalUserProfileRepository _profiles;
    private readonly IPersonalAddressRepository _addresses;
    private readonly IPersonalConnectionRequestRepository _connections;

    public GetPersonalConnectionProfile(
        IPlatformUserRepository users,
        IPersonalUserProfileRepository profiles,
        IPersonalAddressRepository addresses,
        IPersonalConnectionRequestRepository connections)
    {
        _users = users;
        _profiles = profiles;
        _addresses = addresses;
        _connections = connections;
    }

    public async Task<ApplicationResult<PersonalConnectionProfileDto>> ExecuteAsync(
        PlatformUserId viewerUserId,
        PlatformUserId subjectUserId,
        CancellationToken cancellationToken = default)
    {
        if (viewerUserId != subjectUserId)
        {
            var accepted = await _connections
                .ListForUserAsync(viewerUserId, cancellationToken)
                .ConfigureAwait(false);
            var connected = accepted.Any(request =>
                request.Status == PersonalConnectionRequestStatus.Accepted
                && ((request.RequesterUserIdentityId == viewerUserId && request.TargetUserIdentityId == subjectUserId)
                    || (request.RequesterUserIdentityId == subjectUserId && request.TargetUserIdentityId == viewerUserId)));
            if (!connected)
            {
                var subject = await _users.GetByIdAsync(subjectUserId, cancellationToken).ConfigureAwait(false);
                if (subject is null)
                {
                    return ApplicationResult<PersonalConnectionProfileDto>.Failure(
                        ApplicationErrorCodes.UserNotFound,
                        "User identity was not found.");
                }

                var hidden = await _profiles.GetByUserAsync(subjectUserId, cancellationToken).ConfigureAwait(false);
                var hiddenAddresses = await _addresses.ListByUserAsync(subjectUserId, cancellationToken).ConfigureAwait(false);
                return ApplicationResult<PersonalConnectionProfileDto>.Success(
                    PersonalProfileProjection.ForAudience(subject, hidden, PersonalProfileAudience.Public, hiddenAddresses));
            }
        }

        var user = await _users.GetByIdAsync(subjectUserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PersonalConnectionProfileDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "User identity was not found.");
        }

        var profile = await _profiles.GetByUserAsync(subjectUserId, cancellationToken).ConfigureAwait(false);
        var addresses = await _addresses.ListByUserAsync(subjectUserId, cancellationToken).ConfigureAwait(false);
        var audience = viewerUserId == subjectUserId
            ? PersonalProfileAudience.Self
            : PersonalProfileAudience.Connection;
        return ApplicationResult<PersonalConnectionProfileDto>.Success(
            PersonalProfileProjection.ForAudience(user, profile, audience, addresses));
    }
}

public sealed class GetRelationshipScopedPersonalProfile
{
    private readonly IPlatformUserRepository _users;
    private readonly IPersonalAddressRepository _addresses;
    private readonly IPersonalUserProfileRepository _profiles;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly ILinkedCustomerAppUserRepository _customerLinks;

    public GetRelationshipScopedPersonalProfile(
        IPlatformUserRepository users,
        IPersonalAddressRepository addresses,
        IPersonalUserProfileRepository profiles,
        IOrganizationMembershipRepository memberships,
        ILinkedCustomerAppUserRepository customerLinks)
    {
        _users = users;
        _addresses = addresses;
        _profiles = profiles;
        _memberships = memberships;
        _customerLinks = customerLinks;
    }

    public async Task<ApplicationResult<StaffVisiblePersonalProfileDto>> ForStaffAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId personalUserId,
        CancellationToken cancellationToken = default)
    {
        var staffUsers = await _users
            .ListStaffLinkedToPersonalUserAsync(personalUserId, cancellationToken)
            .ConfigureAwait(false);
        var staff = staffUsers.FirstOrDefault(user =>
            user.Status == AccountStatus.Active && user.HomeOrganizationId == organizationId);
        if (staff is null)
        {
            return ApplicationResult<StaffVisiblePersonalProfileDto>.Failure(
                ApplicationErrorCodes.PersonalProfileNotVisible,
                "This organization does not have a staff relationship with that person.");
        }

        var membership = await _memberships
            .FindCurrentByUserAndOrganizationAsync(staff.Id, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null || membership.Status != MembershipStatus.Active)
        {
            return ApplicationResult<StaffVisiblePersonalProfileDto>.Failure(
                ApplicationErrorCodes.PersonalProfileNotVisible,
                "This organization does not have a staff relationship with that person.");
        }

        var person = await _users.GetByIdAsync(personalUserId, cancellationToken).ConfigureAwait(false);
        if (person is null)
        {
            return ApplicationResult<StaffVisiblePersonalProfileDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "User identity was not found.");
        }

        var addresses = await _addresses.ListByUserAsync(personalUserId, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<StaffVisiblePersonalProfileDto>.Success(
            PersonalProfileProjection.ForStaffRelationship(person, PersonalProfileMapper.StaffAddress(addresses)));
    }

    public async Task<ApplicationResult<CustomerVisiblePersonalProfileDto>> ForCustomerAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId personalUserId,
        CancellationToken cancellationToken = default)
    {
        var link = await _customerLinks
            .FindActiveByUserAndOrganizationAsync(personalUserId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (link is null)
        {
            return ApplicationResult<CustomerVisiblePersonalProfileDto>.Failure(
                ApplicationErrorCodes.PersonalProfileNotVisible,
                "This organization does not have a customer relationship with that person.");
        }

        var person = await _users.GetByIdAsync(personalUserId, cancellationToken).ConfigureAwait(false);
        if (person is null)
        {
            return ApplicationResult<CustomerVisiblePersonalProfileDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "User identity was not found.");
        }

        var addresses = await _addresses.ListByUserAsync(personalUserId, cancellationToken).ConfigureAwait(false);
        var profile = await _profiles.GetByUserAsync(personalUserId, cancellationToken).ConfigureAwait(false);
        var photoUrl = PersonalProfileProjection.CustomerVisiblePhotoUrl(
            organizationId.Value,
            personalUserId.Value,
            profile);
        return ApplicationResult<CustomerVisiblePersonalProfileDto>.Success(
            PersonalProfileProjection.ForCustomerRelationship(
                person,
                PersonalProfileMapper.CustomerAddress(addresses),
                photoUrl,
                profile?.Gender));
    }
}
