using ExItS.Platform.Application.Integration.Pos;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationCatalogProvenanceTests
{
    [Fact]
    public void ResolveSourceType_prefers_template_over_global_product()
    {
        var templateId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var globalId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        Assert.Equal(
            OrganizationCatalogProvenance.GlobalTemplate,
            OrganizationCatalogProvenance.ResolveSourceType(templateId, globalId));
    }

    [Fact]
    public void ResolveSourceType_maps_global_product_without_template()
    {
        var globalId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        Assert.Equal(
            OrganizationCatalogProvenance.GlobalCatalog,
            OrganizationCatalogProvenance.ResolveSourceType(null, globalId));
    }

    [Fact]
    public void ResolveSourceType_maps_merchant_created_when_no_refs()
    {
        Assert.Equal(
            OrganizationCatalogProvenance.MerchantCreated,
            OrganizationCatalogProvenance.ResolveSourceType(null, null));
    }
}
