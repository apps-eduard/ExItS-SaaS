using ExItS.Platform.Admin.Services;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class ThemeServiceTests
{
    [Theory]
    [InlineData(null, AdminTheme.System)]
    [InlineData("", AdminTheme.System)]
    [InlineData("system", AdminTheme.System)]
    [InlineData("light", AdminTheme.Light)]
    [InlineData("dark", AdminTheme.Dark)]
    [InlineData("SYSTEM", AdminTheme.System)]
    [InlineData("Light", AdminTheme.Light)]
    [InlineData("Dark", AdminTheme.Dark)]
    [InlineData(" unknown ", AdminTheme.System)]
    public void Parse_accepts_lowercase_and_legacy_pascal_case(string? raw, AdminTheme expected)
    {
        Assert.Equal(expected, ThemeService.Parse(raw));
    }

    [Theory]
    [InlineData(AdminTheme.System, "system")]
    [InlineData(AdminTheme.Light, "light")]
    [InlineData(AdminTheme.Dark, "dark")]
    public void ToStorageValue_writes_lowercase_authoritative_values(AdminTheme theme, string expected)
    {
        Assert.Equal(expected, ThemeService.ToStorageValue(theme));
    }
}
