using ExItS.Platform.Api.Authentication;
using ExItS.Platform.Application.Geography;
using ExItS.Platform.Domain.Geography;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Infrastructure.Geography;
using ExItS.Platform.Infrastructure.Reference;

namespace ExItS.Platform.UnitTests.Geography;

public sealed class PlatformGeographyTests
{
    private readonly IGeographyDirectory _geography = new PlatformGeographyDirectory(new PhilippineLocalityDirectory());

    [Fact]
    public void Countries_come_from_the_single_catalog()
    {
        var countries = _geography.ListCountries();
        Assert.Equal(249, countries.Count);
        Assert.Equal(CountryCatalog.All.Count, countries.Count);
        Assert.Contains(countries, country => country.Code == "PH" && country.Name == "Philippines");
        Assert.Contains(countries, country => country.Code == "SA" && country.Name == "Saudi Arabia");
        Assert.Equal(countries.Count, countries.Select(country => country.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Philippines_config_uses_province_city_and_barangay_without_region()
    {
        var config = _geography.GetConfig("PH");
        Assert.Equal("Province", config.AdministrativeAreaLabel);
        Assert.Equal("City / Municipality", config.CityLabel);
        Assert.Equal("Postal Code", config.PostalCodeLabel);
        Assert.True(config.SupportsBarangay);
        Assert.False(config.RequiresPostalCode);
        Assert.DoesNotContain("Region", config.AdministrativeAreaLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void International_config_hides_barangay_and_uses_configured_labels()
    {
        var saudi = _geography.GetConfig("Saudi Arabia");
        Assert.Equal("SA", saudi.CountryCode);
        Assert.Equal("State / Province", saudi.AdministrativeAreaLabel);
        Assert.Equal("City", saudi.CityLabel);
        Assert.Equal("Postal / ZIP", saudi.PostalCodeLabel);
        Assert.False(saudi.SupportsBarangay);

        var unitedStates = _geography.GetConfig("US");
        Assert.Equal("State", unitedStates.AdministrativeAreaLabel);
        Assert.Equal("ZIP Code", unitedStates.PostalCodeLabel);
        Assert.False(unitedStates.SupportsBarangay);
    }

    [Fact]
    public void Areas_follow_the_country_and_cities_follow_the_area()
    {
        var areas = _geography.ListAreas("Philippines");
        var aklan = Assert.Single(areas, area => area.Name == "Aklan");
        Assert.Equal("PH", aklan.CountryCode);
        Assert.DoesNotContain(areas, area => area.Name.Contains("Region", StringComparison.OrdinalIgnoreCase) && area.Type == "Region");

        var cities = _geography.ListCities(aklan.Id);
        Assert.Contains(cities, city => city.Name.Contains("Kalibo", StringComparison.OrdinalIgnoreCase));
        Assert.All(cities, city => Assert.Equal(aklan.Id, city.AdministrativeAreaId));

        Assert.Empty(_geography.ListAreas("SA"));
        Assert.Empty(_geography.ListCities("not-a-province"));
    }

    [Fact]
    public void Barangays_load_for_the_selected_city()
    {
        var localities = new PhilippineLocalityDirectory();
        var kalibo = localities.Search("Kalibo", 30).Single(city =>
            city.Name.Equals("Kalibo", StringComparison.OrdinalIgnoreCase)
            && city.ProvinceName == "Aklan");
        var kaliboBarangays = _geography.ListBarangays(kalibo.PsgcCode);
        Assert.NotEmpty(kaliboBarangays);
        Assert.Contains(kaliboBarangays, barangay => barangay.Name.Contains("Poblacion", StringComparison.OrdinalIgnoreCase));
        Assert.All(kaliboBarangays, barangay => Assert.Equal(kalibo.PsgcCode, barangay.CityId));

        var manila = _geography.ListBarangays("1380600000");
        Assert.True(manila.Count > 100);

        Assert.Empty(_geography.ListBarangays("not-a-city"));
    }

    [Fact]
    public void Shared_rules_require_barangay_only_when_the_country_supports_it()
    {
        Assert.Null(GeographyAddressRules.NormalizeBarangay("SA", "Olaya"));
        Assert.Equal("Poblacion", GeographyAddressRules.NormalizeBarangay("PH", " Poblacion "));
        Assert.Equal("Philippines", GeographyAddressRules.CanonicalCountryName("ph"));
        Assert.Equal("Saudi Arabia", GeographyAddressRules.CanonicalCountryName("SA"));
        Assert.False(GeographyAddressRules.IsComplete("PH", "1 Main", null, "Kalibo", "Aklan"));
        Assert.True(GeographyAddressRules.IsComplete("SA", "1 Main", null, "Riyadh", "Riyadh"));
    }

    [Theory]
    [InlineData(AccountClass.Personal)]
    [InlineData(AccountClass.Organization)]
    [InlineData(AccountClass.Platform)]
    public void Geography_reference_is_readable_by_every_account_class(AccountClass accountClass)
    {
        Assert.True(AccountScopeGuardMiddleware.IsSharedGeographyReferencePath(
            "/api/v1/platform/reference/geography/countries"));
        Assert.True(accountClass is AccountClass.Personal or AccountClass.Organization or AccountClass.Platform);
    }
}
