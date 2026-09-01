using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;
using Microsoft.EntityFrameworkCore;

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

        CancellationToken cancellationToken = default,

        IReadOnlySet<Guid>? historyBranchIds = null,

        bool hideOrgWideAdjustments = false)

    {

        var withBalance = await LoadAllAsync(

                organizationId,

                customerId,

                historyBranchIds,

                hideOrgWideAdjustments,

                cancellationToken)

            .ConfigureAwait(false);

        var page = withBalance.Skip(skip).Take(take).ToList();

        return (page, withBalance.Count);

    }



    public Task<IReadOnlyList<LedgerEntryDto>> ListAllChronologicalAsync(

        PosOrganizationId organizationId,

        POSCustomerId customerId,

        CancellationToken cancellationToken = default,

        IReadOnlySet<Guid>? historyBranchIds = null,

        bool hideOrgWideAdjustments = false) =>

        LoadAllAsync(

            organizationId,

            customerId,

            historyBranchIds,

            hideOrgWideAdjustments,

            cancellationToken);



    private async Task<IReadOnlyList<LedgerEntryDto>> LoadAllAsync(

        PosOrganizationId organizationId,

        POSCustomerId customerId,

        IReadOnlySet<Guid>? historyBranchIds,

        bool hideOrgWideAdjustments,

        CancellationToken cancellationToken)

    {

        if (historyBranchIds is not null && historyBranchIds.Count == 0)

        {

            return [];

        }



        var orgId = organizationId.Value;

        var custId = customerId.Value;

        var entries = new List<LedgerEntryDto>();



        var creditQuery = _db.CreditEntries.AsNoTracking()

            .Where(c => c.OrganizationId == orgId && c.CustomerId == custId);

        creditQuery = ApplyCreditBranchFilter(creditQuery, orgId, historyBranchIds);



        var credits = await creditQuery

            .Select(c => new

            {

                c.Id,

                c.OrganizationId,

                c.CustomerId,

                c.Amount,

                c.Remarks,

                c.Status,

                c.CreatedAtUtc,

                c.ReversedAtUtc,

                c.ReversalReason,

            })

            .ToListAsync(cancellationToken)

            .ConfigureAwait(false);



        foreach (var c in credits)

        {

            var signed = c.Status == CreditEntryStatus.Active.ToString() ? c.Amount : 0m;

            entries.Add(new LedgerEntryDto(

                c.Id,

                "Credit",

                c.OrganizationId,

                c.CustomerId,

                c.Amount,

                signed,

                c.Remarks,

                c.Status,

                c.CreatedAtUtc,

                null,

                c.ReversedAtUtc,

                c.ReversalReason,

                null,

                null));

        }



        if (!hideOrgWideAdjustments)

        {

            var repayments = await _db.Repayments.AsNoTracking()

                .Where(r => r.OrganizationId == orgId && r.CustomerId == custId)

                .Select(r => new

                {

                    r.Id,

                    r.OrganizationId,

                    r.CustomerId,

                    r.Amount,

                    r.Remarks,

                    r.Status,

                    r.RecordedAtUtc,

                    r.RecordedBy,

                    r.ReversedAtUtc,

                    r.ReversalReason,

                    r.ReversedBy,

                })

                .ToListAsync(cancellationToken)

                .ConfigureAwait(false);



            foreach (var r in repayments)

            {

                var signed = r.Status == RepaymentStatus.Active.ToString() ? -r.Amount : 0m;

                entries.Add(new LedgerEntryDto(

                    r.Id,

                    "Repayment",

                    r.OrganizationId,

                    r.CustomerId,

                    r.Amount,

                    signed,

                    r.Remarks,

                    r.Status,

                    r.RecordedAtUtc,

                    r.RecordedBy,

                    r.ReversedAtUtc,

                    r.ReversalReason,

                    r.ReversedBy,

                    null));

            }



            var writeOffs = await _db.WriteOffs.AsNoTracking()

                .Where(w => w.OrganizationId == orgId && w.CustomerId == custId)

                .Select(w => new

                {

                    w.Id,

                    w.OrganizationId,

                    w.CustomerId,

                    w.Amount,

                    w.Reason,

                    w.Status,

                    w.RecordedAtUtc,

                    w.RecordedBy,

                    w.ReversedAtUtc,

                    w.ReversalReason,

                    w.ReversedBy,

                })

                .ToListAsync(cancellationToken)

                .ConfigureAwait(false);



            foreach (var w in writeOffs)

            {

                var signed = w.Status == WriteOffStatus.Active.ToString() ? -w.Amount : 0m;

                entries.Add(new LedgerEntryDto(

                    w.Id,

                    "WriteOff",

                    w.OrganizationId,

                    w.CustomerId,

                    w.Amount,

                    signed,

                    w.Reason,

                    w.Status,

                    w.RecordedAtUtc,

                    w.RecordedBy,

                    w.ReversedAtUtc,

                    w.ReversalReason,

                    w.ReversedBy,

                    null));

            }

        }



        var ordered = entries

            .OrderBy(e => e.RecordedAtUtc)

            .ThenBy(e => e.EntryId)

            .ToList();



        decimal running = 0m;

        var withBalance = new List<LedgerEntryDto>(ordered.Count);

        foreach (var entry in ordered)

        {

            running += entry.SignedEffect;

            withBalance.Add(entry with { RunningBalance = running });

        }



        return withBalance;

    }



    private IQueryable<CreditEntryRecord> ApplyCreditBranchFilter(

        IQueryable<CreditEntryRecord> query,

        Guid organizationId,

        IReadOnlySet<Guid>? historyBranchIds)

    {

        if (historyBranchIds is null)

        {

            return query;

        }



        var branchIds = historyBranchIds.ToList();

        return query.Where(c =>

            c.SourceSaleId != null

            && _db.Sales.Any(s =>

                s.Id == c.SourceSaleId

                && s.OrganizationId == organizationId

                && s.BranchId != null

                && branchIds.Contains(s.BranchId.Value)));

    }

}
