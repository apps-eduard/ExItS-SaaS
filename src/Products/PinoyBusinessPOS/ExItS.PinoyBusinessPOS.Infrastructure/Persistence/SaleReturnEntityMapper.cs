using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Returns;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class SaleReturnEntityMapper
{
    public static SaleReturn ToDomain(SaleReturnRecord record, IEnumerable<SaleReturnLineRecord> lineRecords)
    {
        var returnId = SaleReturnId.From(record.Id);
        var organizationId = PosOrganizationId.From(record.OrganizationId);

        var lines = lineRecords
            .Select(l => SaleReturnLine.Rehydrate(
                SaleReturnLineId.From(l.Id),
                returnId,
                organizationId,
                SaleLineId.From(l.SaleLineId),
                CatalogProductId.From(l.ProductId),
                l.ProductNameSnapshot,
                UnitOfMeasures.Parse(l.UomSnapshot),
                l.QuantityReturned,
                l.UnitPriceSnapshot,
                l.RefundAmount,
                RestockDispositions.Parse(l.RestockDisposition),
                l.LineReason,
                l.InventoryMovementId))
            .ToList();

        return SaleReturn.Rehydrate(
            returnId,
            organizationId,
            record.ReturnNumber,
            SaleId.From(record.SaleId),
            record.CashierShiftId is null ? null : CashierShiftId.From(record.CashierShiftId.Value),
            record.SourceRegisterId is null ? null : RegisterId.From(record.SourceRegisterId.Value),
            record.RefundRegisterId is null ? null : RegisterId.From(record.RefundRegisterId.Value),
            SalePaymentMethods.Parse(record.RefundMethod),
            SaleReturnStatuses.Parse(record.Status),
            record.ReturnDate,
            record.Reason,
            record.Notes,
            record.TotalRefundAmount,
            record.CreatedAtUtc,
            record.CreatedBy,
            record.CompletedAtUtc,
            lines);
    }

    public static SaleReturnRecord ToRecord(SaleReturn saleReturn) =>
        new()
        {
            Id = saleReturn.Id.Value,
            OrganizationId = saleReturn.OrganizationId.Value,
            ReturnNumber = saleReturn.ReturnNumber,
            SaleId = saleReturn.SaleId.Value,
            CashierShiftId = saleReturn.CashierShiftId?.Value,
            SourceRegisterId = saleReturn.SourceRegisterId?.Value,
            RefundRegisterId = saleReturn.RefundRegisterId?.Value,
            RefundMethod = SalePaymentMethods.ToCode(saleReturn.RefundMethod),
            Status = SaleReturnStatuses.ToCode(saleReturn.Status),
            ReturnDate = saleReturn.ReturnDate,
            Reason = saleReturn.Reason,
            Notes = saleReturn.Notes,
            TotalRefundAmount = saleReturn.TotalRefundAmount,
            CreatedAtUtc = saleReturn.CreatedAtUtc,
            CreatedBy = saleReturn.CreatedBy,
            CompletedAtUtc = saleReturn.CompletedAtUtc
        };

    public static SaleReturnLineRecord ToRecord(SaleReturnLine line) =>
        new()
        {
            Id = line.Id.Value,
            SaleReturnId = line.SaleReturnId.Value,
            OrganizationId = line.OrganizationId.Value,
            SaleLineId = line.SaleLineId.Value,
            ProductId = line.ProductId.Value,
            ProductNameSnapshot = line.ProductNameSnapshot,
            UomSnapshot = UnitOfMeasures.ToCode(line.UomSnapshot),
            QuantityReturned = line.QuantityReturned,
            UnitPriceSnapshot = line.UnitPriceSnapshot,
            RefundAmount = line.RefundAmount,
            RestockDisposition = RestockDispositions.ToCode(line.RestockDisposition),
            LineReason = line.LineReason,
            InventoryMovementId = line.InventoryMovementId
        };

    public static void ApplyLineInventoryMovement(SaleReturnLine line, SaleReturnLineRecord record) =>
        record.InventoryMovementId = line.InventoryMovementId;
}
