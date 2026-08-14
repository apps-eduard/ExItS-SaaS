using ExItS.PinoyBusinessPOS.Application.Identity;

namespace ExItS.PinoyBusinessPOS.UnitTests.Identity;

public sealed class ExItsQrPurposeMatrixTests
{
    [Theory]
    [InlineData("exits://qr/v1/personal/EX-4827-1936", ExItsQrPurposeGuard.Personal)]
    [InlineData("exits://qr/v1/organization/ORG001234", ExItsQrPurposeGuard.Organization)]
    [InlineData("exits://qr/v1/pos-device-registration/opaque-token-1", ExItsQrPurposeGuard.PosDeviceRegistration)]
    public void Parses_typed_purposes(string payload, string expected)
    {
        Assert.True(ExItsQrPurposeGuard.TryParsePurpose(payload, out var purpose, out _));
        Assert.Equal(expected, purpose);
    }

    [Fact]
    public void Sale_customer_rejects_device_qr_with_friendly_message()
    {
        Assert.False(ExItsQrPurposeGuard.TryValidateOrRoute(
            "exits://qr/v1/pos-device-registration/tok",
            ExItsQrPurposeGuard.Personal,
            out _,
            out var error));
        Assert.Contains("device registration", ExItsQrPurposeGuard.MessageForMismatch("sale-customer", ExItsQrPurposeGuard.PosDeviceRegistration), StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Connected_supplier_rejects_personal_qr_message()
    {
        var message = ExItsQrPurposeGuard.MessageForMismatch(
            "connected-supplier",
            ExItsQrPurposeGuard.Personal);
        Assert.Contains("personal", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Business QR", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Connected_supplier_rejects_device_qr_message()
    {
        var message = ExItsQrPurposeGuard.MessageForMismatch(
            "connected-supplier",
            ExItsQrPurposeGuard.PosDeviceRegistration);
        Assert.Contains("device registration", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sale_customer_allows_personal_and_organization()
    {
        Assert.True(ExItsQrPurposeGuard.TryValidateOrRoute(
            "exits://qr/v1/personal/EX-4827-1936",
            null,
            out var personal,
            out _));
        Assert.Equal(ExItsQrPurposeGuard.Personal, personal);

        Assert.True(ExItsQrPurposeGuard.TryValidateOrRoute(
            "exits://qr/v1/organization/ORG001234",
            null,
            out var org,
            out _));
        Assert.Equal(ExItsQrPurposeGuard.Organization, org);
    }
}
