using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Settings;

namespace ExItS.Platform.UnitTests.Settings;

public sealed class PlatformSettingsTests
{
    [Fact]
    public void UpdateGeneral_rejects_invalid_support_email()
    {
        var settings = PlatformSettings.CreateDefaults(DateTimeOffset.UtcNow, "actor@test");
        var ex = Assert.Throws<DomainException>(() =>
            settings.UpdateGeneral(
                "ExItS",
                "not-an-email",
                PlatformBrandingDefaults.Empty,
                DateTimeOffset.UtcNow,
                "actor@test",
                expectedVersion: 1));
        Assert.Equal(DomainErrorCodes.InvalidEmail, ex.ErrorCode);
    }

    [Fact]
    public void UpdateRegional_rejects_invalid_currency_code()
    {
        var settings = PlatformSettings.CreateDefaults(DateTimeOffset.UtcNow, "actor@test");
        var ex = Assert.Throws<DomainException>(() =>
            settings.UpdateRegional(
                "UTC",
                "en-US",
                "xx",
                "US",
                null,
                null,
                DateTimeOffset.UtcNow,
                "actor@test",
                expectedVersion: 1));
        Assert.Equal(DomainErrorCodes.InvalidPlatformSettings, ex.ErrorCode);
    }
}
