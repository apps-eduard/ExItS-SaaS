using System.Globalization;
using System.Text;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using Microsoft.Data.Sqlite;

namespace ExItS.PinoyBusinessPOS.LocalStore;

public sealed class LocalConnectedSupplierStore(
    ILocalContextManager contextManager,
    ILocalDatabasePathResolver pathResolver)
    : ILinkedSupplierProductStore, IConnectedPurchaseOrderDraftStore, ILocalConnectedSupplierStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task UpsertConnectedSuppliersAsync(
        IReadOnlyList<LocalConnectedSupplier> suppliers,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(suppliers);
        if (suppliers.Count == 0) return;
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
            foreach (var supplier in suppliers)
            {
                await using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText =
                    """
                    INSERT INTO local_connected_supplier (
                        relationship_id, supplier_organization_id, buyer_supplier_id, display_name, status, last_synced_utc)
                    VALUES ($relationship, $supplierOrg, $buyerSupplier, $name, $status, $synced)
                    ON CONFLICT(relationship_id) DO UPDATE SET
                        supplier_organization_id = excluded.supplier_organization_id,
                        buyer_supplier_id = excluded.buyer_supplier_id,
                        display_name = excluded.display_name,
                        status = excluded.status,
                        last_synced_utc = excluded.last_synced_utc;
                    """;
                cmd.Parameters.AddWithValue("$relationship", supplier.RelationshipId.ToString("D"));
                cmd.Parameters.AddWithValue("$supplierOrg", supplier.SupplierOrganizationId.ToString("D"));
                cmd.Parameters.AddWithValue("$buyerSupplier",
                    supplier.BuyerSupplierId is Guid id ? id.ToString("D") : DBNull.Value);
                cmd.Parameters.AddWithValue("$name", supplier.DisplayName);
                cmd.Parameters.AddWithValue("$status", supplier.Status);
                cmd.Parameters.AddWithValue("$synced",
                    supplier.LastSyncedUtc is DateTimeOffset synced ? FormatUtc(synced) : DBNull.Value);
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<LocalConnectedSupplier>> ListConnectedSuppliersAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT relationship_id, supplier_organization_id, buyer_supplier_id, display_name, status, last_synced_utc
                FROM local_connected_supplier
                WHERE status = 'Active' AND buyer_supplier_id IS NOT NULL
                ORDER BY display_name COLLATE NOCASE;
                """;
            var items = new List<LocalConnectedSupplier>();
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                items.Add(new(
                    Guid.Parse(reader.GetString(0)),
                    Guid.Parse(reader.GetString(1)),
                    reader.IsDBNull(2) ? null : Guid.Parse(reader.GetString(2)),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.IsDBNull(5) ? null : ParseUtc(reader.GetString(5))));
            }
            return items;
        }
        finally { _gate.Release(); }
    }

    public async Task UpsertRangeAsync(IReadOnlyList<LocalLinkedSupplierProduct> products, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(products);
        if (products.Count == 0) return;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
            foreach (var product in products)
            {
                await using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText =
                    """
                    INSERT INTO local_linked_supplier_product (
                        link_id, relationship_id, supplier_organization_id, buyer_product_id, supplier_product_id,
                        supplier_sku, product_name, unit_of_measure, last_known_order_price, is_orderable,
                        is_active, sync_version, supplier_updated_at_utc, synced_at_utc)
                    VALUES (
                        $link, $relationship, $supplier, $buyerProduct, $supplierProduct,
                        $sku, $name, $uom, $price, $orderable, $active, $version, $supplierUpdated, $synced)
                    ON CONFLICT(link_id) DO UPDATE SET
                        relationship_id = excluded.relationship_id,
                        supplier_organization_id = excluded.supplier_organization_id,
                        buyer_product_id = excluded.buyer_product_id,
                        supplier_product_id = excluded.supplier_product_id,
                        supplier_sku = excluded.supplier_sku,
                        product_name = excluded.product_name,
                        unit_of_measure = excluded.unit_of_measure,
                        last_known_order_price = excluded.last_known_order_price,
                        is_orderable = excluded.is_orderable,
                        is_active = excluded.is_active,
                        sync_version = excluded.sync_version,
                        supplier_updated_at_utc = excluded.supplier_updated_at_utc,
                        synced_at_utc = excluded.synced_at_utc;
                    """;
                AddProductParameters(cmd, product);
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task RemoveIdsAsync(Guid relationshipId, IReadOnlyList<Guid> linkIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(linkIds);
        if (linkIds.Count == 0) return;
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(ct).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
            foreach (var id in linkIds)
            {
                await using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM local_linked_supplier_product WHERE link_id = $id AND relationship_id = $relationship;";
                cmd.Parameters.AddWithValue("$id", id.ToString("D"));
                cmd.Parameters.AddWithValue("$relationship", relationshipId.ToString("D"));
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public Task<IReadOnlyList<LocalLinkedSupplierProduct>> ListByRelationshipAsync(
        Guid relationshipId,
        CancellationToken ct = default) =>
        SearchLocalAsync(relationshipId, null, 500, ct);

    public async Task<IReadOnlyList<LocalLinkedSupplierProduct>> SearchLocalAsync(
        Guid relationshipId,
        string? query,
        int take,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            var sql = new StringBuilder(
                """
                SELECT link_id, relationship_id, supplier_organization_id, buyer_product_id, supplier_product_id,
                       supplier_sku, product_name, unit_of_measure, last_known_order_price, is_orderable,
                       is_active, sync_version, supplier_updated_at_utc, synced_at_utc
                FROM local_linked_supplier_product
                WHERE relationship_id = $relationship AND is_active = 1 AND is_orderable = 1
                """);
            cmd.Parameters.AddWithValue("$relationship", relationshipId.ToString("D"));
            if (!string.IsNullOrWhiteSpace(query))
            {
                sql.Append(" AND (product_name LIKE $query ESCAPE '\\' OR IFNULL(supplier_sku, '') LIKE $query ESCAPE '\\')");
                cmd.Parameters.AddWithValue("$query", $"%{EscapeLike(query.Trim())}%");
            }
            sql.Append(" ORDER BY product_name COLLATE NOCASE LIMIT $take;");
            cmd.Parameters.AddWithValue("$take", take);
            cmd.CommandText = sql.ToString();

            var items = new List<LocalLinkedSupplierProduct>();
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false)) items.Add(ReadProduct(reader));
            return items;
        }
        finally { _gate.Release(); }
    }

    public async Task<long> GetSyncVersionAsync(Guid relationshipId, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT last_sync_version FROM local_connected_supplier_sync_state WHERE relationship_id = $id LIMIT 1;";
            cmd.Parameters.AddWithValue("$id", relationshipId.ToString("D"));
            return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) ?? 0L, CultureInfo.InvariantCulture);
        }
        finally { _gate.Release(); }
    }

    public async Task SetSyncVersionAsync(Guid relationshipId, long syncVersion, DateTimeOffset syncedAtUtc, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT INTO local_connected_supplier_sync_state (relationship_id, last_sync_version, last_synced_utc)
                VALUES ($id, $version, $synced)
                ON CONFLICT(relationship_id) DO UPDATE SET
                    last_sync_version = excluded.last_sync_version,
                    last_synced_utc = excluded.last_synced_utc;
                """;
            cmd.Parameters.AddWithValue("$id", relationshipId.ToString("D"));
            cmd.Parameters.AddWithValue("$version", Math.Max(0, syncVersion));
            cmd.Parameters.AddWithValue("$synced", FormatUtc(syncedAtUtc));
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task SaveAsync(LocalConnectedPurchaseOrderDraft draft, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT INTO local_connected_po_draft (local_id, relationship_id, supplier_id, payload_json, sync_state, updated_at_utc)
                VALUES ($id, $relationship, $supplier, $payload, $state, $updated)
                ON CONFLICT(local_id) DO UPDATE SET
                    relationship_id = excluded.relationship_id,
                    supplier_id = excluded.supplier_id,
                    payload_json = excluded.payload_json,
                    sync_state = excluded.sync_state,
                    updated_at_utc = excluded.updated_at_utc;
                """;
            cmd.Parameters.AddWithValue("$id", draft.LocalId.ToString("D"));
            cmd.Parameters.AddWithValue("$relationship", draft.RelationshipId.ToString("D"));
            cmd.Parameters.AddWithValue("$supplier", draft.SupplierId.ToString("D"));
            cmd.Parameters.AddWithValue("$payload", draft.PayloadJson);
            cmd.Parameters.AddWithValue("$state", draft.SyncState.ToString());
            cmd.Parameters.AddWithValue("$updated", FormatUtc(draft.UpdatedAtUtc));
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task<LocalConnectedPurchaseOrderDraft?> GetAsync(Guid localId, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT local_id, relationship_id, supplier_id, payload_json, sync_state, updated_at_utc
                FROM local_connected_po_draft WHERE local_id = $id LIMIT 1;
                """;
            cmd.Parameters.AddWithValue("$id", localId.ToString("D"));
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;
            return new(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                Guid.Parse(reader.GetString(2)),
                reader.GetString(3),
                Enum.Parse<LocalEntitySyncState>(reader.GetString(4)),
                ParseUtc(reader.GetString(5)));
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteAsync(Guid localId, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM local_connected_po_draft WHERE local_id = $id;";
            cmd.Parameters.AddWithValue("$id", localId.ToString("D"));
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var active = contextManager.ActiveContext ?? throw new InvalidOperationException("Local context is not open.");
        var path = pathResolver.ResolveDatabasePath(
            active.Identity.UserId,
            active.Identity.OrganizationId,
            active.Identity.ProductCode);
        var connection = new SqliteConnection($"Data Source={path}");
        await connection.OpenAsync(ct).ConfigureAwait(false);
        return connection;
    }

    private static void AddProductParameters(SqliteCommand cmd, LocalLinkedSupplierProduct product)
    {
        cmd.Parameters.AddWithValue("$link", product.LinkId.ToString("D"));
        cmd.Parameters.AddWithValue("$relationship", product.RelationshipId.ToString("D"));
        cmd.Parameters.AddWithValue("$supplier", product.SupplierOrganizationId.ToString("D"));
        cmd.Parameters.AddWithValue("$buyerProduct", product.BuyerProductId.ToString("D"));
        cmd.Parameters.AddWithValue("$supplierProduct", product.SupplierProductId.ToString("D"));
        cmd.Parameters.AddWithValue("$sku", (object?)product.SupplierSku ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$name", product.ProductName);
        cmd.Parameters.AddWithValue("$uom", product.UnitOfMeasure);
        cmd.Parameters.AddWithValue("$price", product.LastKnownOrderPrice.ToString(CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$orderable", product.IsOrderable ? 1 : 0);
        cmd.Parameters.AddWithValue("$active", product.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("$version", product.SyncVersion);
        cmd.Parameters.AddWithValue("$supplierUpdated", FormatUtc(product.SupplierUpdatedAtUtc));
        cmd.Parameters.AddWithValue("$synced", FormatUtc(product.SyncedAtUtc));
    }

    private static LocalLinkedSupplierProduct ReadProduct(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(0)),
        Guid.Parse(reader.GetString(1)),
        Guid.Parse(reader.GetString(2)),
        Guid.Parse(reader.GetString(3)),
        Guid.Parse(reader.GetString(4)),
        reader.IsDBNull(5) ? null : reader.GetString(5),
        reader.GetString(6),
        reader.GetString(7),
        decimal.Parse(reader.GetString(8), CultureInfo.InvariantCulture),
        reader.GetInt32(9) == 1,
        reader.GetInt32(10) == 1,
        reader.GetInt64(11),
        ParseUtc(reader.GetString(12)),
        ParseUtc(reader.GetString(13)));

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    private static string FormatUtc(DateTimeOffset value) => value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
    private static DateTimeOffset ParseUtc(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
