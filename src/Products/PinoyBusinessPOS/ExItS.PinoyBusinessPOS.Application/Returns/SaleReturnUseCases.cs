using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Common;
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
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Returns;

public sealed class SaleReturnQueryService
{
    private readonly ISaleReturnRepository _returns;
    private readonly ISaleRepository _sales;

    public SaleReturnQueryService(ISaleReturnRepository returns, ISaleRepository sales)
    {
        _returns = returns;
        _sales = sales;
    }

    public async Task<PosSaleReturnDto?> GetByIdAsync(
        Guid organizationId,
        Guid returnId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var saleReturn = await _returns
            .GetByIdAsync(orgId, SaleReturnId.From(returnId), cancellationToken)
            .ConfigureAwait(false);
        return saleReturn is null ? null : Map(saleReturn);
    }

    public async Task<PagedResult<PosSaleReturnDto>> ListAsync(
        Guid organizationId,
        SaleReturnFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _returns
            .ListAsync(PosOrganizationId.From(organizationId), filter, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<PosSaleReturnDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<PosRefundableSaleDto?> GetRefundableAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var id = SaleId.From(saleId);
        var sale = await _sales.GetByIdAsync(orgId, id, cancellationToken).ConfigureAwait(false);
        if (sale is null)
        {
            return null;
        }

        if (sale.Status != SaleStatus.Completed)
        {
            return new PosRefundableSaleDto(
                sale.Id.Value,
                sale.SaleNumber,
                SalePaymentMethods.ToCode(sale.PaymentMethod),
                sale.Status.ToString(),
                []);
        }

        var prior = await _returns
            .GetPriorTotalsBySaleLineAsync(orgId, id, cancellationToken)
            .ConfigureAwait(false);

        var lines = sale.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l =>
            {
                prior.TryGetValue(l.Id.Value, out var totals);
                var returnedQty = totals?.ReturnedQuantity ?? 0m;
                var refundedAmt = totals?.RefundedAmount ?? 0m;
                var refundableQty = SaleReturnRefundable.RefundableQuantity(l, returnedQty);
                var refundableAmount = SaleReturnRefundable.RefundableAmount(l, refundedAmt);
                return new PosRefundableSaleLineDto(
                    l.Id.Value,
                    l.ProductId.Value,
                    l.NameSnapshot,
                    UnitOfMeasures.ToCode(l.UnitOfMeasureSnapshot),
                    l.Quantity,
                    l.UnitPrice,
                    l.LineTotal,
                    returnedQty,
                    refundableQty,
                    refundedAmt,
                    refundableAmount);
            })
            .Where(l => l.RefundableQuantity > 0m && l.RefundableAmount > 0m)
            .ToList();

        return new PosRefundableSaleDto(
            sale.Id.Value,
            sale.SaleNumber,
            SalePaymentMethods.ToCode(sale.PaymentMethod),
            sale.Status.ToString(),
            lines);
    }

    public static PosSaleReturnDto Map(SaleReturn saleReturn) =>
        new(
            saleReturn.Id.Value,
            saleReturn.OrganizationId.Value,
            saleReturn.ReturnNumber,
            saleReturn.SaleId.Value,
            SalePaymentMethods.ToCode(saleReturn.RefundMethod),
            SaleReturnStatuses.ToCode(saleReturn.Status),
            saleReturn.ReturnDate,
            saleReturn.Reason,
            saleReturn.Notes,
            saleReturn.TotalRefundAmount,
            saleReturn.CreatedAtUtc,
            saleReturn.CreatedBy,
            saleReturn.CompletedAtUtc,
            saleReturn.CashierShiftId?.Value,
            saleReturn.Lines
                .Select(l => new PosSaleReturnLineDto(
                    l.Id.Value,
                    l.SaleLineId.Value,
                    l.ProductId.Value,
                    l.ProductNameSnapshot,
                    UnitOfMeasures.ToCode(l.UomSnapshot),
                    l.QuantityReturned,
                    l.UnitPriceSnapshot,
                    l.RefundAmount,
                    RestockDispositions.ToCode(l.RestockDisposition),
                    l.LineReason,
                    l.InventoryMovementId))
                .ToList());
}

/// <summary>
/// Records a completed sale return atomically with optional restock and tender-specific refund side effects.
/// Online-only; refund method always matches the originating sale.
/// </summary>
public sealed class ProcessSaleReturn
{
    private readonly ISaleReturnRepository _returns;
    private readonly ISaleRepository _sales;
    private readonly ICashierShiftRepository _shifts;
    private readonly ICreditEntryRepository _credits;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly ISaleReturnStockService _returnStock;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ProcessSaleReturn(
        ISaleReturnRepository returns,
        ISaleRepository sales,
        ICashierShiftRepository shifts,
        ICreditEntryRepository credits,
        IOutstandingBalanceService outstanding,
        ISaleReturnStockService returnStock,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _returns = returns;
        _sales = sales;
        _shifts = shifts;
        _credits = credits;
        _outstanding = outstanding;
        _returnStock = returnStock;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<SaleReturn>> ExecuteAsync(
        Guid organizationId,
        Guid saleId,
        string reason,
        IReadOnlyList<CreateSaleReturnLineRequest>? lines,
        Guid actorId,
        string? notes = null,
        Guid? clientReturnId = null,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<SaleReturn>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to process a return.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var id = SaleId.From(saleId);

        try
        {
            if (clientReturnId is not null)
            {
                var existing = await _returns
                    .GetByIdAsync(orgId, SaleReturnId.From(clientReturnId.Value), cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null)
                {
                    return ApplicationResult<SaleReturn>.Success(existing);
                }
            }

            if (lines is null || lines.Count == 0)
            {
                return ApplicationResult<SaleReturn>.Failure(
                    DomainErrorCodes.SaleReturnRequiresAtLeastOneLine,
                    "A return must contain at least one line.");
            }

            var sale = await _sales.GetByIdAsync(orgId, id, cancellationToken).ConfigureAwait(false);
            if (sale is null)
            {
                return ApplicationResult<SaleReturn>.Failure(
                    ApplicationErrorCodes.SaleNotFound,
                    "Sale was not found.");
            }

            CashierShiftId? linkedShiftId = null;
            RegisterId? refundRegisterId = null;
            if (sale.PaymentMethod == SalePaymentMethod.Cash)
            {
                var openShift = await _shifts
                    .FindOpenForActorAsync(orgId, actorId, cancellationToken)
                    .ConfigureAwait(false);
                if (openShift is null)
                {
                    return ApplicationResult<SaleReturn>.Failure(
                        ApplicationErrorCodes.CashierShiftNoOpenShift,
                        "Cash refunds require an open cashier shift for this actor.");
                }

                linkedShiftId = openShift.Id;
                refundRegisterId = openShift.RegisterId;
            }

            var lineDrafts = lines
                .Select(l => new SaleReturnLineDraft(
                    SaleLineId.From(l.SaleLineId),
                    l.Quantity,
                    RestockDispositions.Parse(l.RestockDisposition),
                    l.LineReason))
                .ToList();

            var prior = await _returns
                .GetPriorTotalsBySaleLineAsync(orgId, id, cancellationToken)
                .ConfigureAwait(false);

            var utcNow = _clock.UtcNow;
            var capturedSale = sale;
            var capturedPrior = prior.ToDictionary(
                kvp => kvp.Key,
                kvp => (kvp.Value.ReturnedQuantity, kvp.Value.RefundedAmount));
            var capturedShiftId = linkedShiftId;
            var capturedRefundRegisterId = refundRegisterId;
            var capturedActorId = actorId;

            var saleReturn = await _returns
                .CreateAsync(
                    orgId,
                    ReturnNumbers.BusinessDateOf(utcNow),
                    returnNumber => SaleReturn.CreateCompleted(
                        orgId,
                        returnNumber,
                        capturedSale,
                        lineDrafts,
                        capturedPrior,
                        reason,
                        capturedActorId,
                        utcNow,
                        capturedShiftId,
                        capturedRefundRegisterId,
                        notes: notes,
                        id: clientReturnId is null ? null : SaleReturnId.From(clientReturnId.Value)),
                    async (created, ct) =>
                    {
                        await _returnStock
                            .RestockForReturnAsync(orgId, created, capturedActorId, utcNow, ct)
                            .ConfigureAwait(false);

                        if (capturedSale.PaymentMethod == SalePaymentMethod.Utang)
                        {
                            await ApplyUtangRefundAsync(orgId, capturedSale, created, ct).ConfigureAwait(false);
                        }

                        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            return ApplicationResult<SaleReturn>.Success(saleReturn);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaleReturn>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<SaleReturn>.Failure(ex.ErrorCode, ex.Message);
        }
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
