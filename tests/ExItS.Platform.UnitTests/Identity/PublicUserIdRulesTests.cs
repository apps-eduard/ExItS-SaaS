using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PublicUserIdRulesTests
{
    [Theory]
    [InlineData("EX-4827-1936", "EX-4827-1936")]
    [InlineData("ex-4827-1936", "EX-4827-1936")]
    [InlineData("EX48271936", "EX-4827-1936")]
    [InlineData("exits://user/v1/EX-4827-1936", "EX-4827-1936")]
    [InlineData("exits://user/v1/ex-4827-1936", "EX-4827-1936")]
    [InlineData("exits://qr/v1/personal/EX-4827-1936", "EX-4827-1936")]
    [InlineData("exits://qr/v1/personal/ex-4827-1936", "EX-4827-1936")]
    public void Normalize_accepts_canonical_compact_and_qr_payload(string input, string expected)
    {
        Assert.Equal(expected, PublicUserIdRules.Normalize(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("EX-48-1936")]
    [InlineData("EX-ABCD-1936")]
    [InlineData("user@example.com")]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    public void Normalize_rejects_malformed_and_sensitive_looking_values(string input)
    {
        Assert.Throws<DomainException>(() => PublicUserIdRules.Normalize(input));
    }

    [Fact]
    public void Qr_payload_contains_only_versioned_public_reference()
    {
        var payload = PublicUserIdRules.BuildQrPayload("EX-4827-1936");
        Assert.Equal("exits://qr/v1/personal/EX-4827-1936", payload);
        Assert.DoesNotContain("@", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("token", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("role", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("balance", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateRandom_matches_canonical_format_and_is_case_insensitive_unique_shape()
    {
        var a = PublicUserIdRules.GenerateRandom();
        var b = PublicUserIdRules.GenerateRandom();
        Assert.Matches(@"^EX-\d{4}-\d{4}$", a);
        Assert.Matches(@"^EX-\d{4}-\d{4}$", b);
        Assert.Equal(a, PublicUserIdRules.Normalize(a.ToLowerInvariant()));
    }
}

public sealed class PublicIdentityUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetOrAssign_assigns_immutable_id_and_resolve_is_exact_match_only()
    {
        var users = new InMemoryPlatformUserRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var audit = new NoOpAuditWriter();
        var generator = new SequentialPublicUserIdGenerator();
        var create = new CreatePlatformUser(users, uow, clock, generator);
        var user = (await create.ExecuteAsync("ada", "Ada Lovelace", "ada@example.com")).Value!;

        var get = new GetOrAssignPublicIdentity(users, generator, uow, clock, audit);
        var mine = await get.ExecuteAsync(user.Id);
        Assert.True(mine.IsSuccess);
        Assert.Equal("EX-0000-0001", mine.Value!.PublicUserId);
        Assert.Equal("exits://qr/v1/personal/EX-0000-0001", mine.Value.QrPayload);

        var again = await get.ExecuteAsync(user.Id);
        Assert.Equal(mine.Value.PublicUserId, again.Value!.PublicUserId);

        var other = (await create.ExecuteAsync("bob", "Bob Builder", "bob@example.com")).Value!;
        var resolve = new ResolvePublicUserId(users, audit);
        var found = await resolve.ExecuteAsync(other.Id, new ResolvePublicUserIdRequest("exits://user/v1/EX-0000-0001", "utang"));
        Assert.True(found.IsSuccess);
        Assert.False(found.Value!.IsSelf);
        Assert.Equal("Ada Lovelace", found.Value.DisplayName);
        Assert.StartsWith("a***@", found.Value.MaskedEmail, StringComparison.OrdinalIgnoreCase);

        var self = await resolve.ExecuteAsync(user.Id, new ResolvePublicUserIdRequest("EX-0000-0001", "self-check"));
        Assert.True(self.Value!.IsSelf);

        var missing = await resolve.ExecuteAsync(user.Id, new ResolvePublicUserIdRequest("EX-9999-9999", "probe"));
        Assert.False(missing.IsSuccess);

        var partial = await resolve.ExecuteAsync(user.Id, new ResolvePublicUserIdRequest("EX-0000", "probe"));
        Assert.False(partial.IsSuccess);
    }

    [Fact]
    public void AssignPublicUserId_is_immutable()
    {
        var user = PlatformUser.Create("ada", "Ada Lovelace", "ada@example.com", T0);
        user.AssignPublicUserId("EX-1111-2222", T0);
        user.AssignPublicUserId("EX-1111-2222", T0);
        Assert.Throws<DomainException>(() => user.AssignPublicUserId("EX-3333-4444", T0));
    }
}
