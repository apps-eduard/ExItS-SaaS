using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.UnitTests.Support;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class ValidateAndRenewPlatformSessionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);
    private const string OpaqueToken = "kizy-session";
    private const string TokenHash = "hash:kizy-session";

    [Fact]
    public void Sliding_renewal_skips_persist_when_activity_is_recent()
    {
        Assert.False(ValidateAndRenewPlatformSession.ShouldPersistSlidingRenewal(T0, T0, persistSeconds: 30));
        Assert.False(ValidateAndRenewPlatformSession.ShouldPersistSlidingRenewal(T0, T0.AddSeconds(29), persistSeconds: 30));
        Assert.True(ValidateAndRenewPlatformSession.ShouldPersistSlidingRenewal(T0, T0.AddSeconds(30), persistSeconds: 30));
        Assert.True(ValidateAndRenewPlatformSession.ShouldPersistSlidingRenewal(T0, T0.AddSeconds(1), persistSeconds: 0));
    }

    [Fact]
    public async Task Recent_activity_does_not_write_session_on_parallel_auth()
    {
        var harness = await CreateHarnessAsync(T0);
        harness.Clock.UtcNow = T0.AddSeconds(5);

        var result = await harness.Sut.ExecuteAsync(OpaqueToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, harness.UnitOfWork.SaveCount);
        Assert.Equal(T0, harness.Session.LastActivityAtUtc);
    }

    [Fact]
    public async Task Stale_activity_persists_sliding_renewal()
    {
        var harness = await CreateHarnessAsync(T0);
        harness.Clock.UtcNow = T0.AddSeconds(30);

        var result = await harness.Sut.ExecuteAsync(OpaqueToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, harness.UnitOfWork.SaveCount);
        Assert.Equal(T0.AddSeconds(30), harness.Session.LastActivityAtUtc);
    }

    [Fact]
    public async Task Concurrent_sliding_renewal_conflict_still_authenticates()
    {
        var harness = await CreateHarnessAsync(T0);
        harness.Clock.UtcNow = T0.AddSeconds(30);
        harness.UnitOfWork.ThrowConflict = true;

        var result = await harness.Sut.ExecuteAsync(OpaqueToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(harness.Session.Id.Value, result.Value.SessionId);
        Assert.Equal(1, harness.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Concurrent_sliding_renewal_conflict_rejects_revoked_session()
    {
        var harness = await CreateHarnessAsync(T0);
        harness.Clock.UtcNow = T0.AddSeconds(30);
        harness.UnitOfWork.ThrowConflict = true;
        harness.UnitOfWork.RevokeSessionOnSave = harness.Session;

        var result = await harness.Sut.ExecuteAsync(OpaqueToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SessionInvalid, result.ErrorCode);
    }

    private static async Task<Harness> CreateHarnessAsync(DateTimeOffset utcNow)
    {
        var users = new InMemoryPlatformUserRepository();
        var credentials = new InMemoryPlatformUserCredentialRepository();
        var sessions = new InMemoryPlatformAuthSessionRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var organizations = new InMemoryPlatformOrganizationRepository();
        var preferences = new InMemoryOrganizationContextPreferenceRepository();
        var uow = new RecordingUnitOfWork();
        var clock = new FixedClock(utcNow);
        var user = PlatformUser.Create("kizy", "Kizy Owner", "kizy@example.com", utcNow);
        await users.AddAsync(user);
        var credential = PlatformUserCredential.Create(user.Id, "hash", PlatformUserCredential.Pbkdf2Sha256V1, utcNow);
        await credentials.AddAsync(credential);
        var session = PlatformAuthSession.Create(
            user.Id,
            AccountProfileId.New(),
            AccountClass.Personal,
            TokenHash,
            credential.SecurityStamp,
            utcNow,
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(12));
        await sessions.AddAsync(session);

        var sut = new ValidateAndRenewPlatformSession(
            users,
            credentials,
            sessions,
            memberships,
            organizations,
            preferences,
            new StubSessionTokens(),
            uow,
            clock,
            Options.Create(new PlatformSessionOptions
            {
                SlidingRenewal = true,
                IdleTimeoutMinutes = 30,
                SlidingRenewalPersistSeconds = 30
            }),
            new PlatformMfaReadinessService(
                new NullPlatformMfaFactorStore(),
                Options.Create(new PlatformMfaOptions())));

        return new Harness(sut, session, uow, clock);
    }

    private sealed record Harness(
        ValidateAndRenewPlatformSession Sut,
        PlatformAuthSession Session,
        RecordingUnitOfWork UnitOfWork,
        FixedClock Clock);

    private sealed class StubSessionTokens : IPlatformSessionTokenService
    {
        public string CreateOpaqueToken() => OpaqueToken;

        public string HashToken(string opaqueToken) => $"hash:{opaqueToken}";
    }

    private sealed class RecordingUnitOfWork : IPlatformUnitOfWork
    {
        public int SaveCount { get; private set; }
        public bool ThrowConflict { get; set; }
        public PlatformAuthSession? RevokeSessionOnSave { get; set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            if (RevokeSessionOnSave is not null)
            {
                RevokeSessionOnSave.Revoke(RevokeSessionOnSave.LastActivityAtUtc);
            }

            if (ThrowConflict)
            {
                throw new PersistenceConflictException(
                    ApplicationErrorCodes.ConcurrencyConflict,
                    "A concurrency conflict occurred while saving changes.");
            }

            return Task.CompletedTask;
        }
    }
}
