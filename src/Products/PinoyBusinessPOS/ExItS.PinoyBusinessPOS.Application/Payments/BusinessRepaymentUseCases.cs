using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Payments;

public sealed record BusinessRepaymentDto(
    Guid RepaymentId,
    Guid SellerOrganizationId,
    Guid BuyerOrganizationId,
    Guid ConnectionId,
    decimal Amount,
    string? Remarks,
    string PaymentMethod,
    string? CheckNumber,
    string? BankName,
    DateOnly? CheckDate,
    string? AccountName,
    string? Reference,
    string CheckClearingStatus,
    string Status,
    DateTimeOffset RecordedAtUtc,
    Guid RecordedBy,
    DateTimeOffset? ClearedAtUtc,
    Guid? ClearedBy,
    DateTimeOffset? BouncedAtUtc,
    Guid? BouncedBy,
    string? BounceReason,
    DateTimeOffset? CancelledAtUtc,
    Guid? CancelledBy,
    string? CancelReason,
    DateTimeOffset? ReversedAtUtc,
    string? ReversalReason,
    Guid? ReversedBy);

public sealed record BusinessUtangSummaryDto(
    Guid ConnectionId,
    Guid SellerOrganizationId,
    Guid BuyerOrganizationId,
    decimal OutstandingAmount,
    decimal ActiveCreditTotal,
    decimal ActiveRepaymentTotal,
    decimal PendingCheckAmount,
    decimal OverdueAmount = 0m,
    int OpenReceivableCount = 0,
    decimal OpenReceivableTotal = 0m);

public sealed record BusinessReceivableDto(
    Guid CreditEntryId,
    string SourceType,
    string? SourceReference,
    DateOnly? DueDate,
    decimal OutstandingBalance,
    decimal OriginalAmount,
    DateTimeOffset CreatedAtUtc,
    string? Remarks,
    /// <summary>Open | PartiallyPaid | Paid | Reversed</summary>
    string Status = "Open",
    bool IsOverdue = false,
    /// <summary>Paid at sale/receipt before the credit obligation was posted (usually 0 — netted).</summary>
    decimal PaidAtSourceAmount = 0m,
    /// <summary>Settled repayments applied to this credit via FIFO aging.</summary>
    decimal LaterPaymentsAmount = 0m,
    Guid? SourceId = null);

public static class BusinessRepaymentMapper
{
    public static BusinessRepaymentDto Map(BusinessRepayment repayment) =>
        new(
            repayment.Id.Value,
            repayment.SellerOrganizationId.Value,
            repayment.BuyerOrganizationId.Value,
            repayment.ConnectionId,
            repayment.Amount,
            repayment.Remarks,
            repayment.PaymentMethod.ToString(),
            repayment.CheckNumber,
            repayment.BankName,
            repayment.CheckDate,
            repayment.AccountName,
            repayment.Reference,
            repayment.CheckClearingStatus.ToString(),
            repayment.Status.ToString(),
            repayment.RecordedAtUtc,
            repayment.RecordedBy,
            repayment.ClearedAtUtc,
            repayment.ClearedBy,
            repayment.BouncedAtUtc,
            repayment.BouncedBy,
            repayment.BounceReason,
            repayment.CancelledAtUtc,
            repayment.CancelledBy,
            repayment.CancelReason,
            repayment.ReversedAtUtc,
            repayment.ReversalReason,
            repayment.ReversedBy);
}

public sealed class BusinessOutstandingBalanceService
{
    private readonly IBusinessCreditEntryRepository _credits;
    private readonly IBusinessRepaymentRepository _repayments;
    private readonly IClock _clock;

    public BusinessOutstandingBalanceService(
        IBusinessCreditEntryRepository credits,
        IBusinessRepaymentRepository repayments,
        IClock clock)
    {
        _credits = credits;
        _repayments = repayments;
        _clock = clock;
    }

    public async Task<decimal> GetOutstandingAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var credits = await _credits
            .SumActiveAmountAsync(sellerOrganizationId, buyerOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        var repayments = await _repayments
            .SumSettledAmountAsync(sellerOrganizationId, buyerOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        return credits - repayments;
    }

    public async Task<BusinessUtangSummaryDto> GetSummaryAsync(
        Guid sellerOrganizationId,
        Guid buyerOrganizationId,
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        var seller = PosOrganizationId.From(sellerOrganizationId);
        var buyer = PosOrganizationId.From(buyerOrganizationId);
        var creditItems = await _credits
            .ListChronologicalForBuyerAsync(seller, buyer, cancellationToken)
            .ConfigureAwait(false);
        var credits = creditItems
            .Where(c => c.Status == CreditEntryStatus.Active)
            .Sum(c => c.Amount);
        var repayments = await _repayments.SumSettledAmountAsync(seller, buyer, cancellationToken).ConfigureAwait(false);
        var pending = await _repayments.SumPendingCheckAmountAsync(seller, buyer, cancellationToken).ConfigureAwait(false);
        var aged = CreditFifoAging.AgeBusinessCredits(
            creditItems,
            repayments,
            CreditFifoAging.EffectiveBusinessDateUtc(_clock.UtcNow));
        var overdueAmount = aged.Where(a => a.IsOverdue).Sum(a => a.RemainingUnpaidAmount);
        var open = BusinessCreditPaymentAllocator.FromAged(aged);
        return new BusinessUtangSummaryDto(
            connectionId,
            sellerOrganizationId,
            buyerOrganizationId,
            credits - repayments,
            credits,
            repayments,
            pending,
            overdueAmount,
            open.Count,
            open.Sum(o => o.RemainingUnpaidAmount));
    }

    public async Task<IReadOnlyList<BusinessReceivableDto>> ListOpenReceivablesAsync(
        Guid sellerOrganizationId,
        Guid buyerOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var all = await ListReceivablesAsync(sellerOrganizationId, buyerOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        return all
            .Where(r =>
                (r.Status is "Open" or "PartiallyPaid")
                && r.OutstandingBalance > 0m)
            .ToList();
    }

    /// <summary>
    /// All aged B2B credit obligations for filters Open | Overdue | Paid | All.
    /// Source labels are clean business references (no raw <c>sale:guid|…</c> prefixes).
    /// </summary>
    public async Task<IReadOnlyList<BusinessReceivableDto>> ListReceivablesAsync(
        Guid sellerOrganizationId,
        Guid buyerOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var seller = PosOrganizationId.From(sellerOrganizationId);
        var buyer = PosOrganizationId.From(buyerOrganizationId);
        var creditItems = await _credits
            .ListChronologicalForBuyerAsync(seller, buyer, cancellationToken)
            .ConfigureAwait(false);
        var repayments = await _repayments.SumSettledAmountAsync(seller, buyer, cancellationToken).ConfigureAwait(false);
        var aged = CreditFifoAging.AgeBusinessCredits(
            creditItems,
            repayments,
            CreditFifoAging.EffectiveBusinessDateUtc(_clock.UtcNow));
        var creditById = creditItems.ToDictionary(c => c.Id.Value);

        return aged
            .OrderByDescending(a => a.CreatedAtUtc)
            .ThenByDescending(a => a.CreditEntryId)
            .Select(o =>
            {
                creditById.TryGetValue(o.CreditEntryId, out var credit);
                ConnectedPoUtangObligationProjection.TryResolveSource(
                    o.Remarks,
                    credit?.SourceSaleId?.Value,
                    out var sourceType,
                    out var sourceId,
                    out var sourceReference);

                var later = SaleMoney.RoundMoney(o.Amount - o.RemainingUnpaidAmount);
                if (later < 0m)
                {
                    later = 0m;
                }

                string status;
                if (string.Equals(o.Status, nameof(CreditEntryStatus.Reversed), StringComparison.Ordinal))
                {
                    status = "Reversed";
                }
                else if (o.RemainingUnpaidAmount <= 0m)
                {
                    status = "Paid";
                }
                else if (later > 0m)
                {
                    status = "PartiallyPaid";
                }
                else
                {
                    status = "Open";
                }

                return new BusinessReceivableDto(
                    o.CreditEntryId,
                    sourceType,
                    sourceReference,
                    o.CurrentDueDate,
                    o.RemainingUnpaidAmount,
                    o.Amount,
                    o.CreatedAtUtc,
                    o.Remarks,
                    status,
                    o.IsOverdue,
                    PaidAtSourceAmount: 0m,
                    LaterPaymentsAmount: later,
                    SourceId: sourceId);
            })
            .ToList();
    }
}

public sealed class CreateBusinessRepayment
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IBusinessRepaymentRepository _repayments;
    private readonly IBusinessCreditEntryRepository _credits;
    private readonly BusinessOutstandingBalanceService _outstanding;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ConnectedB2bPaymentMirror? _b2bMirror;

    public CreateBusinessRepayment(
        IConnectedSupplierRelationshipRepository relationships,
        IBusinessRepaymentRepository repayments,
        IBusinessCreditEntryRepository credits,
        BusinessOutstandingBalanceService outstanding,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        ConnectedB2bPaymentMirror? b2bMirror = null)
    {
        _relationships = relationships;
        _repayments = repayments;
        _credits = credits;
        _outstanding = outstanding;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _b2bMirror = b2bMirror;
    }

    public async Task<ApplicationResult<BusinessRepayment>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid connectionId,
        CreateUtangRepaymentCommand command,
        Guid recordedBy,
        CancellationToken cancellationToken = default)
    {
        var resolved = await BusinessCustomerCreditPolicyRelationshipGuard
            .ResolveForSellerAsync(_relationships, sellerOrganizationId, connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (!resolved.IsSuccess)
        {
            return ApplicationResult<BusinessRepayment>.Failure(resolved.ErrorCode!, resolved.ErrorMessage!);
        }

        var relationship = resolved.Value!;
        var activeGate = BusinessCustomerCreditPolicyRelationshipGuard
            .RequireActiveForMutation<BusinessRepayment>(relationship);
        if (!activeGate.IsSuccess)
        {
            return ApplicationResult<BusinessRepayment>.Failure(activeGate.ErrorCode!, activeGate.ErrorMessage!);
        }

        UtangPaymentMethod paymentMethod;
        try
        {
            paymentMethod = string.IsNullOrWhiteSpace(command.PaymentMethod)
                ? UtangPaymentMethod.Cash
                : UtangPaymentMethods.ParseRequired(command.PaymentMethod);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BusinessRepayment>.Failure(ex.ErrorCode, ex.Message);
        }

        var seller = relationship.SupplierOrganizationId;
        var buyer = relationship.BuyerOrganizationId;

        if (command.RepaymentId is not null)
        {
            var existing = await _repayments
                .GetByIdAsync(seller, BusinessRepaymentId.From(command.RepaymentId.Value), cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                if (existing.ConnectionId != connectionId || existing.BuyerOrganizationId != buyer)
                {
                    return ApplicationResult<BusinessRepayment>.Failure(
                        ApplicationErrorCodes.ConcurrencyConflict,
                        "Repayment id is already assigned to another business customer.");
                }

                return ApplicationResult<BusinessRepayment>.Success(existing);
            }
        }

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    var outstanding = await _outstanding.GetOutstandingAsync(seller, buyer, ct).ConfigureAwait(false);
                    if (outstanding <= 0m)
                    {
                        return ApplicationResult<BusinessRepayment>.Failure(
                            DomainErrorCodes.RepaymentOutstandingZero,
                            "Outstanding balance is zero; repayment is not allowed.");
                    }

                    var normalized = Repayment.NormalizeAmount(command.Amount);
                    if (normalized > outstanding)
                    {
                        return ApplicationResult<BusinessRepayment>.Failure(
                            DomainErrorCodes.RepaymentExceedsOutstanding,
                            "Repayment amount exceeds the current outstanding balance.");
                    }

                    var creditItems = await _credits
                        .ListChronologicalForBuyerAsync(seller, buyer, ct)
                        .ConfigureAwait(false);
                    var settled = await _repayments.SumSettledAmountAsync(seller, buyer, ct).ConfigureAwait(false);
                    // Pending checks do not reduce outstanding yet; age from settled only.
                    var aged = CreditFifoAging.AgeBusinessCredits(
                        creditItems,
                        settled,
                        CreditFifoAging.EffectiveBusinessDateUtc(_clock.UtcNow));
                    var open = BusinessCreditPaymentAllocator.FromAged(aged);

                    IReadOnlyList<BusinessCreditPaymentAllocator.AllocationLine> allocationLines;
                    if (command.Allocations is { Count: > 0 })
                    {
                        allocationLines = BusinessCreditPaymentAllocator.ValidateManual(
                            open,
                            command.Allocations
                                .Select(a => new BusinessCreditPaymentAllocator.AllocationLine(a.CreditEntryId, a.Amount))
                                .ToList(),
                            normalized);
                    }
                    else
                    {
                        allocationLines = BusinessCreditPaymentAllocator.AllocateAutomatically(open, normalized);
                    }

                    var repayment = BusinessRepayment.Create(
                        seller,
                        buyer,
                        connectionId,
                        normalized,
                        command.Remarks,
                        recordedBy,
                        _clock.UtcNow,
                        id: command.RepaymentId is null ? null : BusinessRepaymentId.From(command.RepaymentId.Value),
                        paymentMethod: paymentMethod,
                        checkNumber: command.CheckNumber,
                        bankName: command.BankName,
                        checkDate: command.CheckDate,
                        accountName: command.AccountName,
                        reference: command.Reference);
                    await _repayments.AddAsync(repayment, ct).ConfigureAwait(false);

                    var persistedAllocations = allocationLines
                        .Select(line => BusinessRepaymentAllocation.Create(
                            seller,
                            repayment.Id,
                            BusinessCreditEntryId.From(line.CreditEntryId),
                            line.Amount,
                            _clock.UtcNow))
                        .ToList();
                    await _repayments.AddAllocationsAsync(persistedAllocations, ct).ConfigureAwait(false);

                    if (_b2bMirror is not null)
                    {
                        await _b2bMirror
                            .MirrorSellerRepaymentAsync(repayment, persistedAllocations, creditItems, ct)
                            .ConfigureAwait(false);
                    }

                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return ApplicationResult<BusinessRepayment>.Success(repayment);
                }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BusinessRepayment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<BusinessRepayment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ClearBusinessCheckRepayment
{
    private readonly IBusinessRepaymentRepository _repayments;
    private readonly IBusinessCreditEntryRepository _credits;
    private readonly BusinessOutstandingBalanceService _outstanding;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ConnectedB2bPaymentMirror? _b2bMirror;

    public ClearBusinessCheckRepayment(
        IBusinessRepaymentRepository repayments,
        IBusinessCreditEntryRepository credits,
        BusinessOutstandingBalanceService outstanding,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        ConnectedB2bPaymentMirror? b2bMirror = null)
    {
        _repayments = repayments;
        _credits = credits;
        _outstanding = outstanding;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _b2bMirror = b2bMirror;
    }

    public async Task<ApplicationResult<BusinessRepayment>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid repaymentId,
        Guid clearedBy,
        CancellationToken cancellationToken = default)
    {
        var seller = PosOrganizationId.From(sellerOrganizationId);
        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    var repayment = await _repayments
                        .GetByIdAsync(seller, BusinessRepaymentId.From(repaymentId), ct)
                        .ConfigureAwait(false);
                    if (repayment is null)
                    {
                        return ApplicationResult<BusinessRepayment>.Failure(
                            ApplicationErrorCodes.BusinessRepaymentNotFound,
                            "Business repayment was not found.");
                    }

                    if (repayment.CheckClearingStatus == UtangCheckClearingStatus.Cleared)
                    {
                        return ApplicationResult<BusinessRepayment>.Success(repayment);
                    }

                    var outstanding = await _outstanding
                        .GetOutstandingAsync(seller, repayment.BuyerOrganizationId, ct)
                        .ConfigureAwait(false);
                    if (repayment.Amount > outstanding)
                    {
                        return ApplicationResult<BusinessRepayment>.Failure(
                            DomainErrorCodes.CheckClearExceedsOutstanding,
                            "Check amount exceeds the customer's current outstanding balance. Resolve the account before clearing this check.");
                    }

                    repayment.MarkCleared(clearedBy, _clock.UtcNow);
                    await _repayments.UpdateAsync(repayment, ct).ConfigureAwait(false);
                    if (_b2bMirror is not null)
                    {
                        var credits = await _credits
                            .ListChronologicalForBuyerAsync(seller, repayment.BuyerOrganizationId, ct)
                            .ConfigureAwait(false);
                        var allocations = await _repayments
                            .ListAllocationsByRepaymentAsync(seller, repayment.Id, ct)
                            .ConfigureAwait(false);
                        await _b2bMirror
                            .MirrorSellerRepaymentAsync(repayment, allocations, credits, ct)
                            .ConfigureAwait(false);
                    }

                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return ApplicationResult<BusinessRepayment>.Success(repayment);
                }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BusinessRepayment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<BusinessRepayment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class BounceBusinessCheckRepayment
{
    private readonly IBusinessRepaymentRepository _repayments;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public BounceBusinessCheckRepayment(
        IBusinessRepaymentRepository repayments,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _repayments = repayments;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<BusinessRepayment>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid repaymentId,
        Guid bouncedBy,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var seller = PosOrganizationId.From(sellerOrganizationId);
        var repayment = await _repayments
            .GetByIdAsync(seller, BusinessRepaymentId.From(repaymentId), cancellationToken)
            .ConfigureAwait(false);
        if (repayment is null)
        {
            return ApplicationResult<BusinessRepayment>.Failure(
                ApplicationErrorCodes.BusinessRepaymentNotFound,
                "Business repayment was not found.");
        }

        try
        {
            repayment.MarkBounced(bouncedBy, _clock.UtcNow, reason);
            await _repayments.UpdateAsync(repayment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<BusinessRepayment>.Success(repayment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BusinessRepayment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<BusinessRepayment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CancelBusinessCheckRepayment
{
    private readonly IBusinessRepaymentRepository _repayments;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CancelBusinessCheckRepayment(
        IBusinessRepaymentRepository repayments,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _repayments = repayments;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<BusinessRepayment>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid repaymentId,
        Guid cancelledBy,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var seller = PosOrganizationId.From(sellerOrganizationId);
        var repayment = await _repayments
            .GetByIdAsync(seller, BusinessRepaymentId.From(repaymentId), cancellationToken)
            .ConfigureAwait(false);
        if (repayment is null)
        {
            return ApplicationResult<BusinessRepayment>.Failure(
                ApplicationErrorCodes.BusinessRepaymentNotFound,
                "Business repayment was not found.");
        }

        try
        {
            repayment.CancelCheck(cancelledBy, _clock.UtcNow, reason);
            await _repayments.UpdateAsync(repayment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<BusinessRepayment>.Success(repayment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BusinessRepayment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<BusinessRepayment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class BusinessRepaymentQueryService
{
    private readonly IBusinessRepaymentRepository _repayments;

    public BusinessRepaymentQueryService(IBusinessRepaymentRepository repayments) => _repayments = repayments;

    public async Task<PagedResult<BusinessRepaymentDto>> ListByConnectionAsync(
        Guid sellerOrganizationId,
        Guid connectionId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var items = await _repayments
            .ListByConnectionAsync(
                PosOrganizationId.From(sellerOrganizationId),
                connectionId,
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);
        // Page total is approximate when repository does not return count; list page is history UX.
        return new PagedResult<BusinessRepaymentDto>(
            items.Select(BusinessRepaymentMapper.Map).ToList(),
            items.Count < take ? skip + items.Count : skip + items.Count + 1,
            Math.Max(page ?? 1, 1),
            take);
    }
}
