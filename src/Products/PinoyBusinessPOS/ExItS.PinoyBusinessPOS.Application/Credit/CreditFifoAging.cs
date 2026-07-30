using ExItS.PinoyBusinessPOS.Domain.Credit;

namespace ExItS.PinoyBusinessPOS.Application.Credit;

/// <summary>
/// Due-status labels for UI. Color is never the sole signal.
/// </summary>
public enum CreditDueStatus
{
    NoDueDate = 0,
    Upcoming = 1,
    DueSoon = 2,
    DueToday = 3,
    Overdue = 4,
    Paid = 5,
    Reversed = 6
}

public sealed record AgedCreditDto(
    Guid CreditEntryId,
    Guid OrganizationId,
    Guid CustomerId,
    decimal Amount,
    decimal RemainingUnpaidAmount,
    string Remarks,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateOnly? CurrentDueDate,
    string DueStatus,
    bool IsOverdue);

public sealed record CustomerOverdueSummaryDto(
    Guid CustomerId,
    Guid OrganizationId,
    decimal OutstandingAmount,
    decimal OverdueAmount,
    int OverdueCreditCount,
    DateOnly? EarliestOverdueDate,
    DateOnly? NextUpcomingDueDate,
    int CreditsWithoutDueDateCount,
    decimal ActiveCreditTotal,
    decimal ActiveRepaymentTotal,
    int ActiveCreditCount,
    int ActiveRepaymentCount,
    int TotalLedgerEntryCount);

public sealed record OverdueCustomerListItemDto(
    Guid CustomerId,
    Guid OrganizationId,
    string DisplayName,
    decimal OutstandingAmount,
    decimal OverdueAmount,
    int OverdueCreditCount,
    DateOnly? EarliestOverdueDate);

public sealed record CreditDueDateChangeDto(
    Guid ChangeId,
    Guid OrganizationId,
    Guid CreditEntryId,
    Guid CustomerId,
    DateOnly? PreviousDueDate,
    DateOnly? NewDueDate,
    string Reason,
    Guid ChangedBy,
    DateTimeOffset ChangedAtUtc);

/// <summary>
/// FIFO aging: active repayments reduce active credits ordered by CreatedAtUtc ASC, then Id ASC.
/// Read-model only — does not persist allocations or change ledger/outstanding formulas.
/// Effective business date = UTC calendar date (org timezone not yet defined).
/// </summary>
public static class CreditFifoAging
{
    public const int DueSoonDays = 7;

    public static DateOnly EffectiveBusinessDateUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            utcNow = utcNow.ToUniversalTime();
        }

        return DateOnly.FromDateTime(utcNow.UtcDateTime);
    }

    public static IReadOnlyList<AgedCreditDto> AgeCredits(
        IEnumerable<CreditEntry> credits,
        decimal activeRepaymentTotal,
        DateOnly effectiveDate)
    {
        // FIFO: active repayments offset active credits by CreatedAtUtc ASC, then Id ASC.
        // Reversed credits never consume the repayment pool and are never overdue.
        var pool = activeRepaymentTotal < 0m ? 0m : activeRepaymentTotal;
        var results = new List<AgedCreditDto>();
        foreach (var credit in credits.OrderBy(c => c.CreatedAtUtc).ThenBy(c => c.Id.Value))
        {
            decimal remaining;
            if (credit.Status == CreditEntryStatus.Reversed)
            {
                remaining = 0m;
            }
            else
            {
                var applied = Math.Min(pool, credit.Amount);
                remaining = credit.Amount - applied;
                pool -= applied;
            }

            var dueStatus = ResolveDueStatus(credit, remaining, effectiveDate);
            results.Add(new AgedCreditDto(
                credit.Id.Value,
                credit.OrganizationId.Value,
                credit.CustomerId.Value,
                credit.Amount,
                remaining,
                credit.Remarks,
                credit.Status.ToString(),
                credit.CreatedAtUtc,
                credit.CurrentDueDate,
                dueStatus.ToString(),
                dueStatus == CreditDueStatus.Overdue));
        }

        return results;
    }

    public static CreditDueStatus ResolveDueStatus(CreditEntry credit, decimal remainingUnpaid, DateOnly effectiveDate)
    {
        if (credit.Status == CreditEntryStatus.Reversed)
        {
            return CreditDueStatus.Reversed;
        }

        if (remainingUnpaid <= 0m)
        {
            return CreditDueStatus.Paid;
        }

        if (credit.CurrentDueDate is null)
        {
            return CreditDueStatus.NoDueDate;
        }

        if (credit.CurrentDueDate.Value < effectiveDate)
        {
            return CreditDueStatus.Overdue;
        }

        if (credit.CurrentDueDate.Value == effectiveDate)
        {
            return CreditDueStatus.DueToday;
        }

        if (credit.CurrentDueDate.Value <= effectiveDate.AddDays(DueSoonDays))
        {
            return CreditDueStatus.DueSoon;
        }

        return CreditDueStatus.Upcoming;
    }

    public static CustomerOverdueSummaryDto BuildCustomerSummary(
        Guid customerId,
        Guid organizationId,
        IReadOnlyList<AgedCreditDto> aged,
        decimal activeCreditTotal,
        decimal activeRepaymentTotal,
        int activeCreditCount,
        int activeRepaymentCount,
        int totalLedgerEntryCount)
    {
        var outstanding = activeCreditTotal - activeRepaymentTotal;
        var overdue = aged.Where(a => a.IsOverdue).ToList();
        var upcoming = aged
            .Where(a => a.Status == nameof(CreditEntryStatus.Active)
                        && a.RemainingUnpaidAmount > 0m
                        && a.CurrentDueDate is not null
                        && !a.IsOverdue)
            .Select(a => a.CurrentDueDate!.Value)
            .OrderBy(d => d)
            .Cast<DateOnly?>()
            .FirstOrDefault();

        return new CustomerOverdueSummaryDto(
            customerId,
            organizationId,
            outstanding,
            overdue.Sum(a => a.RemainingUnpaidAmount),
            overdue.Count,
            overdue.Select(a => a.CurrentDueDate).Where(d => d is not null).Cast<DateOnly>().OrderBy(d => d).Cast<DateOnly?>().FirstOrDefault(),
            upcoming,
            aged.Count(a => a.Status == nameof(CreditEntryStatus.Active)
                            && a.RemainingUnpaidAmount > 0m
                            && a.CurrentDueDate is null),
            activeCreditTotal,
            activeRepaymentTotal,
            activeCreditCount,
            activeRepaymentCount,
            totalLedgerEntryCount);
    }
}
