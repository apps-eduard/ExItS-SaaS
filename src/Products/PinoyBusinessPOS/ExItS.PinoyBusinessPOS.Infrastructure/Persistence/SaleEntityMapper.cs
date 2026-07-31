using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class SaleEntityMapper
{
    public static Sale ToDomain(SaleRecord record, IEnumerable<SaleLineRecord> lineRecords)
    {
        var saleId = SaleId.From(record.Id);
        var organizationId = PosOrganizationId.From(record.OrganizationId);

        var lines = lineRecords
            .OrderBy(l => l.LineNumber)
            .Select(l => SaleLine.Rehydrate(
                SaleLineId.From(l.Id),
                saleId,
                organizationId,
                CatalogProductId.From(l.ProductId),
                l.LineNumber,
                l.NameSnapshot,
                l.SkuSnapshot,
                l.BarcodeSnapshot,
                UnitOfMeasures.Parse(l.UnitOfMeasureSnapshot),
                l.UnitPrice,
                l.Quantity,
                l.LineTotal))
            .ToList();

        return Sale.Rehydrate(
            saleId,
            organizationId,
            record.SaleNumber,
            Enum.Parse<SaleStatus>(record.Status, ignoreCase: true),
            SalePaymentMethods.Parse(record.PaymentMethod),
            record.Subtotal,
            record.Total,
            record.AmountTendered,
            record.ChangeAmount,
            record.GcashReference,
            record.RecordedAtUtc,
            record.RecordedBy,
            record.VoidedAtUtc,
            record.VoidedBy,
            record.VoidReason,
            record.UpdatedAtUtc,
            lines,
            record.CustomerId is null ? null : POSCustomerId.From(record.CustomerId.Value),
            record.LinkedCreditEntryId is null ? null : CreditEntryId.From(record.LinkedCreditEntryId.Value),
            record.CashierShiftId is null ? null : CashierShiftId.From(record.CashierShiftId.Value));
    }

    public static SaleRecord ToRecord(Sale sale) =>
        new()
        {
            Id = sale.Id.Value,
            OrganizationId = sale.OrganizationId.Value,
            SaleNumber = sale.SaleNumber,
            Status = sale.Status.ToString(),
            PaymentMethod = SalePaymentMethods.ToCode(sale.PaymentMethod),
            Subtotal = sale.Subtotal,
            Total = sale.Total,
            AmountTendered = sale.AmountTendered,
            ChangeAmount = sale.ChangeAmount,
            GcashReference = sale.GCashReference,
            CustomerId = sale.CustomerId?.Value,
            LinkedCreditEntryId = sale.LinkedCreditEntryId?.Value,
            CashierShiftId = sale.CashierShiftId?.Value,
            RecordedAtUtc = sale.RecordedAtUtc,
            RecordedBy = sale.RecordedBy,
            VoidedAtUtc = sale.VoidedAtUtc,
            VoidedBy = sale.VoidedBy,
            VoidReason = sale.VoidReason,
            UpdatedAtUtc = sale.UpdatedAtUtc
        };

    public static SaleLineRecord ToRecord(SaleLine line) =>
        new()
        {
            Id = line.Id.Value,
            SaleId = line.SaleId.Value,
            OrganizationId = line.OrganizationId.Value,
            ProductId = line.ProductId.Value,
            LineNumber = line.LineNumber,
            NameSnapshot = line.NameSnapshot,
            SkuSnapshot = line.SkuSnapshot,
            BarcodeSnapshot = line.BarcodeSnapshot,
            UnitOfMeasureSnapshot = UnitOfMeasures.ToCode(line.UnitOfMeasureSnapshot),
            UnitPrice = line.UnitPrice,
            Quantity = line.Quantity,
            LineTotal = line.LineTotal
        };

    /// <summary>
    /// Applies the only mutable part of a recorded sale: the void outcome. Financial fields, lines,
    /// the sale number and the organization are never rewritten from the aggregate.
    /// </summary>
    public static void ApplyToRecord(Sale sale, SaleRecord record)
    {
        record.Status = sale.Status.ToString();
        record.VoidedAtUtc = sale.VoidedAtUtc;
        record.VoidedBy = sale.VoidedBy;
        record.VoidReason = sale.VoidReason;
        record.UpdatedAtUtc = sale.UpdatedAtUtc;
    }
}
