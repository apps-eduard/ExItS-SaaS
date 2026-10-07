namespace ExItS.Platform.Domain.Personal;

public static class PersonalProfileField
{
    public const string FirstName = "FirstName";
    public const string LastName = "LastName";
    public const string DisplayName = "DisplayName";
    public const string MobileNumber = "MobileNumber";
    public const string Email = "Email";
    public const string Country = "Country";
    public const string AddressLine1 = "AddressLine1";
    public const string Barangay = "Barangay";
    public const string CityMunicipality = "CityMunicipality";
    public const string ProvinceState = "ProvinceState";
}

public sealed record PersonalProfileFacts(
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? MobileNumber,
    string? Email,
    string? Country,
    string? AddressLine1,
    string? Barangay,
    string? CityMunicipality,
    string? ProvinceState,
    bool HasCompleteStaffAddress = false,
    bool HasCustomerLocation = false);

/// <summary>Required for a relationship is not the same as public. These lists are acceptance checks only.</summary>
public static class PersonalProfileRules
{
    public static IReadOnlyList<string> MissingForBase(PersonalProfileFacts facts) =>
        Missing(
            facts,
            PersonalProfileField.FirstName,
            PersonalProfileField.LastName,
            PersonalProfileField.DisplayName,
            PersonalProfileField.MobileNumber,
            PersonalProfileField.Email,
            PersonalProfileField.Country);

    public static IReadOnlyList<string> MissingForStaff(PersonalProfileFacts facts) =>
        Missing(
            facts,
            PersonalProfileField.FirstName,
            PersonalProfileField.LastName,
            PersonalProfileField.MobileNumber,
            PersonalProfileField.Email,
            PersonalProfileField.AddressLine1,
            PersonalProfileField.Barangay,
            PersonalProfileField.CityMunicipality,
            PersonalProfileField.ProvinceState,
            PersonalProfileField.Country);

    public static IReadOnlyList<string> MissingForCustomer(PersonalProfileFacts facts) =>
        Missing(
            facts,
            PersonalProfileField.FirstName,
            PersonalProfileField.LastName,
            PersonalProfileField.MobileNumber,
            PersonalProfileField.Email,
            PersonalProfileField.CityMunicipality,
            PersonalProfileField.ProvinceState,
            PersonalProfileField.Country);

    public static int BaseCompletionPercent(PersonalProfileFacts facts)
    {
        var required = MissingForBase(facts);
        const int total = 6;
        var filled = total - required.Count;
        return (int)Math.Round(filled * 100d / total);
    }

    public static bool Allows(PersonalFieldVisibility visibility, PersonalProfileAudience audience) =>
        audience switch
        {
            PersonalProfileAudience.Self => true,
            PersonalProfileAudience.Connection => visibility is PersonalFieldVisibility.Connections or PersonalFieldVisibility.Public,
            PersonalProfileAudience.Public => visibility is PersonalFieldVisibility.Public,
            _ => false,
        };

    private static IReadOnlyList<string> Missing(PersonalProfileFacts facts, params string[] fields)
    {
        var missing = new List<string>();
        foreach (var field in fields)
        {
            if (!Has(facts, field))
            {
                missing.Add(field);
            }
        }

        return missing;
    }

    private static bool Has(PersonalProfileFacts facts, string field) =>
        field switch
        {
            PersonalProfileField.FirstName => Present(facts.FirstName),
            PersonalProfileField.LastName => Present(facts.LastName),
            PersonalProfileField.DisplayName => Present(facts.DisplayName),
            PersonalProfileField.MobileNumber => Present(facts.MobileNumber),
            PersonalProfileField.Email => Present(facts.Email),
            PersonalProfileField.Country => Present(facts.Country) || facts.HasCustomerLocation || facts.HasCompleteStaffAddress,
            PersonalProfileField.AddressLine1 => facts.HasCompleteStaffAddress || Present(facts.AddressLine1),
            PersonalProfileField.Barangay => facts.HasCompleteStaffAddress
                || Present(facts.Barangay)
                || (Present(facts.Country) && !PersonalAddress.IsPhilippines(facts.Country)),
            PersonalProfileField.CityMunicipality => facts.HasCustomerLocation || facts.HasCompleteStaffAddress || Present(facts.CityMunicipality),
            PersonalProfileField.ProvinceState => facts.HasCustomerLocation || facts.HasCompleteStaffAddress || Present(facts.ProvinceState),
            _ => false,
        };

    private static bool Present(string? value) => !string.IsNullOrWhiteSpace(value);
}

public enum PersonalProfileAudience
{
    Self = 0,
    Connection = 1,
    Public = 2,
}
