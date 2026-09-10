using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Unified Direct Purchases history: Local receipts for the buyer org + B2B seller Sales
/// where BuyerPartyKind=Organization and BuyerOrganizationId=buyer org.
/// Global ORDER BY / OFFSET / LIMIT across both sources (no in-memory merge of full histories).
/// </summary>
internal sealed class DirectPurchaseHistoryQuery : IDirectPurchaseHistoryQuery
{
    private readonly PosDbContext _db;
    private readonly IOrganizationBranchDirectory? _branches;

    public DirectPurchaseHistoryQuery(PosDbContext db, IOrganizationBranchDirectory? branches = null)
    {
        _db = db;
        _branches = branches;
    }

    public async Task<(IReadOnlyList<DirectPurchaseHistoryRawRow> Items, int TotalCount)> ListAsync(
        Guid buyerOrganizationId,
        DirectPurchaseHistoryFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            return ([], 0);
        }

        skip = Math.Max(skip, 0);
        var includeLocal = filter.SourceType is null
            || filter.SourceType == DirectPurchaseHistorySourceTypes.Local;
        var includeB2b = filter.SourceType is null
            || filter.SourceType == DirectPurchaseHistorySourceTypes.B2B;

        if (!includeLocal && !includeB2b)
        {
            return ([], 0);
        }

        var search = string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim();
        var status = filter.Status;
        var fromDate = filter.FromDate;
        var toDate = filter.ToDate;
        var unionSql = BuildUnionSql(includeLocal, includeB2b);

        var countSql =
            $"""
            SELECT COUNT(*)::int AS "Value"
            FROM (
            {unionSql}
            ) history
            WHERE {SearchPredicateSql()}
            """;

        var listSql =
            $"""
            SELECT
                source_id AS "SourceId",
                source_type AS "SourceType",
                occurred_at_utc AS "OccurredAtUtc",
                purchase_date AS "PurchaseDate",
                seller_display_name AS "SellerDisplayName",
                seller_organization_id AS "SellerOrganizationId",
                seller_public_organization_id AS "SellerPublicOrganizationId",
                reference_number AS "ReferenceNumber",
                line_count AS "LineCount",
                total_amount AS "TotalAmount",
                status AS "Status",
                payment_method AS "PaymentMethod",
                seller_store_display_name AS "SellerStoreDisplayName"
            FROM (
            {unionSql}
            ) history
            WHERE {SearchPredicateSql()}
            ORDER BY occurred_at_utc DESC, source_id DESC
            OFFSET @skip
            LIMIT @take
            """;

        var filterParams = BuildFilterParams(buyerOrganizationId, fromDate, toDate, status, search);
        var total = await _db.Database
            .SqlQueryRaw<IntCountRow>(countSql, filterParams)
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);

        var listParams = BuildFilterParams(buyerOrganizationId, fromDate, toDate, status, search)
            .Concat(
            [
                new NpgsqlParameter("skip", skip),
                new NpgsqlParameter("take", take)
            ])
            .Cast<object>()
            .ToArray();

        var rows = await _db.Database
            .SqlQueryRaw<HistorySqlRow>(listSql, listParams)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (rows.Select(MapListRow).ToList(), total.Value);
    }

    public async Task<DirectPurchaseB2bDetailDto?> GetB2bDetailAsync(
        Guid buyerOrganizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var sale = await _db.Sales.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Id == saleId
                    && s.BuyerOrganizationId == buyerOrganizationId
                    && s.BuyerPartyKind == "Organization",
                cancellationToken)
            .ConfigureAwait(false);

        if (sale is null)
        {
            return null;
        }

        var lines = await _db.SaleLines.AsNoTracking()
            .Where(l => l.SaleId == sale.Id && l.OrganizationId == sale.OrganizationId)
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var relationship = await _db.ConnectedSupplierRelationships.AsNoTracking()
            .Where(r =>
                r.BuyerOrganizationId == buyerOrganizationId
                && r.SupplierOrganizationId == sale.OrganizationId)
            .OrderByDescending(r => r.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        string? storeName = relationship?.SupplierBranchNameSnapshot;
        if (string.IsNullOrWhiteSpace(storeName)
            && sale.BranchId is Guid branchId
            && _branches is not null)
        {
            var names = await _branches
                .GetNamesAsync(sale.OrganizationId, [branchId], cancellationToken)
                .ConfigureAwait(false);
            names.TryGetValue(branchId, out storeName);
        }

        var sellerName = FirstNonEmpty(
            relationship?.SupplierDisplayNameSnapshot,
            relationship?.SupplierPublicOrganizationIdSnapshot,
            "Seller");

        return new DirectPurchaseB2bDetailDto(
            sale.Id,
            sale.OrganizationId,
            relationship?.SupplierPublicOrganizationIdSnapshot,
            sellerName,
            string.IsNullOrWhiteSpace(storeName) ? null : storeName.Trim(),
            sale.SaleNumber,
            sale.RecordedAtUtc,
            sale.Status,
            sale.PaymentMethod,
            sale.Subtotal,
            sale.DiscountTotal,
            sale.TaxAmount,
            sale.Total,
            lines.Select(l => new DirectPurchaseB2bLineDto(
                l.LineNumber,
                l.NameSnapshot,
                l.SkuSnapshot,
                l.BarcodeSnapshot,
                l.Quantity,
                l.UnitOfMeasureSnapshot,
                l.UnitPrice,
                l.LineDiscountAmount,
                l.LineTotal)).ToList());
    }

    private static string BuildUnionSql(bool includeLocal, bool includeB2b)
    {
        var parts = new List<string>();
        if (includeLocal)
        {
            parts.Add(
                """
                SELECT
                    r.id AS source_id,
                    'Local'::text AS source_type,
                    (r.purchase_date::timestamp AT TIME ZONE 'UTC') AS occurred_at_utc,
                    r.purchase_date AS purchase_date,
                    COALESCE(NULLIF(BTRIM(r.source_name_snapshot), ''), 'External seller') AS seller_display_name,
                    NULL::uuid AS seller_organization_id,
                    NULL::text AS seller_public_organization_id,
                    COALESCE(NULLIF(BTRIM(r.reference_number), ''), r.receipt_number) AS reference_number,
                    (
                        SELECT COUNT(*)::int
                        FROM pos.direct_purchase_receipt_lines l
                        WHERE l.receipt_id = r.id AND l.organization_id = r.organization_id
                    ) AS line_count,
                    r.total_cost AS total_amount,
                    CASE WHEN r.status = 'Voided' THEN 'Voided' ELSE 'Completed' END AS status,
                    NULL::text AS payment_method,
                    NULL::text AS seller_store_display_name,
                    r.receipt_number AS search_receipt_number,
                    r.reference_number AS search_reference,
                    r.source_name_snapshot AS search_seller_name,
                    NULL::text AS search_seller_public_id,
                    NULL::text AS search_sale_number
                FROM pos.direct_purchase_receipts r
                WHERE r.organization_id = @buyer_org
                  AND (@from_date::date IS NULL OR r.purchase_date >= @from_date::date)
                  AND (@to_date::date IS NULL OR r.purchase_date <= @to_date::date)
                  AND (
                        @status::text IS NULL
                        OR (@status::text = 'Completed' AND r.status <> 'Voided')
                        OR (@status::text = 'Voided' AND r.status = 'Voided')
                        OR (@status::text = 'AwaitingPayment' AND FALSE)
                      )
                """);
        }

        if (includeB2b)
        {
            parts.Add(
                """
                SELECT
                    s.id AS source_id,
                    'B2B'::text AS source_type,
                    s.recorded_at_utc AS occurred_at_utc,
                    (s.recorded_at_utc AT TIME ZONE 'UTC')::date AS purchase_date,
                    COALESCE(
                        NULLIF(BTRIM(rel.supplier_display_name_snapshot), ''),
                        NULLIF(BTRIM(rel.supplier_public_organization_id_snapshot), ''),
                        'Seller') AS seller_display_name,
                    s.organization_id AS seller_organization_id,
                    rel.supplier_public_organization_id_snapshot AS seller_public_organization_id,
                    s.sale_number AS reference_number,
                    (
                        SELECT COUNT(*)::int
                        FROM pos.sale_lines sl
                        WHERE sl.sale_id = s.id AND sl.organization_id = s.organization_id
                    ) AS line_count,
                    s.total AS total_amount,
                    s.status AS status,
                    s.payment_method AS payment_method,
                    rel.supplier_branch_name_snapshot AS seller_store_display_name,
                    NULL::text AS search_receipt_number,
                    NULL::text AS search_reference,
                    rel.supplier_display_name_snapshot AS search_seller_name,
                    rel.supplier_public_organization_id_snapshot AS search_seller_public_id,
                    s.sale_number AS search_sale_number
                FROM pos.sales s
                LEFT JOIN pos.connected_supplier_relationships rel
                    ON rel.buyer_organization_id = @buyer_org
                   AND rel.supplier_organization_id = s.organization_id
                WHERE s.buyer_organization_id = @buyer_org
                  AND s.buyer_party_kind = 'Organization'
                  AND (@from_date::date IS NULL OR (s.recorded_at_utc AT TIME ZONE 'UTC')::date >= @from_date::date)
                  AND (@to_date::date IS NULL OR (s.recorded_at_utc AT TIME ZONE 'UTC')::date <= @to_date::date)
                  AND (
                        @status::text IS NULL
                        OR s.status = @status::text
                      )
                """);
        }

        return string.Join("\nUNION ALL\n", parts);
    }

    private static string SearchPredicateSql() =>
        """
        (
            @search::text IS NULL
            OR strpos(lower(COALESCE(history.search_seller_name, '')), lower(@search::text)) > 0
            OR strpos(lower(COALESCE(history.search_receipt_number, '')), lower(@search::text)) > 0
            OR strpos(lower(COALESCE(history.search_reference, '')), lower(@search::text)) > 0
            OR strpos(lower(COALESCE(history.search_sale_number, '')), lower(@search::text)) > 0
            OR strpos(lower(COALESCE(history.search_seller_public_id, '')), lower(@search::text)) > 0
            OR strpos(lower(COALESCE(history.seller_display_name, '')), lower(@search::text)) > 0
            OR strpos(lower(COALESCE(history.reference_number, '')), lower(@search::text)) > 0
        )
        """;

    private static object[] BuildFilterParams(
        Guid buyerOrganizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? status,
        string? search) =>
        [
            new NpgsqlParameter("buyer_org", buyerOrganizationId),
            new NpgsqlParameter("from_date", (object?)fromDate ?? DBNull.Value)
            {
                NpgsqlDbType = NpgsqlDbType.Date
            },
            new NpgsqlParameter("to_date", (object?)toDate ?? DBNull.Value)
            {
                NpgsqlDbType = NpgsqlDbType.Date
            },
            new NpgsqlParameter("status", (object?)status ?? DBNull.Value)
            {
                NpgsqlDbType = NpgsqlDbType.Text
            },
            new NpgsqlParameter("search", (object?)search ?? DBNull.Value)
            {
                NpgsqlDbType = NpgsqlDbType.Text
            }
        ];

    private static DirectPurchaseHistoryRawRow MapListRow(HistorySqlRow row) =>
        new(
            row.SourceId,
            row.SourceType,
            row.OccurredAtUtc,
            row.PurchaseDate,
            row.SellerDisplayName,
            row.SellerOrganizationId,
            row.SellerPublicOrganizationId,
            row.ReferenceNumber,
            row.LineCount,
            row.TotalAmount,
            row.Status,
            row.PaymentMethod,
            row.SellerStoreDisplayName);

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return "Seller";
    }

    private sealed class IntCountRow
    {
        public int Value { get; set; }
    }

    private sealed class HistorySqlRow
    {
        public Guid SourceId { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public DateTimeOffset OccurredAtUtc { get; set; }
        public DateOnly? PurchaseDate { get; set; }
        public string SellerDisplayName { get; set; } = string.Empty;
        public Guid? SellerOrganizationId { get; set; }
        public string? SellerPublicOrganizationId { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public int LineCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? SellerStoreDisplayName { get; set; }
    }
}
