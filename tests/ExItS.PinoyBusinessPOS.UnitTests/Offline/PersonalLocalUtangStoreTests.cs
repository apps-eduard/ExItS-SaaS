using System.Globalization;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.LocalStore;
using Microsoft.Data.Sqlite;

namespace ExItS.PinoyBusinessPOS.UnitTests.Offline;

public sealed class PersonalLocalUtangStoreTests
{
    [Fact]
    public async Task Persist_contact_enqueues_outbox_once_and_is_idempotent()
    {
        await using var harness = await Harness.CreateAsync();
        var contactId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var command = new LocalPersonalContactUpsertCommand(
            contactId,
            operationId,
            operationId.ToString("N"),
            "Ana Reyes",
            "09171234567",
            null);

        await harness.Store.PersistContactAndEnqueueAsync(command);
        await harness.Store.PersistContactAndEnqueueAsync(command);

        var contacts = await harness.Store.ListContactsAsync();
        Assert.Single(contacts);
        Assert.Equal("Ana Reyes", contacts[0].DisplayName);
        Assert.Equal(LocalPersonalSyncStatus.Pending, contacts[0].SyncStatus);

        await using var raw = new SqliteConnection($"Data Source={harness.DbPath}");
        await raw.OpenAsync();
        await using var cmd = raw.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM offline_operations WHERE operation_id = $id;";
        cmd.Parameters.AddWithValue("$id", operationId.ToString("D"));
        var count = Convert.ToInt64(await cmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        Assert.Equal(1, count);

        await using var typeCmd = raw.CreateCommand();
        typeCmd.CommandText = "SELECT operation_type, organization_id, product_code FROM offline_operations WHERE operation_id = $id;";
        typeCmd.Parameters.AddWithValue("$id", operationId.ToString("D"));
        await using var reader = await typeCmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(OfflineOperationTypes.PersonalContactUpsert, reader.GetString(0));
        Assert.Equal(PersonalLocalScope.PathIsolationMarker.ToString("D"), reader.GetString(1));
        Assert.Equal(PersonalLocalScope.ProductCode, reader.GetString(2));
    }

    [Fact]
    public async Task Persist_lent_and_entry_use_user_id_owner_not_organization_columns()
    {
        await using var harness = await Harness.CreateAsync();
        var contactId = Guid.NewGuid();
        var contactOp = Guid.NewGuid();
        await harness.Store.PersistContactAndEnqueueAsync(new LocalPersonalContactUpsertCommand(
            contactId, contactOp, contactOp.ToString("N"), "Borrower", null, null));

        var relationshipId = Guid.NewGuid();
        var relOp = Guid.NewGuid();
        await harness.Store.PersistRelationshipAndEnqueueAsync(new LocalPersonalRelationshipCreateCommand(
            relationshipId,
            relOp,
            relOp.ToString("N"),
            contactId,
            LocalPersonalDirection.Lent,
            100m,
            "PHP",
            "Initial"));

        var entryId = Guid.NewGuid();
        var entryOp = Guid.NewGuid();
        await harness.Store.PersistEntryAndEnqueueAsync(new LocalPersonalEntryRecordCommand(
            entryId,
            entryOp,
            entryOp.ToString("N"),
            relationshipId,
            "Payment",
            25m,
            "Partial"));

        await using var raw = new SqliteConnection($"Data Source={harness.DbPath}");
        await raw.OpenAsync();

        await using (var cols = raw.CreateCommand())
        {
            cols.CommandText = "PRAGMA table_info(local_personal_contact);";
            var names = new List<string>();
            await using var reader = await cols.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(1));
            }

            Assert.Contains("user_id", names);
            Assert.DoesNotContain("organization_id", names);
        }

        var lent = await harness.Store.ListRelationshipsAsync(LocalPersonalDirection.Lent);
        Assert.Single(lent);
        Assert.Equal(75m, lent[0].Outstanding);

        var entries = await harness.Store.ListEntriesAsync(relationshipId);
        Assert.True(entries.Count >= 2); // initial loan + payment
        Assert.Equal(3, await harness.Store.CountPendingSyncAsync());
    }

    [Fact]
    public async Task Upsert_server_contact_after_mark_synced_does_not_create_second_row()
    {
        await using var harness = await Harness.CreateAsync();
        var localId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var op = Guid.NewGuid();
        await harness.Store.PersistContactAndEnqueueAsync(new LocalPersonalContactUpsertCommand(
            localId, op, op.ToString("N"), "Juan Luna", "12345678910", null));
        await harness.Store.MarkContactSyncedAsync(localId, serverId);

        await harness.Store.UpsertServerContactAsync(new LocalPersonalContact(
            serverId,
            harness.UserId,
            "Juan Luna",
            "12345678910",
            Notes: null,
            LocalPersonalSyncStatus.Synced,
            serverId,
            DateTimeOffset.UtcNow,
            OperationId: null));

        var contacts = await harness.Store.ListContactsAsync();
        Assert.Single(contacts);
        Assert.Equal(localId, contacts[0].Id);
        Assert.Equal(serverId, contacts[0].ServerId);
    }

    [Fact]
    public async Task Upsert_server_contact_preserves_local_notes_when_server_email_null()
    {
        await using var harness = await Harness.CreateAsync();
        var localId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var op = Guid.NewGuid();
        await harness.Store.PersistContactAndEnqueueAsync(new LocalPersonalContactUpsertCommand(
            localId, op, op.ToString("N"), "Juan Luna", "12345678910", "juan@example.com"));
        await harness.Store.MarkContactSyncedAsync(localId, serverId);

        await harness.Store.UpsertServerContactAsync(new LocalPersonalContact(
            serverId,
            harness.UserId,
            "Juan Luna",
            "12345678910",
            Notes: null,
            LocalPersonalSyncStatus.Synced,
            serverId,
            DateTimeOffset.UtcNow,
            OperationId: null));

        var contact = await harness.Store.GetContactAsync(localId);
        Assert.NotNull(contact);
        Assert.Equal("JUAN@EXAMPLE.COM", contact!.Notes);
    }

    [Fact]
    public async Task Mark_relationship_synced_clears_pending_initial_loan_entries()
    {
        await using var harness = await Harness.CreateAsync();
        var contactId = Guid.NewGuid();
        var contactOp = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();
        var relOp = Guid.NewGuid();
        await harness.Store.PersistContactAndEnqueueAsync(new LocalPersonalContactUpsertCommand(
            contactId, contactOp, contactOp.ToString("N"), "Borrower", null, null));
        await harness.Store.PersistRelationshipAndEnqueueAsync(new LocalPersonalRelationshipCreateCommand(
            relationshipId, relOp, relOp.ToString("N"), contactId, LocalPersonalDirection.Lent,
            100m, "PHP", "test loan", contactOp));

        var before = await harness.Store.ListEntriesAsync(relationshipId);
        Assert.Contains(before, e => e.SyncStatus == LocalPersonalSyncStatus.Pending);

        var serverRelId = Guid.NewGuid();
        await harness.Store.MarkRelationshipSyncedAsync(relationshipId, serverRelId, version: 1);

        var rel = await harness.Store.GetRelationshipAsync(relationshipId);
        Assert.NotNull(rel);
        Assert.Equal(LocalPersonalSyncStatus.Synced, rel!.SyncStatus);
        Assert.Equal(serverRelId, rel.ServerId);

        var after = await harness.Store.ListEntriesAsync(relationshipId);
        Assert.All(after, e => Assert.Equal(LocalPersonalSyncStatus.Synced, e.SyncStatus));
    }

    [Fact]
    public async Task Persist_contact_rejects_duplicate_email_case_insensitive()
    {
        await using var harness = await Harness.CreateAsync();
        var firstOp = Guid.NewGuid();
        await harness.Store.PersistContactAndEnqueueAsync(new LocalPersonalContactUpsertCommand(
            Guid.NewGuid(), firstOp, firstOp.ToString("N"), "Ana", null, "friend@example.com"));

        var secondOp = Guid.NewGuid();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.PersistContactAndEnqueueAsync(new LocalPersonalContactUpsertCommand(
                Guid.NewGuid(), secondOp, secondOp.ToString("N"), "Ana Twin", null, "Friend@Example.com")));

        Assert.Equal(LocalPersonalStoreErrors.EmailConflict, ex.Message);
        Assert.Single(await harness.Store.ListContactsAsync());
    }

    [Fact]
    public async Task Persist_contact_allows_multiple_contacts_without_email()
    {
        await using var harness = await Harness.CreateAsync();
        var firstOp = Guid.NewGuid();
        var secondOp = Guid.NewGuid();
        await harness.Store.PersistContactAndEnqueueAsync(new LocalPersonalContactUpsertCommand(
            Guid.NewGuid(), firstOp, firstOp.ToString("N"), "One", null, null));
        await harness.Store.PersistContactAndEnqueueAsync(new LocalPersonalContactUpsertCommand(
            Guid.NewGuid(), secondOp, secondOp.ToString("N"), "Two", null, "   "));

        Assert.Equal(2, (await harness.Store.ListContactsAsync()).Count);
    }

    [Fact]
    public async Task Upsert_server_contact_merges_local_row_by_server_id_without_duplicate()
    {
        await using var harness = await Harness.CreateAsync();
        var localId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var op = Guid.NewGuid();
        await harness.Store.PersistContactAndEnqueueAsync(new LocalPersonalContactUpsertCommand(
            localId, op, op.ToString("N"), "Juan Luna", "12345678910", null));
        await harness.Store.MarkContactSyncedAsync(localId, serverId);

        // Simulate a hydrate that previously inserted a second PK under the server id.
        await using (var raw = new SqliteConnection($"Data Source={harness.DbPath}"))
        {
            await raw.OpenAsync();
            await using var insert = raw.CreateCommand();
            insert.CommandText =
                """
                INSERT INTO local_personal_contact (
                    id, user_id, display_name, phone, notes, sync_status, server_id, updated_at, operation_id)
                VALUES ($id, $user, 'Juan Luna', '12345678910', NULL, 'Synced', $server, $updated, NULL);
                """;
            insert.Parameters.AddWithValue("$id", serverId.ToString("D"));
            insert.Parameters.AddWithValue("$user", harness.UserId.ToString("D"));
            insert.Parameters.AddWithValue("$server", serverId.ToString("D"));
            insert.Parameters.AddWithValue("$updated", DateTimeOffset.UtcNow.ToString("O"));
            await insert.ExecuteNonQueryAsync();
        }

        Assert.Equal(2, (await harness.Store.ListContactsAsync()).Count);

        await harness.Store.UpsertServerContactAsync(new LocalPersonalContact(
            serverId,
            harness.UserId,
            "Juan Luna",
            "12345678910",
            Notes: null,
            LocalPersonalSyncStatus.Synced,
            serverId,
            DateTimeOffset.UtcNow,
            OperationId: null));

        var contacts = await harness.Store.ListContactsAsync();
        Assert.Single(contacts);
        Assert.Equal(localId, contacts[0].Id);
        Assert.Equal(serverId, contacts[0].ServerId);
        Assert.Equal(LocalPersonalSyncStatus.Synced, contacts[0].SyncStatus);

        var byServer = await harness.Store.GetContactAsync(serverId);
        Assert.NotNull(byServer);
        Assert.Equal(localId, byServer!.Id);
    }

    [Fact]
    public async Task OpenAsync_rejects_personal_isolation_marker_and_personal_db_is_separate()
    {
        var root = new TempRoot();
        await using var manager = new LocalContextManager(
            new LocalDatabasePathResolver(root),
            new LocalDatabaseFactory(),
            new LocalDatabaseMigrator());

        var user = Guid.NewGuid();
        var rejected = await manager.OpenAsync(
            user,
            PersonalLocalScope.PathIsolationMarker,
            PersonalLocalScope.ProductCode);
        Assert.False(rejected.Succeeded);
        Assert.Equal("use_open_personal", rejected.ErrorCode);

        var personal = await manager.OpenPersonalAsync(user);
        Assert.True(personal.Succeeded);
        Assert.Equal(LocalDatabaseMigrator.ConnectedSuppliersSchemaVersion, personal.Context!.SchemaVersion);

        var org = Guid.NewGuid();
        var orgOpen = await manager.OpenAsync(user, org, PosProductCodes.PinoyBusinessPos);
        Assert.True(orgOpen.Succeeded);
        Assert.NotEqual(personal.Context.Identity.ContextHash, orgOpen.Context!.Identity.ContextHash);

        root.Dispose();
    }

    private sealed class Harness : IAsyncDisposable
    {
        public required TempRoot Root { get; init; }
        public required LocalPersonalUtangStore Store { get; init; }
        public required string DbPath { get; init; }
        public required LocalContextManager Manager { get; init; }
        public required Guid UserId { get; init; }

        public static async Task<Harness> CreateAsync()
        {
            var root = new TempRoot();
            var resolver = new LocalDatabasePathResolver(root);
            var manager = new LocalContextManager(resolver, new LocalDatabaseFactory(), new LocalDatabaseMigrator());
            var tokens = new MemorySecureTokenStore();
            var protector = new AesGcmLocalPayloadProtector(tokens);
            await protector.EnsureKeyAsync();
            var userId = Guid.NewGuid();
            var current = new CurrentUserContext();
            current.Set(new AuthSession(
                userId,
                "Personal",
                "personal",
                "p@example.com",
                OrganizationId: null,
                OrganizationDisplayName: null,
                IssuedAtUtc: DateTimeOffset.UtcNow,
                ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
                HasPosAccess: false,
                AccessReasonCode: null,
                AccountClass: "Personal"));

            Assert.True((await manager.OpenPersonalAsync(userId)).Succeeded);
            var store = new LocalPersonalUtangStore(
                manager,
                resolver,
                protector,
                new DeviceIdentityProvider(tokens),
                current);

            return new Harness
            {
                Root = root,
                Store = store,
                Manager = manager,
                UserId = userId,
                DbPath = resolver.ResolveDatabasePath(
                    userId,
                    PersonalLocalScope.PathIsolationMarker,
                    PersonalLocalScope.ProductCode)
            };
        }

        public async ValueTask DisposeAsync()
        {
            await Manager.DisposeAsync();
            Root.Dispose();
        }
    }

    private sealed class TempRoot : ILocalStoreRootPathProvider, IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "pos-personal-" + Guid.NewGuid().ToString("N"));

        public TempRoot() => Directory.CreateDirectory(_dir);

        public string GetLocalStoreRootDirectory() => _dir;

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_dir))
                {
                    Directory.Delete(_dir, recursive: true);
                }
            }
            catch
            {
                // best effort
            }
        }
    }

    private sealed class MemorySecureTokenStore : ISecureTokenStore
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

        public Task<string?> GetAsync(string key, CancellationToken ct = default) =>
            Task.FromResult(_values.TryGetValue(key, out var v) ? v : null);

        public Task SetAsync(string key, string value, CancellationToken ct = default)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        public Task ClearAsync(string key, CancellationToken ct = default)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }

        public Task ClearAllSessionKeysAsync(CancellationToken ct = default)
        {
            _values.Clear();
            return Task.CompletedTask;
        }
    }
}
