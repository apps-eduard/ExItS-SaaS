using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;
using Microsoft.Data.Sqlite;

namespace ExItS.PinoyBusinessPOS.LocalStore;

/// <summary>
/// Personal-scope local-first Utang store. Rows are owned by user_id (no organization_id columns).
/// Outbox rows use <see cref="PersonalLocalScope.PathIsolationMarker"/> for the required organization_id slot.
/// </summary>
public sealed class LocalPersonalUtangStore(
    ILocalContextManager contextManager,
    ILocalDatabasePathResolver pathResolver,
    ILocalPayloadProtector payloadProtector,
    IDeviceIdentityProvider deviceIdentity,
    ICurrentUserContext currentUser,
    TimeProvider? timeProvider = null) : ILocalPersonalUtangStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task EnsurePersonalContextAsync(CancellationToken ct = default)
    {
        var session = currentUser.Session ?? throw new InvalidOperationException("session_required");
        if (!string.Equals(session.AccountClass, "Personal", StringComparison.OrdinalIgnoreCase)
            || session.OrganizationId is not null)
        {
            throw new InvalidOperationException("personal_session_required");
        }

        var active = contextManager.ActiveContext;
        if (active is { Status: LocalContextInitStatus.Ready }
            && PersonalLocalScope.IsPersonalContext(active.Identity.OrganizationId, active.Identity.ProductCode)
            && active.Identity.UserId == session.UserId)
        {
            return;
        }

        var open = await contextManager.OpenPersonalAsync(session.UserId, ct).ConfigureAwait(false);
        if (!open.Succeeded)
        {
            throw new InvalidOperationException(open.ErrorCode ?? "personal_context_unavailable");
        }
    }

    public async Task<IReadOnlyList<LocalPersonalContact>> ListContactsAsync(CancellationToken ct = default)
    {
        var active = await RequirePersonalContextAsync(ct).ConfigureAwait(false);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT id, user_id, display_name, phone, notes, sync_status, server_id, updated_at, operation_id
                FROM local_personal_contact
                WHERE user_id = $user
                ORDER BY display_name COLLATE NOCASE, id;
                """;
            cmd.Parameters.AddWithValue("$user", active.Identity.UserId.ToString("D"));
            var rows = new List<LocalPersonalContact>();
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                rows.Add(ReadContact(reader));
            }

            return rows;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LocalPersonalContact?> GetContactAsync(Guid contactId, CancellationToken ct = default)
    {
        var active = await RequirePersonalContextAsync(ct).ConfigureAwait(false);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT id, user_id, display_name, phone, notes, sync_status, server_id, updated_at, operation_id
                FROM local_personal_contact
                WHERE user_id = $user AND (id = $id OR server_id = $id)
                ORDER BY CASE WHEN id = $id THEN 0 ELSE 1 END
                LIMIT 1;
                """;
            cmd.Parameters.AddWithValue("$user", active.Identity.UserId.ToString("D"));
            cmd.Parameters.AddWithValue("$id", contactId.ToString("D"));
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            return await reader.ReadAsync(ct).ConfigureAwait(false) ? ReadContact(reader) : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LocalPersonalContact?> FindContactByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);
        var needle = normalizedEmail.Trim().ToUpperInvariant();
        var active = await RequirePersonalContextAsync(ct).ConfigureAwait(false);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT id, user_id, display_name, phone, notes, sync_status, server_id, updated_at, operation_id
                FROM local_personal_contact
                WHERE user_id = $user
                  AND notes IS NOT NULL
                  AND upper(trim(notes)) = $email
                ORDER BY updated_at DESC, id
                LIMIT 1;
                """;
            cmd.Parameters.AddWithValue("$user", active.Identity.UserId.ToString("D"));
            cmd.Parameters.AddWithValue("$email", needle);
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            return await reader.ReadAsync(ct).ConfigureAwait(false) ? ReadContact(reader) : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<LocalPersonalRelationship>> ListRelationshipsAsync(
        string direction,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(direction);
        var active = await RequirePersonalContextAsync(ct).ConfigureAwait(false);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT id, user_id, contact_id, direction, outstanding, currency, sync_status,
                       server_id, version, updated_at, operation_id
                FROM local_personal_relationship
                WHERE user_id = $user AND direction = $direction
                ORDER BY updated_at DESC, id;
                """;
            cmd.Parameters.AddWithValue("$user", active.Identity.UserId.ToString("D"));
            cmd.Parameters.AddWithValue("$direction", direction.Trim().ToLowerInvariant());
            var rows = new List<LocalPersonalRelationship>();
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                rows.Add(ReadRelationship(reader));
            }

            return rows;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LocalPersonalRelationship?> GetRelationshipAsync(
        Guid relationshipId,
        CancellationToken ct = default)
    {
        var active = await RequirePersonalContextAsync(ct).ConfigureAwait(false);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT id, user_id, contact_id, direction, outstanding, currency, sync_status,
                       server_id, version, updated_at, operation_id
                FROM local_personal_relationship
                WHERE user_id = $user AND id = $id
                LIMIT 1;
                """;
            cmd.Parameters.AddWithValue("$user", active.Identity.UserId.ToString("D"));
            cmd.Parameters.AddWithValue("$id", relationshipId.ToString("D"));
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            return await reader.ReadAsync(ct).ConfigureAwait(false) ? ReadRelationship(reader) : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<LocalPersonalEntry>> ListEntriesAsync(
        Guid relationshipId,
        CancellationToken ct = default)
    {
        var active = await RequirePersonalContextAsync(ct).ConfigureAwait(false);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT e.id, e.relationship_id, e.entry_type, e.amount, e.note, e.occurred_at,
                       e.sync_status, e.server_id, e.operation_id, e.created_at
                FROM local_personal_entry e
                INNER JOIN local_personal_relationship r ON r.id = e.relationship_id
                WHERE r.user_id = $user AND e.relationship_id = $rel
                ORDER BY e.occurred_at DESC, e.id;
                """;
            cmd.Parameters.AddWithValue("$user", active.Identity.UserId.ToString("D"));
            cmd.Parameters.AddWithValue("$rel", relationshipId.ToString("D"));
            var rows = new List<LocalPersonalEntry>();
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                rows.Add(ReadEntry(reader));
            }

            return rows;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LocalPersonalAggregates> GetAggregatesAsync(CancellationToken ct = default)
    {
        var contacts = await ListContactsAsync(ct).ConfigureAwait(false);
        var lent = await ListRelationshipsAsync(LocalPersonalDirection.Lent, ct).ConfigureAwait(false);
        var borrowed = await ListRelationshipsAsync(LocalPersonalDirection.Borrowed, ct).ConfigureAwait(false);
        return new LocalPersonalAggregates(
            contacts.Count,
            lent.Count + borrowed.Count,
            lent.Sum(r => r.Outstanding),
            borrowed.Sum(r => r.Outstanding));
    }

    public async Task PersistContactAndEnqueueAsync(
        LocalPersonalContactUpsertCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var active = await RequirePersonalContextAsync(ct).ConfigureAwait(false);
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);
        var now = _clock.GetUtcNow();
        var normalizedEmail = NormalizeOptionalEmail(command.Notes);
        var payload = JsonSerializer.SerializeToUtf8Bytes(
            new PersonalContactPayload(
                OfflineGrantScopeKind.Personal,
                command.ContactId,
                command.DisplayName,
                command.Phone,
                normalizedEmail),
            JsonOptions);

        await PersistWithQueueAsync(
            active,
            command.OperationId,
            OfflineOperationTypes.PersonalContactUpsert,
            command.IdempotencyKey,
            payload,
            command.ContactId,
            dependsOn: null,
            async (connection, tx) =>
            {
                await using var exists = connection.CreateCommand();
                exists.Transaction = tx;
                exists.CommandText = "SELECT COUNT(1) FROM offline_operations WHERE operation_id = $id;";
                exists.Parameters.AddWithValue("$id", command.OperationId.ToString("D"));
                var count = Convert.ToInt64(await exists.ExecuteScalarAsync(ct).ConfigureAwait(false), CultureInfo.InvariantCulture);
                if (count > 0)
                {
                    return; // idempotent by operation_id
                }

                if (normalizedEmail is not null)
                {
                    await using var dup = connection.CreateCommand();
                    dup.Transaction = tx;
                    dup.CommandText =
                        """
                        SELECT id FROM local_personal_contact
                        WHERE user_id = $user
                          AND notes IS NOT NULL
                          AND upper(trim(notes)) = $email
                          AND id != $id
                        LIMIT 1;
                        """;
                    dup.Parameters.AddWithValue("$user", active.Identity.UserId.ToString("D"));
                    dup.Parameters.AddWithValue("$email", normalizedEmail);
                    dup.Parameters.AddWithValue("$id", command.ContactId.ToString("D"));
                    var conflictingId = (string?)await dup.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    if (conflictingId is not null)
                    {
                        throw new InvalidOperationException(LocalPersonalStoreErrors.EmailConflict);
                    }
                }

                await using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText =
                    """
                    INSERT INTO local_personal_contact (
                        id, user_id, display_name, phone, notes, sync_status, server_id, updated_at, operation_id)
                    VALUES ($id, $user, $name, $phone, $notes, $status, NULL, $updated, $op)
                    ON CONFLICT(id) DO UPDATE SET
                        display_name = excluded.display_name,
                        phone = excluded.phone,
                        notes = excluded.notes,
                        sync_status = excluded.sync_status,
                        updated_at = excluded.updated_at,
                        operation_id = excluded.operation_id;
                    """;
                cmd.Parameters.AddWithValue("$id", command.ContactId.ToString("D"));
                cmd.Parameters.AddWithValue("$user", active.Identity.UserId.ToString("D"));
                cmd.Parameters.AddWithValue("$name", command.DisplayName.Trim());
                cmd.Parameters.AddWithValue("$phone", (object?)command.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$notes", (object?)normalizedEmail ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$status", LocalPersonalSyncStatus.Pending);
                cmd.Parameters.AddWithValue("$updated", FormatUtc(now));
                cmd.Parameters.AddWithValue("$op", command.OperationId.ToString("D"));
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            },
            ct).ConfigureAwait(false);
    }

    public async Task PersistRelationshipAndEnqueueAsync(
        LocalPersonalRelationshipCreateCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var direction = command.Direction.Trim().ToLowerInvariant();
        if (direction is not (LocalPersonalDirection.Lent or LocalPersonalDirection.Borrowed))
        {
            throw new ArgumentOutOfRangeException(nameof(command), "invalid_direction");
        }

        var active = await RequirePersonalContextAsync(ct).ConfigureAwait(false);
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);
        var now = _clock.GetUtcNow();
        var currency = string.IsNullOrWhiteSpace(command.Currency) ? "PHP" : command.Currency.Trim().ToUpperInvariant();
        var payload = JsonSerializer.SerializeToUtf8Bytes(
            new PersonalRelationshipPayload(
                OfflineGrantScopeKind.Personal,
                command.RelationshipId,
                command.ContactId,
                direction,
                command.InitialAmount,
                currency,
                command.Notes),
            JsonOptions);

        await PersistWithQueueAsync(
            active,
            command.OperationId,
            OfflineOperationTypes.PersonalRelationshipCreate,
            command.IdempotencyKey,
            payload,
            command.RelationshipId,
            command.DependsOnContactOperationId,
            async (connection, tx) =>
            {
                await using var exists = connection.CreateCommand();
                exists.Transaction = tx;
                exists.CommandText = "SELECT COUNT(1) FROM offline_operations WHERE operation_id = $id;";
                exists.Parameters.AddWithValue("$id", command.OperationId.ToString("D"));
                var count = Convert.ToInt64(await exists.ExecuteScalarAsync(ct).ConfigureAwait(false), CultureInfo.InvariantCulture);
                if (count > 0)
                {
                    return;
                }

                await using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText =
                    """
                    INSERT INTO local_personal_relationship (
                        id, user_id, contact_id, direction, outstanding, currency, sync_status,
                        server_id, version, updated_at, operation_id)
                    VALUES ($id, $user, $contact, $direction, $outstanding, $currency, $status,
                        NULL, 0, $updated, $op)
                    ON CONFLICT(id) DO NOTHING;
                    """;
                cmd.Parameters.AddWithValue("$id", command.RelationshipId.ToString("D"));
                cmd.Parameters.AddWithValue("$user", active.Identity.UserId.ToString("D"));
                cmd.Parameters.AddWithValue("$contact", command.ContactId.ToString("D"));
                cmd.Parameters.AddWithValue("$direction", direction);
                cmd.Parameters.AddWithValue(
                    "$outstanding",
                    command.InitialAmount.ToString(CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("$currency", currency);
                cmd.Parameters.AddWithValue("$status", LocalPersonalSyncStatus.Pending);
                cmd.Parameters.AddWithValue("$updated", FormatUtc(now));
                cmd.Parameters.AddWithValue("$op", command.OperationId.ToString("D"));
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                if (command.InitialAmount > 0)
                {
                    var entryId = Guid.NewGuid();
                    await using var entry = connection.CreateCommand();
                    entry.Transaction = tx;
                    entry.CommandText =
                        """
                        INSERT INTO local_personal_entry (
                            id, relationship_id, entry_type, amount, note, occurred_at,
                            sync_status, server_id, operation_id, created_at)
                        VALUES ($id, $rel, 'Loan', $amount, $note, $occurred, $status, NULL, $op, $created);
                        """;
                    entry.Parameters.AddWithValue("$id", entryId.ToString("D"));
                    entry.Parameters.AddWithValue("$rel", command.RelationshipId.ToString("D"));
                    entry.Parameters.AddWithValue(
                        "$amount",
                        command.InitialAmount.ToString(CultureInfo.InvariantCulture));
                    entry.Parameters.AddWithValue("$note", (object?)command.Notes ?? DBNull.Value);
                    entry.Parameters.AddWithValue("$occurred", FormatUtc(now));
                    entry.Parameters.AddWithValue("$status", LocalPersonalSyncStatus.Pending);
                    entry.Parameters.AddWithValue("$op", command.OperationId.ToString("D"));
                    entry.Parameters.AddWithValue("$created", FormatUtc(now));
                    await entry.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }
            },
            ct).ConfigureAwait(false);
    }

    public async Task PersistEntryAndEnqueueAsync(
        LocalPersonalEntryRecordCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var active = await RequirePersonalContextAsync(ct).ConfigureAwait(false);
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);
        var now = _clock.GetUtcNow();
        var occurred = command.OccurredAtUtc ?? now;
        var payload = JsonSerializer.SerializeToUtf8Bytes(
            new PersonalEntryPayload(
                OfflineGrantScopeKind.Personal,
                command.EntryId,
                command.RelationshipId,
                command.EntryType,
                command.Amount,
                command.Note,
                occurred),
            JsonOptions);

        await PersistWithQueueAsync(
            active,
            command.OperationId,
            OfflineOperationTypes.PersonalEntryRecord,
            command.IdempotencyKey,
            payload,
            command.EntryId,
            command.DependsOnRelationshipOperationId,
            async (connection, tx) =>
            {
                await using var exists = connection.CreateCommand();
                exists.Transaction = tx;
                exists.CommandText = "SELECT COUNT(1) FROM offline_operations WHERE operation_id = $id;";
                exists.Parameters.AddWithValue("$id", command.OperationId.ToString("D"));
                var count = Convert.ToInt64(await exists.ExecuteScalarAsync(ct).ConfigureAwait(false), CultureInfo.InvariantCulture);
                if (count > 0)
                {
                    return;
                }

                await using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText =
                    """
                    INSERT INTO local_personal_entry (
                        id, relationship_id, entry_type, amount, note, occurred_at,
                        sync_status, server_id, operation_id, created_at)
                    VALUES ($id, $rel, $type, $amount, $note, $occurred, $status, NULL, $op, $created)
                    ON CONFLICT(id) DO NOTHING;
                    """;
                cmd.Parameters.AddWithValue("$id", command.EntryId.ToString("D"));
                cmd.Parameters.AddWithValue("$rel", command.RelationshipId.ToString("D"));
                cmd.Parameters.AddWithValue("$type", command.EntryType.Trim());
                cmd.Parameters.AddWithValue("$amount", command.Amount.ToString(CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("$note", (object?)command.Note ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$occurred", FormatUtc(occurred));
                cmd.Parameters.AddWithValue("$status", LocalPersonalSyncStatus.Pending);
                cmd.Parameters.AddWithValue("$op", command.OperationId.ToString("D"));
                cmd.Parameters.AddWithValue("$created", FormatUtc(now));
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                var delta = string.Equals(command.EntryType, "Payment", StringComparison.OrdinalIgnoreCase)
                    ? -command.Amount
                    : command.Amount;
                await using var bal = connection.CreateCommand();
                bal.Transaction = tx;
                bal.CommandText =
                    """
                    UPDATE local_personal_relationship
                    SET outstanding = CAST(
                            (CAST(outstanding AS REAL) + $delta) AS TEXT),
                        updated_at = $updated,
                        sync_status = $status
                    WHERE id = $id AND user_id = $user;
                    """;
                bal.Parameters.AddWithValue("$delta", delta);
                bal.Parameters.AddWithValue("$updated", FormatUtc(now));
                bal.Parameters.AddWithValue("$status", LocalPersonalSyncStatus.Pending);
                bal.Parameters.AddWithValue("$id", command.RelationshipId.ToString("D"));
                bal.Parameters.AddWithValue("$user", active.Identity.UserId.ToString("D"));
                await bal.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            },
            ct).ConfigureAwait(false);
    }

    public async Task UpsertServerContactAsync(LocalPersonalContact contact, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(contact);
        var active = await RequirePersonalContextAsync(ct).ConfigureAwait(false);
        if (contact.UserId != active.Identity.UserId)
        {
            throw new InvalidOperationException("user_mismatch");
        }

        var serverKey = (contact.ServerId ?? contact.Id).ToString("D");
        var userKey = contact.UserId.ToString("D");

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

            // Merge by server_id (or id) so a post-sync hydrate does not insert a second row
            // when the local PK is still the client-generated Guid.
            string? existingId = null;
            await using (var find = connection.CreateCommand())
            {
                find.Transaction = tx;
                find.CommandText =
                    """
                    SELECT id FROM local_personal_contact
                    WHERE user_id = $user AND (id = $server OR server_id = $server)
                    ORDER BY CASE
                        WHEN server_id = $server AND id != $server THEN 0
                        WHEN id = $server THEN 1
                        ELSE 2
                    END
                    LIMIT 1;
                    """;
                find.Parameters.AddWithValue("$user", userKey);
                find.Parameters.AddWithValue("$server", serverKey);
                existingId = (string?)await find.ExecuteScalarAsync(ct).ConfigureAwait(false);
            }

            if (existingId is not null)
            {
                await using var update = connection.CreateCommand();
                update.Transaction = tx;
                update.CommandText =
                    """
                    UPDATE local_personal_contact
                    SET display_name = $name,
                        phone = $phone,
                        notes = COALESCE($notes, notes),
                        sync_status = $status,
                        server_id = $server,
                        updated_at = $updated,
                        operation_id = NULL
                    WHERE id = $id AND user_id = $user
                      AND sync_status != 'Pending';
                    """;
                // Empty/whitespace Notes → DBNull so COALESCE keeps any prior local email.
                var notes = NormalizeOptionalEmail(contact.Notes);
                update.Parameters.AddWithValue("$id", existingId);
                update.Parameters.AddWithValue("$user", userKey);
                update.Parameters.AddWithValue("$name", contact.DisplayName);
                update.Parameters.AddWithValue("$phone", (object?)contact.Phone ?? DBNull.Value);
                update.Parameters.AddWithValue("$notes", (object?)notes ?? DBNull.Value);
                update.Parameters.AddWithValue("$status", LocalPersonalSyncStatus.Synced);
                update.Parameters.AddWithValue("$server", serverKey);
                update.Parameters.AddWithValue("$updated", FormatUtc(contact.UpdatedAtUtc));
                await update.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                // Drop orphan hydrate duplicates (same server identity, different local PK).
                await using var cleanup = connection.CreateCommand();
                cleanup.Transaction = tx;
                cleanup.CommandText =
                    """
                    DELETE FROM local_personal_contact
                    WHERE user_id = $user
                      AND id != $keep
                      AND (id = $server OR server_id = $server);
                    """;
                cleanup.Parameters.AddWithValue("$user", userKey);
                cleanup.Parameters.AddWithValue("$keep", existingId);
                cleanup.Parameters.AddWithValue("$server", serverKey);
                await cleanup.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            else
            {
                await using var insert = connection.CreateCommand();
                insert.Transaction = tx;
                insert.CommandText =
                    """
                    INSERT INTO local_personal_contact (
                        id, user_id, display_name, phone, notes, sync_status, server_id, updated_at, operation_id)
                    VALUES ($id, $user, $name, $phone, $notes, $status, $server, $updated, NULL);
                    """;
                insert.Parameters.AddWithValue("$id", serverKey);
                insert.Parameters.AddWithValue("$user", userKey);
                insert.Parameters.AddWithValue("$name", contact.DisplayName);
                insert.Parameters.AddWithValue("$phone", (object?)contact.Phone ?? DBNull.Value);
                insert.Parameters.AddWithValue("$notes", (object?)NormalizeOptionalEmail(contact.Notes) ?? DBNull.Value);
                insert.Parameters.AddWithValue("$status", LocalPersonalSyncStatus.Synced);
                insert.Parameters.AddWithValue("$server", serverKey);
                insert.Parameters.AddWithValue("$updated", FormatUtc(contact.UpdatedAtUtc));
                await insert.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpsertServerRelationshipAsync(
        LocalPersonalRelationship relationship,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(relationship);
        var active = await RequirePersonalContextAsync(ct).ConfigureAwait(false);
        if (relationship.UserId != active.Identity.UserId)
        {
            throw new InvalidOperationException("user_mismatch");
        }

        var serverKey = (relationship.ServerId ?? relationship.Id).ToString("D");
        var userKey = relationship.UserId.ToString("D");

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

            string? existingId = null;
            await using (var find = connection.CreateCommand())
            {
                find.Transaction = tx;
                find.CommandText =
                    """
                    SELECT id FROM local_personal_relationship
                    WHERE user_id = $user AND (id = $server OR server_id = $server)
                    ORDER BY CASE
                        WHEN server_id = $server AND id != $server THEN 0
                        WHEN id = $server THEN 1
                        ELSE 2
                    END
                    LIMIT 1;
                    """;
                find.Parameters.AddWithValue("$user", userKey);
                find.Parameters.AddWithValue("$server", serverKey);
                existingId = (string?)await find.ExecuteScalarAsync(ct).ConfigureAwait(false);
            }

            // Prefer local contact row id when the server contact id was stored as server_id.
            var contactKey = relationship.ContactId.ToString("D");
            await using (var resolveContact = connection.CreateCommand())
            {
                resolveContact.Transaction = tx;
                resolveContact.CommandText =
                    """
                    SELECT id FROM local_personal_contact
                    WHERE user_id = $user AND (id = $contact OR server_id = $contact)
                    ORDER BY CASE WHEN id = $contact THEN 0 ELSE 1 END
                    LIMIT 1;
                    """;
                resolveContact.Parameters.AddWithValue("$user", userKey);
                resolveContact.Parameters.AddWithValue("$contact", contactKey);
                if (await resolveContact.ExecuteScalarAsync(ct).ConfigureAwait(false) is string resolved)
                {
                    contactKey = resolved;
                }
            }

            if (existingId is not null)
            {
                await using var update = connection.CreateCommand();
                update.Transaction = tx;
                update.CommandText =
                    """
                    UPDATE local_personal_relationship
                    SET contact_id = $contact,
                        direction = $direction,
                        outstanding = $outstanding,
                        currency = $currency,
                        sync_status = $status,
                        server_id = $server,
                        version = $version,
                        updated_at = $updated,
                        operation_id = NULL
                    WHERE id = $id AND user_id = $user
                      AND sync_status != 'Pending';
                    """;
                update.Parameters.AddWithValue("$id", existingId);
                update.Parameters.AddWithValue("$user", userKey);
                update.Parameters.AddWithValue("$contact", contactKey);
                update.Parameters.AddWithValue("$direction", relationship.Direction);
                update.Parameters.AddWithValue(
                    "$outstanding",
                    relationship.Outstanding.ToString(CultureInfo.InvariantCulture));
                update.Parameters.AddWithValue("$currency", relationship.Currency);
                update.Parameters.AddWithValue("$status", LocalPersonalSyncStatus.Synced);
                update.Parameters.AddWithValue("$server", serverKey);
                update.Parameters.AddWithValue("$version", relationship.Version);
                update.Parameters.AddWithValue("$updated", FormatUtc(relationship.UpdatedAtUtc));
                await update.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                await using var cleanup = connection.CreateCommand();
                cleanup.Transaction = tx;
                cleanup.CommandText =
                    """
                    DELETE FROM local_personal_relationship
                    WHERE user_id = $user
                      AND id != $keep
                      AND (id = $server OR server_id = $server);
                    """;
                cleanup.Parameters.AddWithValue("$user", userKey);
                cleanup.Parameters.AddWithValue("$keep", existingId);
                cleanup.Parameters.AddWithValue("$server", serverKey);
                await cleanup.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            else
            {
                await using var insert = connection.CreateCommand();
                insert.Transaction = tx;
                insert.CommandText =
                    """
                    INSERT INTO local_personal_relationship (
                        id, user_id, contact_id, direction, outstanding, currency, sync_status,
                        server_id, version, updated_at, operation_id)
                    VALUES ($id, $user, $contact, $direction, $outstanding, $currency, $status,
                        $server, $version, $updated, NULL);
                    """;
                insert.Parameters.AddWithValue("$id", serverKey);
                insert.Parameters.AddWithValue("$user", userKey);
                insert.Parameters.AddWithValue("$contact", contactKey);
                insert.Parameters.AddWithValue("$direction", relationship.Direction);
                insert.Parameters.AddWithValue(
                    "$outstanding",
                    relationship.Outstanding.ToString(CultureInfo.InvariantCulture));
                insert.Parameters.AddWithValue("$currency", relationship.Currency);
                insert.Parameters.AddWithValue("$status", LocalPersonalSyncStatus.Synced);
                insert.Parameters.AddWithValue("$server", serverKey);
                insert.Parameters.AddWithValue("$version", relationship.Version);
                insert.Parameters.AddWithValue("$updated", FormatUtc(relationship.UpdatedAtUtc));
                await insert.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> CountPendingSyncAsync(CancellationToken ct = default)
    {
        var active = await RequirePersonalContextAsync(ct).ConfigureAwait(false);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT COUNT(1) FROM offline_operations
                WHERE organization_id = $org
                  AND product_code = $product
                  AND operation_type LIKE 'personal.%'
                  AND queue_state IN ('Pending', 'RetryableFailure', 'Syncing', 'BlockedByAccess',
                                      'PermanentFailure', 'Conflict');
                """;
            cmd.Parameters.AddWithValue("$org", PersonalLocalScope.PathIsolationMarker.ToString("D"));
            cmd.Parameters.AddWithValue("$product", PersonalLocalScope.ProductCode);
            var scalar = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return Convert.ToInt32(scalar ?? 0, CultureInfo.InvariantCulture);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task MarkContactSyncedAsync(Guid contactId, Guid serverId, CancellationToken ct = default) =>
        MarkEntitySyncedAsync(
            "local_personal_contact",
            contactId,
            serverId,
            version: null,
            ct);

    public async Task MarkRelationshipSyncedAsync(
        Guid relationshipId,
        Guid serverId,
        int version,
        CancellationToken ct = default)
    {
        var active = await RequirePersonalContextAsync(ct).ConfigureAwait(false);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

            await using (var rel = connection.CreateCommand())
            {
                rel.Transaction = tx;
                rel.CommandText =
                    """
                    UPDATE local_personal_relationship
                    SET sync_status = $status, server_id = $server, version = $version, operation_id = NULL
                    WHERE id = $id;
                    """;
                rel.Parameters.AddWithValue("$status", LocalPersonalSyncStatus.Synced);
                rel.Parameters.AddWithValue("$server", serverId.ToString("D"));
                rel.Parameters.AddWithValue("$version", version);
                rel.Parameters.AddWithValue("$id", relationshipId.ToString("D"));
                await rel.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            // Initial loan rows are created locally with the relationship op — clear pending without a
            // separate entry.record outbox item (server already recorded InitialLoanAmount).
            await using (var entries = connection.CreateCommand())
            {
                entries.Transaction = tx;
                entries.CommandText =
                    """
                    UPDATE local_personal_entry
                    SET sync_status = $status, operation_id = NULL
                    WHERE relationship_id = $rel AND sync_status = $pending;
                    """;
                entries.Parameters.AddWithValue("$status", LocalPersonalSyncStatus.Synced);
                entries.Parameters.AddWithValue("$rel", relationshipId.ToString("D"));
                entries.Parameters.AddWithValue("$pending", LocalPersonalSyncStatus.Pending);
                await entries.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task MarkEntrySyncedAsync(Guid entryId, Guid serverId, CancellationToken ct = default) =>
        MarkEntitySyncedAsync(
            "local_personal_entry",
            entryId,
            serverId,
            version: null,
            ct);

    private async Task MarkEntitySyncedAsync(
        string table,
        Guid id,
        Guid serverId,
        int? version,
        CancellationToken ct)
    {
        var active = await RequirePersonalContextAsync(ct).ConfigureAwait(false);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = version is null
                ? $"""
                   UPDATE {table}
                   SET sync_status = $status, server_id = $server, operation_id = NULL
                   WHERE id = $id;
                   """
                : $"""
                   UPDATE {table}
                   SET sync_status = $status, server_id = $server, version = $version, operation_id = NULL
                   WHERE id = $id;
                   """;
            cmd.Parameters.AddWithValue("$status", LocalPersonalSyncStatus.Synced);
            cmd.Parameters.AddWithValue("$server", serverId.ToString("D"));
            cmd.Parameters.AddWithValue("$id", id.ToString("D"));
            if (version is int v)
            {
                cmd.Parameters.AddWithValue("$version", v);
            }

            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task PersistWithQueueAsync(
        LocalContextSnapshot active,
        Guid operationId,
        string operationType,
        string idempotencyKey,
        byte[] plaintextPayload,
        Guid entityId,
        Guid? dependsOn,
        Func<SqliteConnection, SqliteTransaction, Task> persistBody,
        CancellationToken ct)
    {
        if (!await payloadProtector.IsKeyAvailableAsync(ct).ConfigureAwait(false))
        {
            await payloadProtector.EnsureKeyAsync(ct).ConfigureAwait(false);
        }

        var deviceId = await deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
        var payloadHash = Convert.ToHexString(SHA256.HashData(plaintextPayload)).ToLowerInvariant();
        var aad = OfflinePayloadBinding.BuildAssociatedData(active.Identity.ContextHash, operationId, operationType);
        var encrypted = await payloadProtector.EncryptAsync(plaintextPayload, aad, ct).ConfigureAwait(false);
        var now = _clock.GetUtcNow();

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

            await using (var exists = connection.CreateCommand())
            {
                exists.Transaction = tx;
                exists.CommandText = "SELECT COUNT(1) FROM offline_operations WHERE operation_id = $id;";
                exists.Parameters.AddWithValue("$id", operationId.ToString("D"));
                var count = Convert.ToInt64(await exists.ExecuteScalarAsync(ct).ConfigureAwait(false), CultureInfo.InvariantCulture);
                if (count > 0)
                {
                    await tx.CommitAsync(ct).ConfigureAwait(false);
                    return;
                }
            }

            await persistBody(connection, tx).ConfigureAwait(false);

            await using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText =
                """
                INSERT INTO offline_operations (
                    operation_id, device_id, user_id, organization_id, product_code,
                    operation_type, payload_version, ciphertext, nonce, tag, payload_hash,
                    idempotency_key, created_utc, next_attempt_utc, attempt_count, queue_state,
                    last_attempt_utc, failure_code, failure_summary, server_reference, concurrency_token,
                    claimed_by, claimed_utc, depends_on_operation_id, entity_id)
                VALUES (
                    $operation_id, $device_id, $user_id, $organization_id, $product_code,
                    $operation_type, 1, $ciphertext, $nonce, $tag, $payload_hash,
                    $idempotency_key, $created_utc, $next_attempt_utc, 0, $queue_state,
                    NULL, NULL, NULL, NULL, NULL,
                    NULL, NULL, $depends_on, $entity_id);
                """;
            cmd.Parameters.AddWithValue("$operation_id", operationId.ToString("D"));
            cmd.Parameters.AddWithValue("$device_id", deviceId);
            cmd.Parameters.AddWithValue("$user_id", active.Identity.UserId.ToString("D"));
            cmd.Parameters.AddWithValue("$organization_id", PersonalLocalScope.PathIsolationMarker.ToString("D"));
            cmd.Parameters.AddWithValue("$product_code", PersonalLocalScope.ProductCode);
            cmd.Parameters.AddWithValue("$operation_type", operationType);
            cmd.Parameters.AddWithValue("$ciphertext", encrypted.Ciphertext);
            cmd.Parameters.AddWithValue("$nonce", encrypted.Nonce);
            cmd.Parameters.AddWithValue("$tag", encrypted.Tag);
            cmd.Parameters.AddWithValue("$payload_hash", payloadHash);
            cmd.Parameters.AddWithValue("$idempotency_key", idempotencyKey);
            cmd.Parameters.AddWithValue("$created_utc", FormatUtc(now));
            cmd.Parameters.AddWithValue("$next_attempt_utc", FormatUtc(now));
            cmd.Parameters.AddWithValue("$queue_state", nameof(OfflineQueueState.Pending));
            cmd.Parameters.AddWithValue(
                "$depends_on",
                dependsOn is Guid dep ? dep.ToString("D") : DBNull.Value);
            cmd.Parameters.AddWithValue("$entity_id", entityId.ToString("D"));
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<LocalContextSnapshot> RequirePersonalContextAsync(CancellationToken ct)
    {
        await EnsurePersonalContextAsync(ct).ConfigureAwait(false);
        var active = contextManager.ActiveContext
                     ?? throw new InvalidOperationException("personal_context_required");
        if (!PersonalLocalScope.IsPersonalContext(active.Identity.OrganizationId, active.Identity.ProductCode))
        {
            throw new InvalidOperationException("personal_context_required");
        }

        return active;
    }

    private async Task EnsureEncryptionKeyAsync(CancellationToken ct)
    {
        if (!await payloadProtector.IsKeyAvailableAsync(ct).ConfigureAwait(false))
        {
            await payloadProtector.EnsureKeyAsync(ct).ConfigureAwait(false);
        }
    }

    private async Task<SqliteConnection> OpenConnectionAsync(LocalContextSnapshot active, CancellationToken ct)
    {
        var path = pathResolver.ResolveDatabasePath(
            active.Identity.UserId,
            active.Identity.OrganizationId,
            active.Identity.ProductCode);
        var connection = new SqliteConnection($"Data Source={path}");
        await connection.OpenAsync(ct).ConfigureAwait(false);
        return connection;
    }

    private static string FormatUtc(DateTimeOffset value) =>
        value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

    /// <summary>Email is stored in Notes; normalize like Platform PersonalContact.</summary>
    private static string? NormalizeOptionalEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmed = email.Trim();
        return trimmed.Length == 0 ? null : trimmed.ToUpperInvariant();
    }

    private static LocalPersonalContact ReadContact(SqliteDataReader reader) =>
        new(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.GetString(5),
            reader.IsDBNull(6) ? null : Guid.Parse(reader.GetString(6)),
            DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.IsDBNull(8) ? null : Guid.Parse(reader.GetString(8)));

    private static LocalPersonalRelationship ReadRelationship(SqliteDataReader reader) =>
        new(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            Guid.Parse(reader.GetString(2)),
            reader.GetString(3),
            decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
            reader.GetString(5),
            reader.GetString(6),
            reader.IsDBNull(7) ? null : Guid.Parse(reader.GetString(7)),
            reader.GetInt32(8),
            DateTimeOffset.Parse(reader.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.IsDBNull(10) ? null : Guid.Parse(reader.GetString(10)));

    private static LocalPersonalEntry ReadEntry(SqliteDataReader reader) =>
        new(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.GetString(2),
            decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.GetString(6),
            reader.IsDBNull(7) ? null : Guid.Parse(reader.GetString(7)),
            reader.IsDBNull(8) ? null : Guid.Parse(reader.GetString(8)),
            DateTimeOffset.Parse(reader.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

    private sealed record PersonalContactPayload(
        OfflineGrantScopeKind ScopeKind,
        Guid ContactId,
        string DisplayName,
        string? Phone,
        string? Notes);

    private sealed record PersonalRelationshipPayload(
        OfflineGrantScopeKind ScopeKind,
        Guid RelationshipId,
        Guid ContactId,
        string Direction,
        decimal InitialAmount,
        string Currency,
        string? Notes);

    private sealed record PersonalEntryPayload(
        OfflineGrantScopeKind ScopeKind,
        Guid EntryId,
        Guid RelationshipId,
        string EntryType,
        decimal Amount,
        string? Note,
        DateTimeOffset OccurredAtUtc);
}
