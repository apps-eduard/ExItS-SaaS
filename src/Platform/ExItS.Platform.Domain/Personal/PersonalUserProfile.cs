using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

public enum PersonalFieldVisibility
{
    Private = 0,
    Connections = 1,
    Public = 2,
}

/// <summary>
/// The person's own profile. Addresses live on <see cref="PersonalAddress"/>.
/// Email and mobile stay on <see cref="PlatformUser"/>.
/// </summary>
public sealed class PersonalUserProfile
{
    public const int MaxNameLength = 100;
    public const int MaxPhotoLength = 2048;

    public PlatformUserId UserIdentityId { get; }
    public string? MiddleName { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? Gender { get; private set; }
    public string? Nationality { get; private set; }
    public string? ProfilePhotoUrl { get; private set; }
    public string? AlternativeMobile { get; private set; }
    public PersonalFieldVisibility ShowProfilePhoto { get; private set; }
    public PersonalFieldVisibility ShowDisplayName { get; private set; }
    public PersonalFieldVisibility ShowCity { get; private set; }
    public PersonalFieldVisibility ShowMobile { get; private set; }
    public PersonalFieldVisibility ShowEmail { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private PersonalUserProfile(
        PlatformUserId userIdentityId,
        string? middleName,
        DateOnly? dateOfBirth,
        string? gender,
        string? nationality,
        string? profilePhotoUrl,
        string? alternativeMobile,
        PersonalFieldVisibility showProfilePhoto,
        PersonalFieldVisibility showDisplayName,
        PersonalFieldVisibility showCity,
        PersonalFieldVisibility showMobile,
        PersonalFieldVisibility showEmail,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        UserIdentityId = userIdentityId;
        MiddleName = middleName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Nationality = nationality;
        ProfilePhotoUrl = profilePhotoUrl;
        AlternativeMobile = alternativeMobile;
        ShowProfilePhoto = showProfilePhoto;
        ShowDisplayName = showDisplayName;
        ShowCity = showCity;
        ShowMobile = showMobile;
        ShowEmail = showEmail;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static PersonalUserProfile Create(PlatformUserId userIdentityId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(userIdentityId);
        EnsureUtc(utcNow);
        return new PersonalUserProfile(
            userIdentityId,
            null,
            null,
            null,
            null,
            null,
            null,
            PersonalFieldVisibility.Private,
            PersonalFieldVisibility.Connections,
            PersonalFieldVisibility.Private,
            PersonalFieldVisibility.Private,
            PersonalFieldVisibility.Private,
            utcNow,
            utcNow);
    }

    public static PersonalUserProfile Rehydrate(
        PlatformUserId userIdentityId,
        string? middleName,
        DateOnly? dateOfBirth,
        string? gender,
        string? nationality,
        string? profilePhotoUrl,
        string? alternativeMobile,
        PersonalFieldVisibility showProfilePhoto,
        PersonalFieldVisibility showDisplayName,
        PersonalFieldVisibility showCity,
        PersonalFieldVisibility showMobile,
        PersonalFieldVisibility showEmail,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            userIdentityId,
            middleName,
            dateOfBirth,
            gender,
            nationality,
            profilePhotoUrl,
            alternativeMobile,
            showProfilePhoto,
            showDisplayName,
            showCity,
            showMobile,
            showEmail,
            createdAtUtc,
            updatedAtUtc);

    public void Update(PersonalUserProfileDraft draft, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(draft);
        EnsureUtc(utcNow);
        MiddleName = TrimOptional(draft.MiddleName, MaxNameLength, "Middle name");
        DateOfBirth = draft.DateOfBirth;
        Gender = TrimOptional(draft.Gender, 32, "Gender");
        Nationality = TrimOptional(draft.Nationality, MaxNameLength, "Nationality");
        ProfilePhotoUrl = TrimOptional(draft.ProfilePhotoUrl, MaxPhotoLength, "Profile photo");
        AlternativeMobile = TrimOptional(draft.AlternativeMobile, 32, "Alternative mobile");
        ShowProfilePhoto = draft.ShowProfilePhoto;
        ShowDisplayName = draft.ShowDisplayName;
        ShowCity = draft.ShowCity;
        ShowMobile = draft.ShowMobile;
        ShowEmail = draft.ShowEmail;
        UpdatedAtUtc = utcNow;
    }

    public void SetProfilePhotoUrl(string? profilePhotoUrl, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        ProfilePhotoUrl = TrimOptional(profilePhotoUrl, MaxPhotoLength, "Profile photo");
        UpdatedAtUtc = utcNow;
    }

    public static PersonalFieldVisibility ParseVisibility(string? value, PersonalFieldVisibility fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (!Enum.TryParse<PersonalFieldVisibility>(value.Trim(), ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDisplayName,
                "Profile visibility must be Private, Connections, or Public.");
        }

        return parsed;
    }

    private static string? TrimOptional(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDisplayName,
                $"{label} must be {maxLength} characters or fewer.");
        }

        return trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}

public sealed record PersonalUserProfileDraft(
    string? MiddleName,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Nationality,
    string? ProfilePhotoUrl,
    string? AlternativeMobile,
    PersonalFieldVisibility ShowProfilePhoto,
    PersonalFieldVisibility ShowDisplayName,
    PersonalFieldVisibility ShowCity,
    PersonalFieldVisibility ShowMobile,
    PersonalFieldVisibility ShowEmail);
