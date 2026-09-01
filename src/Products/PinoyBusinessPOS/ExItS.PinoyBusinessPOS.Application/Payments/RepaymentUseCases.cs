using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Parties;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.Application.Payments;

public sealed record RepaymentSyncPageDto(
    List<RepaymentDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    DateTimeOffset? NextCheckpointUtc);

public sealed record RepaymentDto(
    Guid RepaymentId,
    Guid OrganizationId,
    Guid CustomerId,
    decimal Amount,
    string? Remarks,
    string Status,
    DateTimeOffset RecordedAtUtc,
    Guid RecordedBy,
    DateTimeOffset? ReversedAtUtc,
    string? ReversalReason,
    Guid? ReversedBy);

public sealed class OutstandingBalanceService : IOutstandingBalanceService
{
    private readonly ICreditEntryRepository _credits;
    private readonly IRepaymentRepository _repayments;
    private readonly IWriteOffRepository _writeOffs;
    private readonly IClock _clock;

    public OutstandingBalanceService(
        ICreditEntryRepository credits,
        IRepaymentRepository repayments,
        IWriteOffRepository writeOffs,
        IClock clock)
    {
        _credits = credits;
        _repayments = repayments;
        _writeOffs = writeOffs;
        _clock = clock;
    }

    public async Task<decimal> GetOutstandingAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default)
    {
        var credits = await _credits.SumActiveAmountAsync(organizationId, customerId, cancellationToken)
            .ConfigureAwait(false);
        var repayments = await _repayments.SumActiveAmountAsync(organizationId, customerId, cancellationToken)
            .ConfigureAwait(false);
        var writeOffs = await _writeOffs.SumActiveAmountAsync(organizationId, customerId, cancellationToken)
            .ConfigureAwait(false);
        return credits - repayments - writeOffs;
    }

    public async Task<CustomerUtangSummaryDto> GetSummaryAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var custId = POSCustomerId.From(customerId);
        var activeCredits = await _credits.SumActiveAmountAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var activeRepayments = await _repayments.SumActiveAmountAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var activeWriteOffs = await _writeOffs.SumActiveAmountAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var activeCreditCount = await _credits.CountActiveAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var activeRepaymentCount = await _repayments.CountActiveAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var activeWriteOffCount = await _writeOffs.CountActiveAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        var (creditItems, creditTotal) = await _credits.ListByCustomerAsync(orgId, custId, 0, 10_000, cancellationToken).ConfigureAwait(false);
        var (_, repaymentTotal) = await _repayments.ListByCustomerAsync(orgId, custId, 0, 1, cancellationToken).ConfigureAwait(false);
        var (_, writeOffTotal) = await _writeOffs.ListByCustomerAsync(orgId, custId, 0, 1, cancellationToken).ConfigureAwait(false);

        var aged = CreditFifoAging.AgeCredits(
            creditItems,
            activeRepayments + activeWriteOffs,
            CreditFifoAging.EffectiveBusinessDateUtc(_clock.UtcNow));
        var overdueSummary = CreditFifoAging.BuildCustomerSummary(
            customerId,
            organizationId,
            aged,
            activeCredits,
            activeRepayments,
            activeCreditCount,
            activeRepaymentCount,
            creditTotal + repaymentTotal + writeOffTotal,
            activeWriteOffs,
            activeWriteOffCount);

        return new CustomerUtangSummaryDto(
            customerId,
            organizationId,
            overdueSummary.OutstandingAmount,
            overdueSummary.ActiveCreditTotal,
            overdueSummary.ActiveRepaymentTotal,
            overdueSummary.ActiveCreditCount,
            overdueSummary.ActiveRepaymentCount,
            overdueSummary.TotalLedgerEntryCount,
            overdueSummary.OverdueAmount,
            overdueSummary.OverdueCreditCount,
            overdueSummary.EarliestOverdueDate,
            overdueSummary.NextUpcomingDueDate,
            overdueSummary.CreditsWithoutDueDateCount,
            overdueSummary.ActiveWriteOffTotal,
            overdueSummary.ActiveWriteOffCount);
    }
}

public sealed class RepaymentQueryService
{
    private readonly IRepaymentRepository _repayments;

    public RepaymentQueryService(IRepaymentRepository repayments) => _repayments = repayments;

    public async Task<RepaymentDto?> GetByIdAsync(
        Guid organizationId,
        Guid repaymentId,
        CancellationToken cancellationToken = default)
    {
        var repayment = await _repayments
            .GetByIdAsync(PosOrganizationId.From(organizationId), RepaymentId.From(repaymentId), cancellationToken)
            .ConfigureAwait(false);
        return repayment is null ? null : Map(repayment);
    }

    public async Task<PagedResult<RepaymentDto>> ListByCustomerAsync(
        Guid organizationId,
        Guid customerId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _repayments
            .ListByCustomerAsync(
                PosOrganizationId.From(organizationId),
                POSCustomerId.From(customerId),
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<RepaymentDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<RepaymentSyncPageDto> ListForSyncAsync(
        Guid organizationId,
        DateTimeOffset? sinceUtc,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _repayments
            .ListCreatedSinceAsync(PosOrganizationId.From(organizationId), sinceUtc, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var mapped = items.Select(Map).ToList();
        DateTimeOffset? nextCheckpoint = null;
        foreach (var repayment in mapped)
        {
            var candidate = repayment.ReversedAtUtc ?? repayment.RecordedAtUtc;
            if (nextCheckpoint is null || candidate > nextCheckpoint)
            {
                nextCheckpoint = candidate;
            }
        }

        return new RepaymentSyncPageDto(mapped, total, Math.Max(page ?? 1, 1), take, nextCheckpoint);
    }

    public static RepaymentDto Map(Repayment repayment) =>
        new(
            repayment.Id.Value,
            repayment.OrganizationId.Value,
            repayment.CustomerId.Value,
            repayment.Amount,
            repayment.Remarks,
            repayment.Status.ToString(),
            repayment.RecordedAtUtc,
            repayment.RecordedBy,
            repayment.ReversedAtUtc,
            repayment.ReversalReason,
            repayment.ReversedBy);
}

public sealed class UtangLedgerQueryService
{
    private readonly IUtangLedgerQuery _ledger;
    private readonly PartyBranchHistoryScopeService _historyScope;

    public UtangLedgerQueryService(IUtangLedgerQuery ledger, PartyBranchHistoryScopeService historyScope)
    {
        _ledger = ledger;
        _historyScope = historyScope;
    }

    public async Task<PagedResult<LedgerEntryDto>> ListAsync(
        Guid organizationId,
        Guid customerId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var branchIds = _historyScope.GetPermittedHistoryBranchIds(organizationId);
        var hideAdjustments = _historyScope.ShouldHideOrgWideLedgerAdjustments();
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _ledger
            .ListAsync(
                PosOrganizationId.From(organizationId),
                POSCustomerId.From(customerId),
                skip,
                take,
                cancellationToken,
                branchIds,
                hideAdjustments)
            .ConfigureAwait(false);

        return new PagedResult<LedgerEntryDto>(
            items,
            total,
            Math.Max(page ?? 1, 1),
            take);
    }
}

public sealed class CreateRepayment
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IRepaymentRepository _repayments;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateRepayment(
        IPOSCustomerRepository customers,
        IRepaymentRepository repayments,
        IOutstandingBalanceService outstanding,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _repayments = repayments;
        _outstanding = outstanding;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Repayment>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        decimal amount,
        string? remarks,
        Guid recordedBy,
        Guid? clientRepaymentId = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var custId = POSCustomerId.From(customerId);
        var customer = await _customers.GetByIdAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<Repayment>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        if (clientRepaymentId is not null)
        {
            var existing = await _repayments
                .GetByIdAsync(orgId, RepaymentId.From(clientRepaymentId.Value), cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                if (existing.CustomerId != custId)
                {
                    return ApplicationResult<Repayment>.Failure(
                        ApplicationErrorCodes.ConcurrencyConflict,
                        "Repayment id is already assigned to another customer.");
                }

                return ApplicationResult<Repayment>.Success(existing);
            }
        }

        // Inactive-customer policy (P6-WP03): allow repayment against existing debt.
        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    var outstanding = await _outstanding.GetOutstandingAsync(orgId, custId, ct).ConfigureAwait(false);
                    if (outstanding <= 0m)
                    {
                        return ApplicationResult<Repayment>.Failure(
                            DomainErrorCodes.RepaymentOutstandingZero,
                            "Outstanding balance is zero; repayment is not allowed.");
                    }

                    var normalized = Repayment.NormalizeAmount(amount);
                    if (normalized > outstanding)
                    {
                        return ApplicationResult<Repayment>.Failure(
                            DomainErrorCodes.RepaymentExceedsOutstanding,
                            "Repayment amount exceeds the current outstanding balance.");
                    }

                    var repayment = clientRepaymentId is null
                        ? Repayment.Create(orgId, custId, normalized, remarks, recordedBy, _clock.UtcNow)
                        : Repayment.Create(
                            orgId,
                            custId,
                            normalized,
                            remarks,
                            recordedBy,
                            _clock.UtcNow,
                            id: RepaymentId.From(clientRepaymentId.Value));
                    await _repayments.AddAsync(repayment, ct).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return ApplicationResult<Repayment>.Success(repayment);
                }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Repayment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Repayment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ReverseRepayment
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IRepaymentRepository _repayments;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReverseRepayment(
        IPOSCustomerRepository customers,
        IRepaymentRepository repayments,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _repayments = repayments;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<Repayment>> ExecuteAsync(
        Guid organizationId,
        Guid repaymentId,
        string reason,
        Guid reversedBy,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var repayment = await _repayments
            .GetByIdAsync(orgId, RepaymentId.From(repaymentId), cancellationToken)
            .ConfigureAwait(false);
        if (repayment is null)
        {
            return ApplicationResult<Repayment>.Failure(
                ApplicationErrorCodes.RepaymentNotFound,
                "Repayment was not found.");
        }

        var customer = await _customers
            .GetByIdAsync(orgId, repayment.CustomerId, cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<Repayment>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        try
        {
            repayment.Reverse(reason, reversedBy, _clock.UtcNow);
            await _repayments.UpdateAsync(repayment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<Repayment>.Success(repayment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Repayment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<Repayment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
