using ExItS.Platform.Application.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformPasswordPolicyTests
{
    [Fact]
    public void Validate_accepts_strong_password()
    {
        Assert.Null(PlatformPasswordPolicy.Validate("Correct-Horse-9!", new PlatformPasswordOptions()));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("short")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoDigitHere!!")]
    [InlineData("NoSpecialChar12")]
    public void Validate_rejects_weak_password_with_production_defaults(string password)
    {
        Assert.NotNull(PlatformPasswordPolicy.Validate(password, new PlatformPasswordOptions()));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("a")]
    [InlineData("x")]
    [InlineData("12")]
    [InlineData("abc")]
    public void Validate_accepts_simple_password_with_local_validation_options(string password)
    {
        var localValidation = new PlatformPasswordOptions
        {
            MinimumLength = 1,
            MaximumLength = 128,
            RequireUppercase = false,
            RequireLowercase = false,
            RequireDigit = false,
            RequireNonAlphanumeric = false,
        };

        Assert.Null(PlatformPasswordPolicy.Validate(password, localValidation));
    }

    [Fact]
    public void ApplyLocalValidationRelaxation_then_Validate_accepts_single_character()
    {
        var options = new PlatformPasswordOptions();
        PlatformPasswordOptions.ApplyLocalValidationRelaxation(options);
        Assert.Equal(1, options.MinimumLength);
        Assert.False(options.RequireUppercase);
        Assert.False(options.RequireLowercase);
        Assert.False(options.RequireDigit);
        Assert.False(options.RequireNonAlphanumeric);
        Assert.Null(PlatformPasswordPolicy.Validate("1", options));
    }

    [Theory]
  [InlineData("")]
  public void Validate_rejects_empty_password_even_with_local_validation_options(string password)
    {
        var localValidation = new PlatformPasswordOptions
        {
            MinimumLength = 1,
            MaximumLength = 128,
            RequireUppercase = false,
            RequireLowercase = false,
            RequireDigit = false,
            RequireNonAlphanumeric = false,
        };

        Assert.NotNull(PlatformPasswordPolicy.Validate(password, localValidation));
    }

    [Fact]
    public void Production_defaults_remain_strict()
    {
        var defaults = new PlatformPasswordOptions();

        Assert.Equal(12, defaults.MinimumLength);
        Assert.Equal(128, defaults.MaximumLength);
        Assert.True(defaults.RequireUppercase);
        Assert.True(defaults.RequireLowercase);
        Assert.True(defaults.RequireDigit);
        Assert.True(defaults.RequireNonAlphanumeric);
    }
}
