using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;
using Microsoft.Data.Sqlite;

namespace ExItS.PinoyBusinessPOS.LocalStore;

/// <summary>
/// Encrypted local customer/credit read model with transactional outbox enqueue.
/// Sensitive fields are AES-GCM encrypted; plaintext is never logged.
/// </summary>
public sealed class LocalEncryptedCustomerCreditStore(
    ILocalContextManager contextManager,
    ILocalDatabasePathResolver pathResolver,
    ILocalPayloadProtector payloadProtector,
    IDeviceIdentityProvider deviceIdentity,
    ICurrentUserContext currentUser,
    TimeProvider? timeProvider = null) : ILocalCustomerCreditStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task UpsertServerCustomerAsync(LocalCustomerProjection customer, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(customer);
        var active = RequireActiveContext();
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);
        var encrypted = await EncryptCustomerFieldsAsync(active, customer.CustomerId, customer, ct).ConfigureAwait(false);
        var createdUtc = FormatUtc(customer.CreatedAtUtc);
        var updatedUtc = FormatUtc(customer.UpdatedAtUtc);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT INTO local_customer_projection (
                    customer_id, organization_id, status, entity_state, concurrency_token,
                    pending_operation_id, created_utc, updated_utc, ciphertext, nonce, tag,
                    conflict_server_json, safe_failure_code)
                VALUES (
                    $id, $org, $status, $state, $token, NULL, $created, $updated,
                    $ciphertext, $nonce, $tag, NULL, NULL)
                ON CONFLICT(customer_id) DO UPDATE SET
                    organization_id = excluded.organization_id,
                    status = excluded.status,
                    entity_state = excluded.entity_state,
                    concurrency_token = excluded.concurrency_token,
                    pending_operation_id = NULL,
                    updated_utc = excluded.updated_utc,
                    ciphertext = excluded.ciphertext,
                    nonce = excluded.nonce,
                    tag = excluded.tag,
                    conflict_server_json = NULL,
                    safe_failure_code = NULL;
                """;
            BindCustomerProjection(cmd, customer, encrypted, updatedUtc);
            cmd.Parameters.AddWithValue("$created", createdUtc);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpsertServerCreditAsync(LocalCreditProjection credit, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credit);
        var active = RequireActiveContext();
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);
        var encrypted = await EncryptCreditFieldsAsync(active, credit.CreditEntryId, credit, ct).ConfigureAwait(false);
        var now = FormatUtc(credit.CreatedAtUtc);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT INTO local_credit_projection (
                    credit_entry_id, customer_id, organization_id, entity_state,
                    pending_operation_id, depends_on_operation_id, created_utc,
                    ciphertext, nonce, tag, safe_failure_code)
                VALUES (
                    $id, $customer, $org, $state, NULL, NULL, $created,
                    $ciphertext, $nonce, $tag, NULL)
                ON CONFLICT(credit_entry_id) DO UPDATE SET
                    customer_id = excluded.customer_id,
                    organization_id = excluded.organization_id,
                    entity_state = excluded.entity_state,
                    pending_operation_id = NULL,
                    depends_on_operation_id = NULL,
                    ciphertext = excluded.ciphertext,
                    nonce = excluded.nonce,
                    tag = excluded.tag,
                    safe_failure_code = NULL;
                """;
            BindCreditProjection(cmd, credit, encrypted, now, dependsOnOperationId: null);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetConfirmedOutstandingAsync(Guid customerId, decimal confirmedOutstanding, CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);
        var encrypted = await EncryptDecimalAsync(
            active,
            customerId,
            confirmedOutstanding,
            ct).ConfigureAwait(false);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT INTO local_customer_balance (
                    customer_id, confirmed_ciphertext, confirmed_nonce, confirmed_tag,
                    pending_ciphertext, pending_nonce, pending_tag)
                VALUES ($id, $ciphertext, $nonce, $tag, NULL, NULL, NULL)
                ON CONFLICT(customer_id) DO UPDATE SET
                    confirmed_ciphertext = excluded.confirmed_ciphertext,
                    confirmed_nonce = excluded.confirmed_nonce,
                    confirmed_tag = excluded.confirmed_tag;
                """;
            cmd.Parameters.AddWithValue("$id", customerId.ToString("D"));
            cmd.Parameters.AddWithValue("$ciphertext", encrypted.Ciphertext);
            cmd.Parameters.AddWithValue("$nonce", encrypted.Nonce);
            cmd.Parameters.AddWithValue("$tag", encrypted.Tag);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task PersistCustomerCreateAndEnqueueAsync(LocalCustomerCreateCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var active = RequireActiveContext();
        ValidateSessionOrg(active);
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);

        var now = _clock.GetUtcNow();
        var customer = new LocalCustomerProjection(
            command.CustomerId,
            active.Identity.OrganizationId,
            command.DisplayName,
            command.MobileNumber,
            command.Address,
            command.Notes,
            "Active",
            now,
            now,
            LocalEntitySyncState.PendingCreate,
            command.OperationId,
            null,
            null,
            null);

        var encrypted = await EncryptCustomerFieldsAsync(active, command.CustomerId, customer, ct).ConfigureAwait(false);
        var payloadJson = JsonSerializer.SerializeToUtf8Bytes(new CustomerCreatePayload(
            command.CustomerId,
            command.DisplayName,
            command.MobileNumber,
            command.Address,
            command.Notes), JsonOptions);

        await PersistWithQueueAsync(
            active,
            command.OperationId,
            OfflineOperationTypes.CustomerCreate,
            1,
            command.IdempotencyKey,
            payloadJson,
            entityId: command.CustomerId,
            dependsOn: null,
            concurrencyToken: null,
            async (connection, tx, deviceId, payloadHash, queueEncrypted, queueNow) =>
            {
                await InsertCustomerProjectionAsync(
                    connection, tx, customer, encrypted, FormatUtc(now), queueNow, ct).ConfigureAwait(false);
            },
            ct).ConfigureAwait(false);
    }

    public async Task PersistCustomerUpdateAndEnqueueAsync(LocalCustomerUpdateCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var active = RequireActiveContext();
        ValidateSessionOrg(active);
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);

        var now = _clock.GetUtcNow();
        var customer = new LocalCustomerProjection(
            command.CustomerId,
            active.Identity.OrganizationId,
            command.DisplayName,
            command.MobileNumber,
            command.Address,
            command.Notes,
            "Active",
            now,
            now,
            LocalEntitySyncState.PendingUpdate,
            command.OperationId,
            command.ExpectedConcurrencyToken,
            null,
            null);

        var encrypted = await EncryptCustomerFieldsAsync(active, command.CustomerId, customer, ct).ConfigureAwait(false);
        var payloadJson = JsonSerializer.SerializeToUtf8Bytes(new CustomerUpdatePayload(
            command.CustomerId,
            command.DisplayName,
            command.MobileNumber,
            command.Address,
            command.Notes,
            command.ExpectedConcurrencyToken), JsonOptions);

        await PersistWithQueueAsync(
            active,
            command.OperationId,
            OfflineOperationTypes.CustomerUpdate,
            1,
            command.IdempotencyKey,
            payloadJson,
            entityId: command.CustomerId,
            dependsOn: null,
            concurrencyToken: command.ExpectedConcurrencyToken,
            async (connection, tx, deviceId, payloadHash, queueEncrypted, queueNow) =>
            {
                await UpdateCustomerProjectionForPendingUpdateAsync(
                    connection, tx, customer, encrypted, FormatUtc(now), queueNow, ct).ConfigureAwait(false);
            },
            ct).ConfigureAwait(false);
    }

    public async Task PersistCreditCreateAndEnqueueAsync(LocalCreditCreateCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var active = RequireActiveContext();
        ValidateSessionOrg(active);
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);

        var now = _clock.GetUtcNow();
        var credit = new LocalCreditProjection(
            command.CreditEntryId,
            command.CustomerId,
            active.Identity.OrganizationId,
            command.Amount,
            command.Remarks,
            "Active",
            now,
            LocalEntitySyncState.PendingCreate,
            command.OperationId,
            command.DependsOnCustomerCreateOperationId,
            null);

        var encrypted = await EncryptCreditFieldsAsync(active, command.CreditEntryId, credit, ct).ConfigureAwait(false);
        var payloadJson = JsonSerializer.SerializeToUtf8Bytes(new CreditCreatePayload(
            command.CreditEntryId,
            command.CustomerId,
            FormatDecimal(command.Amount),
            command.Remarks), JsonOptions);

        await PersistWithQueueAsync(
            active,
            command.OperationId,
            OfflineOperationTypes.CreditCreate,
            1,
            command.IdempotencyKey,
            payloadJson,
            entityId: command.CreditEntryId,
            dependsOn: command.DependsOnCustomerCreateOperationId,
            concurrencyToken: null,
            async (connection, tx, deviceId, payloadHash, queueEncrypted, queueNow) =>
            {
                await InsertCreditProjectionAsync(
                    connection,
                    tx,
                    credit,
                    encrypted,
                    FormatUtc(now),
                    queueNow,
                    ct).ConfigureAwait(false);
                await AddPendingBalanceAsync(connection, tx, active, command.CustomerId, command.Amount, ct)
                    .ConfigureAwait(false);
            },
            ct).ConfigureAwait(false);
    }

    public async Task MarkCustomerStateAsync(
        Guid customerId,
        LocalEntitySyncState state,
        string? concurrencyToken = null,
        string? conflictServerJson = null,
        string? safeFailureCode = null,
        CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                UPDATE local_customer_projection
                SET entity_state = $state,
                    concurrency_token = COALESCE($token, concurrency_token),
                    pending_operation_id = CASE WHEN $clearPending = 1 THEN NULL ELSE pending_operation_id END,
                    conflict_server_json = $conflict,
                    safe_failure_code = $failure
                WHERE customer_id = $id;
                """;
            cmd.Parameters.AddWithValue("$state", state.ToString());
            cmd.Parameters.AddWithValue("$token", (object?)concurrencyToken ?? DBNull.Value);
            cmd.Parameters.AddWithValue(
                "$clearPending",
                state is LocalEntitySyncState.ServerConfirmed or LocalEntitySyncState.Conflict
                    or LocalEntitySyncState.Rejected ? 1 : 0);
            cmd.Parameters.AddWithValue("$conflict", (object?)conflictServerJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$failure", (object?)safeFailureCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$id", customerId.ToString("D"));
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MarkCreditStateAsync(
        Guid creditEntryId,
        LocalEntitySyncState state,
        string? safeFailureCode = null,
        CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

            LocalCreditProjection? before = null;
            if (state == LocalEntitySyncState.Rejected)
            {
                before = await TryReadCreditProjectionAsync(connection, tx, active, creditEntryId, ct)
                    .ConfigureAwait(false);
            }

            await using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText =
                """
                UPDATE local_credit_projection
                SET entity_state = $state,
                    pending_operation_id = CASE WHEN $clearPending = 1 THEN NULL ELSE pending_operation_id END,
                    safe_failure_code = $failure
                WHERE credit_entry_id = $id;
                """;
            cmd.Parameters.AddWithValue("$state", state.ToString());
            cmd.Parameters.AddWithValue(
                "$clearPending",
                state is LocalEntitySyncState.ServerConfirmed or LocalEntitySyncState.Conflict
                    or LocalEntitySyncState.Rejected ? 1 : 0);
            cmd.Parameters.AddWithValue("$failure", (object?)safeFailureCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$id", creditEntryId.ToString("D"));
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            if (state == LocalEntitySyncState.Rejected
                && before?.EntityState == LocalEntitySyncState.PendingCreate)
            {
                await SubtractPendingBalanceAsync(connection, tx, active, before.CustomerId, before.Amount, ct)
                    .ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DiscardLocalCustomerUpdateAsync(
        Guid customerId,
        LocalCustomerProjection serverVersion,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(serverVersion);
        await UpsertServerCustomerAsync(serverVersion, ct).ConfigureAwait(false);
    }

    public async Task<LocalCustomerProjection?> GetCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);

        await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT customer_id, organization_id, status, entity_state, concurrency_token,
                   pending_operation_id, created_utc, updated_utc, ciphertext, nonce, tag,
                   conflict_server_json, safe_failure_code
            FROM local_customer_projection
            WHERE customer_id = $id AND organization_id = $org;
            """;
        cmd.Parameters.AddWithValue("$id", customerId.ToString("D"));
        cmd.Parameters.AddWithValue("$org", active.Identity.OrganizationId.ToString("D"));
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        return await ReadCustomerProjectionAsync(active, reader, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<LocalCustomerProjection>> ListCustomersAsync(
        string? search,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);

        await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT customer_id, organization_id, status, entity_state, concurrency_token,
                   pending_operation_id, created_utc, updated_utc, ciphertext, nonce, tag,
                   conflict_server_json, safe_failure_code
            FROM local_customer_projection
            WHERE organization_id = $org;
            """;
        cmd.Parameters.AddWithValue("$org", active.Identity.OrganizationId.ToString("D"));

        var all = new List<LocalCustomerProjection>();
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            all.Add(await ReadCustomerProjectionAsync(active, reader, ct).ConfigureAwait(false));
        }

        var filtered = FilterCustomers(all, search);
        return filtered
            .OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 500))
            .ToList();
    }

    public async Task<int> CountCustomersAsync(string? search, CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);

        await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT customer_id, organization_id, status, entity_state, concurrency_token,
                   pending_operation_id, created_utc, updated_utc, ciphertext, nonce, tag,
                   conflict_server_json, safe_failure_code
            FROM local_customer_projection
            WHERE organization_id = $org;
            """;
        cmd.Parameters.AddWithValue("$org", active.Identity.OrganizationId.ToString("D"));

        var all = new List<LocalCustomerProjection>();
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            all.Add(await ReadCustomerProjectionAsync(active, reader, ct).ConfigureAwait(false));
        }

        return FilterCustomers(all, search).Count;
    }

    public async Task<IReadOnlyList<LocalCreditProjection>> ListCreditsAsync(Guid customerId, CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);

        await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT credit_entry_id, customer_id, organization_id, entity_state, pending_operation_id,
                   depends_on_operation_id, created_utc, ciphertext, nonce, tag, safe_failure_code
            FROM local_credit_projection
            WHERE customer_id = $customer AND organization_id = $org
            ORDER BY created_utc ASC;
            """;
        cmd.Parameters.AddWithValue("$customer", customerId.ToString("D"));
        cmd.Parameters.AddWithValue("$org", active.Identity.OrganizationId.ToString("D"));

        var list = new List<LocalCreditProjection>();
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(await ReadCreditProjectionAsync(active, reader, ct).ConfigureAwait(false));
        }

        return list;
    }

    public async Task<LocalCustomerBalanceProjection> GetBalanceAsync(Guid customerId, CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);

        await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT confirmed_ciphertext, confirmed_nonce, confirmed_tag,
                   pending_ciphertext, pending_nonce, pending_tag
            FROM local_customer_balance
            WHERE customer_id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", customerId.ToString("D"));
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            return new LocalCustomerBalanceProjection(customerId, 0m, 0m, 0m);
        }

        var confirmed = await DecryptBalanceColumnAsync(
            active, customerId, reader, "confirmed_ciphertext", "confirmed_nonce", "confirmed_tag", ct)
            .ConfigureAwait(false);
        var pending = await DecryptBalanceColumnAsync(
            active, customerId, reader, "pending_ciphertext", "pending_nonce", "pending_tag", ct)
            .ConfigureAwait(false);

        return new LocalCustomerBalanceProjection(
            customerId,
            confirmed,
            pending,
            confirmed + pending);
    }

    public async Task SetDownloadCheckpointAsync(string stream, DateTimeOffset checkpointUtc, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stream);
        var active = RequireActiveContext();

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT INTO local_download_checkpoint (stream, checkpoint_utc)
                VALUES ($stream, $utc)
                ON CONFLICT(stream) DO UPDATE SET checkpoint_utc = excluded.checkpoint_utc;
                """;
            cmd.Parameters.AddWithValue("$stream", stream);
            cmd.Parameters.AddWithValue("$utc", FormatUtc(checkpointUtc));
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DateTimeOffset?> GetDownloadCheckpointAsync(string stream, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stream);
        var active = RequireActiveContext();

        await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT checkpoint_utc FROM local_download_checkpoint WHERE stream = $stream;";
        cmd.Parameters.AddWithValue("$stream", stream);
        var value = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        if (value is null or DBNull)
        {
            return null;
        }

        return DateTimeOffset.Parse(
            Convert.ToString(value, CultureInfo.InvariantCulture)!,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
    }

    public async Task MarkDependentsBlockedAsync(Guid dependencyOperationId, string failureCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCode);
        var active = RequireActiveContext();
        var dep = dependencyOperationId.ToString("D");

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                UPDATE local_credit_projection
                SET entity_state = $state,
                    safe_failure_code = $code
                WHERE depends_on_operation_id = $dep
                  AND entity_state IN ($pendingCreate, $pendingUpdate, $syncing);
                """;
            cmd.Parameters.AddWithValue("$state", LocalEntitySyncState.Conflict.ToString());
            cmd.Parameters.AddWithValue("$code", failureCode);
            cmd.Parameters.AddWithValue("$dep", dep);
            cmd.Parameters.AddWithValue("$pendingCreate", LocalEntitySyncState.PendingCreate.ToString());
            cmd.Parameters.AddWithValue("$pendingUpdate", LocalEntitySyncState.PendingUpdate.ToString());
            cmd.Parameters.AddWithValue("$syncing", LocalEntitySyncState.Syncing.ToString());
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
        int payloadVersion,
        string idempotencyKey,
        byte[] payloadJson,
        Guid? entityId,
        Guid? dependsOn,
        string? concurrencyToken,
        Func<SqliteConnection, SqliteTransaction, string, string, EncryptedPayload, string, Task> writeProjection,
        CancellationToken ct)
    {
        var deviceId = await deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
        var payloadHash = Convert.ToHexString(SHA256.HashData(payloadJson)).ToLowerInvariant();
        var aad = OfflinePayloadBinding.BuildAssociatedData(active.Identity.ContextHash, operationId, operationType);
        var queueEncrypted = await payloadProtector.EncryptAsync(payloadJson, aad, ct).ConfigureAwait(false);
        var now = _clock.GetUtcNow();
        var queueNow = FormatUtc(now);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

            await writeProjection(connection, tx, deviceId, payloadHash, queueEncrypted, queueNow).ConfigureAwait(false);

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
            cmd.Parameters.AddWithValue("$operation_id", operationId.ToString("D"));
            cmd.Parameters.AddWithValue("$device_id", deviceId);
            cmd.Parameters.AddWithValue("$user_id", active.Identity.UserId.ToString("D"));
            cmd.Parameters.AddWithValue("$organization_id", active.Identity.OrganizationId.ToString("D"));
            cmd.Parameters.AddWithValue("$product_code", active.Identity.ProductCode);
            cmd.Parameters.AddWithValue("$operation_type", operationType);
            cmd.Parameters.AddWithValue("$payload_version", payloadVersion);
            cmd.Parameters.AddWithValue("$ciphertext", queueEncrypted.Ciphertext);
            cmd.Parameters.AddWithValue("$nonce", queueEncrypted.Nonce);
            cmd.Parameters.AddWithValue("$tag", queueEncrypted.Tag);
            cmd.Parameters.AddWithValue("$payload_hash", payloadHash);
            cmd.Parameters.AddWithValue("$idempotency_key", idempotencyKey);
            cmd.Parameters.AddWithValue("$created_utc", queueNow);
            cmd.Parameters.AddWithValue("$next_attempt_utc", queueNow);
            cmd.Parameters.AddWithValue("$queue_state", nameof(OfflineQueueState.Pending));
            cmd.Parameters.AddWithValue("$concurrency_token", (object?)concurrencyToken ?? DBNull.Value);
            cmd.Parameters.AddWithValue(
                "$depends_on",
                dependsOn is Guid dep ? dep.ToString("D") : DBNull.Value);
            cmd.Parameters.AddWithValue(
                "$entity_id",
                entityId is Guid entity ? entity.ToString("D") : DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task InsertCustomerProjectionAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        LocalCustomerProjection customer,
        EncryptedPayload encrypted,
        string createdUtc,
        string updatedUtc,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT INTO local_customer_projection (
                customer_id, organization_id, status, entity_state, concurrency_token,
                pending_operation_id, created_utc, updated_utc, ciphertext, nonce, tag,
                conflict_server_json, safe_failure_code)
            VALUES (
                $id, $org, $status, $state, NULL, $pending, $created, $updated,
                $ciphertext, $nonce, $tag, NULL, NULL);
            """;
        BindCustomerProjection(cmd, customer, encrypted, updatedUtc);
        cmd.Parameters.AddWithValue("$created", createdUtc);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task UpdateCustomerProjectionForPendingUpdateAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        LocalCustomerProjection customer,
        EncryptedPayload encrypted,
        string updatedUtc,
        string _,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            UPDATE local_customer_projection
            SET status = $status,
                entity_state = $state,
                concurrency_token = $token,
                pending_operation_id = $pending,
                updated_utc = $updated,
                ciphertext = $ciphertext,
                nonce = $nonce,
                tag = $tag,
                conflict_server_json = NULL,
                safe_failure_code = NULL
            WHERE customer_id = $id;
            """;
        BindCustomerProjection(cmd, customer, encrypted, updatedUtc);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task InsertCreditProjectionAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        LocalCreditProjection credit,
        EncryptedPayload encrypted,
        string createdUtc,
        string _,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT INTO local_credit_projection (
                credit_entry_id, customer_id, organization_id, entity_state,
                pending_operation_id, depends_on_operation_id, created_utc,
                ciphertext, nonce, tag, safe_failure_code)
            VALUES (
                $id, $customer, $org, $state, $pending, $depends, $created,
                $ciphertext, $nonce, $tag, NULL);
            """;
        BindCreditProjection(cmd, credit, encrypted, createdUtc, credit.DependsOnOperationId?.ToString("D"));
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private async Task AddPendingBalanceAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        LocalContextSnapshot active,
        Guid customerId,
        decimal amount,
        CancellationToken ct)
    {
        decimal currentPending = 0m;
        await using var select = connection.CreateCommand();
        select.Transaction = tx;
        select.CommandText =
            """
            SELECT pending_ciphertext, pending_nonce, pending_tag
            FROM local_customer_balance
            WHERE customer_id = $id;
            """;
        select.Parameters.AddWithValue("$id", customerId.ToString("D"));
        await using var balanceReader = await select.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (await balanceReader.ReadAsync(ct).ConfigureAwait(false)
            && !balanceReader.IsDBNull(balanceReader.GetOrdinal("pending_ciphertext")))
        {
            var plaintext = await DecryptBalanceBytesAsync(
                active,
                customerId,
                (byte[])balanceReader["pending_ciphertext"],
                (byte[])balanceReader["pending_nonce"],
                (byte[])balanceReader["pending_tag"],
                ct).ConfigureAwait(false);
            currentPending = ParseDecimal(plaintext);
        }

        var newPending = currentPending + amount;
        var encrypted = await EncryptDecimalForBalanceAsync(active, customerId, newPending, ct).ConfigureAwait(false);

        await using var upsert = connection.CreateCommand();
        upsert.Transaction = tx;
        upsert.CommandText =
            """
            INSERT INTO local_customer_balance (
                customer_id, confirmed_ciphertext, confirmed_nonce, confirmed_tag,
                pending_ciphertext, pending_nonce, pending_tag)
            VALUES ($id, NULL, NULL, NULL, $ciphertext, $nonce, $tag)
            ON CONFLICT(customer_id) DO UPDATE SET
                pending_ciphertext = excluded.pending_ciphertext,
                pending_nonce = excluded.pending_nonce,
                pending_tag = excluded.pending_tag;
            """;
        upsert.Parameters.AddWithValue("$id", customerId.ToString("D"));
        upsert.Parameters.AddWithValue("$ciphertext", encrypted.Ciphertext);
        upsert.Parameters.AddWithValue("$nonce", encrypted.Nonce);
        upsert.Parameters.AddWithValue("$tag", encrypted.Tag);
        await upsert.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private async Task SubtractPendingBalanceAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        LocalContextSnapshot active,
        Guid customerId,
        decimal amount,
        CancellationToken ct)
    {
        decimal currentPending = 0m;
        await using var select = connection.CreateCommand();
        select.Transaction = tx;
        select.CommandText =
            """
            SELECT pending_ciphertext, pending_nonce, pending_tag
            FROM local_customer_balance
            WHERE customer_id = $id;
            """;
        select.Parameters.AddWithValue("$id", customerId.ToString("D"));
        await using var balanceReader = await select.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (await balanceReader.ReadAsync(ct).ConfigureAwait(false)
            && !balanceReader.IsDBNull(balanceReader.GetOrdinal("pending_ciphertext")))
        {
            var plaintext = await DecryptBalanceBytesAsync(
                active,
                customerId,
                (byte[])balanceReader["pending_ciphertext"],
                (byte[])balanceReader["pending_nonce"],
                (byte[])balanceReader["pending_tag"],
                ct).ConfigureAwait(false);
            currentPending = ParseDecimal(plaintext);
        }

        var newPending = Math.Max(0m, currentPending - amount);
        var encrypted = await EncryptDecimalForBalanceAsync(active, customerId, newPending, ct).ConfigureAwait(false);

        await using var upsert = connection.CreateCommand();
        upsert.Transaction = tx;
        upsert.CommandText =
            """
            INSERT INTO local_customer_balance (
                customer_id, confirmed_ciphertext, confirmed_nonce, confirmed_tag,
                pending_ciphertext, pending_nonce, pending_tag)
            VALUES ($id, NULL, NULL, NULL, $ciphertext, $nonce, $tag)
            ON CONFLICT(customer_id) DO UPDATE SET
                pending_ciphertext = excluded.pending_ciphertext,
                pending_nonce = excluded.pending_nonce,
                pending_tag = excluded.pending_tag;
            """;
        upsert.Parameters.AddWithValue("$id", customerId.ToString("D"));
        upsert.Parameters.AddWithValue("$ciphertext", encrypted.Ciphertext);
        upsert.Parameters.AddWithValue("$nonce", encrypted.Nonce);
        upsert.Parameters.AddWithValue("$tag", encrypted.Tag);
        await upsert.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private async Task<LocalCreditProjection?> TryReadCreditProjectionAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        LocalContextSnapshot active,
        Guid creditEntryId,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            SELECT credit_entry_id, customer_id, organization_id, entity_state, pending_operation_id,
                   depends_on_operation_id, created_utc, ciphertext, nonce, tag, safe_failure_code
            FROM local_credit_projection
            WHERE credit_entry_id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", creditEntryId.ToString("D"));
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        return await ReadCreditProjectionAsync(active, reader, ct).ConfigureAwait(false);
    }

    private async Task<byte[]> DecryptBalanceBytesAsync(
        LocalContextSnapshot active,
        Guid customerId,
        byte[] ciphertext,
        byte[] nonce,
        byte[] tag,
        CancellationToken ct)
    {
        var aad = OfflinePayloadBinding.BuildBalanceAssociatedData(active.Identity.ContextHash, customerId);
        return await payloadProtector.DecryptAsync(new EncryptedPayload(ciphertext, nonce, tag), aad, ct)
            .ConfigureAwait(false);
    }

    private async Task<EncryptedPayload> EncryptDecimalForBalanceAsync(
        LocalContextSnapshot active,
        Guid customerId,
        decimal value,
        CancellationToken ct)
    {
        var aad = OfflinePayloadBinding.BuildBalanceAssociatedData(active.Identity.ContextHash, customerId);
        var plaintext = Encoding.UTF8.GetBytes(FormatDecimal(value));
        return await payloadProtector.EncryptAsync(plaintext, aad, ct).ConfigureAwait(false);
    }

    private async Task<EncryptedPayload> EncryptDecimalAsync(
        LocalContextSnapshot active,
        Guid customerId,
        decimal value,
        CancellationToken ct) =>
        await EncryptDecimalForBalanceAsync(active, customerId, value, ct).ConfigureAwait(false);

    private async Task<EncryptedPayload> EncryptCustomerFieldsAsync(
        LocalContextSnapshot active,
        Guid customerId,
        LocalCustomerProjection customer,
        CancellationToken ct)
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(new CustomerFieldPayload(
            customer.DisplayName,
            customer.MobileNumber,
            customer.Address,
            customer.Notes), JsonOptions);
        var aad = OfflinePayloadBinding.BuildCustomerAssociatedData(active.Identity.ContextHash, customerId);
        return await payloadProtector.EncryptAsync(plaintext, aad, ct).ConfigureAwait(false);
    }

    private async Task<EncryptedPayload> EncryptCreditFieldsAsync(
        LocalContextSnapshot active,
        Guid creditEntryId,
        LocalCreditProjection credit,
        CancellationToken ct)
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(new CreditFieldPayload(
            FormatDecimal(credit.Amount),
            credit.Remarks,
            credit.Status), JsonOptions);
        var aad = OfflinePayloadBinding.BuildCreditAssociatedData(active.Identity.ContextHash, creditEntryId);
        return await payloadProtector.EncryptAsync(plaintext, aad, ct).ConfigureAwait(false);
    }

    private async Task<LocalCustomerProjection> ReadCustomerProjectionAsync(
        LocalContextSnapshot active,
        SqliteDataReader reader,
        CancellationToken ct)
    {
        var customerId = Guid.Parse(reader.GetString(reader.GetOrdinal("customer_id")));
        var ciphertext = (byte[])reader["ciphertext"];
        var nonce = (byte[])reader["nonce"];
        var tag = (byte[])reader["tag"];
        var aad = OfflinePayloadBinding.BuildCustomerAssociatedData(active.Identity.ContextHash, customerId);
        var plaintext = await payloadProtector
            .DecryptAsync(new EncryptedPayload(ciphertext, nonce, tag), aad, ct)
            .ConfigureAwait(false);
        var fields = JsonSerializer.Deserialize<CustomerFieldPayload>(plaintext, JsonOptions)
            ?? throw new InvalidOperationException("customer_payload_invalid");

        var pendingOrdinal = reader.GetOrdinal("pending_operation_id");
        return new LocalCustomerProjection(
            customerId,
            Guid.Parse(reader.GetString(reader.GetOrdinal("organization_id"))),
            fields.DisplayName,
            fields.MobileNumber,
            fields.Address,
            fields.Notes,
            reader.GetString(reader.GetOrdinal("status")),
            DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("created_utc")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("updated_utc")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Enum.Parse<LocalEntitySyncState>(reader.GetString(reader.GetOrdinal("entity_state")), ignoreCase: true),
            reader.IsDBNull(pendingOrdinal) ? null : Guid.Parse(reader.GetString(pendingOrdinal)),
            reader.IsDBNull(reader.GetOrdinal("concurrency_token")) ? null : reader.GetString(reader.GetOrdinal("concurrency_token")),
            reader.IsDBNull(reader.GetOrdinal("conflict_server_json")) ? null : reader.GetString(reader.GetOrdinal("conflict_server_json")),
            reader.IsDBNull(reader.GetOrdinal("safe_failure_code")) ? null : reader.GetString(reader.GetOrdinal("safe_failure_code")));
    }

    private async Task<LocalCreditProjection> ReadCreditProjectionAsync(
        LocalContextSnapshot active,
        SqliteDataReader reader,
        CancellationToken ct)
    {
        var creditEntryId = Guid.Parse(reader.GetString(reader.GetOrdinal("credit_entry_id")));
        var ciphertext = (byte[])reader["ciphertext"];
        var nonce = (byte[])reader["nonce"];
        var tag = (byte[])reader["tag"];
        var aad = OfflinePayloadBinding.BuildCreditAssociatedData(active.Identity.ContextHash, creditEntryId);
        var plaintext = await payloadProtector
            .DecryptAsync(new EncryptedPayload(ciphertext, nonce, tag), aad, ct)
            .ConfigureAwait(false);
        var fields = JsonSerializer.Deserialize<CreditFieldPayload>(plaintext, JsonOptions)
            ?? throw new InvalidOperationException("credit_payload_invalid");

        var pendingOrdinal = reader.GetOrdinal("pending_operation_id");
        var dependsOrdinal = reader.GetOrdinal("depends_on_operation_id");
        return new LocalCreditProjection(
            creditEntryId,
            Guid.Parse(reader.GetString(reader.GetOrdinal("customer_id"))),
            Guid.Parse(reader.GetString(reader.GetOrdinal("organization_id"))),
            ParseDecimal(fields.Amount),
            fields.Remarks,
            fields.Status,
            DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("created_utc")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Enum.Parse<LocalEntitySyncState>(reader.GetString(reader.GetOrdinal("entity_state")), ignoreCase: true),
            reader.IsDBNull(pendingOrdinal) ? null : Guid.Parse(reader.GetString(pendingOrdinal)),
            reader.IsDBNull(dependsOrdinal) ? null : Guid.Parse(reader.GetString(dependsOrdinal)),
            reader.IsDBNull(reader.GetOrdinal("safe_failure_code")) ? null : reader.GetString(reader.GetOrdinal("safe_failure_code")));
    }

    private async Task<decimal> DecryptBalanceColumnAsync(
        LocalContextSnapshot active,
        Guid customerId,
        SqliteDataReader reader,
        string ciphertextColumn,
        string nonceColumn,
        string tagColumn,
        CancellationToken ct)
    {
        if (reader.IsDBNull(reader.GetOrdinal(ciphertextColumn)))
        {
            return 0m;
        }

        var bytes = await DecryptBalanceBytesAsync(
            active,
            customerId,
            (byte[])reader[ciphertextColumn],
            (byte[])reader[nonceColumn],
            (byte[])reader[tagColumn],
            ct).ConfigureAwait(false);
        return ParseDecimal(bytes);
    }

    private static void BindCustomerProjection(
        SqliteCommand cmd,
        LocalCustomerProjection customer,
        EncryptedPayload encrypted,
        string updatedUtc)
    {
        cmd.Parameters.AddWithValue("$id", customer.CustomerId.ToString("D"));
        cmd.Parameters.AddWithValue("$org", customer.OrganizationId.ToString("D"));
        cmd.Parameters.AddWithValue("$status", customer.Status);
        cmd.Parameters.AddWithValue("$state", customer.EntityState.ToString());
        cmd.Parameters.AddWithValue("$token", (object?)customer.ConcurrencyToken ?? DBNull.Value);
        cmd.Parameters.AddWithValue(
            "$pending",
            customer.PendingOperationId is Guid pending ? pending.ToString("D") : DBNull.Value);
        cmd.Parameters.AddWithValue("$updated", updatedUtc);
        cmd.Parameters.AddWithValue("$ciphertext", encrypted.Ciphertext);
        cmd.Parameters.AddWithValue("$nonce", encrypted.Nonce);
        cmd.Parameters.AddWithValue("$tag", encrypted.Tag);
    }

    private static void BindCreditProjection(
        SqliteCommand cmd,
        LocalCreditProjection credit,
        EncryptedPayload encrypted,
        string createdUtc,
        string? dependsOnOperationId)
    {
        cmd.Parameters.AddWithValue("$id", credit.CreditEntryId.ToString("D"));
        cmd.Parameters.AddWithValue("$customer", credit.CustomerId.ToString("D"));
        cmd.Parameters.AddWithValue("$org", credit.OrganizationId.ToString("D"));
        cmd.Parameters.AddWithValue("$state", credit.EntityState.ToString());
        cmd.Parameters.AddWithValue(
            "$pending",
            credit.PendingOperationId is Guid pending ? pending.ToString("D") : DBNull.Value);
        cmd.Parameters.AddWithValue("$depends", (object?)dependsOnOperationId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", createdUtc);
        cmd.Parameters.AddWithValue("$ciphertext", encrypted.Ciphertext);
        cmd.Parameters.AddWithValue("$nonce", encrypted.Nonce);
        cmd.Parameters.AddWithValue("$tag", encrypted.Tag);
    }

    private static IReadOnlyList<LocalCustomerProjection> FilterCustomers(
        IReadOnlyList<LocalCustomerProjection> customers,
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return customers;
        }

        return customers
            .Where(c =>
                c.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (c.MobileNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }

    public async Task<LocalEntityStateCounts> GetEntityStateCountsAsync(CancellationToken ct = default)
    {
        var active = contextManager.ActiveContext;
        if (active is null || active.Status != LocalContextInitStatus.Ready)
        {
            return LocalEntityStateCounts.Empty;
        }

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            var customers = await ReadEntityStateCountsAsync(connection, "local_customer_projection", ct)
                .ConfigureAwait(false);
            var credits = await ReadEntityStateCountsAsync(connection, "local_credit_projection", ct)
                .ConfigureAwait(false);
            return new LocalEntityStateCounts(customers, credits);
        }
        catch
        {
            return LocalEntityStateCounts.Empty;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task<IReadOnlyDictionary<LocalEntitySyncState, int>> ReadEntityStateCountsAsync(
        SqliteConnection connection,
        string table,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT entity_state, COUNT(*) FROM {table} GROUP BY entity_state";
        var counts = new Dictionary<LocalEntitySyncState, int>();
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            if (Enum.TryParse<LocalEntitySyncState>(reader.GetString(0), out var state))
            {
                counts[state] = reader.GetInt32(1);
            }
        }

        return counts;
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

    private void ValidateSessionOrg(LocalContextSnapshot active)
    {
        var session = currentUser.Session
            ?? throw new InvalidOperationException("session_required");
        if (session.OrganizationId is not Guid orgId || orgId != active.Identity.OrganizationId)
        {
            throw new InvalidOperationException("organization_mismatch");
        }
    }

    private async Task EnsureEncryptionKeyAsync(CancellationToken ct)
    {
        if (!await payloadProtector.IsKeyAvailableAsync(ct).ConfigureAwait(false))
        {
            await payloadProtector.EnsureKeyAsync(ct).ConfigureAwait(false);
        }

        if (!await payloadProtector.IsKeyAvailableAsync(ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException("local_payload_key_unavailable");
        }
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

    private static string FormatUtc(DateTimeOffset value) =>
        value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

    private static string FormatDecimal(decimal value) =>
        value.ToString("F2", CultureInfo.InvariantCulture);

    private static decimal ParseDecimal(ReadOnlySpan<byte> utf8) =>
        decimal.Parse(Encoding.UTF8.GetString(utf8), NumberStyles.Number, CultureInfo.InvariantCulture);

    private static decimal ParseDecimal(string value) =>
        decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    private sealed record CustomerFieldPayload(
        string DisplayName,
        string? MobileNumber,
        string? Address,
        string? Notes);

    private sealed record CreditFieldPayload(string Amount, string Remarks, string Status);

    private sealed record CustomerCreatePayload(
        Guid CustomerId,
        string DisplayName,
        string? MobileNumber,
        string? Address,
        string? Notes);

    private sealed record CustomerUpdatePayload(
        Guid CustomerId,
        string DisplayName,
        string? MobileNumber,
        string? Address,
        string? Notes,
        string ExpectedUpdatedAtUtc);

    private sealed record CreditCreatePayload(
        Guid CreditEntryId,
        Guid CustomerId,
        string Amount,
        string Remarks);
}
