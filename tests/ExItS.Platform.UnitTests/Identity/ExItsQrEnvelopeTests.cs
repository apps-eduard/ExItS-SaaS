using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class ExItsQrEnvelopeTests
{
    [Fact]
    public void Build_and_parse_personal_canonical()
    {
        var payload = ExItsQrEnvelope.Build(ExItsQrPurpose.Personal, "EX-4827-1936");
        Assert.Equal("exits://qr/v1/personal/EX-4827-1936", payload);
        var parsed = ExItsQrEnvelope.Parse(payload);
        Assert.Equal(ExItsQrPurpose.Personal, parsed.Purpose);
        Assert.Equal("EX-4827-1936", parsed.Subject);
    }

    [Fact]
    public void Build_and_parse_organization()
    {
        var payload = ExItsQrEnvelope.Build(ExItsQrPurpose.Organization, "ORG001842");
        Assert.Equal("exits://qr/v1/organization/ORG001842", payload);
        var parsed = ExItsQrEnvelope.Parse(payload);
        Assert.Equal(ExItsQrPurpose.Organization, parsed.Purpose);
        Assert.Equal("ORG001842", parsed.Subject);
    }

    [Fact]
    public void Build_and_parse_pos_device_registration()
    {
        const string token = "abcdefghijklmnopqrstuvwxyz012345";
        var payload = ExItsQrEnvelope.Build(ExItsQrPurpose.PosDeviceRegistration, token);
        Assert.Equal("exits://qr/v1/pos-device-registration/" + token, payload);
        var parsed = ExItsQrEnvelope.Parse(payload);
        Assert.Equal(ExItsQrPurpose.PosDeviceRegistration, parsed.Purpose);
        Assert.Equal(token, parsed.Subject);
    }

    [Fact]
    public void Parse_accepts_legacy_personal_payload()
    {
        var parsed = ExItsQrEnvelope.Parse("exits://user/v1/EX-4827-1936");
        Assert.Equal(ExItsQrPurpose.Personal, parsed.Purpose);
        Assert.Equal("EX-4827-1936", parsed.Subject);
    }

    [Theory]
    [InlineData("")]
    [InlineData("exits://qr/v2/personal/EX-4827-1936")]
    [InlineData("exits://qr/v1/unknown/EX-4827-1936")]
    [InlineData("exits://qr/v1/personal/")]
    [InlineData("not-a-qr")]
    public void Parse_rejects_unknown_version_type_or_malformed(string payload)
    {
        Assert.Throws<DomainException>(() => ExItsQrEnvelope.Parse(payload));
    }

    [Fact]
    public void EnsureExpectedPurpose_rejects_mismatch()
    {
        var personal = ExItsQrEnvelope.Parse("exits://qr/v1/personal/EX-4827-1936");
        Assert.Throws<DomainException>(() =>
            ExItsQrEnvelope.EnsureExpectedPurpose(personal, ExItsQrPurpose.PosDeviceRegistration));

        var org = ExItsQrEnvelope.Parse("exits://qr/v1/organization/ORG001842");
        Assert.Throws<DomainException>(() =>
            ExItsQrEnvelope.EnsureExpectedPurpose(org, ExItsQrPurpose.Personal));
    }
}
