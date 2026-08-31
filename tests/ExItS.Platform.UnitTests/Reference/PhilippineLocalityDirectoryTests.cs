using ExItS.Platform.Application.Reference;
using ExItS.Platform.Infrastructure.Reference;

namespace ExItS.Platform.UnitTests.Reference;

public sealed class PhilippineLocalityDirectoryTests
{
    private readonly IPhilippineLocalityDirectory _directory = new PhilippineLocalityDirectory();

    [Fact]
    public void Snapshot_contains_known_localities_and_unique_codes()
    {
        Assert.True(_directory.Metadata.RecordCount > 1000);
        Assert.Equal("2026-06-30", _directory.Metadata.AsOf);
        Assert.Equal("PH", _directory.Metadata.Country);

        var bacolod = _directory.GetByPsgcCode("1830200000");
        Assert.NotNull(bacolod);
        Assert.Equal(PhilippineLocalityType.City, bacolod!.Type);
        Assert.Contains("Bacolod", bacolod.Name, StringComparison.OrdinalIgnoreCase);
        Assert.Null(bacolod.ProvinceName);

        var quezonCity = _directory.GetByPsgcCode("1381300000");
        Assert.NotNull(quezonCity);
        Assert.Equal(PhilippineLocalityType.City, quezonCity!.Type);

        var pateros = _directory.GetByPsgcCode("1381701000");
        Assert.NotNull(pateros);
        Assert.Equal(PhilippineLocalityType.Municipality, pateros!.Type);
        Assert.Null(pateros.ProvinceName);
    }

    [Fact]
    public void Search_ranks_exact_and_prefix_and_is_case_insensitive()
    {
        var results = _directory.Search("bacolod", limit: 10);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.PsgcCode == "1830200000");

        var upper = _directory.Search("BACOLOD", limit: 10);
        Assert.Equal(results.Select(r => r.PsgcCode), upper.Select(r => r.PsgcCode));
    }

    [Fact]
    public void Search_matches_province_and_region()
    {
        var byProvince = _directory.Search("negros occidental", limit: 20);
        Assert.Contains(byProvince, r =>
            string.Equals(r.ProvinceName, "Negros Occidental", StringComparison.OrdinalIgnoreCase));

        var byRegion = _directory.Search("national capital", limit: 20);
        Assert.Contains(byRegion, r =>
            r.RegionName.Contains("National Capital", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Search_does_not_dump_all_on_short_query()
    {
        Assert.Empty(_directory.Search("a"));
        Assert.Empty(_directory.Search(""));
    }

    [Fact]
    public void Unknown_and_non_city_codes_are_absent()
    {
        Assert.Null(_directory.GetByPsgcCode("9999999999"));
        Assert.False(_directory.Contains("1300000000")); // region
        Assert.False(_directory.Contains("1804500000")); // province Negros Occidental
    }

    [Fact]
    public void Same_name_localities_remain_distinct_by_psgc()
    {
        var results = _directory.Search("quezon", limit: 30);
        var namedQuezon = results.Where(r =>
            string.Equals(r.Name, "Quezon", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r.Name, "Quezon City", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(namedQuezon.Count >= 2);
        Assert.Equal(namedQuezon.Count, namedQuezon.Select(r => r.PsgcCode).Distinct().Count());
    }
}
