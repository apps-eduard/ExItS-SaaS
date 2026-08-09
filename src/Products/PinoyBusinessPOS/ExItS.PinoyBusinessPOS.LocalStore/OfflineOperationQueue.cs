using System.Globalization;
using System.Security.Cryptography;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;
using Microsoft.Data.Sqlite;

namespace ExItS.PinoyBusinessPOS.LocalStore;

/// <summary>
/// SQLite outbox for the active local context. Payloads are stored encrypted; key never enters SQLite.
/// </summary>
public sealed class OfflineOperationQueue(
    ILocalContextManager contextManager,
    ILocalDatabasePathResolver pathResolver,
    ILocalPayloadProtector payloadProtector,
    IDeviceIdentityProvider deviceIdentity,
    ICurrentUserContext currentUser,
    TimeProvider? timeProvider = null) : IOfflineOperationQueue
{
    public const int SchemaVersionWithQueue = 2;
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task EnqueueAsync(OfflineEnqueueRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var active = RequireActiveContext();
        var session = currentUser.Session
            ?? throw new InvalidOperationException("session_required");
        if (PersonalLocalScope.IsPersonalContext(active.Identity.OrganizationId, active.Identity.ProductCode))
        {
            if (session.OrganizationId is not null
                || !string.Equals(session.AccountClass, "Personal", StringComparison.OrdinalIgnoreCase)
                || session.UserId != active.Identity.UserId)
            {
                throw new InvalidOperationException("personal_context_mismatch");
            }
        }
        else if (session.OrganizationId is not Guid orgId || orgId != active.Identity.OrganizationId)
        {
            throw new InvalidOperationException("organization_mismatch");
        }

        if (!await payloadProtector.IsKeyAvailableAsync(ct).ConfigureAwait(false))
        {
            await payloadProtector.EnsureKeyAsync(ct).ConfigureAwait(false);
        }

        if (!await payloadProtector.IsKeyAvailableAsync(ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException("local_payload_key_unavailable");
        }

        var deviceId = await deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
        var payloadHash = Convert.ToHexString(SHA256.HashData(request.PlaintextPayload.Span)).ToLowerInvariant();
        var aad = OfflinePayloadBinding.BuildAssociatedData(active.Identity.ContextHash, request.OperationId, request.OperationType);
        var encrypted = await payloadProtector.EncryptAsync(request.PlaintextPayload, aad, ct).ConfigureAwait(false);
        var now = _clock.GetUtcNow();

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
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
                    $operation_type, $payload_version, $ciphertext, $nonce, $tag, $payload_hash,
                    $idempotency_key, $created_utc, $next_attempt_utc, 0, $queue_state,
                    NULL, NULL, NULL, NULL, $concurrency_token,
                    NULL, NULL, $depends_on, $entity_id);
                """;
            cmd.Parameters.AddWithValue("$operation_id", request.OperationId.ToString("D"));
            cmd.Parameters.AddWithValue("$device_id", deviceId);
            cmd.Parameters.AddWithValue("$user_id", active.Identity.UserId.ToString("D"));
            cmd.Parameters.AddWithValue("$organization_id", active.Identity.OrganizationId.ToString("D"));
            cmd.Parameters.AddWithValue("$product_code", active.Identity.ProductCode);
            cmd.Parameters.AddWithValue("$operation_type", request.OperationType);
            cmd.Parameters.AddWithValue("$payload_version", request.PayloadVersion);
            cmd.Parameters.AddWithValue("$ciphertext", encrypted.Ciphertext);
            cmd.Parameters.AddWithValue("$nonce", encrypted.Nonce);
            cmd.Parameters.AddWithValue("$tag", encrypted.Tag);
            cmd.Parameters.AddWithValue("$payload_hash", payloadHash);
            cmd.Parameters.AddWithValue("$idempotency_key", request.IdempotencyKey);
            cmd.Parameters.AddWithValue("$created_utc", now.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$next_attempt_utc", now.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$queue_state", nameof(OfflineQueueState.Pending));
            cmd.Parameters.AddWithValue("$concurrency_token", (object?)request.ConcurrencyToken ?? DBNull.Value);
            cmd.Parameters.AddWithValue(
                "$depends_on",
                request.DependsOnOperationId is Guid dep ? dep.ToString("D") : DBNull.Value);
            cmd.Parameters.AddWithValue(
                "$entity_id",
                request.EntityId is Guid entity ? entity.ToString("D") : DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RecoverAbandonedSyncingAsync(CancellationToken ct = default)
    {
        var active = contextManager.ActiveContext;
        if (active is null || active.Status != LocalContextInitStatus.Ready)
        {
            return;
        }

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                UPDATE offline_operations
                SET queue_state = $pending, claimed_by = NULL, claimed_utc = NULL
                WHERE queue_state = $syncing;
                """;
            cmd.Parameters.AddWithValue("$pending", nameof(OfflineQueueState.Pending));
            cmd.Parameters.AddWithValue("$syncing", nameof(OfflineQueueState.Syncing));
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReclaimBlockedByAccessAsync(CancellationToken ct = default)
    {
        var active = contextManager.ActiveContext;
        if (active is null || active.Status != LocalContextInitStatus.Ready)
        {
            return;
        }

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            var now = _clock.GetUtcNow().UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
            cmd.CommandText =
                """
                UPDATE offline_operations
                SET queue_state = $pending,
                    claimed_by = NULL,
                    claimed_utc = NULL,
                    next_attempt_utc = $now,
                    attempt_count = 0,
                    failure_code = NULL,
                    failure_summary = NULL
                WHERE queue_state = $blocked;
                """;
            cmd.Parameters.AddWithValue("$pending", nameof(OfflineQueueState.Pending));
            cmd.Parameters.AddWithValue("$blocked", nameof(OfflineQueueState.BlockedByAccess));
            cmd.Parameters.AddWithValue("$now", now);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReclaimFailedForManualRetryAsync(CancellationToken ct = default)
    {
        var active = contextManager.ActiveContext;
        if (active is null || active.Status != LocalContextInitStatus.Ready)
        {
            return;
        }

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            var now = _clock.GetUtcNow().UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
            cmd.CommandText =
                """
                UPDATE offline_operations
                SET queue_state = $pending,
                    claimed_by = NULL,
                    claimed_utc = NULL,
                    next_attempt_utc = $now,
                    attempt_count = 0,
                    failure_code = NULL,
                    failure_summary = NULL
                WHERE queue_state IN ($permanent, $conflict);
                """;
            cmd.Parameters.AddWithValue("$pending", nameof(OfflineQueueState.Pending));
            cmd.Parameters.AddWithValue("$permanent", nameof(OfflineQueueState.PermanentFailure));
            cmd.Parameters.AddWithValue("$conflict", nameof(OfflineQueueState.Conflict));
            cmd.Parameters.AddWithValue("$now", now);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OfflineOperationEnvelope?> TryClaimNextAsync(string claimToken, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claimToken);
        var active = RequireActiveContext();
        var now = _clock.GetUtcNow().UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

            await using (var select = connection.CreateCommand())
            {
                select.Transaction = tx;
                select.CommandText =
                    """
                    SELECT operation_id, depends_on_operation_id FROM offline_operations
                    WHERE queue_state IN ($pending, $retryable)
                      AND next_attempt_utc <= $now
                    ORDER BY created_utc ASC, operation_id ASC;
                    """;
                select.Parameters.AddWithValue("$pending", nameof(OfflineQueueState.Pending));
                select.Parameters.AddWithValue("$retryable", nameof(OfflineQueueState.RetryableFailure));
                select.Parameters.AddWithValue("$now", now);

                await using var reader = await select.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    var operationId = reader.GetString(0);
                    var dependsOn = reader.IsDBNull(1) ? null : reader.GetString(1);

                    if (dependsOn is not null)
                    {
                        var depState = await GetQueueStateAsync(connection, tx, dependsOn, ct).ConfigureAwait(false);
                        if (depState is null)
                        {
                            continue;
                        }

                        if (depState is OfflineQueueState.PermanentFailure or OfflineQueueState.Conflict)
                        {
                            await MarkDependencyFailedAsync(connection, tx, operationId, now, ct).ConfigureAwait(false);
                            continue;
                        }

                        if (depState is not OfflineQueueState.Succeeded)
                        {
                            continue;
                        }
                    }

                    await using var update = connection.CreateCommand();
                    update.Transaction = tx;
                    update.CommandText =
                        """
                        UPDATE offline_operations
                        SET queue_state = $syncing,
                            claimed_by = $claim,
                            claimed_utc = $now,
                            last_attempt_utc = $now,
                            attempt_count = attempt_count + 1
                        WHERE operation_id = $id
                          AND queue_state IN ($pending, $retryable);
                        """;
                    update.Parameters.AddWithValue("$syncing", nameof(OfflineQueueState.Syncing));
                    update.Parameters.AddWithValue("$claim", claimToken);
                    update.Parameters.AddWithValue("$now", now);
                    update.Parameters.AddWithValue("$id", operationId);
                    update.Parameters.AddWithValue("$pending", nameof(OfflineQueueState.Pending));
                    update.Parameters.AddWithValue("$retryable", nameof(OfflineQueueState.RetryableFailure));
                    var rows = await update.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                    if (rows == 1)
                    {
                        await tx.CommitAsync(ct).ConfigureAwait(false);
                        return await LoadEnvelopeAsync(connection, operationId, ct).ConfigureAwait(false);
                    }
                }

                await tx.CommitAsync(ct).ConfigureAwait(false);
                return null;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MarkSucceededAsync(Guid operationId, string? serverReference, CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        var now = _clock.GetUtcNow().UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                UPDATE offline_operations
                SET queue_state = $state,
                    server_reference = $ref,
                    failure_code = NULL,
                    failure_summary = NULL,
                    claimed_by = NULL,
                    claimed_utc = NULL,
                    last_attempt_utc = $now
                WHERE operation_id = $id;
                """;
            cmd.Parameters.AddWithValue("$state", nameof(OfflineQueueState.Succeeded));
            cmd.Parameters.AddWithValue("$ref", (object?)serverReference ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$now", now);
            cmd.Parameters.AddWithValue("$id", operationId.ToString("D"));
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MarkFailureAsync(
        Guid operationId,
        OfflineFailureClass failureClass,
        string failureCode,
        string? failureSummary,
        DateTimeOffset? nextAttemptUtc,
        int attemptCount,
        CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        var state = failureClass switch
        {
            OfflineFailureClass.Transient => OfflineQueueState.RetryableFailure,
            OfflineFailureClass.Conflict => OfflineQueueState.Conflict,
            OfflineFailureClass.AccessBlocked => OfflineQueueState.BlockedByAccess,
            _ => OfflineQueueState.PermanentFailure
        };

        var now = _clock.GetUtcNow().UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText =
                """
                UPDATE offline_operations
                SET queue_state = $state,
                    failure_code = $code,
                    failure_summary = $summary,
                    next_attempt_utc = $next,
                    attempt_count = $attempts,
                    claimed_by = NULL,
                    claimed_utc = NULL,
                    last_attempt_utc = $now
                WHERE operation_id = $id;
                """;
            cmd.Parameters.AddWithValue("$state", state.ToString());
            cmd.Parameters.AddWithValue("$code", failureCode);
            cmd.Parameters.AddWithValue("$summary", (object?)failureSummary ?? DBNull.Value);
            cmd.Parameters.AddWithValue(
                "$next",
                (nextAttemptUtc ?? _clock.GetUtcNow()).UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$attempts", attemptCount);
            cmd.Parameters.AddWithValue("$now", now);
            cmd.Parameters.AddWithValue("$id", operationId.ToString("D"));
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            if (state is OfflineQueueState.PermanentFailure or OfflineQueueState.Conflict)
            {
                await MarkDependentsConflictAsync(connection, tx, operationId.ToString("D"), now, ct)
                    .ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OfflineQueueCounts> GetCountsAsync(CancellationToken ct = default)
    {
        var active = contextManager.ActiveContext;
        if (active is null || active.Status != LocalContextInitStatus.Ready)
        {
            return new OfflineQueueCounts(0, 0, 0, 0, 0, 0, 0);
        }

        await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT queue_state, COUNT(1) AS c FROM offline_operations GROUP BY queue_state;";
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            map[reader.GetString(0)] = reader.GetInt32(1);
        }

        return new OfflineQueueCounts(
            Pending: Get(map, nameof(OfflineQueueState.Pending)),
            Syncing: Get(map, nameof(OfflineQueueState.Syncing)),
            Succeeded: Get(map, nameof(OfflineQueueState.Succeeded)),
            RetryableFailure: Get(map, nameof(OfflineQueueState.RetryableFailure)),
            PermanentFailure: Get(map, nameof(OfflineQueueState.PermanentFailure)),
            Conflict: Get(map, nameof(OfflineQueueState.Conflict)),
            BlockedByAccess: Get(map, nameof(OfflineQueueState.BlockedByAccess)));
    }

    public async Task<IReadOnlyList<OfflineOperationEnvelope>> ListSafeMetadataAsync(int take, CancellationToken ct = default)
    {
        var active = contextManager.ActiveContext;
        if (active is null || active.Status != LocalContextInitStatus.Ready)
        {
            return [];
        }

        await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT operation_id, device_id, user_id, organization_id, product_code, operation_type,
                   payload_version, payload_hash, idempotency_key, created_utc, next_attempt_utc,
                   attempt_count, queue_state, last_attempt_utc, failure_code, failure_summary,
                   server_reference, concurrency_token, depends_on_operation_id, entity_id
            FROM offline_operations
            ORDER BY created_utc DESC, operation_id DESC
            LIMIT $take;
            """;
        cmd.Parameters.AddWithValue("$take", Math.Clamp(take, 1, 100));
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var list = new List<OfflineOperationEnvelope>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(ReadEnvelope(reader));
        }

        return list;
    }

    public async Task<bool> HasUnsyncedWorkAsync(CancellationToken ct = default)
    {
        var counts = await GetCountsAsync(ct).ConfigureAwait(false);
        return counts.UnsyncedWork > 0;
    }

    public async Task SetLastSyncedUtcAsync(DateTimeOffset utc, CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO local_sync_meta (key, value) VALUES ('last_synced_utc', $value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            """;
        cmd.Parameters.AddWithValue("$value", utc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<DateTimeOffset?> GetLastSyncedUtcAsync(CancellationToken ct = default)
    {
        var active = contextManager.ActiveContext;
        if (active is null || active.Status != LocalContextInitStatus.Ready)
        {
            return null;
        }

        await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT value FROM local_sync_meta WHERE key = 'last_synced_utc';";
        var value = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        if (value is null or DBNull)
        {
            return null;
        }

        return DateTimeOffset.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    /// <summary>Loads ciphertext for processing; never logged.</summary>
    public async Task<(OfflineOperationEnvelope Envelope, EncryptedPayload Encrypted)?> TryLoadEncryptedAsync(
        Guid operationId,
        CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT operation_id, device_id, user_id, organization_id, product_code, operation_type,
                   payload_version, payload_hash, idempotency_key, created_utc, next_attempt_utc,
                   attempt_count, queue_state, last_attempt_utc, failure_code, failure_summary,
                   server_reference, concurrency_token, depends_on_operation_id, entity_id,
                   ciphertext, nonce, tag
            FROM offline_operations
            WHERE operation_id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", operationId.ToString("D"));
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        var envelope = ReadEnvelope(reader);
        var encrypted = new EncryptedPayload(
            (byte[])reader["ciphertext"],
            (byte[])reader["nonce"],
            (byte[])reader["tag"]);
        return (envelope, encrypted);
    }

    private LocalContextSnapshot RequireActiveContext()
    {
        var active = contextManager.ActiveContext;
        if (active is null || active.Status != LocalContextInitStatus.Ready)
        {
            throw new InvalidOperationException("local_context_not_open");
        }

        return active;
    }

    private async Task<SqliteConnection> OpenConnectionAsync(LocalContextSnapshot active, CancellationToken ct)
    {
        var path = pathResolver.ResolveDatabasePath(
            active.Identity.UserId,
            active.Identity.OrganizationId,
            active.Identity.ProductCode);
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);
        return connection;
    }

    private static async Task<OfflineOperationEnvelope?> LoadEnvelopeAsync(
        SqliteConnection connection,
        string operationId,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT operation_id, device_id, user_id, organization_id, product_code, operation_type,
                   payload_version, payload_hash, idempotency_key, created_utc, next_attempt_utc,
                   attempt_count, queue_state, last_attempt_utc, failure_code, failure_summary,
                   server_reference, concurrency_token, depends_on_operation_id, entity_id
            FROM offline_operations
            WHERE operation_id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", operationId);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        return ReadEnvelope(reader);
    }

    private static OfflineOperationEnvelope ReadEnvelope(SqliteDataReader reader)
    {
        var dependsOnOrdinal = reader.GetOrdinal("depends_on_operation_id");
        var entityOrdinal = reader.GetOrdinal("entity_id");
        return new(
            Guid.Parse(reader.GetString(reader.GetOrdinal("operation_id"))),
            reader.GetString(reader.GetOrdinal("device_id")),
            Guid.Parse(reader.GetString(reader.GetOrdinal("user_id"))),
            Guid.Parse(reader.GetString(reader.GetOrdinal("organization_id"))),
            reader.GetString(reader.GetOrdinal("product_code")),
            reader.GetString(reader.GetOrdinal("operation_type")),
            reader.GetInt32(reader.GetOrdinal("payload_version")),
            reader.GetString(reader.GetOrdinal("payload_hash")),
            reader.GetString(reader.GetOrdinal("idempotency_key")),
            DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("created_utc")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("next_attempt_utc")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.GetInt32(reader.GetOrdinal("attempt_count")),
            Enum.Parse<OfflineQueueState>(reader.GetString(reader.GetOrdinal("queue_state")), ignoreCase: true),
            reader.IsDBNull(reader.GetOrdinal("last_attempt_utc"))
                ? null
                : DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("last_attempt_utc")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.IsDBNull(reader.GetOrdinal("failure_code")) ? null : reader.GetString(reader.GetOrdinal("failure_code")),
            reader.IsDBNull(reader.GetOrdinal("failure_summary")) ? null : reader.GetString(reader.GetOrdinal("failure_summary")),
            reader.IsDBNull(reader.GetOrdinal("server_reference")) ? null : reader.GetString(reader.GetOrdinal("server_reference")),
            reader.IsDBNull(reader.GetOrdinal("concurrency_token")) ? null : reader.GetString(reader.GetOrdinal("concurrency_token")),
            reader.IsDBNull(dependsOnOrdinal) ? null : Guid.Parse(reader.GetString(dependsOnOrdinal)),
            reader.IsDBNull(entityOrdinal) ? null : Guid.Parse(reader.GetString(entityOrdinal)));
    }

    private static async Task<OfflineQueueState?> GetQueueStateAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        string operationId,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT queue_state FROM offline_operations WHERE operation_id = $id;";
        cmd.Parameters.AddWithValue("$id", operationId);
        var value = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        if (value is null or DBNull)
        {
            return null;
        }

        return Enum.Parse<OfflineQueueState>(Convert.ToString(value, CultureInfo.InvariantCulture)!, ignoreCase: true);
    }

    private static async Task MarkDependencyFailedAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        string operationId,
        string now,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            UPDATE offline_operations
            SET queue_state = $conflict,
                failure_code = $code,
                failure_summary = NULL,
                claimed_by = NULL,
                claimed_utc = NULL,
                last_attempt_utc = $now
            WHERE operation_id = $id
              AND queue_state IN ($pending, $retryable);
            """;
        cmd.Parameters.AddWithValue("$conflict", nameof(OfflineQueueState.Conflict));
        cmd.Parameters.AddWithValue("$code", "dependency_failed");
        cmd.Parameters.AddWithValue("$now", now);
        cmd.Parameters.AddWithValue("$id", operationId);
        cmd.Parameters.AddWithValue("$pending", nameof(OfflineQueueState.Pending));
        cmd.Parameters.AddWithValue("$retryable", nameof(OfflineQueueState.RetryableFailure));
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task MarkDependentsConflictAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        string dependencyOperationId,
        string now,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            UPDATE offline_operations
            SET queue_state = $conflict,
                failure_code = $code,
                failure_summary = NULL,
                claimed_by = NULL,
                claimed_utc = NULL,
                last_attempt_utc = $now
            WHERE depends_on_operation_id = $dep
              AND queue_state IN ($pending, $retryable, $syncing);
            """;
        cmd.Parameters.AddWithValue("$conflict", nameof(OfflineQueueState.Conflict));
        cmd.Parameters.AddWithValue("$code", "dependency_failed");
        cmd.Parameters.AddWithValue("$now", now);
        cmd.Parameters.AddWithValue("$dep", dependencyOperationId);
        cmd.Parameters.AddWithValue("$pending", nameof(OfflineQueueState.Pending));
        cmd.Parameters.AddWithValue("$retryable", nameof(OfflineQueueState.RetryableFailure));
        cmd.Parameters.AddWithValue("$syncing", nameof(OfflineQueueState.Syncing));
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static int Get(Dictionary<string, int> map, string key) =>
        map.TryGetValue(key, out var value) ? value : 0;
}
