using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;
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
    public async Task CompleteExternalLogin_accepts_a_mailbox_or_comma_as_the_google_name()
    {
        var sut = CreateSut(out var users, out _, out _);
        var mailbox = await sut.ExecuteAsync(
            new ExternalLoginIdentity("google", "sub-mail", "person.name@gmail.com", true, "person.name@gmail.com"),
            null,
            null);
        Assert.True(mailbox.IsSuccess);
        Assert.Equal("person.name", (await users.GetByNormalizedEmailAsync("person.name@gmail.com"))!.DisplayName);

        var comma = await sut.ExecuteAsync(
            new ExternalLoginIdentity("google", "sub-comma", "jr.owner@gmail.com", true, "Uytoco, Jr."),
            null,
            null);
        Assert.True(comma.IsSuccess);
        Assert.Equal("Uytoco Jr.", (await users.GetByNormalizedEmailAsync("jr.owner@gmail.com"))!.DisplayName);
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

    [Fact]
    public async Task CompleteExternalLogin_stores_the_provider_picture_until_the_user_uploads_one()
    {
        var profiles = new MemoryPersonalProfiles();
        var sut = CreateSut(out _, out _, out _, profiles);
        var picture = "https://lh3.googleusercontent.com/a/photo";
        var created = await sut.ExecuteAsync(
            new ExternalLoginIdentity("google", "sub-photo", "photo@example.com", true, "Photo User", picture),
            null,
            null);

        Assert.True(created.IsSuccess);
        Assert.Equal(picture, profiles.Profile!.ProfilePhotoUrl);

        var uploaded = PersonalUserProfile.Create(PlatformUserId.From(created.Value!.UserId), T0);
        uploaded.SetProfilePhotoUrl("/api/v1/personal/profile/photo?v=1", T0);
        profiles.Profile = uploaded;
        var again = await sut.ExecuteAsync(
            new ExternalLoginIdentity("google", "sub-photo", "photo@example.com", true, "Photo User", "https://lh3.googleusercontent.com/a/other"),
            null,
            null);

        Assert.True(again.IsSuccess);
        Assert.Equal("/api/v1/personal/profile/photo?v=1", profiles.Profile.ProfilePhotoUrl);
    }

    private static CompleteExternalLogin CreateSut(
        out InMemoryPlatformUserRepository users,
        out InMemoryPlatformUserCredentialRepository credentials,
        out InMemoryPlatformExternalLoginRepository externals,
        IPersonalUserProfileRepository? personalProfiles = null)
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
                Options.Create(new PlatformMfaOptions())),
            personalProfiles);
    }

    private sealed class MemoryPersonalProfiles : IPersonalUserProfileRepository
    {
        public PersonalUserProfile? Profile { get; set; }

        public Task<PersonalUserProfile?> GetByUserAsync(
            PlatformUserId userIdentityId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Profile is not null && Profile.UserIdentityId == userIdentityId ? Profile : null);

        public Task AddAsync(PersonalUserProfile profile, CancellationToken cancellationToken = default)
        {
            Profile = profile;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PersonalUserProfile profile, CancellationToken cancellationToken = default)
        {
            Profile = profile;
            return Task.CompletedTask;
        }
    }
}

