using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Returns;

public sealed class ReturnBatchQueryService
{
    private readonly IReturnBatchRepository _batches;
    private readonly ISaleRepository _sales;

    public ReturnBatchQueryService(IReturnBatchRepository batches, ISaleRepository sales)
    {
        _batches = batches;
        _sales = sales;
    }

    public async Task<ReturnBatchDto?> GetByIdAsync(
        Guid organizationId,
        Guid returnBatchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _batches
            .GetByIdAsync(PosOrganizationId.From(organizationId), ReturnBatchId.From(returnBatchId), cancellationToken)
            .ConfigureAwait(false);
        if (batch is null)
        {
            return null;
        }

        if (batch.SaleId is null)
        {
            return Map(batch, BuildFinancialSummary(batch, sale: null, [batch]));
        }

        var sale = await _sales
            .GetByIdAsync(batch.OrganizationId, batch.SaleId, cancellationToken)
            .ConfigureAwait(false);
        var saleBatches = await _batches
            .ListBySaleIdAsync(batch.OrganizationId, batch.SaleId, cancellationToken)
            .ConfigureAwait(false);
        var summary = BuildFinancialSummary(batch, sale, saleBatches);
        return Map(batch, summary);
    }

    public async Task<IReadOnlyList<ReturnBatchDto>> ListBySaleIdAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var saleDomainId = SaleId.From(saleId);
        var items = await _batches
            .ListBySaleIdAsync(orgId, saleDomainId, cancellationToken)
            .ConfigureAwait(false);
        var sale = await _sales.GetByIdAsync(orgId, saleDomainId, cancellationToken).ConfigureAwait(false);
        return items.Select(item => Map(item, BuildFinancialSummary(item, sale, items))).ToList();
    }

    public async Task<ReturnBatchReviewPreviewDto?> GetReviewPreviewAsync(
        Guid organizationId,
        Guid returnBatchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _batches
            .GetByIdAsync(PosOrganizationId.From(organizationId), ReturnBatchId.From(returnBatchId), cancellationToken)
            .ConfigureAwait(false);
        if (batch is null)
        {
            return null;
        }

        var sale = batch.SaleId is null
            ? null
            : await _sales
                .GetByIdAsync(batch.OrganizationId, batch.SaleId, cancellationToken)
                .ConfigureAwait(false);
        var saleBatches = batch.SaleId is null
            ? [batch]
            : await _batches
                .ListBySaleIdAsync(batch.OrganizationId, batch.SaleId, cancellationToken)
                .ConfigureAwait(false);

        return new ReturnBatchReviewPreviewDto(
            batch.Id.Value,
            batch.BatchNumber,
            ReturnBatchStatuses.ToCode(batch.Status),
            ReturnBatchRefundStatuses.ToCode(batch.RefundStatus),
            batch.AcceptedReturnValue,
            batch.RefundDueAmount,
            batch.RefundedAmount,
            BuildFinancialSummary(batch, sale, saleBatches),
            batch.Lines.Select(MapLine).ToList());
    }

    public async Task<IReadOnlyList<ReturnBatchTimelineEventDto>?> GetTimelineAsync(
        Guid organizationId,
        Guid returnBatchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _batches
            .GetByIdAsync(PosOrganizationId.From(organizationId), ReturnBatchId.From(returnBatchId), cancellationToken)
            .ConfigureAwait(false);
        return batch?.Timeline
            .OrderBy(e => e.CreatedAtUtc)
            .Select(MapTimeline)
            .ToList();
    }

    public static ReturnBatchDto Map(ReturnBatch batch) =>
        Map(batch, BuildFinancialSummary(batch, sale: null, [batch]));

    public static ReturnBatchDto Map(ReturnBatch batch, ReturnBatchFinancialSummaryDto financialSummary) =>
        new(
            batch.Id.Value,
            batch.OrganizationId.Value,
            batch.SaleId?.Value,
            batch.BranchId?.Value,
            batch.BatchNumber,
            ReturnBatchStatuses.ToCode(batch.Status),
            ReturnBatchRefundStatuses.ToCode(batch.RefundStatus),
            batch.AcceptedReturnValue,
            batch.RefundDueAmount,
            batch.RefundedAmount,
            batch.SaleReturnId?.Value,
            batch.Reason,
            batch.Notes,
            batch.CreatedAtUtc,
            batch.CreatedBy,
            batch.UpdatedAtUtc,
            batch.FinalizedAtUtc,
            batch.FinalizedBy,
            financialSummary,
            batch.Lines.Select(MapLine).ToList(),
            batch.Refunds.Select(MapRefund).ToList(),
            ReturnBatchSourceTypes.ToCode(batch.SourceType),
            batch.PurchaseOrderId?.Value,
            batch.ConnectedPurchaseOrderId?.Value,
            batch.BuyerOrganizationId?.Value,
            batch.SellerOrganizationId?.Value,
            batch.BuyerBranchId?.Value,
            batch.SellerBranchId?.Value,
            batch.PaymentTimingSnapshot?.ToString(),
            batch.PoNumberSnapshot,
            batch.SellerReceivedAtUtc,
            batch.SellerReceivedBy);

    private static ReturnBatchLineDto MapLine(ReturnBatchLine line) =>
        new(
            line.Id.Value,
            line.SaleLineId?.Value,
            line.ProductId.Value,
            line.ProductNameSnapshot,
            UnitOfMeasures.ToCode(line.UomSnapshot),
            line.UnitPriceSnapshot,
            line.LineTotalSnapshot,
            line.AcceptedQuantity,
            line.RefundAmountSnapshot,
            line.SellableQuantity,
            line.DamagedQuantity,
            line.InspectionNote,
            line.ClassifiedAtUtc,
            line.ClassifiedBy,
            line.PurchaseOrderLineId?.Value,
            line.SupplierProductId?.Value);

    private static ReturnBatchRefundDto MapRefund(ReturnBatchRefund refund) =>
        new(
            refund.Id.Value,
            refund.Amount,
            SalePaymentMethods.ToCode(refund.Method),
            refund.Reference,
            refund.Note,
            refund.ClientRefundId,
            refund.CreatedAtUtc,
            refund.CreatedBy);

    private static ReturnBatchTimelineEventDto MapTimeline(ReturnBatchAuditEvent timeline) =>
        new(
            timeline.Id,
            ReturnBatchAuditEventTypes.ToCode(timeline.EventType),
            timeline.PayloadJson,
            timeline.CreatedAtUtc,
            timeline.CreatedBy);

    private static ReturnBatchFinancialSummaryDto BuildFinancialSummary(
        ReturnBatch batch,
        Sale? sale,
        IReadOnlyList<ReturnBatch> saleBatches)
    {
        var settled = sale is null ? 0m : SaleReturnSettlementResolver.ResolveSettledPayments(sale);
        var priorFinalized = saleBatches
            .Where(b => b.Id != batch.Id && b.Status == ReturnBatchStatus.Finalized)
            .ToList();
        var priorAccepted = priorFinalized.Sum(b => b.AcceptedReturnValue);
        var priorRefunded = priorFinalized.Sum(b => b.RefundedAmount);
        var settlement = sale is null
            ? new ReturnBatchFinancialSettlementResult(0m, batch.RefundDueAmount, 0m, 0m, 0m)
            : ReturnBatchFinancialSettlement.Evaluate(
                sale.Total,
                settled,
                batch.AcceptedReturnValue,
                priorAccepted,
                priorRefunded);
        var totals = batch.Lines.Aggregate(
            (Returned: 0m, Sellable: 0m, Damaged: 0m),
            (acc, line) => (
                acc.Returned + line.AcceptedQuantity,
                acc.Sellable + (line.SellableQuantity ?? 0m),
                acc.Damaged + (line.DamagedQuantity ?? 0m)));
        var refundRemaining = Math.Max(0m, batch.RefundDueAmount - batch.RefundedAmount);

        return new ReturnBatchFinancialSummaryDto(
            sale?.Total ?? 0m,
            batch.AcceptedReturnValue,
            settled,
            settlement.ObligationReducedAmount,
            settlement.RemainingDue,
            batch.RefundDueAmount,
            batch.RefundedAmount,
            refundRemaining,
            totals.Returned,
            totals.Sellable,
            totals.Damaged);
    }
}

public sealed class AcceptReturnBatch
{
    private readonly IReturnBatchRepository _batches;
    private readonly ISaleRepository _sales;
    private readonly ISaleReturnRepository _returns;
    private readonly ISaleMutationLock _saleMutationLock;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository? _branchBalances;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AcceptReturnBatch(
        IReturnBatchRepository batches,
        ISaleRepository sales,
        ISaleReturnRepository returns,
        ISaleMutationLock saleMutationLock,
        IInventoryRepository inventory,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IInventoryBranchBalanceRepository? branchBalances = null)
    {
        _batches = batches;
        _sales = sales;
        _returns = returns;
        _saleMutationLock = saleMutationLock;
        _inventory = inventory;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _branchBalances = branchBalances;
    }

    public async Task<ApplicationResult<ReturnBatch>> ExecuteAsync(
        Guid organizationId,
        Guid saleId,
        string reason,
        IReadOnlyList<AcceptReturnBatchLineRequest>? lines,
        Guid actorId,
        string? notes = null,
        Guid? clientReturnBatchId = null,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<ReturnBatch>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to accept a return batch.");
        }

        if (lines is null || lines.Count == 0)
        {
            return ApplicationResult<ReturnBatch>.Failure(
                DomainErrorCodes.ReturnBatchRequiresAtLeastOneLine,
                "A return batch must contain at least one line.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var saleDomainId = SaleId.From(saleId);
        var lineDrafts = lines
            .Select(l => new ReturnBatchAcceptedLineDraft(SaleLineId.From(l.SaleLineId), l.AcceptedQuantity))
            .ToList();

        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                await _saleMutationLock.AcquireAsync(orgId, saleDomainId, ct).ConfigureAwait(false);

                if (clientReturnBatchId is not null)
                {
                    var existing = await _batches
                        .GetByIdAsync(orgId, ReturnBatchId.From(clientReturnBatchId.Value), ct)
                        .ConfigureAwait(false);
                    if (existing is not null)
                    {
                        return ApplicationResult<ReturnBatch>.Success(existing);
                    }
                }

                var sale = await _sales.GetByIdAsync(orgId, saleDomainId, ct).ConfigureAwait(false);
                if (sale is null)
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        ApplicationErrorCodes.SaleNotFound,
                        "Sale was not found.");
                }

                var prior = await _returns.GetPriorTotalsBySaleLineAsync(orgId, saleDomainId, ct).ConfigureAwait(false);
                var utcNow = _clock.UtcNow;

                var created = await _batches
                    .CreateAsync(
                        orgId,
                        ReturnBatchNumbers.BusinessDateOf(utcNow),
                        number => ReturnBatch.CreateAccepted(
                            orgId,
                            number,
                            sale,
                            lineDrafts,
                            prior.ToDictionary(k => k.Key, v => (v.Value.ReturnedQuantity, v.Value.RefundedAmount)),
                            reason,
                            actorId,
                            utcNow,
                            notes,
                            clientReturnBatchId is null ? null : ReturnBatchId.From(clientReturnBatchId.Value)),
                        (batch, afterCt) => IncreasePendingReturnsAsync(batch, sale, utcNow, afterCt),
                        ct)
                    .ConfigureAwait(false);

                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                return ApplicationResult<ReturnBatch>.Success(created);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ReturnBatch>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ReturnBatch>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task IncreasePendingReturnsAsync(
        ReturnBatch batch,
        Sale sale,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var quantitiesByProduct = batch.Lines
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.AcceptedQuantity));
        if (quantitiesByProduct.Count == 0)
        {
            return;
        }

        var accounts = await _inventory
            .ListByProductIdsAsync(batch.OrganizationId, quantitiesByProduct.Keys.ToList(), cancellationToken)
            .ConfigureAwait(false);
        var accountsByProduct = accounts.Where(a => a.IsTracked).ToDictionary(a => a.ProductId);

        foreach (var (productId, quantity) in quantitiesByProduct)
        {
            if (!accountsByProduct.TryGetValue(productId, out var account))
            {
                continue;
            }

            account.IncreasePendingReturn(quantity);
            account.Touch(utcNow);
            await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
        }

        if (_branchBalances is null || sale.BranchId is null)
        {
            return;
        }

        var branchId = sale.BranchId;
        var balances = await _branchBalances
            .ListByBranchAndProductIdsAsync(batch.OrganizationId, branchId, quantitiesByProduct.Keys.ToList(), cancellationToken)
            .ConfigureAwait(false);
        var balanceByProduct = balances.ToDictionary(b => b.ProductId);

        foreach (var (productId, quantity) in quantitiesByProduct)
        {
            if (!accountsByProduct.ContainsKey(productId))
            {
                continue;
            }

            if (!balanceByProduct.TryGetValue(productId, out var balance))
            {
                balance = InventoryBranchBalance.Create(batch.OrganizationId, branchId, productId, 0m, utcNow);
            }

            balance.IncreasePendingReturn(quantity, utcNow);
            await _branchBalances.UpsertAsync(balance, cancellationToken).ConfigureAwait(false);
        }
    }
}

public sealed class ClassifyReturnBatchLine
{
    private readonly IReturnBatchRepository _batches;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ClassifyReturnBatchLine(
        IReturnBatchRepository batches,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _batches = batches;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ReturnBatch>> ExecuteAsync(
        Guid organizationId,
        Guid returnBatchId,
        Guid returnBatchLineId,
        decimal sellableQuantity,
        decimal damagedQuantity,
        Guid actorId,
        string? inspectionNote = null,
        DateTimeOffset? expectedUpdatedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var batchId = ReturnBatchId.From(returnBatchId);
        try
        {
            var batch = await _batches.GetByIdAsync(orgId, batchId, cancellationToken).ConfigureAwait(false);
            if (batch is null)
            {
                return ApplicationResult<ReturnBatch>.Failure(
                    ApplicationErrorCodes.ReturnBatchNotFound,
                    "Return batch was not found.");
            }

            if (ReturnBatchConcurrency.IsMismatch(batch.UpdatedAtUtc, expectedUpdatedAtUtc))
            {
                return ApplicationResult<ReturnBatch>.Failure(
                    ApplicationErrorCodes.ConcurrencyConflict,
                    "Return batch has changed. Reload before classifying.");
            }

            batch.ClassifyLine(
                ReturnBatchLineId.From(returnBatchLineId),
                sellableQuantity,
                damagedQuantity,
                inspectionNote,
                actorId,
                _clock.UtcNow);
            await _batches.UpdateAsync(batch, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ReturnBatch>.Success(batch);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ReturnBatch>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ReturnBatch>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class FinalizeReturnBatch
{
    private readonly IReturnBatchRepository _batches;
    private readonly ISaleRepository _sales;
    private readonly ISaleMutationLock _saleMutationLock;
    private readonly ISaleReturnRepository _returns;
    private readonly ISaleReturnStockService _returnStock;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository? _branchBalances;
    private readonly ICashierShiftRepository _shifts;
    private readonly ICreditEntryRepository _credits;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public FinalizeReturnBatch(
        IReturnBatchRepository batches,
        ISaleRepository sales,
        ISaleMutationLock saleMutationLock,
        ISaleReturnRepository returns,
        ISaleReturnStockService returnStock,
        IInventoryRepository inventory,
        ICashierShiftRepository shifts,
        ICreditEntryRepository credits,
        IOutstandingBalanceService outstanding,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IInventoryBranchBalanceRepository? branchBalances = null)
    {
        _batches = batches;
        _sales = sales;
        _saleMutationLock = saleMutationLock;
        _returns = returns;
        _returnStock = returnStock;
        _inventory = inventory;
        _shifts = shifts;
        _credits = credits;
        _outstanding = outstanding;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _branchBalances = branchBalances;
    }

    public async Task<ApplicationResult<ReturnBatch>> ExecuteAsync(
        Guid organizationId,
        Guid returnBatchId,
        DateTimeOffset expectedUpdatedAtUtc,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var batchId = ReturnBatchId.From(returnBatchId);
        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var batch = await _batches.GetByIdAsync(orgId, batchId, ct).ConfigureAwait(false);
                if (batch is null)
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        ApplicationErrorCodes.ReturnBatchNotFound,
                        "Return batch was not found.");
                }

                if (batch.Status == ReturnBatchStatus.Finalized)
                {
                    return ApplicationResult<ReturnBatch>.Success(batch);
                }

                if (ReturnBatchConcurrency.IsMismatch(batch.UpdatedAtUtc, expectedUpdatedAtUtc))
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        ApplicationErrorCodes.ConcurrencyConflict,
                        "Return batch has changed. Reload before finalizing.");
                }

                if (batch.SaleId is not { } saleId)
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        DomainErrorCodes.ReturnBatchSourceMismatch,
                        "Connected purchase-order returns are finalized through the connected-return flow.");
                }

                await _saleMutationLock.AcquireAsync(orgId, saleId, ct).ConfigureAwait(false);

                var sale = await _sales.GetByIdAsync(orgId, saleId, ct).ConfigureAwait(false);
                if (sale is null)
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        ApplicationErrorCodes.SaleNotFound,
                        "Sale was not found.");
                }

                if (!batch.AllLinesClassified)
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        DomainErrorCodes.ReturnBatchNotReadyForFinalize,
                        "All lines must be classified before finalizing.");
                }

                var utcNow = _clock.UtcNow;
                await DecreasePendingReturnsAsync(batch, sale, utcNow, ct).ConfigureAwait(false);

                var prior = await _returns.GetPriorTotalsBySaleLineAsync(orgId, saleId, ct).ConfigureAwait(false);
                var priorSnapshot = prior.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (kvp.Value.ReturnedQuantity, kvp.Value.RefundedAmount));
                var otherBatches = await _batches
                    .ListBySaleIdAsync(orgId, saleId, ct)
                    .ConfigureAwait(false);
                var priorFinalized = otherBatches
                    .Where(b => b.Id != batch.Id && b.Status == ReturnBatchStatus.Finalized)
                    .ToList();
                var priorAccepted = priorFinalized.Sum(b => b.AcceptedReturnValue);
                var alreadyRefunded = priorFinalized.Sum(b => b.RefundedAmount);
                var settledPayments = SaleReturnSettlementResolver.ResolveSettledPayments(sale);
                var settlement = ReturnBatchFinancialSettlement.Evaluate(
                    sale.Total,
                    settledPayments,
                    batch.AcceptedReturnValue,
                    priorAccepted,
                    alreadyRefunded);

                var cashShift = sale.PaymentMethod == SalePaymentMethod.Cash
                    ? await _shifts.FindOpenForActorAsync(orgId, actorId, ct).ConfigureAwait(false)
                    : null;
                var shouldRecordCashRefundNow = sale.PaymentMethod == SalePaymentMethod.Cash
                    && settlement.RefundDue > 0m
                    && cashShift is not null;

                CashierShiftId? linkedShiftId = shouldRecordCashRefundNow ? cashShift!.Id : null;
                RegisterId? refundRegisterId = shouldRecordCashRefundNow ? cashShift!.RegisterId : null;

                var saleReturn = await _returns
                    .CreateAsync(
                        orgId,
                        ReturnNumbers.BusinessDateOf(utcNow),
                        number => SaleReturn.CreateCompleted(
                            orgId,
                            number,
                            sale,
                            ToSaleReturnDrafts(batch.Lines),
                            priorSnapshot,
                            batch.Reason,
                            actorId,
                            utcNow,
                            linkedShiftId,
                            refundRegisterId,
                            notes: batch.Notes,
                            requireCashShift: shouldRecordCashRefundNow),
                        async (created, afterCt) =>
                        {
                            await _returnStock
                                .RestockForReturnAsync(orgId, created, sale, actorId, utcNow, afterCt)
                                .ConfigureAwait(false);
                            if (sale.PaymentMethod == SalePaymentMethod.Utang)
                            {
                                await ApplyUtangRefundAsync(orgId, sale, created, afterCt).ConfigureAwait(false);
                            }
                        },
                        ct)
                    .ConfigureAwait(false);

                var refundStatus = ResolveRefundStatus(
                    sale,
                    settlement,
                    shouldRecordCashRefundNow);
                var refundDue = refundStatus == ReturnBatchRefundStatus.RefundDue
                    ? settlement.RefundDue
                    : 0m;
                var refundedAmount = refundStatus == ReturnBatchRefundStatus.Refunded
                    ? settlement.RefundDue
                    : 0m;

                batch.MarkFinalized(
                    saleReturn.Id,
                    refundStatus,
                    refundDue,
                    refundedAmount,
                    actorId,
                    utcNow);

                await _batches.UpdateAsync(batch, ct).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                return ApplicationResult<ReturnBatch>.Success(batch);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ReturnBatch>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ReturnBatch>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task DecreasePendingReturnsAsync(
        ReturnBatch batch,
        Sale sale,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var grouped = batch.Lines
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.AcceptedQuantity));
        if (grouped.Count == 0)
        {
            return;
        }

        var accounts = await _inventory
            .ListByProductIdsAsync(batch.OrganizationId, grouped.Keys.ToList(), cancellationToken)
            .ConfigureAwait(false);
        var trackedByProduct = accounts.Where(a => a.IsTracked).ToDictionary(a => a.ProductId);
        foreach (var (productId, qty) in grouped)
        {
            if (!trackedByProduct.TryGetValue(productId, out var account))
            {
                continue;
            }

            account.DecreasePendingReturn(qty);
            account.Touch(utcNow);
            await _inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
        }

        if (_branchBalances is null || sale.BranchId is null)
        {
            return;
        }

        var branchId = sale.BranchId;
        var balances = await _branchBalances
            .ListByBranchAndProductIdsAsync(batch.OrganizationId, branchId, grouped.Keys.ToList(), cancellationToken)
            .ConfigureAwait(false);
        var byProduct = balances.ToDictionary(b => b.ProductId);
        foreach (var (productId, qty) in grouped)
        {
            if (!trackedByProduct.ContainsKey(productId))
            {
                continue;
            }

            if (!byProduct.TryGetValue(productId, out var balance))
            {
                balance = InventoryBranchBalance.Create(batch.OrganizationId, branchId, productId, 0m, utcNow);
            }

            balance.DecreasePendingReturn(qty, utcNow);
            await _branchBalances.UpsertAsync(balance, cancellationToken).ConfigureAwait(false);
        }
    }

    private IReadOnlyList<SaleReturnLineDraft> ToSaleReturnDrafts(IReadOnlyList<ReturnBatchLine> lines) =>
        lines.Select(l => new SaleReturnLineDraft(
            l.SaleLineId!,
            l.AcceptedQuantity,
            RestockDisposition.ReturnToStock,
            null,
            l.SellableQuantity,
            l.DamagedQuantity)).ToList();

    private static ReturnBatchRefundStatus ResolveRefundStatus(
        Sale sale,
        ReturnBatchFinancialSettlementResult settlement,
        bool shouldRecordCashRefundNow)
    {
        if (sale.PaymentMethod == SalePaymentMethod.Utang)
        {
            return ReturnBatchRefundStatus.CreditReduced;
        }

        if (settlement.RefundDue > 0m)
        {
            if (shouldRecordCashRefundNow)
            {
                return ReturnBatchRefundStatus.Refunded;
            }

            return ReturnBatchRefundStatus.RefundDue;
        }

        if (settlement.ObligationReducedAmount > 0m)
        {
            return ReturnBatchRefundStatus.ObligationReduced;
        }

        return shouldRecordCashRefundNow
            ? ReturnBatchRefundStatus.Refunded
            : ReturnBatchRefundStatus.RefundDue;
    }

    private async Task ApplyUtangRefundAsync(
        PosOrganizationId organizationId,
        Sale sale,
        SaleReturn saleReturn,
        CancellationToken cancellationToken)
    {
        if (sale.LinkedCreditEntryId is null || sale.CustomerId is null)
        {
            throw new DomainException(
                DomainErrorCodes.SaleUtangLinkageInvalid,
                "Utang sale is missing customer or linked credit entry.");
        }

        var credit = await _credits
            .GetByIdAsync(organizationId, sale.CustomerId, sale.LinkedCreditEntryId, cancellationToken)
            .ConfigureAwait(false);
        if (credit is null)
        {
            throw new DomainException(
                ApplicationErrorCodes.CreditEntryNotFound,
                "Linked credit entry was not found.");
        }

        if (credit.SourceSaleId is null || credit.SourceSaleId.Value != sale.Id.Value)
        {
            throw new DomainException(
                DomainErrorCodes.SaleUtangLinkageInvalid,
                "Linked credit entry does not reference this sale.");
        }

        if (credit.Status != CreditEntryStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.SaleReturnUtangOutstandingInsufficient,
                "Linked Utang credit is not active; return refund cannot be applied.");
        }

        var outstanding = await _outstanding
            .GetOutstandingAsync(organizationId, sale.CustomerId, cancellationToken)
            .ConfigureAwait(false);
        if (outstanding < saleReturn.TotalRefundAmount)
        {
            throw new DomainException(
                DomainErrorCodes.SaleReturnUtangOutstandingInsufficient,
                "Outstanding Utang balance is less than the refund amount.");
        }

        credit.ReduceForSaleReturn(saleReturn.TotalRefundAmount, _clock.UtcNow);
        await _credits.UpdateAsync(credit, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class RecordReturnBatchRefund
{
    private readonly IReturnBatchRepository _batches;
    private readonly ISaleMutationLock _saleMutationLock;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RecordReturnBatchRefund(
        IReturnBatchRepository batches,
        ISaleMutationLock saleMutationLock,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _batches = batches;
        _saleMutationLock = saleMutationLock;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ReturnBatch>> ExecuteAsync(
        Guid organizationId,
        Guid returnBatchId,
        decimal amount,
        string method,
        Guid actorId,
        string? reference = null,
        string? note = null,
        string? clientRefundId = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var batchId = ReturnBatchId.From(returnBatchId);
        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var batch = await _batches.GetByIdAsync(orgId, batchId, ct).ConfigureAwait(false);
                if (batch is null)
                {
                    return ApplicationResult<ReturnBatch>.Failure(
                        ApplicationErrorCodes.ReturnBatchNotFound,
                        "Return batch was not found.");
                }

                if (batch.SaleId is { } saleId)
                {
                    await _saleMutationLock.AcquireAsync(orgId, saleId, ct).ConfigureAwait(false);
                }

                batch.RecordRefund(
                    amount,
                    SalePaymentMethods.Parse(method),
                    reference,
                    note,
                    actorId,
                    _clock.UtcNow,
                    clientRefundId);

                await _batches.UpdateAsync(batch, ct).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                return ApplicationResult<ReturnBatch>.Success(batch);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ReturnBatch>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<ReturnBatch>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
