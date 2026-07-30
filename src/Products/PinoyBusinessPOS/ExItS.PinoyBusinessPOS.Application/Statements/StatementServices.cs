using System.Globalization;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.Application.Statements;

public sealed class CustomerStatementService : ICustomerStatementService
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IUtangLedgerQuery _ledger;
    private readonly ICreditEntryRepository _credits;
    private readonly IRepaymentRepository _repayments;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IClock _clock;

    public CustomerStatementService(
        IPOSCustomerRepository customers,
        IUtangLedgerQuery ledger,
        ICreditEntryRepository credits,
        IRepaymentRepository repayments,
        IOutstandingBalanceService outstanding,
        IPosCommercialAccessAccessor access,
        IClock clock)
    {
        _customers = customers;
        _ledger = ledger;
        _credits = credits;
        _repayments = repayments;
        _outstanding = outstanding;
        _access = access;
        _clock = clock;
    }

    public async Task<ApplicationResult<CustomerStatementDto>> GenerateAsync(
        PosOrganizationId organizationId,
        Guid customerId,
        DateOnly periodStart,
        DateOnly periodEnd,
        string? organizationDisplayName,
        string currencyCode,
        string cultureName,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ViewGenerateStatement);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<CustomerStatementDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        if (periodEnd < periodStart)
        {
            return ApplicationResult<CustomerStatementDto>.Failure(
                ApplicationErrorCodes.StatementInvalidPeriod,
                "Statement period end must be on or after period start.");
        }

        var customer = await _customers
            .GetByIdAsync(organizationId, POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<CustomerStatementDto>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        var ledger = await _ledger
            .ListAllChronologicalAsync(organizationId, customer.Id, cancellationToken)
            .ConfigureAwait(false);

        var periodStartUtc = new DateTimeOffset(periodStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var periodEndExclusiveUtc = new DateTimeOffset(periodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var opening = ledger
            .Where(e => e.RecordedAtUtc < periodStartUtc)
            .Sum(e => e.SignedEffect);

        var periodEntries = ledger
            .Where(e => e.RecordedAtUtc >= periodStartUtc && e.RecordedAtUtc < periodEndExclusiveUtc)
            .ToList();

        var credits = (await _credits
                .ListByCustomerAsync(organizationId, customer.Id, 0, int.MaxValue, cancellationToken)
                .ConfigureAwait(false)).Items;
        var creditById = credits.ToDictionary(c => c.Id.Value);

        var activeRepaymentTotal = await _repayments
            .SumActiveAmountAsync(organizationId, customer.Id, cancellationToken)
            .ConfigureAwait(false);
        var aged = CreditFifoAging.AgeCredits(
            credits,
            activeRepaymentTotal,
            CreditFifoAging.EffectiveBusinessDateUtc(_clock.UtcNow));
        var agedById = aged.ToDictionary(a => a.CreditEntryId);

        decimal running = opening;
        var lines = new List<CustomerStatementLineDto>(periodEntries.Count);
        decimal periodCredit = 0m;
        decimal periodRepayment = 0m;
        decimal periodRevCredit = 0m;
        decimal periodRevRepayment = 0m;

        foreach (var entry in periodEntries)
        {
            running += entry.SignedEffect;
            var isCredit = string.Equals(entry.EntryType, "Credit", StringComparison.OrdinalIgnoreCase);
            var isReversed = string.Equals(entry.Status, "Reversed", StringComparison.OrdinalIgnoreCase);

            if (isCredit)
            {
                if (isReversed)
                {
                    periodRevCredit += entry.Amount;
                }
                else
                {
                    periodCredit += entry.Amount;
                }
            }
            else
            {
                if (isReversed)
                {
                    periodRevRepayment += entry.Amount;
                }
                else
                {
                    periodRepayment += entry.Amount;
                }
            }

            DateOnly? dueDate = null;
            string? dueStatus = null;
            var isOverdue = false;
            if (isCredit && creditById.TryGetValue(entry.EntryId, out var credit))
            {
                dueDate = credit.CurrentDueDate;
                if (agedById.TryGetValue(entry.EntryId, out var agedCredit))
                {
                    dueStatus = agedCredit.DueStatus;
                    isOverdue = agedCredit.IsOverdue;
                }
            }

            lines.Add(new CustomerStatementLineDto(
                entry.EntryId,
                entry.EntryType,
                entry.RecordedAtUtc,
                entry.Amount,
                entry.SignedEffect,
                entry.Status,
                entry.Remarks,
                dueDate,
                dueStatus,
                isOverdue,
                isReversed,
                running));
        }

        var closing = opening + periodEntries.Sum(e => e.SignedEffect);
        var outstanding = await _outstanding
            .GetOutstandingAsync(organizationId, customer.Id, cancellationToken)
            .ConfigureAwait(false);

        // Closing for the selected period must reconcile with ledger running balance through period end.
        var ledgerClosing = ledger
            .Where(e => e.RecordedAtUtc < periodEndExclusiveUtc)
            .Select(e => e.RunningBalance)
            .LastOrDefault() ?? 0m;
        if (closing != ledgerClosing)
        {
            closing = ledgerClosing;
        }

        var overdueAmount = aged.Where(a => a.IsOverdue).Sum(a => a.RemainingUnpaidAmount);
        var overdueCount = aged.Count(a => a.IsOverdue);

        return ApplicationResult<CustomerStatementDto>.Success(new CustomerStatementDto(
            organizationId.Value,
            organizationDisplayName,
            customer.Id.Value,
            customer.DisplayName,
            periodStart,
            periodEnd,
            opening,
            closing,
            periodCredit,
            periodRepayment,
            periodRevCredit,
            periodRevRepayment,
            outstanding,
            overdueAmount,
            overdueCount,
            _clock.UtcNow,
            string.IsNullOrWhiteSpace(currencyCode) ? "PHP" : currencyCode,
            string.IsNullOrWhiteSpace(cultureName) ? CultureInfo.CurrentCulture.Name : cultureName,
            lines));
    }
}

public sealed class RepaymentReceiptService : IRepaymentReceiptService
{
    public const string Disclaimer =
        "This is a repayment receipt projection, not a tax invoice. No tax, BIR invoice number, or cashier settlement is implied.";

    private readonly IRepaymentRepository _repayments;
    private readonly IPOSCustomerRepository _customers;
    private readonly IUtangLedgerQuery _ledger;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IClock _clock;

    public RepaymentReceiptService(
        IRepaymentRepository repayments,
        IPOSCustomerRepository customers,
        IUtangLedgerQuery ledger,
        IPosCommercialAccessAccessor access,
        IClock clock)
    {
        _repayments = repayments;
        _customers = customers;
        _ledger = ledger;
        _access = access;
        _clock = clock;
    }

    /// <summary>Deterministic receipt identity derived from the immutable repayment id (no separate receipt table).</summary>
    public static string BuildReceiptReference(Guid repaymentId) =>
        $"RCPT-{repaymentId:N}".ToUpperInvariant();

    public async Task<ApplicationResult<RepaymentReceiptDto>> GetAsync(
        PosOrganizationId organizationId,
        Guid repaymentId,
        string? organizationDisplayName,
        string currencyCode,
        string cultureName,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ViewGenerateReceipt);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<RepaymentReceiptDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var repayment = await _repayments
            .GetByIdAsync(organizationId, RepaymentId.From(repaymentId), cancellationToken)
            .ConfigureAwait(false);
        if (repayment is null)
        {
            return ApplicationResult<RepaymentReceiptDto>.Failure(
                ApplicationErrorCodes.ReceiptNotFound,
                "Repayment receipt was not found.");
        }

        var customer = await _customers
            .GetByIdAsync(organizationId, repayment.CustomerId, cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<RepaymentReceiptDto>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        var ledger = await _ledger
            .ListAllChronologicalAsync(organizationId, repayment.CustomerId, cancellationToken)
            .ConfigureAwait(false);

        decimal? outstandingAfter = null;
        foreach (var entry in ledger)
        {
            if (entry.EntryId == repayment.Id.Value
                && string.Equals(entry.EntryType, "Repayment", StringComparison.OrdinalIgnoreCase))
            {
                outstandingAfter = entry.RunningBalance;
                break;
            }
        }

        var isReversed = repayment.Status == RepaymentStatus.Reversed;
        var statusLabel = isReversed ? "Reversed" : repayment.Status.ToString();

        return ApplicationResult<RepaymentReceiptDto>.Success(new RepaymentReceiptDto(
            BuildReceiptReference(repayment.Id.Value),
            repayment.Id.Value,
            organizationId.Value,
            organizationDisplayName,
            customer.Id.Value,
            customer.DisplayName,
            repayment.Amount,
            repayment.Remarks,
            repayment.RecordedAtUtc,
            repayment.RecordedBy,
            statusLabel,
            isReversed,
            repayment.ReversedAtUtc,
            repayment.ReversalReason,
            repayment.ReversedBy,
            outstandingAfter,
            _clock.UtcNow,
            string.IsNullOrWhiteSpace(currencyCode) ? "PHP" : currencyCode,
            string.IsNullOrWhiteSpace(cultureName) ? CultureInfo.CurrentCulture.Name : cultureName,
            Disclaimer));
    }
}
