using ExItS.Platform.Application.LocalValidation;

namespace ExItS.Platform.UnitTests.LocalValidation;

public sealed class BnplLocalValidationAccessFixturesTests
{
    [Fact]
    public void Maria_and_carlo_have_bnpl_access_fixtures_ana_and_daniel_do_not()
    {
        Assert.True(BnplLocalValidationAccessFixtures.MariaSantos.HasBnplProductAccess);
        Assert.Equal(BnplLocalValidationAccessFixtures.OwnerPreset, BnplLocalValidationAccessFixtures.MariaSantos.CapabilityPreset);
        Assert.True(BnplLocalValidationAccessFixtures.MariaSantos.OrganizationWideBranchAccess);

        Assert.True(BnplLocalValidationAccessFixtures.CarloReyes.HasBnplProductAccess);
        Assert.Equal(BnplLocalValidationAccessFixtures.SalesPreset, BnplLocalValidationAccessFixtures.CarloReyes.CapabilityPreset);
        Assert.False(BnplLocalValidationAccessFixtures.CarloReyes.OrganizationWideBranchAccess);

        Assert.False(BnplLocalValidationAccessFixtures.AnaCruz.HasBnplProductAccess);
        Assert.False(BnplLocalValidationAccessFixtures.DanielGarcia.HasBnplProductAccess);
    }

    [Fact]
    public void Fixture_keys_align_with_local_validation_identity_catalog()
    {
        Assert.NotNull(LocalValidationIdentityCatalog.FindByKey(BnplLocalValidationAccessFixtures.MariaSantosKey));
        Assert.NotNull(LocalValidationIdentityCatalog.FindByKey(BnplLocalValidationAccessFixtures.CarloReyesKey));
        Assert.True(LocalValidationIdentityCatalog.FindByKey(BnplLocalValidationAccessFixtures.MariaSantosKey)!.GrantBnplProductAccess);
        Assert.True(LocalValidationIdentityCatalog.FindByKey(BnplLocalValidationAccessFixtures.CarloReyesKey)!.GrantBnplProductAccess);
        Assert.False(LocalValidationIdentityCatalog.FindByKey(BnplLocalValidationAccessFixtures.AnaCruzKey)!.GrantBnplProductAccess);
    }
}
