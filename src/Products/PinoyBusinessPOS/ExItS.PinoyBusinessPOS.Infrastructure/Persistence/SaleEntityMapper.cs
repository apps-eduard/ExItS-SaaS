using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class SaleEntityMapper
{
    public static Sale ToDomain(
        SaleRecord record,
        IEnumerable<SaleLineRecord> lineRecords,
        IEnumerable<SaleCommercialDiscountAdjustmentRecord>? discountRecords = null,
        IEnumerable<SalePriceOverrideAdjustmentRecord>? priceOverrideRecords = null)
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
                l.LineTotal,
                SellingModes.Parse(l.SellingModeSnapshot),
                l.SellingUnitId is null ? null : ProductUnitId.From(l.SellingUnitId.Value),
                l.SellingUnitNameSnapshot,
                l.EnteredQuantity,
                l.MultiplierToBaseSnapshot,
                l.GrossLineTotal,
                l.LineDiscountAmount,
                l.SaleDiscountAllocatedAmount))
            .ToList();

        var discounts = discountRecords?
            .OrderBy(d => d.RecordedAtUtc)
            .Select(d => SaleCommercialDiscountAdjustment.Rehydrate(
                SaleCommercialDiscountAdjustmentId.From(d.Id),
                saleId,
                organizationId,
                SaleCommercialDiscountRules.ParseScope(d.Scope),
                SaleCommercialDiscountRules.ParseMethod(d.Method),
                SaleCommercialDiscountRules.ParseSource(d.Source),
                d.RequestedValue,
                d.CalculatedAmount,
                d.Reason,
                d.SaleLineId is null ? null : SaleLineId.From(d.SaleLineId.Value),
                d.AppliedBy,
                d.RecordedAtUtc))
            .ToList();

        var priceOverrides = priceOverrideRecords?
            .OrderBy(d => d.RecordedAtUtc)
            .Select(d => SalePriceOverrideAdjustment.Rehydrate(
                SalePriceOverrideAdjustmentId.From(d.Id),
                saleId,
                organizationId,
                SaleLineId.From(d.SaleLineId),
                d.BaselineUnitPrice,
                d.AppliedUnitPrice,
                d.Reason,
                d.AppliedBy,
                d.RecordedAtUtc))
            .ToList();

        return Sale.Rehydrate(
            saleId,
            organizationId,
            record.SaleNumber,
            Enum.Parse<SaleStatus>(record.Status, ignoreCase: true),
            SalePaymentMethods.Parse(record.PaymentMethod),
            record.Subtotal,
            record.Total,
            record.TaxAmount,
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
            record.CashierShiftId is null ? null : CashierShiftId.From(record.CashierShiftId.Value),
            record.RegisterId is null ? null : RegisterId.From(record.RegisterId.Value),
            SaleBuyerParty.Rehydrate(
                SaleBuyerParty.ParseKind(record.BuyerPartyKind),
                record.BuyerDisplayNameSnapshot,
                record.BuyerPersonalPublicUserId,
                record.BuyerOrganizationId,
                record.BuyerPublicOrganizationId),
            Enum.Parse<SaleStockReservationState>(record.StockReservationState, ignoreCase: true),
            record.BranchId is null ? null : PosBranchId.From(record.BranchId.Value),
            record.GrossSubtotal,
            record.LineDiscountTotal,
            record.SaleDiscountTotal,
            discounts,
            priceOverrides);
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
            TaxAmount = sale.TaxAmount,
            GrossSubtotal = sale.GrossSubtotal,
            LineDiscountTotal = sale.LineDiscountTotal,
            SaleDiscountTotal = sale.SaleDiscountTotal,
            DiscountTotal = sale.DiscountTotal,
            AmountTendered = sale.AmountTendered,
            ChangeAmount = sale.ChangeAmount,
            GcashReference = sale.GCashReference,
            CustomerId = sale.CustomerId?.Value,
            BuyerPartyKind = SaleBuyerParty.ToCode(sale.BuyerParty.Kind),
            BuyerDisplayNameSnapshot = sale.BuyerParty.DisplayNameSnapshot,
            BuyerPersonalPublicUserId = sale.BuyerParty.PersonalPublicUserId,
            BuyerOrganizationId = sale.BuyerParty.BuyerOrganizationId,
            BuyerPublicOrganizationId = sale.BuyerParty.BuyerPublicOrganizationId,
            LinkedCreditEntryId = sale.LinkedCreditEntryId?.Value,
            CashierShiftId = sale.CashierShiftId?.Value,
            RegisterId = sale.RegisterId?.Value,
            BranchId = sale.BranchId?.Value,
            StockReservationState = sale.StockReservationState.ToString(),
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
            SellingModeSnapshot = SellingModes.ToCode(line.SellingModeSnapshot),
            UnitPrice = line.UnitPrice,
            Quantity = line.Quantity,
            LineTotal = line.LineTotal,
            GrossLineTotal = line.GrossLineTotal,
            LineDiscountAmount = line.LineDiscountAmount,
            SaleDiscountAllocatedAmount = line.SaleDiscountAllocatedAmount,
            SellingUnitId = line.SellingUnitId?.Value,
            SellingUnitNameSnapshot = line.SellingUnitNameSnapshot,
            EnteredQuantity = line.EnteredQuantity,
            MultiplierToBaseSnapshot = line.MultiplierToBaseSnapshot
        };

    public static SaleCommercialDiscountAdjustmentRecord ToRecord(SaleCommercialDiscountAdjustment adjustment) =>
        new()
        {
            Id = adjustment.Id.Value,
            SaleId = adjustment.SaleId.Value,
            OrganizationId = adjustment.OrganizationId.Value,
            Scope = SaleCommercialDiscountRules.ToCode(adjustment.Scope),
            Method = SaleCommercialDiscountRules.ToCode(adjustment.Method),
            Source = SaleCommercialDiscountRules.ToCode(adjustment.Source),
            RequestedValue = adjustment.RequestedValue,
            CalculatedAmount = adjustment.CalculatedAmount,
            Reason = adjustment.Reason,
            SaleLineId = adjustment.SaleLineId?.Value,
            AppliedBy = adjustment.AppliedBy,
            RecordedAtUtc = adjustment.RecordedAtUtc
        };

    public static SalePriceOverrideAdjustmentRecord ToRecord(SalePriceOverrideAdjustment adjustment) =>
        new()
        {
            Id = adjustment.Id.Value,
            SaleId = adjustment.SaleId.Value,
            OrganizationId = adjustment.OrganizationId.Value,
            SaleLineId = adjustment.SaleLineId.Value,
            BaselineUnitPrice = adjustment.BaselineUnitPrice,
            AppliedUnitPrice = adjustment.AppliedUnitPrice,
            Reason = adjustment.Reason,
            AppliedBy = adjustment.AppliedBy,
            RecordedAtUtc = adjustment.RecordedAtUtc
        };

    /// <summary>
    /// Applies mutable sale outcomes after checkout: void audit, awaiting→completed finalize,
    /// and safe provider/manual payment reference. Financial lines and identity fields stay fixed.
    /// </summary>
    public static void ApplyToRecord(Sale sale, SaleRecord record)
    {
        record.Status = sale.Status.ToString();
        record.GcashReference = sale.GCashReference;
        record.VoidedAtUtc = sale.VoidedAtUtc;
        record.VoidedBy = sale.VoidedBy;
        record.VoidReason = sale.VoidReason;
        record.StockReservationState = sale.StockReservationState.ToString();
        record.UpdatedAtUtc = sale.UpdatedAtUtc;
    }
}
