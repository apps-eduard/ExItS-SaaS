using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Geography;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

public enum PersonalAddressType
{
    Home = 0,
    Office = 1,
    Other = 2,
}

/// <summary>
/// One of the person's own addresses. Country chooses the field set. Address type does not.
/// </summary>
public sealed class PersonalAddress
{
    public const int MaxTextLength = 200;
    public const int MaxNameLength = 100;

    public PersonalAddressId Id { get; }
    public PlatformUserId UserIdentityId { get; }
    public PersonalAddressType AddressType { get; private set; }
    public string Country { get; private set; }
    public string AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? Barangay { get; private set; }
    public string CityMunicipality { get; private set; }
    public string ProvinceState { get; private set; }
    public string? PostalCode { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private PersonalAddress(
        PersonalAddressId id,
        PlatformUserId userIdentityId,
        PersonalAddressType addressType,
        string country,
        string addressLine1,
        string? addressLine2,
        string? barangay,
        string cityMunicipality,
        string provinceState,
        string? postalCode,
        bool isPrimary,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        UserIdentityId = userIdentityId;
        AddressType = addressType;
        Country = country;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        Barangay = barangay;
        CityMunicipality = cityMunicipality;
        ProvinceState = provinceState;
        PostalCode = postalCode;
        IsPrimary = isPrimary;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static bool IsPhilippines(string? country) => CountryCatalog.IsPhilippines(country);

    public static PersonalAddress Create(
        PlatformUserId userIdentityId,
        PersonalAddressDraft draft,
        bool isPrimary,
        DateTimeOffset utcNow,
        PersonalAddressId? id = null)
    {
        ArgumentNullException.ThrowIfNull(userIdentityId);
        ArgumentNullException.ThrowIfNull(draft);
        EnsureUtc(utcNow);
        var normalized = Normalize(draft);
        return new PersonalAddress(
            id ?? PersonalAddressId.New(),
            userIdentityId,
            normalized.AddressType,
            normalized.Country!,
            normalized.AddressLine1!,
            normalized.AddressLine2,
            normalized.Barangay,
            normalized.CityMunicipality!,
            normalized.ProvinceState!,
            normalized.PostalCode,
            isPrimary,
            utcNow,
            utcNow);
    }

    public static PersonalAddress Rehydrate(
        PersonalAddressId id,
        PlatformUserId userIdentityId,
        PersonalAddressType addressType,
        string country,
        string addressLine1,
        string? addressLine2,
        string? barangay,
        string cityMunicipality,
        string provinceState,
        string? postalCode,
        bool isPrimary,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            userIdentityId,
            addressType,
            country,
            addressLine1,
            addressLine2,
            barangay,
            cityMunicipality,
            provinceState,
            postalCode,
            isPrimary,
            createdAtUtc,
            updatedAtUtc);

    public void Update(PersonalAddressDraft draft, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(draft);
        EnsureUtc(utcNow);
        var normalized = Normalize(draft);
        AddressType = normalized.AddressType;
        Country = normalized.Country!;
        AddressLine1 = normalized.AddressLine1!;
        AddressLine2 = normalized.AddressLine2;
        Barangay = normalized.Barangay;
        CityMunicipality = normalized.CityMunicipality!;
        ProvinceState = normalized.ProvinceState!;
        PostalCode = normalized.PostalCode;
        UpdatedAtUtc = utcNow;
    }

    public void MarkPrimary(bool isPrimary, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        IsPrimary = isPrimary;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>Clears every other primary flag, then marks the selected address.</summary>
    public static void EnforceSinglePrimary(IEnumerable<PersonalAddress> addresses, PersonalAddressId primaryId, DateTimeOffset utcNow)
    {
        var matched = false;
        foreach (var address in addresses)
        {
            var selected = address.Id == primaryId;
            address.MarkPrimary(selected, utcNow);
            matched |= selected;
        }

        if (!matched)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalAddressId,
                "The primary address was not found for this person.");
        }
    }

    public static PersonalAddressType ParseType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !Enum.TryParse<PersonalAddressType>(value.Trim(), ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw new DomainException(
                DomainErrorCodes.PersonalAddressIncomplete,
                "Address type must be Home, Office, or Other.");
        }

        return parsed;
    }

    public bool IsComplete() =>
        PersonalAddressRules.IsComplete(Country, AddressLine1, Barangay, CityMunicipality, ProvinceState, PostalCode);

    private static PersonalAddressDraft Normalize(PersonalAddressDraft draft)
    {
        var country = GeographyAddressRules.CanonicalCountryName(Required(draft.Country, MaxNameLength, "Country"));
        var line1 = Required(draft.AddressLine1, MaxTextLength, "Address line 1");
        var city = Required(draft.CityMunicipality, MaxNameLength, "City");
        var province = Required(draft.ProvinceState, MaxNameLength, "Province");
        var barangay = GeographyAddressRules.NormalizeBarangay(country, Optional(draft.Barangay, MaxNameLength, "Barangay"));
        var postalCode = Optional(draft.PostalCode, 16, "Postal code");
        GeographyAddressRules.EnsureRequired(country, barangay, postalCode);

        return new PersonalAddressDraft(
            draft.AddressType,
            country,
            line1,
            Optional(draft.AddressLine2, MaxTextLength, "Address line 2"),
            barangay,
            city,
            province,
            postalCode);
    }

    private static string Required(string? value, int maxLength, string label)
    {
        var trimmed = Optional(value, maxLength, label);
        if (trimmed is null)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalAddressIncomplete,
                $"{label} is required.");
        }

        return trimmed;
    }

    private static string? Optional(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalAddressIncomplete,
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

public sealed record PersonalAddressDraft(
    PersonalAddressType AddressType,
    string? Country,
    string? AddressLine1,
    string? AddressLine2,
    string? Barangay,
    string? CityMunicipality,
    string? ProvinceState,
    string? PostalCode);

public static class PersonalAddressRules
{
    public static bool IsComplete(
        string? country,
        string? addressLine1,
        string? barangay,
        string? cityMunicipality,
        string? provinceState,
        string? postalCode = null) =>
        GeographyAddressRules.IsComplete(country, addressLine1, barangay, cityMunicipality, provinceState, postalCode);
}

public sealed class PersonalAddressId : IEquatable<PersonalAddressId>
{
    public Guid Value { get; }

    private PersonalAddressId(Guid value) => Value = value;

    public static PersonalAddressId New() => new(Guid.NewGuid());

    public static PersonalAddressId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalAddressId, "Personal address id is required.");
        }

        return new PersonalAddressId(value);
    }

    public bool Equals(PersonalAddressId? other) => other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is PersonalAddressId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PersonalAddressId? left, PersonalAddressId? right) => Equals(left, right);

    public static bool operator !=(PersonalAddressId? left, PersonalAddressId? right) => !Equals(left, right);
}
