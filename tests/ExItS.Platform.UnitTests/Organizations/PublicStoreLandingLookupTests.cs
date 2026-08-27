using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class PublicStoreLandingLookupTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Active_organization_returns_minimal_public_landing()
    {
        var orgs = new InMemoryPlatformOrganizationRepository();
        var audit = new NoOpAuditWriter();
        var org = PlatformOrganization.Create("Kizy Store", "kizy", T0);
        org.AssignPublicOrganizationId("ORG123456", T0);
        await orgs.AddAsync(org);

        var useCase = new LookupPublicStoreLanding(orgs, audit);
        var result = await useCase.ExecuteAsync("ORG123456");

        Assert.True(result.IsSuccess);
        Assert.Equal("ORG123456", result.Value!.PublicOrganizationId);
        Assert.Equal("Kizy Store", result.Value.DisplayName);
        Assert.True(result.Value.OrderingAvailable);

        var dtoType = typeof(PublicStoreLandingDto);
        Assert.Null(dtoType.GetProperty("OrganizationId"));
        Assert.Null(dtoType.GetProperty("ContactEmail"));
        Assert.Null(dtoType.GetProperty("ContactPhone"));
        Assert.Null(dtoType.GetProperty("LegalName"));
        Assert.Equal(3, dtoType.GetProperties().Length);
    }

    [Fact]
    public async Task Unknown_organization_fails_safe_generic()
    {
        var useCase = new LookupPublicStoreLanding(new InMemoryPlatformOrganizationRepository(), new NoOpAuditWriter());
        var result = await useCase.ExecuteAsync("ORG999999");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OrganizationNotFound, result.ErrorCode);
        Assert.Equal("This store is unavailable.", result.ErrorMessage);
    }

    [Fact]
    public async Task Suspended_organization_fails_safe_without_reason_leak()
    {
        var orgs = new InMemoryPlatformOrganizationRepository();
        var org = PlatformOrganization.Create("Hidden Store", "hidden", T0);
        org.AssignPublicOrganizationId("ORG654321", T0);
        org.Suspend(T0);
        await orgs.AddAsync(org);

        var useCase = new LookupPublicStoreLanding(orgs, new NoOpAuditWriter());
        var result = await useCase.ExecuteAsync("ORG654321");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OrganizationNotFound, result.ErrorCode);
        Assert.DoesNotContain("suspend", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Legacy_organization_qr_payload_resolves_same_as_public_id()
    {
        var orgs = new InMemoryPlatformOrganizationRepository();
        var org = PlatformOrganization.Create("Legacy QR Store", "legacy", T0);
        org.AssignPublicOrganizationId("ORG111222", T0);
        await orgs.AddAsync(org);

        var useCase = new LookupPublicStoreLanding(orgs, new NoOpAuditWriter());
        var payload = PublicOrganizationIdRules.BuildQrPayload("ORG111222");
        var result = await useCase.ExecuteAsync(payload);

        Assert.True(result.IsSuccess);
        Assert.Equal("ORG111222", result.Value!.PublicOrganizationId);
    }
}
