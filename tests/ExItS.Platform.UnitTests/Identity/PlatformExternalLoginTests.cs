using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.UnitTests.Support;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformExternalLoginTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateForExternalLogin_rejects_password_login_flag()
    {
        var credential = PlatformUserCredential.CreateForExternalLogin(PlatformUserId.New(), T0, emailVerified: true);
        Assert.False(credential.SupportsPasswordLogin);
        Assert.NotNull(credential.EmailVerifiedAtUtc);
        Assert.False(string.IsNullOrWhiteSpace(credential.SecurityStamp));
    }

    [Fact]
    public async Task CompleteExternalLogin_creates_user_without_roles_and_issues_session()
    {
        var sut = CreateSut(out var users, out var credentials, out _);
        var result = await sut.ExecuteAsync(
            new ExternalLoginIdentity("google", "sub-1", "owner@example.com", true, "Store Owner"),
            "127.0.0.1",
            "test-agent");

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.SessionToken));
        Assert.Equal(0, result.Value.ActiveOrganizationCount);
        Assert.Equal("None", result.Value.OrganizationSelectionState);
        Assert.Equal("Personal", result.Value.AccountClass);
        Assert.Equal(1, users.AddCount);
        Assert.False((await credentials.GetByUserIdAsync(PlatformUserId.From(result.Value.UserId)))!.SupportsPasswordLogin);
    }

    [Fact]
    public async Task CompleteExternalLogin_links_existing_email_without_creating_duplicate_user()
    {
        var sut = CreateSut(out var users, out _, out var externals);
        var existing = PlatformUser.Create("owner1", "Owner One", "owner@example.com", T0);
        await users.AddAsync(existing);

        var result = await sut.ExecuteAsync(
            new ExternalLoginIdentity("facebook", "fb-99", "owner@example.com", true, "Owner One"),
            null,
            null);

        Assert.True(result.IsSuccess);
        Assert.Equal(existing.Id.Value, result.Value!.UserId);
        Assert.Equal(1, users.AddCount);
        Assert.NotNull(await externals.FindByProviderSubjectAsync("facebook", "fb-99"));
    }

    [Fact]
    public async Task CompleteExternalLogin_requires_verified_email()
    {
        var sut = CreateSut(out _, out _, out _);
        var result = await sut.ExecuteAsync(
            new ExternalLoginIdentity("google", "sub-2", "x@example.com", false, "X"),
            null,
            null);
        Assert.Equal(ApplicationErrorCodes.ExternalAuthEmailUnverified, result.ErrorCode);
    }

    private static CompleteExternalLogin CreateSut(
        out InMemoryPlatformUserRepository users,
        out InMemoryPlatformUserCredentialRepository credentials,
        out InMemoryPlatformExternalLoginRepository externals)
    {
        users = new InMemoryPlatformUserRepository();
        credentials = new InMemoryPlatformUserCredentialRepository();
        externals = new InMemoryPlatformExternalLoginRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var roles = new InMemoryPlatformRoleAssignmentRepository();
        var profiles = new InMemoryAccountProfileRepository();
        var ensure = new EnsureAccountProfilesForUser(
            profiles,
            roles,
            memberships,
            new NoOpUnitOfWork(),
            new FixedClock(T0));

        return new CompleteExternalLogin(
            users,
            credentials,
            externals,
            new InMemoryPlatformAuthSessionRepository(),
            memberships,
            new InMemoryPlatformOrganizationRepository(),
            new InMemoryOrganizationContextPreferenceRepository(),
            ensure,
            new StubSessionTokenService(),
            new NoOpAuditWriter(),
            new NoOpUnitOfWork(),
            new FixedClock(T0),
            Options.Create(new PlatformSessionOptions()),
            new PlatformMfaReadinessService(
                new NullPlatformMfaFactorStore(),
                Options.Create(new PlatformMfaOptions())));
    }
}

