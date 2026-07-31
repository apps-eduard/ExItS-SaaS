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
    [InlineData("short")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoDigitHere!!")]
    [InlineData("NoSpecialChar12")]
    public void Validate_rejects_weak_password(string password)
    {
        Assert.NotNull(PlatformPasswordPolicy.Validate(password, new PlatformPasswordOptions()));
    }
}
