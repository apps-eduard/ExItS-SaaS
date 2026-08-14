using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Sales;
using Microsoft.Data.Sqlite;

namespace ExItS.PinoyBusinessPOS.LocalStore;

/// <summary>
/// Local selling-catalog cache, open-shift snapshot, and atomic cash-sale + outbox persistence.
/// Catalog fields are non-PHI merchant selling data stored in cleartext for search; sale checkout
/// payloads in the outbox remain AES-GCM encrypted like other offline operations.
/// </summary>
public sealed class LocalSellingCatalogAndCashSaleStore(
    ILocalContextManager contextManager,
    ILocalDatabasePathResolver pathResolver,
    ILocalPayloadProtector payloadProtector,
    IDeviceIdentityProvider deviceIdentity,
    ICurrentUserContext currentUser,
    TimeProvider? timeProvider = null) : ILocalSellingCatalogStore, ILocalCashSaleStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task ReplaceCatalogAsync(
        IReadOnlyList<PosProductCategoryDto> categories,
        IReadOnlyList<PosCatalogProductDto> products,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(products);
        var active = RequireActiveContext();
        var now = FormatUtc(_clock.GetUtcNow());

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

            await using (var clearCats = connection.CreateCommand())
            {
                clearCats.Transaction = tx;
                clearCats.CommandText = "DELETE FROM local_catalog_category;";
                await clearCats.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            await using (var clearProducts = connection.CreateCommand())
            {
                clearProducts.Transaction = tx;
                clearProducts.CommandText = "DELETE FROM local_catalog_product;";
                await clearProducts.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            foreach (var category in categories)
            {
                await UpsertCategoryRowAsync(connection, tx, category, now, ct).ConfigureAwait(false);
            }

            foreach (var product in products)
            {
                await UpsertProductRowAsync(connection, tx, product, now, ct).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpsertProductsAsync(IReadOnlyList<PosCatalogProductDto> products, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(products);
        if (products.Count == 0)
        {
            return;
        }

        var active = RequireActiveContext();
        var now = FormatUtc(_clock.GetUtcNow());
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
            foreach (var product in products)
            {
                await UpsertProductRowAsync(connection, tx, product, now, ct).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpsertCategoriesAsync(IReadOnlyList<PosProductCategoryDto> categories, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(categories);
        if (categories.Count == 0)
        {
            return;
        }

        var active = RequireActiveContext();
        var now = FormatUtc(_clock.GetUtcNow());
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
            foreach (var category in categories)
            {
                await UpsertCategoryRowAsync(connection, tx, category, now, ct).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveOpenShiftSnapshotAsync(PosCashierShiftDto shift, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(shift);
        var active = RequireActiveContext();
        if (shift.OrganizationId != active.Identity.OrganizationId)
        {
            throw new InvalidOperationException("Shift organization mismatch.");
        }

        var json = JsonSerializer.Serialize(shift, JsonOptions);
        var now = FormatUtc(_clock.GetUtcNow());
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT INTO local_open_shift_snapshot (organization_id, shift_json, captured_utc)
                VALUES ($org, $json, $captured)
                ON CONFLICT(organization_id) DO UPDATE SET
                    shift_json = excluded.shift_json,
                    captured_utc = excluded.captured_utc;
                """;
            cmd.Parameters.AddWithValue("$org", active.Identity.OrganizationId.ToString("D"));
            cmd.Parameters.AddWithValue("$json", json);
            cmd.Parameters.AddWithValue("$captured", now);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearOpenShiftSnapshotAsync(CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM local_open_shift_snapshot WHERE organization_id = $org;";
            cmd.Parameters.AddWithValue("$org", active.Identity.OrganizationId.ToString("D"));
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<PosCashierShiftDto?> LoadOpenShiftSnapshotAsync(CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT shift_json FROM local_open_shift_snapshot
                WHERE organization_id = $org
                LIMIT 1;
                """;
            cmd.Parameters.AddWithValue("$org", active.Identity.OrganizationId.ToString("D"));
            var json = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) as string;
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<PosCashierShiftDto>(json, JsonOptions);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<PosProductCategoryDto>> ListCategoriesAsync(CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT category_id, organization_id, name, status, updated_utc
                FROM local_catalog_category
                WHERE organization_id = $org
                ORDER BY name COLLATE NOCASE;
                """;
            cmd.Parameters.AddWithValue("$org", active.Identity.OrganizationId.ToString("D"));
            var list = new List<PosProductCategoryDto>();
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                list.Add(new PosProductCategoryDto(
                    Guid.Parse(reader.GetString(0)),
                    Guid.Parse(reader.GetString(1)),
                    reader.GetString(2),
                    reader.GetString(3),
                    ParseUtc(reader.GetString(4)),
                    ParseUtc(reader.GetString(4))));
            }

            return list;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<PosCatalogProductDto>> SearchProductsAsync(
        string? search,
        Guid? categoryId,
        int take,
        CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        take = Math.Clamp(take, 1, 200);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            var sql = new StringBuilder(
                """
                SELECT product_id, organization_id, name, description, sku, barcode, category_id,
                       unit_of_measure, selling_mode, selling_price, status, is_tracked, on_hand_quantity, stock_status, updated_utc,
                       IFNULL(can_be_purchased, 1), IFNULL(can_be_sold, 1), IFNULL(can_be_used_as_ingredient, 0),
                       IFNULL(is_produced, 0), usage_preset
                FROM local_catalog_product
                WHERE organization_id = $org
                  AND status = 'Active'
                """);
            cmd.Parameters.AddWithValue("$org", active.Identity.OrganizationId.ToString("D"));
            if (categoryId is Guid cat)
            {
                sql.Append(" AND category_id = $category");
                cmd.Parameters.AddWithValue("$category", cat.ToString("D"));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql.Append(
                    """
                     AND (
                        name LIKE $q ESCAPE '\'
                        OR IFNULL(sku, '') LIKE $q ESCAPE '\'
                        OR IFNULL(barcode, '') LIKE $q ESCAPE '\')
                    """);
                cmd.Parameters.AddWithValue("$q", "%" + EscapeLike(search.Trim()) + "%");
            }

            sql.Append(" ORDER BY name COLLATE NOCASE LIMIT $take;");
            cmd.Parameters.AddWithValue("$take", take);
            cmd.CommandText = sql.ToString();

            var list = new List<PosCatalogProductDto>();
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                list.Add(ReadProduct(reader));
            }

            for (var i = 0; i < list.Count; i++)
            {
                var units = await LoadSellUnitsAsync(connection, list[i].ProductId, ct).ConfigureAwait(false);
                list[i] = list[i] with { Units = units };
            }

            return list;
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<PosCatalogProductDto?> FindBySkuAsync(string sku, CancellationToken ct = default) =>
        FindExactAsync("sku", sku, ct);

    public Task<PosCatalogProductDto?> FindByBarcodeAsync(string barcode, CancellationToken ct = default) =>
        FindExactAsync("barcode", barcode, ct);

    public async Task ApplyLocalInventoryDeductionAsync(
        IReadOnlyList<(Guid ProductId, decimal Quantity)> deductions,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(deductions);
        if (deductions.Count == 0)
        {
            return;
        }

        var active = RequireActiveContext();
        var now = FormatUtc(_clock.GetUtcNow());
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
            foreach (var (productId, quantity) in deductions)
            {
                if (quantity <= 0m)
                {
                    continue;
                }

                await DeductTrackedOnHandAsync(
                        connection,
                        tx,
                        active.Identity.OrganizationId,
                        productId,
                        quantity,
                        now,
                        ct)
                    .ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task PersistCashSaleAndEnqueueAsync(LocalCashSaleCommitCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!string.Equals(
                command.CheckoutRequest.PaymentMethod,
                PosSaleOptions.CashPaymentMethod,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only Cash sales may be persisted offline.");
        }

        var active = RequireActiveContext();
        await EnsureEncryptionKeyAsync(ct).ConfigureAwait(false);

        var payloadJson = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(command.CheckoutRequest, JsonOptions));
        var receiptJson = JsonSerializer.Serialize(command.Lines, JsonOptions);
        var deviceId = await deviceIdentity.GetOrCreateDeviceIdAsync(ct).ConfigureAwait(false);
        var payloadHash = Convert.ToHexString(SHA256.HashData(payloadJson)).ToLowerInvariant();
        var aad = OfflinePayloadBinding.BuildAssociatedData(
            active.Identity.ContextHash,
            command.OperationId,
            OfflineOperationTypes.SaleCheckout);
        var queueEncrypted = await payloadProtector.EncryptAsync(payloadJson, aad, ct).ConfigureAwait(false);
        var now = _clock.GetUtcNow();
        var nowText = FormatUtc(now);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);

            await using (var existsCmd = connection.CreateCommand())
            {
                existsCmd.CommandText = "SELECT 1 FROM local_cash_sale WHERE sale_id = $id LIMIT 1;";
                existsCmd.Parameters.AddWithValue("$id", command.SaleId.ToString("D"));
                var exists = await existsCmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                if (exists is not null)
                {
                    // Idempotent local commit — never duplicate queue rows for the same sale.
                    return;
                }
            }

            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

            await using (var saleCmd = connection.CreateCommand())
            {
                saleCmd.Transaction = tx;
                saleCmd.CommandText =
                    """
                    INSERT INTO local_cash_sale (
                        sale_id, organization_id, sale_number, shift_id, payment_method,
                        subtotal, total, amount_tendered, change_amount, recorded_at_utc, recorded_by,
                        entity_state, pending_operation_id, idempotency_key, server_reference,
                        receipt_json, safe_failure_code, created_utc, updated_utc)
                    VALUES (
                        $sale_id, $org, $sale_number, $shift_id, $payment_method,
                        $subtotal, $total, $tendered, $change, $recorded_at, $recorded_by,
                        $state, $pending, $idempotency, NULL,
                        $receipt, NULL, $created, $updated);
                    """;
                saleCmd.Parameters.AddWithValue("$sale_id", command.SaleId.ToString("D"));
                saleCmd.Parameters.AddWithValue("$org", active.Identity.OrganizationId.ToString("D"));
                saleCmd.Parameters.AddWithValue("$sale_number", command.SaleNumber);
                saleCmd.Parameters.AddWithValue("$shift_id", command.ShiftId.ToString("D"));
                saleCmd.Parameters.AddWithValue("$payment_method", "Cash");
                saleCmd.Parameters.AddWithValue("$subtotal", DecimalText(command.Subtotal));
                saleCmd.Parameters.AddWithValue("$total", DecimalText(command.Total));
                saleCmd.Parameters.AddWithValue("$tendered", DecimalText(command.AmountTendered));
                saleCmd.Parameters.AddWithValue("$change", DecimalText(command.ChangeAmount));
                saleCmd.Parameters.AddWithValue("$recorded_at", nowText);
                saleCmd.Parameters.AddWithValue("$recorded_by", command.RecordedBy.ToString("D"));
                saleCmd.Parameters.AddWithValue("$state", (int)LocalEntitySyncState.PendingCreate);
                saleCmd.Parameters.AddWithValue("$pending", command.OperationId.ToString("D"));
                saleCmd.Parameters.AddWithValue("$idempotency", command.IdempotencyKey);
                saleCmd.Parameters.AddWithValue("$receipt", receiptJson);
                saleCmd.Parameters.AddWithValue("$created", nowText);
                saleCmd.Parameters.AddWithValue("$updated", nowText);
                await saleCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            foreach (var line in command.Lines.Where(l => l.IsTracked && l.Quantity > 0m))
            {
                await DeductTrackedOnHandAsync(
                        connection,
                        tx,
                        active.Identity.OrganizationId,
                        line.ProductId,
                        line.Quantity,
                        nowText,
                        ct)
                    .ConfigureAwait(false);
            }

            await using (var queueCmd = connection.CreateCommand())
            {
                queueCmd.Transaction = tx;
                queueCmd.CommandText =
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
                        NULL, NULL, NULL, NULL, NULL,
                        NULL, NULL, NULL, $entity_id);
                    """;
                queueCmd.Parameters.AddWithValue("$operation_id", command.OperationId.ToString("D"));
                queueCmd.Parameters.AddWithValue("$device_id", deviceId);
                queueCmd.Parameters.AddWithValue("$user_id", active.Identity.UserId.ToString("D"));
                queueCmd.Parameters.AddWithValue("$organization_id", active.Identity.OrganizationId.ToString("D"));
                queueCmd.Parameters.AddWithValue("$product_code", active.Identity.ProductCode);
                queueCmd.Parameters.AddWithValue("$operation_type", OfflineOperationTypes.SaleCheckout);
                queueCmd.Parameters.AddWithValue(
                    "$payload_version",
                    OfflineOperationTypes.SaleCheckoutPayloadVersions.Current);
                queueCmd.Parameters.AddWithValue("$ciphertext", queueEncrypted.Ciphertext);
                queueCmd.Parameters.AddWithValue("$nonce", queueEncrypted.Nonce);
                queueCmd.Parameters.AddWithValue("$tag", queueEncrypted.Tag);
                queueCmd.Parameters.AddWithValue("$payload_hash", payloadHash);
                queueCmd.Parameters.AddWithValue("$idempotency_key", command.IdempotencyKey);
                queueCmd.Parameters.AddWithValue("$created_utc", nowText);
                queueCmd.Parameters.AddWithValue("$next_attempt_utc", nowText);
                queueCmd.Parameters.AddWithValue("$queue_state", nameof(OfflineQueueState.Pending));
                queueCmd.Parameters.AddWithValue("$entity_id", command.SaleId.ToString("D"));
                await queueCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LocalCashSaleProjection?> GetBySaleIdAsync(Guid saleId, CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT sale_id, organization_id, sale_number, shift_id, payment_method,
                       subtotal, total, amount_tendered, change_amount, recorded_at_utc, recorded_by,
                       entity_state, pending_operation_id, idempotency_key, server_reference,
                       receipt_json, safe_failure_code
                FROM local_cash_sale
                WHERE sale_id = $id
                LIMIT 1;
                """;
            cmd.Parameters.AddWithValue("$id", saleId.ToString("D"));
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                return null;
            }

            var receiptJson = reader.GetString(15);
            var lines = JsonSerializer.Deserialize<List<LocalCashSaleLineSnapshot>>(receiptJson, JsonOptions)
                        ?? [];
            return new LocalCashSaleProjection(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : Guid.Parse(reader.GetString(3)),
                reader.GetString(4),
                ParseDecimal(reader.GetString(5)),
                ParseDecimal(reader.GetString(6)),
                reader.IsDBNull(7) ? null : ParseDecimal(reader.GetString(7)),
                reader.IsDBNull(8) ? null : ParseDecimal(reader.GetString(8)),
                ParseUtc(reader.GetString(9)),
                Guid.Parse(reader.GetString(10)),
                (LocalEntitySyncState)reader.GetInt32(11),
                reader.IsDBNull(12) ? null : Guid.Parse(reader.GetString(12)),
                reader.GetString(13),
                reader.IsDBNull(14) ? null : reader.GetString(14),
                lines,
                reader.IsDBNull(16) ? null : reader.GetString(16));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MarkSyncedAsync(Guid saleId, string serverReference, CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        var now = FormatUtc(_clock.GetUtcNow());
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                UPDATE local_cash_sale
                SET entity_state = $state,
                    pending_operation_id = NULL,
                    server_reference = $server,
                    safe_failure_code = NULL,
                    updated_utc = $updated
                WHERE sale_id = $id
                  AND organization_id = $org;
                """;
            cmd.Parameters.AddWithValue("$state", (int)LocalEntitySyncState.ServerConfirmed);
            cmd.Parameters.AddWithValue("$server", serverReference);
            cmd.Parameters.AddWithValue("$updated", now);
            cmd.Parameters.AddWithValue("$id", saleId.ToString("D"));
            cmd.Parameters.AddWithValue("$org", active.Identity.OrganizationId.ToString("D"));
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MarkSyncFailedAsync(Guid saleId, string safeFailureCode, CancellationToken ct = default)
    {
        var active = RequireActiveContext();
        var now = FormatUtc(_clock.GetUtcNow());
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                UPDATE local_cash_sale
                SET entity_state = $state,
                    safe_failure_code = $code,
                    updated_utc = $updated
                WHERE sale_id = $id
                  AND organization_id = $org;
                """;
            cmd.Parameters.AddWithValue("$state", (int)LocalEntitySyncState.Rejected);
            cmd.Parameters.AddWithValue("$code", safeFailureCode);
            cmd.Parameters.AddWithValue("$updated", now);
            cmd.Parameters.AddWithValue("$id", saleId.ToString("D"));
            cmd.Parameters.AddWithValue("$org", active.Identity.OrganizationId.ToString("D"));
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<PosCatalogProductDto?> FindExactAsync(string column, string value, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var active = RequireActiveContext();
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenConnectionAsync(active, ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                $"""
                SELECT product_id, organization_id, name, description, sku, barcode, category_id,
                       unit_of_measure, selling_mode, selling_price, status, is_tracked, on_hand_quantity, stock_status, updated_utc,
                       IFNULL(can_be_purchased, 1), IFNULL(can_be_sold, 1), IFNULL(can_be_used_as_ingredient, 0),
                       IFNULL(is_produced, 0), usage_preset
                FROM local_catalog_product
                WHERE organization_id = $org
                  AND status = 'Active'
                  AND {column} = $value
                LIMIT 1;
                """;
            cmd.Parameters.AddWithValue("$org", active.Identity.OrganizationId.ToString("D"));
            cmd.Parameters.AddWithValue("$value", value.Trim());
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                return null;
            }

            return ReadProduct(reader);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task UpsertCategoryRowAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        PosProductCategoryDto category,
        string now,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT INTO local_catalog_category (category_id, organization_id, name, status, updated_utc)
            VALUES ($id, $org, $name, $status, $updated)
            ON CONFLICT(category_id) DO UPDATE SET
                organization_id = excluded.organization_id,
                name = excluded.name,
                status = excluded.status,
                updated_utc = excluded.updated_utc;
            """;
        cmd.Parameters.AddWithValue("$id", category.CategoryId.ToString("D"));
        cmd.Parameters.AddWithValue("$org", category.OrganizationId.ToString("D"));
        cmd.Parameters.AddWithValue("$name", category.Name);
        cmd.Parameters.AddWithValue("$status", category.Status);
        cmd.Parameters.AddWithValue("$updated", now);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task UpsertProductRowAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        PosCatalogProductDto product,
        string now,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT INTO local_catalog_product (
                product_id, organization_id, name, description, sku, barcode, category_id,
                unit_of_measure, selling_mode, selling_price, status, is_tracked, on_hand_quantity, stock_status, updated_utc,
                can_be_purchased, can_be_sold, can_be_used_as_ingredient, is_produced, usage_preset)
            VALUES (
                $id, $org, $name, $description, $sku, $barcode, $category,
                $uom, $sellingMode, $price, $status, $tracked, $onhand, $stock, $updated,
                $canPurchase, $canSell, $asIngredient, $produced, $usagePreset)
            ON CONFLICT(product_id) DO UPDATE SET
                organization_id = excluded.organization_id,
                name = excluded.name,
                description = excluded.description,
                sku = excluded.sku,
                barcode = excluded.barcode,
                category_id = excluded.category_id,
                unit_of_measure = excluded.unit_of_measure,
                selling_mode = excluded.selling_mode,
                selling_price = excluded.selling_price,
                status = excluded.status,
                is_tracked = excluded.is_tracked,
                on_hand_quantity = excluded.on_hand_quantity,
                stock_status = excluded.stock_status,
                updated_utc = excluded.updated_utc,
                can_be_purchased = excluded.can_be_purchased,
                can_be_sold = excluded.can_be_sold,
                can_be_used_as_ingredient = excluded.can_be_used_as_ingredient,
                is_produced = excluded.is_produced,
                usage_preset = excluded.usage_preset;
            """;
        cmd.Parameters.AddWithValue("$id", product.ProductId.ToString("D"));
        cmd.Parameters.AddWithValue("$org", product.OrganizationId.ToString("D"));
        cmd.Parameters.AddWithValue("$name", product.Name);
        cmd.Parameters.AddWithValue("$description", (object?)product.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$sku", (object?)product.Sku ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$barcode", (object?)product.Barcode ?? DBNull.Value);
        cmd.Parameters.AddWithValue(
            "$category",
            product.CategoryId is Guid c ? c.ToString("D") : DBNull.Value);
        cmd.Parameters.AddWithValue("$uom", product.UnitOfMeasure);
        cmd.Parameters.AddWithValue(
            "$sellingMode",
            string.IsNullOrWhiteSpace(product.SellingMode) ? "PerItem" : product.SellingMode);
        cmd.Parameters.AddWithValue("$price", DecimalText(product.SellingPrice));
        cmd.Parameters.AddWithValue("$status", product.Status);
        cmd.Parameters.AddWithValue("$tracked", product.IsTracked ? 1 : 0);
        cmd.Parameters.AddWithValue("$onhand", DecimalText(product.OnHandQuantity));
        cmd.Parameters.AddWithValue("$stock", product.StockStatus);
        cmd.Parameters.AddWithValue("$updated", now);
        cmd.Parameters.AddWithValue("$canPurchase", product.CanBePurchased ? 1 : 0);
        cmd.Parameters.AddWithValue("$canSell", product.CanBeSold ? 1 : 0);
        cmd.Parameters.AddWithValue("$asIngredient", product.CanBeUsedAsIngredient ? 1 : 0);
        cmd.Parameters.AddWithValue("$produced", product.IsProduced ? 1 : 0);
        cmd.Parameters.AddWithValue("$usagePreset", (object?)product.UsagePreset ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

        await using var clearUnits = connection.CreateCommand();
        clearUnits.Transaction = tx;
        clearUnits.CommandText = "DELETE FROM local_catalog_product_unit WHERE product_id = $id;";
        clearUnits.Parameters.AddWithValue("$id", product.ProductId.ToString("D"));
        await clearUnits.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

        if (product.Units is { Count: > 0 })
        {
            foreach (var unit in product.Units.Where(u =>
                         string.Equals(u.Kind, "Sell", StringComparison.OrdinalIgnoreCase) && u.IsActive))
            {
                await using var unitCmd = connection.CreateCommand();
                unitCmd.Transaction = tx;
                unitCmd.CommandText =
                    """
                    INSERT INTO local_catalog_product_unit (
                        unit_id, product_id, display_name, multiplier_to_base, selling_price,
                        allows_custom_quantity, sort_order, is_active)
                    VALUES ($unitId, $productId, $display, $multiplier, $price, $custom, $sort, $active);
                    """;
                unitCmd.Parameters.AddWithValue("$unitId", unit.UnitId.ToString("D"));
                unitCmd.Parameters.AddWithValue("$productId", product.ProductId.ToString("D"));
                unitCmd.Parameters.AddWithValue("$display", unit.DisplayName);
                unitCmd.Parameters.AddWithValue("$multiplier", DecimalText(unit.MultiplierToBase));
                unitCmd.Parameters.AddWithValue("$price", DecimalText(unit.SellingPrice ?? 0m));
                unitCmd.Parameters.AddWithValue("$custom", unit.AllowsCustomQuantity ? 1 : 0);
                unitCmd.Parameters.AddWithValue("$sort", unit.SortOrder);
                unitCmd.Parameters.AddWithValue("$active", unit.IsActive ? 1 : 0);
                await unitCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
        }
    }

    private static PosCatalogProductDto ReadProduct(SqliteDataReader reader)
    {
        Guid? categoryId = reader.IsDBNull(6) ? null : Guid.Parse(reader.GetString(6));
        var updated = ParseUtc(reader.GetString(14));
        var canPurchase = reader.FieldCount > 15 ? reader.GetInt32(15) == 1 : true;
        var canSell = reader.FieldCount > 16 ? reader.GetInt32(16) == 1 : true;
        var asIngredient = reader.FieldCount > 17 && reader.GetInt32(17) == 1;
        var produced = reader.FieldCount > 18 && reader.GetInt32(18) == 1;
        var usagePreset = reader.FieldCount > 19 && !reader.IsDBNull(19) ? reader.GetString(19) : "BuyAndSell";
        return new PosCatalogProductDto(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            categoryId,
            reader.GetString(7),
            string.IsNullOrWhiteSpace(reader.GetString(8)) ? "PerItem" : reader.GetString(8),
            ParseDecimal(reader.GetString(9)),
            reader.GetString(10),
            updated,
            updated,
            IsTracked: reader.GetInt32(11) == 1,
            OnHandQuantity: ParseDecimal(reader.GetString(12)),
            StockStatus: reader.GetString(13),
            CanBePurchased: canPurchase,
            CanBeSold: canSell,
            CanBeUsedAsIngredient: asIngredient,
            IsProduced: produced,
            UsagePreset: usagePreset);
    }

    private static async Task<IReadOnlyList<PosCatalogProductUnitDto>> LoadSellUnitsAsync(
        SqliteConnection connection,
        Guid productId,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT unit_id, product_id, display_name, multiplier_to_base, selling_price,
                   allows_custom_quantity, sort_order, is_active
            FROM local_catalog_product_unit
            WHERE product_id = $id AND is_active = 1
            ORDER BY sort_order, display_name;
            """;
        cmd.Parameters.AddWithValue("$id", productId.ToString("D"));
        var list = new List<PosCatalogProductUnitDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(new PosCatalogProductUnitDto(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                "Sell",
                reader.GetString(2),
                reader.GetString(2),
                ParseDecimal(reader.GetString(3)),
                ParseDecimal(reader.GetString(4)),
                reader.GetInt32(5) == 1,
                reader.GetInt32(7) == 1,
                reader.GetInt32(6)));
        }

        return list;
    }

    private async Task EnsureEncryptionKeyAsync(CancellationToken ct)
    {
        if (!await payloadProtector.IsKeyAvailableAsync(ct).ConfigureAwait(false))
        {
            await payloadProtector.EnsureKeyAsync(ct).ConfigureAwait(false);
        }
    }

    private LocalContextSnapshot RequireActiveContext()
    {
        var active = contextManager.ActiveContext
                     ?? throw new InvalidOperationException("Local context is not open.");
        if (currentUser.Session?.OrganizationId is Guid org
            && org != active.Identity.OrganizationId)
        {
            throw new InvalidOperationException("Session organization does not match local context.");
        }

        return active;
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

    private static async Task DeductTrackedOnHandAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        Guid organizationId,
        Guid productId,
        decimal quantity,
        string updatedUtc,
        CancellationToken ct)
    {
        await using var selectCmd = connection.CreateCommand();
        selectCmd.Transaction = tx;
        selectCmd.CommandText =
            """
            SELECT on_hand_quantity, is_tracked
            FROM local_catalog_product
            WHERE product_id = $id
              AND organization_id = $org
            LIMIT 1;
            """;
        selectCmd.Parameters.AddWithValue("$id", productId.ToString("D"));
        selectCmd.Parameters.AddWithValue("$org", organizationId.ToString("D"));

        string onHandText;
        long isTrackedFlag;
        await using (var reader = await selectCmd.ExecuteReaderAsync(ct).ConfigureAwait(false))
        {
            if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                return;
            }

            onHandText = reader.GetString(0);
            isTrackedFlag = reader.GetInt64(1);
        }

        if (isTrackedFlag != 1)
        {
            return;
        }

        var onHand = ParseDecimal(onHandText);
        var next = Math.Max(0m, onHand - quantity);
        var stockStatus = next <= 0m
            ? "OutOfStock"
            : next <= 5m
                ? "LowStock"
                : "InStock";

        await using var updateCmd = connection.CreateCommand();
        updateCmd.Transaction = tx;
        updateCmd.CommandText =
            """
            UPDATE local_catalog_product
            SET on_hand_quantity = $onhand,
                stock_status = $status,
                updated_utc = $updated
            WHERE product_id = $id
              AND organization_id = $org
              AND is_tracked = 1;
            """;
        updateCmd.Parameters.AddWithValue("$onhand", DecimalText(next));
        updateCmd.Parameters.AddWithValue("$status", stockStatus);
        updateCmd.Parameters.AddWithValue("$updated", updatedUtc);
        updateCmd.Parameters.AddWithValue("$id", productId.ToString("D"));
        updateCmd.Parameters.AddWithValue("$org", organizationId.ToString("D"));
        await updateCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static DateTimeOffset ParseUtc(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static string DecimalText(decimal value) =>
        value.ToString(CultureInfo.InvariantCulture);

    private static decimal ParseDecimal(string value) =>
        decimal.Parse(value, CultureInfo.InvariantCulture);

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
