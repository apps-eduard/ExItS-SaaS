using ExItS.Platform.Application.Personal;

namespace ExItS.Platform.UnitTests.Personal;

public sealed class PersonalProfilePhotoRulesTests
{
    [Fact]
    public void Accepts_jpeg_png_and_webp()
    {
        Assert.True(PersonalProfilePhotoRules.TryDetectContentType([0xFF, 0xD8, 0xFF, 0x00], out var jpeg));
        Assert.Equal("image/jpeg", jpeg);
        Assert.True(PersonalProfilePhotoRules.TryDetectContentType(
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            out var png));
        Assert.Equal("image/png", png);
        Assert.True(PersonalProfilePhotoRules.TryDetectContentType(
            "RIFF0000WEBP"u8.ToArray(),
            out var webp));
        Assert.Equal("image/webp", webp);
    }

    [Fact]
    public void Rejects_empty_oversized_and_non_images()
    {
        Assert.False(PersonalProfilePhotoRules.TryDetectContentType([], out _));
        Assert.False(PersonalProfilePhotoRules.TryDetectContentType(new byte[PersonalProfilePhotoRules.MaxBytes + 1], out _));
        Assert.False(PersonalProfilePhotoRules.TryDetectContentType("not-an-image"u8.ToArray(), out _));
    }
}
