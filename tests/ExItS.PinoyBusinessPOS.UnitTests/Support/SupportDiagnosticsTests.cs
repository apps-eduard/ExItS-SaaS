using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Support;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.UnitTests.Support;

public sealed class SupportDiagnosticsTests
{
    [Fact]
    public async Task Personal_user_can_view_personal_diagnostics_via_service()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var session = PersonalSession(userId, profileId);
        var personal = new StubProvider(SupportDiagnosticsScope.Personal, SupportDiagnosticsAccessKind.Allowed)
        {
            Snapshot = SamplePersonal(userId, profileId, pending: 2, failed: 1)
        };
        var org = new StubProvider(SupportDiagnosticsScope.Organization, SupportDiagnosticsAccessKind.Forbidden);
        var svc = new SupportDiagnosticsService(new FakeCurrentUser(session), [personal, org]);

        var result = await svc.CaptureForCurrentSessionAsync();

        Assert.Equal(SupportDiagnosticsAccessKind.Allowed, result.Access);
        Assert.Equal(SupportDiagnosticsScope.Personal, result.Snapshot!.Scope);
        Assert.Equal(2, result.Snapshot.PendingSyncCount);
        Assert.Null(result.Snapshot.OrganizationId);
    }

    [Fact]
    public async Task Owner_can_view_organization_diagnostics_via_service()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var session = OrgSession(userId, orgId);
        var personal = new StubProvider(SupportDiagnosticsScope.Personal, SupportDiagnosticsAccessKind.Forbidden);
        var org = new StubProvider(SupportDiagnosticsScope.Organization, SupportDiagnosticsAccessKind.Allowed)
        {
            Snapshot = SampleOrg(userId, orgId, pending: 5, failed: 3)
        };
        var svc = new SupportDiagnosticsService(new FakeCurrentUser(session), [personal, org]);

        var result = await svc.CaptureForCurrentSessionAsync();

        Assert.Equal(SupportDiagnosticsAccessKind.Allowed, result.Access);
        Assert.Equal(orgId, result.Snapshot!.OrganizationId);
        Assert.Null(result.Snapshot.PersonalProfileId);
    }

    [Fact]
    public async Task Non_owner_blocked_even_with_org_session()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var provider = CreateOrgProvider(userId, orgId, owner: false, pending: 4, failed: 2);

        Assert.Equal(
            SupportDiagnosticsAccessKind.Forbidden,
            await provider.EvaluateAccessAsync(OrgSession(userId, orgId)));
    }

    [Fact]
    public async Task Owner_capture_scoped_counts_and_no_personal_profile()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var provider = CreateOrgProvider(userId, orgId, owner: true, pending: 4, failed: 2);

        var snap = await provider.CaptureAsync(OrgSession(userId, orgId));
        Assert.Equal(orgId, snap.OrganizationId);
        Assert.Equal(4, snap.PendingSyncCount);
        Assert.Equal(2, snap.FailedSyncCount);
        Assert.Null(snap.PersonalProfileId);
        Assert.Equal("Owner", snap.CurrentRole);
    }

    [Fact]
    public async Task Staff_locked_identity_cannot_use_personal_diagnostics()
    {
        var provider = CreatePersonalProvider(Guid.NewGuid(), Guid.NewGuid());
        var staffLike = PersonalSession(Guid.NewGuid(), Guid.NewGuid()) with
        {
            OrganizationContextLocked = true
        };

        Assert.Equal(SupportDiagnosticsAccessKind.Forbidden, await provider.EvaluateAccessAsync(staffLike));
    }

    [Fact]
    public async Task Personal_capture_never_includes_organization_fields()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var provider = CreatePersonalProvider(userId, profileId, pending: 3, failed: 1);
        var snap = await provider.CaptureAsync(PersonalSession(userId, profileId));

        Assert.Equal(SupportDiagnosticsScope.Personal, snap.Scope);
        Assert.Null(snap.OrganizationId);
        Assert.Null(snap.PublicOrganizationId);
        Assert.Equal(profileId, snap.PersonalProfileId);
        Assert.Equal(3, snap.PendingSyncCount);
    }

    [Fact]
    public async Task Org_capture_ignores_queue_when_active_context_is_another_org()
    {
        var userId = Guid.NewGuid();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var provider = new OrganizationSupportDiagnosticsProvider(
            new AlwaysOnlineConnectivity(),
            new FixedDevice(),
            new FixedAppInfo(),
            new MatchingLocalContext(userId, orgB, personal: false),
            new CountingQueue(9, 9),
            new ScopedGrant(userId, orgA, OfflineGrantScopeKind.Organization, PosRoleCodes.Owner),
            new CountingOrgRetry(),
            new StubOwnerProbe(true),
            new FixedRoleReader("Owner"),
            TimeProvider.System);

        var snap = await provider.CaptureAsync(OrgSession(userId, orgA));
        Assert.Equal(orgA, snap.OrganizationId);
        Assert.Equal(0, snap.PendingSyncCount);
        Assert.Equal(0, snap.FailedSyncCount);
    }

    [Fact]
    public void Copied_report_contains_no_secrets()
    {
        var report = SupportDiagnosticsReportFormatter.Format(
            SamplePersonal(Guid.NewGuid(), Guid.NewGuid(), 1, 0));

        Assert.Contains("Scope: Personal", report, StringComparison.Ordinal);
        Assert.DoesNotContain("access_token", report, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh_token", report, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HashBase64", report, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer ", report, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password=", report, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("omits credentials", report, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Retry_sync_uses_personal_retry_seam()
    {
        var retry = new CountingPersonalRetry();
        var userId = Guid.NewGuid();
        var provider = new PersonalSupportDiagnosticsProvider(
            new AlwaysOnlineConnectivity(),
            new FixedDevice(),
            new FixedAppInfo(),
            new MatchingLocalContext(userId, PersonalLocalScope.PathIsolationMarker, personal: true),
            new NoopPersonalStore(),
            new CountingQueue(0, 0),
            new ScopedGrant(userId, null, OfflineGrantScopeKind.Personal, null),
            retry,
            TimeProvider.System);

        await provider.RetrySyncAsync();
        Assert.Equal(1, retry.Calls);
    }

    [Fact]
    public async Task Retry_sync_uses_organization_retry_seam()
    {
        var retry = new CountingOrgRetry();
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var provider = new OrganizationSupportDiagnosticsProvider(
            new AlwaysOnlineConnectivity(),
            new FixedDevice(),
            new FixedAppInfo(),
            new MatchingLocalContext(userId, orgId, personal: false),
            new CountingQueue(0, 0),
            new ScopedGrant(userId, orgId, OfflineGrantScopeKind.Organization, PosRoleCodes.Owner),
            retry,
            new StubOwnerProbe(true),
            new FixedRoleReader("Owner"),
            TimeProvider.System);

        await provider.RetrySyncAsync();
        Assert.Equal(1, retry.Calls);
    }

    [Fact]
    public void Public_org_id_extracted_from_staff_username_host()
    {
        Assert.Equal("ORG001842", SupportDiagnosticsPublicIds.TryExtractPublicOrganizationId("maria@ORG001842"));
    }

    [Fact]
    public void Offline_owner_grant_requires_matching_org_and_owner_role()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var session = OrgSession(userId, orgId);
        var ownerGrant = new OfflineOperatingGrant(
            OfflineOperatingGrant.CurrentSchemaVersion, userId, orgId, "Shop", "device",
            PosRoleCodes.Owner, Array.Empty<string>(), null, "Name", "user", "user@example.com",
            now, now, now.AddDays(1), OfflineGrantScopeKind.Organization);
        var cashierGrant = ownerGrant with { RoleCode = PosRoleCodes.Cashier };

        Assert.True(PlatformOrganizationOwnerProbe.MatchesOfflineOwnerGrant(ownerGrant, session, orgId, now));
        Assert.False(PlatformOrganizationOwnerProbe.MatchesOfflineOwnerGrant(ownerGrant, session, otherOrg, now));
        Assert.False(PlatformOrganizationOwnerProbe.MatchesOfflineOwnerGrant(cashierGrant, session, orgId, now));
    }

    private static PersonalSupportDiagnosticsProvider CreatePersonalProvider(
        Guid userId,
        Guid profileId,
        int pending = 0,
        int failed = 0) =>
        new(
            new AlwaysOnlineConnectivity(),
            new FixedDevice(),
            new FixedAppInfo(),
            new MatchingLocalContext(userId, PersonalLocalScope.PathIsolationMarker, personal: true),
            new NoopPersonalStore(),
            new CountingQueue(pending, failed),
            new ScopedGrant(userId, null, OfflineGrantScopeKind.Personal, null),
            new CountingPersonalRetry(),
            TimeProvider.System);

    private static OrganizationSupportDiagnosticsProvider CreateOrgProvider(
        Guid userId,
        Guid orgId,
        bool owner,
        int pending,
        int failed) =>
        new(
            new AlwaysOnlineConnectivity(),
            new FixedDevice(),
            new FixedAppInfo(),
            new MatchingLocalContext(userId, orgId, personal: false),
            new CountingQueue(pending, failed),
            new ScopedGrant(userId, orgId, OfflineGrantScopeKind.Organization, PosRoleCodes.Owner),
            new CountingOrgRetry(),
            new StubOwnerProbe(owner),
            new FixedRoleReader("Owner"),
            TimeProvider.System);

    private static AuthSession PersonalSession(Guid userId, Guid? profileId) =>
        new(
            userId, "Pat", "pat", "pat@example.com", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
            false, null, AccountClass: "Personal", AccountProfileId: profileId);

    private static AuthSession OrgSession(Guid userId, Guid orgId) =>
        new(
            userId, "Owner", "owner", "owner@example.com", orgId, "Shop",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
            true, null);

    private static SupportDiagnosticsSnapshot SamplePersonal(Guid userId, Guid? profileId, int pending, int failed) =>
        new(
            DateTimeOffset.UtcNow, SupportDiagnosticsScope.Personal, "Offline", "abcd1234…", "1.0+1",
            "Healthy", 6, DateTimeOffset.UtcNow, pending, failed, "Personal · valid · unlocked",
            DateTimeOffset.UtcNow.AddDays(1), true, DateTimeOffset.UtcNow, userId, profileId,
            null, null, null, null);

    private static SupportDiagnosticsSnapshot SampleOrg(Guid userId, Guid orgId, int pending, int failed) =>
        new(
            DateTimeOffset.UtcNow, SupportDiagnosticsScope.Organization, "Online", "abcd1234…", "1.0+1",
            "Healthy", 6, DateTimeOffset.UtcNow, pending, failed, "Organization · valid · unlocked",
            DateTimeOffset.UtcNow.AddDays(1), true, DateTimeOffset.UtcNow, userId, null,
            orgId, "ORG001842", "Owner", "Shop");

    private sealed class StubProvider(
        SupportDiagnosticsScope scope,
        SupportDiagnosticsAccessKind access) : ISupportDiagnosticsProvider
    {
        public SupportDiagnosticsScope Scope { get; } = scope;
        public SupportDiagnosticsSnapshot? Snapshot { get; init; }

        public Task<SupportDiagnosticsAccessKind> EvaluateAccessAsync(AuthSession? session, CancellationToken ct = default) =>
            Task.FromResult(access);

        public Task<SupportDiagnosticsSnapshot> CaptureAsync(AuthSession session, CancellationToken ct = default) =>
            Task.FromResult(Snapshot ?? SupportDiagnosticsSnapshot.EmptyDenied(Scope, DateTimeOffset.UtcNow));

        public Task RetrySyncAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubOwnerProbe(bool owner) : IOrganizationOwnerProbe
    {
        public Task<bool> IsOwnerAsync(AuthSession session, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult(owner);
    }

    private sealed class FixedRoleReader(string? role) : ISupportDiagnosticsRoleReader
    {
        public Task<string?> GetCurrentRoleLabelAsync(CancellationToken ct = default) =>
            Task.FromResult(role);
    }

    private sealed class CountingPersonalRetry : IPersonalDiagnosticsSyncRetry
    {
        public int Calls { get; private set; }
        public Task RetryAsync(CancellationToken ct = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class CountingOrgRetry : IOrganizationDiagnosticsSyncRetry
    {
        public int Calls { get; private set; }
        public Task RetryAsync(CancellationToken ct = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCurrentUser(AuthSession? session) : ICurrentUserContext
    {
        public AuthSession? Session { get; private set; } = session;
        public bool IsAuthenticated => Session is not null;
        public bool HasPosAccess => Session?.HasPosAccess == true;
        public event Func<Task>? Changed;
        public void Set(AuthSession? s) => Session = s;
        public void Clear() => Session = null;
    }

    private sealed class AlwaysOnlineConnectivity : IConnectivityService
    {
        public event EventHandler<ConnectivityStatus>? ConnectivityChanged;
        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(true);
    }

    private sealed class FixedDevice : IDeviceIdentityProvider
    {
        public Task<string> GetOrCreateDeviceIdAsync(CancellationToken ct = default) =>
            Task.FromResult("device-abcdefgh");
    }

    private sealed class FixedAppInfo : IAppInfoService
    {
        public string AppName => "ExItS";
        public string Version => "1.0+test";
        public string EnvironmentName => "Development";
    }

    private sealed class MatchingLocalContext(Guid userId, Guid orgId, bool personal) : ILocalContextManager
    {
        public LocalContextSnapshot? ActiveContext { get; private set; } = new(
            new LocalContextIdentity(
                "hash",
                userId,
                orgId,
                personal ? PersonalLocalScope.ProductCode : "exits.pinoybusinesspos"),
            "db.sqlite",
            6,
            DateTimeOffset.UtcNow,
            LocalContextInitStatus.Ready);

        public Task<LocalContextOpenResult> OpenAsync(Guid u, Guid o, string product, CancellationToken ct = default) =>
            Task.FromResult(new LocalContextOpenResult(true, ActiveContext));

        public Task<LocalContextOpenResult> OpenPersonalAsync(Guid u, CancellationToken ct = default) =>
            OpenAsync(u, PersonalLocalScope.PathIsolationMarker, PersonalLocalScope.ProductCode, ct);

        public Task CloseAsync(CancellationToken ct = default)
        {
            ActiveContext = null;
            return Task.CompletedTask;
        }
    }

    private sealed class CountingQueue(int pending, int failed) : IOfflineOperationQueue
    {
        public Task EnqueueAsync(OfflineEnqueueRequest request, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecoverAbandonedSyncingAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task ReclaimBlockedByAccessAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task ReclaimFailedForManualRetryAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<OfflineOperationEnvelope?> TryClaimNextAsync(string claimToken, CancellationToken ct = default) =>
            Task.FromResult<OfflineOperationEnvelope?>(null);
        public Task MarkSucceededAsync(Guid operationId, string? serverReference, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkFailureAsync(Guid operationId, OfflineFailureClass failureClass, string failureCode, string? failureSummary, DateTimeOffset? nextAttemptUtc, int attemptCount, CancellationToken ct = default) => Task.CompletedTask;
        public Task<OfflineQueueCounts> GetCountsAsync(CancellationToken ct = default) =>
            Task.FromResult(new OfflineQueueCounts(pending, 0, 0, failed, 0, 0, 0));
        public Task<IReadOnlyList<OfflineOperationEnvelope>> ListSafeMetadataAsync(int take, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OfflineOperationEnvelope>>([]);
        public Task<bool> HasUnsyncedWorkAsync(CancellationToken ct = default) => Task.FromResult(pending + failed > 0);
        public Task SetLastSyncedUtcAsync(DateTimeOffset utc, CancellationToken ct = default) => Task.CompletedTask;
        public Task<DateTimeOffset?> GetLastSyncedUtcAsync(CancellationToken ct = default) =>
            Task.FromResult<DateTimeOffset?>(DateTimeOffset.UtcNow);
        public Task<(OfflineOperationEnvelope Envelope, EncryptedPayload Encrypted)?> TryLoadEncryptedAsync(Guid operationId, CancellationToken ct = default) =>
            Task.FromResult<(OfflineOperationEnvelope Envelope, EncryptedPayload Encrypted)?>(null);
    }

    private sealed class ScopedGrant(
        Guid userId,
        Guid? orgId,
        OfflineGrantScopeKind scope,
        string? role) : IOfflineOperatingGrantService
    {
        public bool IsUnlockedThisProcess => true;
        public OfflineOperatingGrant? ActiveUnlockedGrant => Build();
        public Task EstablishFromOnlineSessionAsync(AuthSession session, string deviceId, string? roleCode, CancellationToken ct = default) => Task.CompletedTask;
        public Task ClearAsync(CancellationToken ct = default) => Task.CompletedTask;
        public void LockThisProcess() { }
        public Task<bool> HasPinConfiguredAsync(CancellationToken ct = default) => Task.FromResult(true);
        public Task<OfflinePinSetupResult> SetPinAsync(string pin, CancellationToken ct = default) =>
            Task.FromResult(new OfflinePinSetupResult(true));
        public Task<OfflineColdStartOffer> EvaluateColdStartOfferAsync(CancellationToken ct = default) =>
            Task.FromResult(new OfflineColdStartOffer(true, ActiveUnlockedGrant, null));
        public Task<OfflinePinUnlockResult> UnlockWithPinAsync(string pin, CancellationToken ct = default) =>
            Task.FromResult(new OfflinePinUnlockResult(OfflinePinUnlockStatus.Succeeded, ActiveUnlockedGrant));
        public Task<OfflineOperatingGrant?> PeekStoredGrantAsync(CancellationToken ct = default) =>
            Task.FromResult(Build());

        private OfflineOperatingGrant? Build() => new(
            OfflineOperatingGrant.CurrentSchemaVersion, userId, orgId,
            orgId is null ? PersonalLocalScope.DisplayName : "Shop", "device", role,
            Array.Empty<string>(), null, "Name", "user", "user@example.com",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), scope);
    }

    private sealed class NoopPersonalStore : ILocalPersonalUtangStore
    {
        public Task EnsurePersonalContextAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<LocalPersonalContact>> ListContactsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<LocalPersonalContact>>([]);
        public Task<LocalPersonalContact?> GetContactAsync(Guid contactId, CancellationToken ct = default) =>
            Task.FromResult<LocalPersonalContact?>(null);
        public Task<LocalPersonalContact?> FindContactByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct = default) =>
            Task.FromResult<LocalPersonalContact?>(null);
        public Task<IReadOnlyList<LocalPersonalRelationship>> ListRelationshipsAsync(string direction, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<LocalPersonalRelationship>>([]);
        public Task<LocalPersonalRelationship?> GetRelationshipAsync(Guid relationshipId, CancellationToken ct = default) =>
            Task.FromResult<LocalPersonalRelationship?>(null);
        public Task<IReadOnlyList<LocalPersonalEntry>> ListEntriesAsync(Guid relationshipId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<LocalPersonalEntry>>([]);
        public Task<LocalPersonalAggregates> GetAggregatesAsync(CancellationToken ct = default) =>
            Task.FromResult(new LocalPersonalAggregates(0, 0, 0, 0));
        public Task PersistContactAndEnqueueAsync(LocalPersonalContactUpsertCommand command, CancellationToken ct = default) => Task.CompletedTask;
        public Task PersistRelationshipAndEnqueueAsync(LocalPersonalRelationshipCreateCommand command, CancellationToken ct = default) => Task.CompletedTask;
        public Task PersistEntryAndEnqueueAsync(LocalPersonalEntryRecordCommand command, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpsertServerContactAsync(LocalPersonalContact contact, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpsertServerRelationshipAsync(LocalPersonalRelationship relationship, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> CountPendingSyncAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task MarkContactSyncedAsync(Guid contactId, Guid serverId, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkRelationshipSyncedAsync(Guid relationshipId, Guid serverId, int version, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkEntrySyncedAsync(Guid entryId, Guid serverId, CancellationToken ct = default) => Task.CompletedTask;
    }

}
