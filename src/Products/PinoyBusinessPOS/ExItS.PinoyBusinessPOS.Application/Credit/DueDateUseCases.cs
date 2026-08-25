using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Credit;

public sealed class OverdueQueryService
{
    private readonly ICreditEntryRepository _credits;
    private readonly IRepaymentRepository _repayments;
    private readonly IWriteOffRepository _writeOffs;
    private readonly IPOSCustomerRepository _customers;
    private readonly IClock _clock;

    public OverdueQueryService(
        ICreditEntryRepository credits,
        IRepaymentRepository repayments,
        IWriteOffRepository writeOffs,
        IPOSCustomerRepository customers,
        IClock clock)
    {
        _credits = credits;
        _repayments = repayments;
        _writeOffs = writeOffs;
        _customers = customers;
        _clock = clock;
    }

    public async Task<CustomerOverdueSummaryDto?> GetCustomerSummaryAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var custId = POSCustomerId.From(customerId);
        var customer = await _customers.GetByIdAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return null;
        }

        var (credits, creditTotal) = await _credits.ListByCustomerAsync(orgId, custId, 0, 10_000, cancellationToken).ConfigureAwait(false);
        var activeRepayments = await _repayments.SumActiveAmountAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var activeWriteOffs = await _writeOffs.SumActiveAmountAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var activeCreditTotal = await _credits.SumActiveAmountAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var activeCreditCount = await _credits.CountActiveAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var activeRepaymentCount = await _repayments.CountActiveAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var activeWriteOffCount = await _writeOffs.CountActiveAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var (_, repaymentTotal) = await _repayments.ListByCustomerAsync(orgId, custId, 0, 1, cancellationToken).ConfigureAwait(false);
        var (_, writeOffTotal) = await _writeOffs.ListByCustomerAsync(orgId, custId, 0, 1, cancellationToken).ConfigureAwait(false);

        var aged = CreditFifoAging.AgeCredits(
            credits,
            activeRepayments + activeWriteOffs,
            CreditFifoAging.EffectiveBusinessDateUtc(_clock.UtcNow));

        return CreditFifoAging.BuildCustomerSummary(
            customerId,
            organizationId,
            aged,
            activeCreditTotal,
            activeRepayments,
            activeCreditCount,
            activeRepaymentCount,
            creditTotal + repaymentTotal + writeOffTotal,
            activeWriteOffs,
            activeWriteOffCount);
    }

    public async Task<PagedResult<AgedCreditDto>> ListCustomerCreditsAsync(
        Guid organizationId,
        Guid customerId,
        string? filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var custId = POSCustomerId.From(customerId);
        var (credits, _) = await _credits.ListByCustomerAsync(orgId, custId, 0, 10_000, cancellationToken).ConfigureAwait(false);
        var activeRepayments = await _repayments.SumActiveAmountAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var activeWriteOffs = await _writeOffs.SumActiveAmountAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var aged = CreditFifoAging.AgeCredits(
            credits,
            activeRepayments + activeWriteOffs,
            CreditFifoAging.EffectiveBusinessDateUtc(_clock.UtcNow));

        var filtered = ApplyFilter(aged, filter).ToList();
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        return new PagedResult<AgedCreditDto>(
            filtered.Skip(skip).Take(take).ToList(),
            filtered.Count,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<PagedResult<AgedCreditDto>> ListOrganizationOverdueCreditsAsync(
        Guid organizationId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var credits = await _credits.ListActiveByOrganizationAsync(orgId, cancellationToken).ConfigureAwait(false);
        var byCustomer = credits.GroupBy(c => c.CustomerId.Value);
        var effective = CreditFifoAging.EffectiveBusinessDateUtc(_clock.UtcNow);
        var overdue = new List<AgedCreditDto>();

        foreach (var group in byCustomer)
        {
            var custId = POSCustomerId.From(group.Key);
            var activeRepayments = await _repayments.SumActiveAmountAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
            var activeWriteOffs = await _writeOffs.SumActiveAmountAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
            var aged = CreditFifoAging.AgeCredits(group, activeRepayments + activeWriteOffs, effective);
            overdue.AddRange(aged.Where(a => a.IsOverdue));
        }

        overdue = overdue
            .OrderBy(a => a.CurrentDueDate)
            .ThenBy(a => a.CreatedAtUtc)
            .ThenBy(a => a.CreditEntryId)
            .ToList();

        var (skip, take) = PosPagination.Normalize(page, pageSize);
        return new PagedResult<AgedCreditDto>(
            overdue.Skip(skip).Take(take).ToList(),
            overdue.Count,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<PagedResult<OverdueCustomerListItemDto>> ListOrganizationOverdueCustomersAsync(
        Guid organizationId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var credits = await _credits.ListActiveByOrganizationAsync(orgId, cancellationToken).ConfigureAwait(false);
        var byCustomer = credits.GroupBy(c => c.CustomerId.Value);
        var effective = CreditFifoAging.EffectiveBusinessDateUtc(_clock.UtcNow);
        var items = new List<OverdueCustomerListItemDto>();

        foreach (var group in byCustomer)
        {
            var custId = POSCustomerId.From(group.Key);
            var customer = await _customers.GetByIdAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
            if (customer is null)
            {
                continue;
            }

            var activeRepayments = await _repayments.SumActiveAmountAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
            var activeWriteOffs = await _writeOffs.SumActiveAmountAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
            var activeCredits = await _credits.SumActiveAmountAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
            var aged = CreditFifoAging.AgeCredits(group, activeRepayments + activeWriteOffs, effective);
            var overdue = aged.Where(a => a.IsOverdue).ToList();
            if (overdue.Count == 0)
            {
                continue;
            }

            items.Add(new OverdueCustomerListItemDto(
                customer.Id.Value,
                organizationId,
                customer.DisplayName,
                activeCredits - activeRepayments - activeWriteOffs,
                overdue.Sum(a => a.RemainingUnpaidAmount),
                overdue.Count,
                overdue.Select(a => a.CurrentDueDate).Where(d => d is not null).Cast<DateOnly>().OrderBy(d => d).Cast<DateOnly?>().FirstOrDefault()));
        }

        items = items
            .OrderBy(i => i.EarliestOverdueDate)
            .ThenBy(i => i.DisplayName)
            .ThenBy(i => i.CustomerId)
            .ToList();

        var (skip, take) = PosPagination.Normalize(page, pageSize);
        return new PagedResult<OverdueCustomerListItemDto>(
            items.Skip(skip).Take(take).ToList(),
            items.Count,
            Math.Max(page ?? 1, 1),
            take);
    }

    private static IEnumerable<AgedCreditDto> ApplyFilter(IEnumerable<AgedCreditDto> aged, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter) || filter.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return aged;
        }

        return filter.Trim().ToLowerInvariant() switch
        {
            "overdue" => aged.Where(a => a.IsOverdue),
            "duesoon" or "due_soon" => aged.Where(a =>
                a.DueStatus is nameof(CreditDueStatus.DueSoon)
                    or nameof(CreditDueStatus.DueToday)
                    or nameof(CreditDueStatus.Upcoming)),
            "noduedate" or "no_due_date" => aged.Where(a => a.DueStatus == nameof(CreditDueStatus.NoDueDate)),
            "paid" => aged.Where(a => a.DueStatus == nameof(CreditDueStatus.Paid)),
            "reversed" => aged.Where(a => a.DueStatus == nameof(CreditDueStatus.Reversed)),
            "upcoming" => aged.Where(a => a.DueStatus == nameof(CreditDueStatus.Upcoming)),
            _ => aged
        };
    }
}

public sealed class SetCreditDueDate
{
    private readonly ICreditEntryRepository _credits;
    private readonly ICreditDueDateChangeRepository _history;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SetCreditDueDate(
        ICreditEntryRepository credits,
        ICreditDueDateChangeRepository history,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _credits = credits;
        _history = history;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CreditEntry>> ExecuteAsync(
        Guid organizationId,
        Guid creditEntryId,
        DateOnly? newDueDate,
        string reason,
        Guid changedBy,
        DateOnly? expectedCurrentDueDate = null,
        bool checkExpectedDueDate = false,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var entry = await _credits.GetByIdForOrganizationAsync(orgId, CreditEntryId.From(creditEntryId), cancellationToken)
            .ConfigureAwait(false);
        if (entry is null)
        {
            return ApplicationResult<CreditEntry>.Failure(
                ApplicationErrorCodes.CreditEntryNotFound,
                "Credit entry was not found.");
        }

        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var current = await _credits
                    .GetByIdForOrganizationAsync(orgId, CreditEntryId.From(creditEntryId), ct)
                    .ConfigureAwait(false);
                if (current is null)
                {
                    return ApplicationResult<CreditEntry>.Failure(
                        ApplicationErrorCodes.CreditEntryNotFound,
                        "Credit entry was not found.");
                }

                // CurrentDueDate is the practical concurrency token for due-date mutations (entries lack UpdatedAtUtc).
                if (checkExpectedDueDate && current.CurrentDueDate != expectedCurrentDueDate)
                {
                    return ApplicationResult<CreditEntry>.Failure(
                        ApplicationErrorCodes.ConcurrencyConflict,
                        "Credit due date was changed by another session.");
                }

                var change = CreditDueDateChange.Create(
                    orgId,
                    current.Id,
                    current.CustomerId,
                    current.CurrentDueDate,
                    newDueDate,
                    reason,
                    changedBy,
                    _clock.UtcNow);

                current.ApplyCurrentDueDate(newDueDate);
                await _history.AddAsync(change, ct).ConfigureAwait(false);
                await _credits.UpdateAsync(current, ct).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                return ApplicationResult<CreditEntry>.Success(current);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CreditEntry>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CreditEntry>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CreditDueDateHistoryQuery
{
    private readonly ICreditDueDateChangeRepository _history;
    private readonly ICreditEntryRepository _credits;

    public CreditDueDateHistoryQuery(ICreditDueDateChangeRepository history, ICreditEntryRepository credits)
    {
        _history = history;
        _credits = credits;
    }

    public async Task<PagedResult<CreditDueDateChangeDto>?> ListAsync(
        Guid organizationId,
        Guid creditEntryId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var entry = await _credits
            .GetByIdForOrganizationAsync(orgId, CreditEntryId.From(creditEntryId), cancellationToken)
            .ConfigureAwait(false);
        if (entry is null)
        {
            return null;
        }

        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _history
            .ListByCreditAsync(orgId, CreditEntryId.From(creditEntryId), skip, take, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<CreditDueDateChangeDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static CreditDueDateChangeDto Map(CreditDueDateChange change) =>
        new(
            change.Id.Value,
            change.OrganizationId.Value,
            change.CreditEntryId.Value,
            change.CustomerId.Value,
            change.PreviousDueDate,
            change.NewDueDate,
            change.Reason,
            change.ChangedBy,
            change.ChangedAtUtc);
}
