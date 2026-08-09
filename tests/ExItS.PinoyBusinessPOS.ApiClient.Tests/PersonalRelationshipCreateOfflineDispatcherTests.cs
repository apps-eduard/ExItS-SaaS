using ExItS.PinoyBusinessPOS.ApiClient;
using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.ApiClient.Tests;

public sealed class PersonalRelationshipCreateOfflineDispatcherTests
{
    [Fact]
    public async Task ResolveServerContactId_uses_server_id_when_local_pk_differs()
    {
        var localId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var store = new FakePersonalStore(new LocalPersonalContact(
            localId,
            Guid.NewGuid(),
            "Ana",
            null,
            null,
            LocalPersonalSyncStatus.Synced,
            serverId,
            DateTimeOffset.UtcNow,
            null));

        var resolved = await PersonalRelationshipCreateOfflineDispatcher.ResolveServerContactIdAsync(
            store, localId, CancellationToken.None);

        Assert.Equal(serverId, resolved);
    }

    [Fact]
    public async Task ResolveServerContactId_returns_null_while_contact_still_pending()
    {
        var localId = Guid.NewGuid();
        var store = new FakePersonalStore(new LocalPersonalContact(
            localId,
            Guid.NewGuid(),
            "Ana",
            null,
            null,
            LocalPersonalSyncStatus.Pending,
            ServerId: null,
            DateTimeOffset.UtcNow,
            Guid.NewGuid()));

        var resolved = await PersonalRelationshipCreateOfflineDispatcher.ResolveServerContactIdAsync(
            store, localId, CancellationToken.None);

        Assert.Null(resolved);
    }

    [Fact]
    public async Task ResolveServerContactId_uses_local_id_when_already_synced_without_separate_server_id()
    {
        var id = Guid.NewGuid();
        var store = new FakePersonalStore(new LocalPersonalContact(
            id,
            Guid.NewGuid(),
            "Ana",
            null,
            null,
            LocalPersonalSyncStatus.Synced,
            ServerId: null,
            DateTimeOffset.UtcNow,
            null));

        var resolved = await PersonalRelationshipCreateOfflineDispatcher.ResolveServerContactIdAsync(
            store, id, CancellationToken.None);

        Assert.Equal(id, resolved);
    }

    private sealed class FakePersonalStore(LocalPersonalContact contact) : ILocalPersonalUtangStore
    {
        public Task EnsurePersonalContextAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<LocalPersonalContact>> ListContactsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<LocalPersonalContact>>([contact]);
        public Task<LocalPersonalContact?> GetContactAsync(Guid contactId, CancellationToken ct = default) =>
            Task.FromResult<LocalPersonalContact?>(
                contact.Id == contactId || contact.ServerId == contactId ? contact : null);
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
        public Task UpsertServerContactAsync(LocalPersonalContact c, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpsertServerRelationshipAsync(LocalPersonalRelationship relationship, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> CountPendingSyncAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task MarkContactSyncedAsync(Guid contactId, Guid serverId, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkRelationshipSyncedAsync(Guid relationshipId, Guid serverId, int version, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkEntrySyncedAsync(Guid entryId, Guid serverId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
