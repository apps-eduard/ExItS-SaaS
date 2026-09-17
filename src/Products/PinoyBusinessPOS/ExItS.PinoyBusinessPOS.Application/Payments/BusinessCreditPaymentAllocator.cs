using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Payments;

/// <summary>
/// Hybrid B2B payment allocation: default FIFO by due date then created time; optional manual lines.
/// </summary>
public static class BusinessCreditPaymentAllocator
{
    public sealed record OpenReceivable(
        Guid CreditEntryId,
        decimal RemainingUnpaidAmount,
        DateOnly? CurrentDueDate,
        DateTimeOffset CreatedAtUtc,
        string Remarks);

    public sealed record AllocationLine(Guid CreditEntryId, decimal Amount);

    /// <summary>
    /// Canonical open-receivable order: oldest due date first (nulls last), then oldest created, then id.
    /// </summary>
    public static IOrderedEnumerable<T> OrderOpenReceivables<T>(
        IEnumerable<T> items,
        Func<T, DateOnly?> dueDate,
        Func<T, DateTimeOffset> createdAt,
        Func<T, Guid> id) =>
        items
            .OrderBy(x => dueDate(x) is null)
            .ThenBy(x => dueDate(x) ?? DateOnly.MaxValue)
            .ThenBy(createdAt)
            .ThenBy(id);

    public static IReadOnlyList<OpenReceivable> FromAged(IEnumerable<AgedCreditDto> aged) =>
        OrderOpenReceivables(
                aged.Where(a =>
                    a.Status == nameof(Domain.Credit.CreditEntryStatus.Active)
                    && a.RemainingUnpaidAmount > 0m),
                a => a.CurrentDueDate,
                a => a.CreatedAtUtc,
                a => a.CreditEntryId)
            .Select(a => new OpenReceivable(
                a.CreditEntryId,
                a.RemainingUnpaidAmount,
                a.CurrentDueDate,
                a.CreatedAtUtc,
                a.Remarks))
            .ToList();

    /// <summary>
    /// Auto-allocate payment across open receivables until exhausted.
    /// </summary>
    public static IReadOnlyList<AllocationLine> AllocateAutomatically(
        IReadOnlyList<OpenReceivable> open,
        decimal paymentAmount)
    {
        var remaining = SaleMoney.RoundMoney(paymentAmount);
        if (remaining <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRepaymentAmount,
                "Payment amount must be greater than zero.");
        }

        var ordered = OrderOpenReceivables(
                open.Where(o => o.RemainingUnpaidAmount > 0m),
                o => o.CurrentDueDate,
                o => o.CreatedAtUtc,
                o => o.CreditEntryId)
            .ToList();

        var lines = new List<AllocationLine>();
        foreach (var receivable in ordered)
        {
            if (remaining <= 0m)
            {
                break;
            }

            var apply = remaining > receivable.RemainingUnpaidAmount
                ? receivable.RemainingUnpaidAmount
                : remaining;
            apply = SaleMoney.RoundMoney(apply);
            if (apply <= 0m)
            {
                continue;
            }

            lines.Add(new AllocationLine(receivable.CreditEntryId, apply));
            remaining = SaleMoney.RoundMoney(remaining - apply);
        }

        if (remaining > 0m)
        {
            throw new DomainException(
                DomainErrorCodes.RepaymentExceedsOutstanding,
                "Payment amount exceeds open receivable balances.");
        }

        return lines;
    }

    /// <summary>
    /// Validate manual allocations: sum equals payment, each &gt; 0, each ≤ remaining, no duplicates.
    /// </summary>
    public static IReadOnlyList<AllocationLine> ValidateManual(
        IReadOnlyList<OpenReceivable> open,
        IReadOnlyList<AllocationLine> requested,
        decimal paymentAmount)
    {
        var payment = SaleMoney.RoundMoney(paymentAmount);
        if (payment <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRepaymentAmount,
                "Payment amount must be greater than zero.");
        }

        if (requested is null || requested.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.RepaymentAllocationSumMismatch,
                "Manual allocation requires at least one receivable line.");
        }

        var openById = open.ToDictionary(o => o.CreditEntryId);
        var seen = new HashSet<Guid>();
        var lines = new List<AllocationLine>(requested.Count);
        decimal sum = 0m;

        foreach (var line in requested)
        {
            var amount = SaleMoney.RoundMoney(line.Amount);
            if (amount <= 0m)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidRepaymentAllocationAmount,
                    "Allocation amount must be greater than zero.");
            }

            if (!seen.Add(line.CreditEntryId))
            {
                throw new DomainException(
                    DomainErrorCodes.RepaymentAllocationDuplicateReceivable,
                    "Each receivable may appear only once in allocations.");
            }

            if (!openById.TryGetValue(line.CreditEntryId, out var receivable))
            {
                throw new DomainException(
                    DomainErrorCodes.RepaymentAllocationUnknownReceivable,
                    "Allocation targets an unknown or fully paid receivable.");
            }

            if (amount - receivable.RemainingUnpaidAmount > 0.0000001m)
            {
                throw new DomainException(
                    DomainErrorCodes.RepaymentAllocationExceedsReceivable,
                    "Allocation exceeds the receivable outstanding balance.");
            }

            lines.Add(new AllocationLine(line.CreditEntryId, amount));
            sum = SaleMoney.RoundMoney(sum + amount);
        }

        if (Math.Abs(sum - payment) > 0.0000001m)
        {
            throw new DomainException(
                DomainErrorCodes.RepaymentAllocationSumMismatch,
                "Total allocations must equal the payment amount.");
        }

        var openTotal = SaleMoney.RoundMoney(open.Sum(o => o.RemainingUnpaidAmount));
        if (payment - openTotal > 0.0000001m)
        {
            throw new DomainException(
                DomainErrorCodes.RepaymentExceedsOutstanding,
                "Payment amount exceeds total open receivables.");
        }

        return lines;
    }
}
