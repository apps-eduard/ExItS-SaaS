using ExItS.PinoyBusinessPOS.Application.Identity;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class ExItsQrPurposeGuardTests
{
    [Theory]
    [InlineData("exits://qr/v1/personal/EX-4827-1936", ExItsQrPurposeGuard.Personal)]
    [InlineData("exits://user/v1/EX-4827-1936", ExItsQrPurposeGuard.Personal)]
    [InlineData("exits://qr/v1/organization/ORG001842", ExItsQrPurposeGuard.Organization)]
    [InlineData("exits://qr/v1/pos-device-registration/abcdefghijklmnopqrstuvwxyz012345", ExItsQrPurposeGuard.PosDeviceRegistration)]
    public void TryParsePurpose_recognizes_envelope_types(string payload, string expected)
    {
        Assert.True(ExItsQrPurposeGuard.TryParsePurpose(payload, out var purpose, out _));
        Assert.Equal(expected, purpose);
    }

    [Fact]
    public void TryValidateOrRoute_rejects_personal_when_device_registration_expected()
    {
        var ok = ExItsQrPurposeGuard.TryValidateOrRoute(
            "exits://qr/v1/personal/EX-4827-1936",
            ExItsQrPurposeGuard.PosDeviceRegistration,
            out _,
            out var error);
        Assert.False(ok);
        Assert.Contains("not the right type", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryValidateOrRoute_rejects_organization_when_personal_expected()
    {
        var ok = ExItsQrPurposeGuard.TryValidateOrRoute(
            "exits://qr/v1/organization/ORG001842",
            ExItsQrPurposeGuard.Personal,
            out _,
            out var error);
        Assert.False(ok);
        Assert.Equal(ExItsQrPurposeGuard.MismatchMessage, error);
    }

    [Fact]
    public void TryValidateOrRoute_routes_by_type_when_no_expected_purpose()
    {
        Assert.True(ExItsQrPurposeGuard.TryValidateOrRoute(
            "exits://qr/v1/organization/ORG001842",
            expectedPurpose: null,
            out var purpose,
            out var error));
        Assert.Equal(ExItsQrPurposeGuard.Organization, purpose);
        Assert.Null(error);
    }

    [Fact]
    public void Device_register_page_guards_purpose_mismatch()
    {
        var page = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Devices", "PosDeviceRegister.razor"));
        Assert.Contains("ExItsQrPurposeGuard", page, StringComparison.Ordinal);
        Assert.Contains("PosDeviceRegistration", page, StringComparison.Ordinal);
        Assert.Contains("ResolveQrAsync", page, StringComparison.Ordinal);
        Assert.Contains("RedeemPosDeviceRegistrationTokenAsync", page, StringComparison.Ordinal);
        Assert.Contains("Device_QrPurposeMismatch", page, StringComparison.Ordinal);
        Assert.Contains("Device_ScanRegistrationCode", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Org_devices_page_can_show_registration_code()
    {
        var page = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Organization", "OrgPosDevices.razor"));
        Assert.Contains("CreatePosDeviceRegistrationTokenAsync", page, StringComparison.Ordinal);
        Assert.Contains("Device_ShowRegistrationCode", page, StringComparison.Ordinal);
        Assert.Contains("Device_RegistrationExpiresIn", page, StringComparison.Ordinal);
        Assert.Contains("Device_Revoke", page, StringComparison.Ordinal);
        Assert.Contains("pos-devices__card", page, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"qr\")", page, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"warning\")", page, StringComparison.Ordinal);
        Assert.Contains("LocalQrCodeRenderer", page, StringComparison.Ordinal);
        Assert.DoesNotContain("WP06", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Org_business_qr_page_uses_organization_public_identity()
    {
        var page = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Organization", "OrgBusinessQr.razor"));
        Assert.Contains("@page \"/org/business-qr\"", page, StringComparison.Ordinal);
        Assert.Contains("GetOrganizationPublicIdentityAsync", page, StringComparison.Ordinal);
        Assert.Contains("exits://qr/v1/organization/", page, StringComparison.Ordinal);
        Assert.Contains("Org_BusinessQrSubtitle", page, StringComparison.Ordinal);
        Assert.Contains("Use this to identify or connect with this business.",
            File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx")),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Platform_access_client_wires_qr_and_registration_token_apis()
    {
        var client = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.ApiClient",
            "PlatformAccessClient.cs"));
        Assert.Contains("/api/v1/organizations/", client, StringComparison.Ordinal);
        Assert.Contains("/public-identity", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/organizations/resolve-public-id", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/qr/resolve", client, StringComparison.Ordinal);
        Assert.Contains("registration-tokens", client, StringComparison.Ordinal);
        Assert.Contains("CreatePosDeviceRegistrationTokenAsync", client, StringComparison.Ordinal);
        Assert.Contains("RedeemPosDeviceRegistrationTokenAsync", client, StringComparison.Ordinal);
        Assert.Contains("ResolveQrAsync", client, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_resolve_rejects_non_personal_qr_locally()
    {
        var resolve = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Personal", "PublicUserResolve.razor"));
        Assert.Contains("ExItsQrPurposeGuard", resolve, StringComparison.Ordinal);
        Assert.Contains("ExItsQrPurposeGuard.Personal", resolve, StringComparison.Ordinal);
        Assert.Contains("MismatchMessage", resolve, StringComparison.Ordinal);
    }

    private static string MauiProject() =>
        Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
