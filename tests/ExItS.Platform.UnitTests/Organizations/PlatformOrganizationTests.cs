using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class PlatformOrganizationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddMinutes(5);

    [Fact]
    public void Create_valid_organization()
    {
        var org = PlatformOrganization.Create("Acme Clinics Group", "Acme-Clinics", T0);
        Assert.Equal(OrganizationStatus.Active, org.Status);
        Assert.Equal("Acme Clinics Group", org.DisplayName);
        Assert.Equal("acme-clinics", org.Slug);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Create_rejects_invalid_name(string name)
    {
        var ex = Assert.Throws<DomainException>(() => PlatformOrganization.Create(name, "acme", T0));
        Assert.Equal(DomainErrorCodes.InvalidDisplayName, ex.ErrorCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-acme")]
    [InlineData("Acme_Clinics")]
    [InlineData("a")]
    public void Create_rejects_invalid_slug(string slug)
    {
        var ex = Assert.Throws<DomainException>(() => PlatformOrganization.Create("Acme Group", slug, T0));
        Assert.Equal(DomainErrorCodes.InvalidOrganizationSlug, ex.ErrorCode);
    }

    [Fact]
    public void Suspend_reactivate_and_close_follow_transitions()
    {
        var org = PlatformOrganization.Create("Acme Group", "acme-group", T0);
        org.Suspend(T1);
        Assert.Equal(OrganizationStatus.Suspended, org.Status);

        var t2 = T1.AddMinutes(1);
        org.Reactivate(t2);
        Assert.Equal(OrganizationStatus.Active, org.Status);

        var t3 = t2.AddMinutes(1);
        org.Close(t3);
        Assert.Equal(OrganizationStatus.Closed, org.Status);
    }

    [Fact]
    public void Closed_organization_cannot_reactivate_or_rename()
    {
        var org = PlatformOrganization.Create("Acme Group", "acme-group", T0);
        org.Close(T1);

        var reactivate = Assert.Throws<DomainException>(() => org.Reactivate(T1.AddMinutes(1)));
        Assert.Equal(DomainErrorCodes.InvalidOrganizationStatusTransition, reactivate.ErrorCode);

        var rename = Assert.Throws<DomainException>(() => org.Rename("New Name", T1.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.OrganizationNotActive, rename.ErrorCode);
    }
}
