using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Unified chronological ledger read model (credits ∪ repayments). Does not persist a separate ledger table.
/// Ordering: RecordedAtUtc ASC, EntryId ASC. Running balance is computed chronologically.
/// </summary>
internal sealed class UtangLedgerQuery : IUtangLedgerQuery
{
    private readonly PosDbContext _db;

    public UtangLedgerQuery(PosDbContext db) => _db = db;

    public async Task<(IReadOnlyList<LedgerEntryDto> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var withBalance = await LoadAllAsync(organizationId, customerId, cancellationToken).ConfigureAwait(false);
        var page = withBalance.Skip(skip).Take(take).ToList();
        return (page, withBalance.Count);
    }

    public Task<IReadOnlyList<LedgerEntryDto>> ListAllChronologicalAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default) =>
        LoadAllAsync(organizationId, customerId, cancellationToken);

    private async Task<IReadOnlyList<LedgerEntryDto>> LoadAllAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken)
    {
        // Load full chronological set for this customer to compute running balances correctly.
        // MVP-scale history; not a materialized ledger table.
        const string sql =
            """
            SELECT
                id AS "EntryId",
                entry_type AS "EntryType",
                organization_id AS "OrganizationId",
                customer_id AS "CustomerId",
                amount AS "Amount",
                signed_effect AS "SignedEffect",
                remarks AS "Remarks",
                status AS "Status",
                recorded_at_utc AS "RecordedAtUtc",
                recorded_by AS "RecordedBy",
                reversed_at_utc AS "ReversedAtUtc",
                reversal_reason AS "ReversalReason",
                reversed_by AS "ReversedBy"
            FROM (
                SELECT
                    c.id,
                    'Credit'::text AS entry_type,
                    c.organization_id,
                    c.customer_id,
                    c.amount,
                    CASE WHEN c.status = 'Active' THEN c.amount ELSE 0 END AS signed_effect,
                    c.remarks,
                    c.status,
                    c.created_at_utc AS recorded_at_utc,
                    NULL::uuid AS recorded_by,
                    c.reversed_at_utc,
                    c.reversal_reason,
                    NULL::uuid AS reversed_by
                FROM pos.credit_entries c
                WHERE c.organization_id = @org AND c.customer_id = @customer
                UNION ALL
                SELECT
                    r.id,
                    'Repayment'::text AS entry_type,
                    r.organization_id,
                    r.customer_id,
                    r.amount,
                    CASE WHEN r.status = 'Active' THEN -r.amount ELSE 0 END AS signed_effect,
                    r.remarks,
                    r.status,
                    r.recorded_at_utc,
                    r.recorded_by,
                    r.reversed_at_utc,
                    r.reversal_reason,
                    r.reversed_by
                FROM pos.repayments r
                WHERE r.organization_id = @org AND r.customer_id = @customer
            ) ledger
            ORDER BY recorded_at_utc ASC, id ASC
            """;

        var rows = await _db.Database
            .SqlQueryRaw<LedgerSqlRow>(
                sql,
                new NpgsqlParameter("org", organizationId.Value),
                new NpgsqlParameter("customer", customerId.Value))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        decimal running = 0m;
        var withBalance = new List<LedgerEntryDto>(rows.Count);
        foreach (var row in rows)
        {
            running += row.SignedEffect;
            withBalance.Add(new LedgerEntryDto(
                row.EntryId,
                row.EntryType,
                row.OrganizationId,
                row.CustomerId,
                row.Amount,
                row.SignedEffect,
                row.Remarks,
                row.Status,
                row.RecordedAtUtc,
                row.RecordedBy,
                row.ReversedAtUtc,
                row.ReversalReason,
                row.ReversedBy,
                running));
        }

        return withBalance;
    }

    private sealed class LedgerSqlRow
    {
        public Guid EntryId { get; set; }
        public string EntryType { get; set; } = string.Empty;
        public Guid OrganizationId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }
        public decimal SignedEffect { get; set; }
        public string? Remarks { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset RecordedAtUtc { get; set; }
        public Guid? RecordedBy { get; set; }
        public DateTimeOffset? ReversedAtUtc { get; set; }
        public string? ReversalReason { get; set; }
        public Guid? ReversedBy { get; set; }
    }
}
