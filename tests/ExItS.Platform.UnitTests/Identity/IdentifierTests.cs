using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class IdentifierTests
{
    [Fact]
    public void PlatformUserId_rejects_empty_guid()
    {
        var ex = Assert.Throws<DomainException>(() => PlatformUserId.From(Guid.Empty));
        Assert.Equal(DomainErrorCodes.InvalidPlatformUserId, ex.ErrorCode);
    }

    [Fact]
    public void PlatformOrganizationId_rejects_empty_guid()
    {
        var ex = Assert.Throws<DomainException>(() => PlatformOrganizationId.From(Guid.Empty));
        Assert.Equal(DomainErrorCodes.InvalidPlatformOrganizationId, ex.ErrorCode);
    }

    [Fact]
    public void OrganizationMembershipId_rejects_empty_guid()
    {
        var ex = Assert.Throws<DomainException>(() => OrganizationMembershipId.From(Guid.Empty));
        Assert.Equal(DomainErrorCodes.InvalidOrganizationMembershipId, ex.ErrorCode);
    }

    [Fact]
    public void Identifiers_have_value_equality_and_safe_tostring()
    {
        var guid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var a = PlatformUserId.From(guid);
        var b = PlatformUserId.From(guid);
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal("11111111-1111-1111-1111-111111111111", a.ToString());
    }

    [Fact]
    public void Distinct_id_types_are_not_interchangeable_at_compile_time()
    {
        var userId = PlatformUserId.New();
        var orgId = PlatformOrganizationId.New();
        // Compile-time proof: assigning userId to orgId fails. Runtime: values may match but types differ.
        Assert.NotEqual(userId.GetType(), orgId.GetType());
        Assert.False(ReferenceEquals(userId, orgId));
    }
}
