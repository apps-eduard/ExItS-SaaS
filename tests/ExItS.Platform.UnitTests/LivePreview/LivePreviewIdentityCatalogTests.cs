using ExItS.Platform.Application.LivePreview;

namespace ExItS.Platform.UnitTests.LivePreview;

public sealed class LivePreviewIdentityCatalogTests
{
    [Fact]
    public void Catalog_includes_required_preview_identities_with_existing_role_codes()
    {
        Assert.Contains(LivePreviewIdentityCatalog.All, i => i.Key == "platform-admin" && i.AssignPlatformAdministrator);
        Assert.Contains(LivePreviewIdentityCatalog.All, i => i.Key == "org-admin" && i.PosLocalRoleCode == "Owner");
        Assert.Contains(LivePreviewIdentityCatalog.All, i => i.Key == "pos-cashier" && i.PosLocalRoleCode == "Cashier");
        Assert.Contains(LivePreviewIdentityCatalog.All, i => i.Key == "no-pos" && !i.GrantPosProductAccess);
        Assert.Contains(LivePreviewIdentityCatalog.All, i => i.Key == "no-org" && !i.HasOrganizationMembership);

        Assert.Equal(5, LivePreviewIdentityCatalog.All.Count);
        Assert.NotNull(LivePreviewIdentityCatalog.FindByKey("ORG-ADMIN"));
        Assert.Null(LivePreviewIdentityCatalog.FindByKey("missing"));
    }
}
