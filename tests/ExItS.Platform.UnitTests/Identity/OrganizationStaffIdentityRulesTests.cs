using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class StaffLoginNameRulesTests
{
    [Fact]
    public void NormalizeLocalPartFromEmail_strips_non_alnum_and_truncates()
    {
        Assert.Equal("mariasantostag", StaffLoginNameRules.NormalizeLocalPartFromEmail("Maria.Santos+tag@example.com"));
        Assert.Equal("maria", StaffLoginNameRules.NormalizeLocalPartFromEmail("maria@example.com"));
        Assert.Equal("staff", StaffLoginNameRules.NormalizeLocalPartFromEmail("!!!@example.com"));
        Assert.Equal(32, StaffLoginNameRules.NormalizeLocalPartFromEmail($"{new string('a', 40)}@example.com").Length);
    }

    [Fact]
    public void Build_applies_collision_suffix_maria_then_maria2()
    {
        Assert.Equal("maria@ORG001842", StaffLoginNameRules.Build("maria", "ORG001842"));
        Assert.Equal("maria2@ORG001842", StaffLoginNameRules.Build("maria", "org001842", collisionSuffix: 2));
        Assert.Equal(
            "maria@org001842",
            PlatformUser.NormalizeEmail(StaffLoginNameRules.Build("maria", "ORG001842")));
    }

    [Fact]
    public void FormatForDisplay_uppercases_org_host()
    {
        Assert.Equal("maria@ORG001842", StaffLoginNameRules.FormatForDisplay("Maria@org001842"));
    }

    [Fact]
    public void DeriveUsername_is_stable_and_username_safe()
    {
        var username = StaffLoginNameRules.DeriveUsername("maria@ORG001842");
        Assert.Equal("maria_org001842", username);
        Assert.Matches(@"^[a-z0-9][a-z0-9._-]*[a-z0-9]$", username);
    }

    [Fact]
    public void Build_rejects_invalid_local_part()
    {
        var ex = Assert.Throws<DomainException>(() => StaffLoginNameRules.Build("Maria!", "ORG001842"));
        Assert.Equal(DomainErrorCodes.InvalidEmail, ex.ErrorCode);
    }
}

public sealed class PublicOrganizationIdRulesTests
{
    [Theory]
    [InlineData("ORG001842", "ORG001842")]
    [InlineData("org001842", "ORG001842")]
    [InlineData(" Org184200 ", "ORG184200")]
    public void Normalize_accepts_canonical_and_case_variants(string input, string expected)
    {
        Assert.Equal(expected, PublicOrganizationIdRules.Normalize(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ORG1842")]
    [InlineData("ORGABCDEF")]
    [InlineData("EX-4827-1936")]
    public void Normalize_rejects_malformed_values(string input)
    {
        var ex = Assert.Throws<DomainException>(() => PublicOrganizationIdRules.Normalize(input));
        Assert.Equal(DomainErrorCodes.InvalidPublicOrganizationId, ex.ErrorCode);
    }

    [Fact]
    public void GenerateRandom_matches_canonical_format()
    {
        var id = PublicOrganizationIdRules.GenerateRandom();
        Assert.Matches(@"^ORG\d{6}$", id);
        Assert.Equal(id, PublicOrganizationIdRules.Normalize(id.ToLowerInvariant()));
    }

    [Fact]
    public void TryNormalize_returns_false_for_invalid()
    {
        Assert.False(PublicOrganizationIdRules.TryNormalize("bad", out var normalized));
        Assert.Equal(string.Empty, normalized);
    }
}
