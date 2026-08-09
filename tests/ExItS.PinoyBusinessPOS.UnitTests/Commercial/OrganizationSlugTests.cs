using ExItS.PinoyBusinessPOS.Application.Commercial;

namespace ExItS.PinoyBusinessPOS.UnitTests.Commercial;

public sealed class OrganizationSlugTests
{
    [Theory]
    [InlineData("My Store", "my-store")]
    [InlineData("  Juan's Sari-Sari  ", "juan-s-sari-sari")]
    [InlineData("ABC123", "abc123")]
    [InlineData("---Hello---", "hello")]
    public void SuggestFromDisplayName_normalizes_to_slug(string name, string expected) =>
        Assert.Equal(expected, OrganizationSlug.SuggestFromDisplayName(name));

    [Fact]
    public void SuggestFromDisplayName_empty_returns_empty() =>
        Assert.Equal(string.Empty, OrganizationSlug.SuggestFromDisplayName("   "));

    [Theory]
    [InlineData("my-store", true)]
    [InlineData("a1", true)]
    [InlineData("My-Store", false)]
    [InlineData("-bad", false)]
    [InlineData("bad-", false)]
    [InlineData("bad--slug", false)]
    [InlineData("a", false)]
    public void IsValidFormat_checks_slug_rules(string slug, bool expected) =>
        Assert.Equal(expected, OrganizationSlug.IsValidFormat(slug));
}
