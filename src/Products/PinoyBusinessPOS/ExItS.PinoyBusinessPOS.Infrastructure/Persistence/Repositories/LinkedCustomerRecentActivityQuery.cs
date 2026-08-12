using ExItS.PinoyBusinessPOS.Application.Statements;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Linked-customer activity: database ORDER BY + OFFSET/LIMIT before materialization.
/// Optional notBeforeUtc / beforeUtc enforce free-window and older-settled bounds server-side (WP06/WP10).
/// </summary>
internal sealed class LinkedCustomerRecentActivityQuery : ILinkedCustomerRecentActivityQuery
{
    private readonly PosDbContext _db;

    public LinkedCustomerRecentActivityQuery(PosDbContext db) => _db = db;

    public async Task<IReadOnlyList<LinkedCustomerActivityRawRow>> ListRecentDescendingAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        DateTimeOffset? notBeforeUtc = null,
        DateTimeOffset? beforeUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            return [];
        }

        skip = Math.Max(skip, 0);
        take = Math.Min(take, LinkedCustomerStatementLimits.MaxPageSize + 1);

        const string sql =
            """
            SELECT
                id AS "EntryId",
                entry_type AS "EntryType",
                amount AS "Amount",
                signed_effect AS "SignedEffect",
                status AS "Status",
                recorded_at_utc AS "RecordedAtUtc",
                source_sale_id AS "SourceSaleId"
            FROM (
                SELECT
                    c.id,
                    'Credit'::text AS entry_type,
                    c.amount,
                    CASE WHEN c.status = 'Active' THEN c.amount ELSE 0 END AS signed_effect,
                    c.status,
                    c.created_at_utc AS recorded_at_utc,
                    c.source_sale_id
                FROM pos.credit_entries c
                WHERE c.organization_id = @org AND c.customer_id = @customer
                  AND (@not_before IS NULL OR c.created_at_utc >= @not_before)
                  AND (@before IS NULL OR c.created_at_utc < @before)
                UNION ALL
                SELECT
                    r.id,
                    'Repayment'::text AS entry_type,
                    r.amount,
                    CASE WHEN r.status = 'Active' THEN -r.amount ELSE 0 END AS signed_effect,
                    r.status,
                    r.recorded_at_utc,
                    NULL::uuid AS source_sale_id
                FROM pos.repayments r
                WHERE r.organization_id = @org AND r.customer_id = @customer
                  AND (@not_before IS NULL OR r.recorded_at_utc >= @not_before)
                  AND (@before IS NULL OR r.recorded_at_utc < @before)
                UNION ALL
                SELECT
                    s.id,
                    'Sale'::text AS entry_type,
                    s.total AS amount,
                    0::numeric AS signed_effect,
                    s.status,
                    s.recorded_at_utc,
                    s.id AS source_sale_id
                FROM pos.sales s
                WHERE s.organization_id = @org
                  AND s.customer_id = @customer
                  AND s.payment_method <> 'Utang'
                  AND s.linked_credit_entry_id IS NULL
                  AND s.status IN ('Completed', 'Voided')
                  AND (@not_before IS NULL OR s.recorded_at_utc >= @not_before)
                  AND (@before IS NULL OR s.recorded_at_utc < @before)
            ) ledger
            ORDER BY recorded_at_utc DESC, id DESC
            OFFSET @skip
            LIMIT @take
            """;

        var rows = await _db.Database
            .SqlQueryRaw<ActivitySqlRow>(
                sql,
                new NpgsqlParameter("org", organizationId.Value),
                new NpgsqlParameter("customer", customerId.Value),
                new NpgsqlParameter("skip", skip),
                new NpgsqlParameter("take", take),
                new NpgsqlParameter("not_before", (object?)notBeforeUtc ?? DBNull.Value)
                {
                    NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.TimestampTz
                },
                new NpgsqlParameter("before", (object?)beforeUtc ?? DBNull.Value)
                {
                    NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.TimestampTz
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Map(rows);
    }

    public async Task<IReadOnlyList<LinkedCustomerActivityRawRow>> ListActiveDescendingAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            return [];
        }

        skip = Math.Max(skip, 0);
        take = Math.Min(take, LinkedCustomerStatementLimits.MaxPageSize + 1);

        const string sql =
            """
            SELECT
                id AS "EntryId",
                entry_type AS "EntryType",
                amount AS "Amount",
                signed_effect AS "SignedEffect",
                status AS "Status",
                recorded_at_utc AS "RecordedAtUtc",
                source_sale_id AS "SourceSaleId"
            FROM (
                SELECT
                    c.id,
                    'Credit'::text AS entry_type,
                    c.amount,
                    c.amount AS signed_effect,
                    c.status,
                    c.created_at_utc AS recorded_at_utc,
                    c.source_sale_id
                FROM pos.credit_entries c
                WHERE c.organization_id = @org AND c.customer_id = @customer AND c.status = 'Active'
                UNION ALL
                SELECT
                    r.id,
                    'Repayment'::text AS entry_type,
                    r.amount,
                    -r.amount AS signed_effect,
                    r.status,
                    r.recorded_at_utc,
                    NULL::uuid AS source_sale_id
                FROM pos.repayments r
                WHERE r.organization_id = @org AND r.customer_id = @customer AND r.status = 'Active'
            ) ledger
            ORDER BY recorded_at_utc DESC, id DESC
            OFFSET @skip
            LIMIT @take
            """;

        var rows = await _db.Database
            .SqlQueryRaw<ActivitySqlRow>(
                sql,
                new NpgsqlParameter("org", organizationId.Value),
                new NpgsqlParameter("customer", customerId.Value),
                new NpgsqlParameter("skip", skip),
                new NpgsqlParameter("take", take))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Map(rows);
    }

    private static IReadOnlyList<LinkedCustomerActivityRawRow> Map(List<ActivitySqlRow> rows) =>
        rows
            .Select(r => new LinkedCustomerActivityRawRow(
                r.EntryId,
                r.EntryType,
                r.Amount,
                r.SignedEffect,
                r.Status,
                r.RecordedAtUtc,
                r.SourceSaleId))
            .ToList();

    private sealed class ActivitySqlRow
    {
        public Guid EntryId { get; set; }
        public string EntryType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal SignedEffect { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset RecordedAtUtc { get; set; }
        public Guid? SourceSaleId { get; set; }
    }
}
