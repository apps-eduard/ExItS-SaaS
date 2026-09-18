using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Returns;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class ReturnBatchEntityMapper
{
    public static ReturnBatch ToDomain(
        ReturnBatchRecord record,
        IEnumerable<ReturnBatchLineRecord> lineRecords,
        IEnumerable<ReturnBatchRefundRecord>? refundRecords = null,
        IEnumerable<ReturnBatchAuditEventRecord>? timelineRecords = null)
    {
        var batchId = ReturnBatchId.From(record.Id);
        var organizationId = PosOrganizationId.From(record.OrganizationId);
        var lines = lineRecords.Select(l => ReturnBatchLine.Rehydrate(
            ReturnBatchLineId.From(l.Id),
            batchId,
            organizationId,
            l.SaleLineId is null ? null : SaleLineId.From(l.SaleLineId.Value),
            CatalogProductId.From(l.ProductId),
            l.ProductNameSnapshot,
            UnitOfMeasures.Parse(l.UomSnapshot),
            l.UnitPriceSnapshot,
            l.LineTotalSnapshot,
            l.AcceptedQuantity,
            l.RefundAmountSnapshot,
            l.SellableQuantity,
            l.DamagedQuantity,
            l.InspectionNote,
            l.ClassifiedAtUtc,
            l.ClassifiedBy,
            l.PurchaseOrderLineId is null ? null : PurchaseOrderLineId.From(l.PurchaseOrderLineId.Value),
            l.SupplierProductId is null ? null : CatalogProductId.From(l.SupplierProductId.Value))).ToList();
        var refunds = (refundRecords ?? [])
            .OrderBy(r => r.CreatedAtUtc)
            .Select(r => ReturnBatchRefund.Rehydrate(
                ReturnBatchRefundId.From(r.Id),
                batchId,
                r.Amount,
                SalePaymentMethods.Parse(r.Method),
                r.Reference,
                r.Note,
                r.ClientRefundId,
                r.CreatedBy,
                r.CreatedAtUtc))
            .ToList();
        var timeline = (timelineRecords ?? [])
            .OrderBy(t => t.CreatedAtUtc)
            .Select(t => ReturnBatchAuditEvent.Rehydrate(
                t.Id,
                batchId,
                ReturnBatchAuditEventTypes.Parse(t.EventType),
                t.PayloadJson,
                t.CreatedAtUtc,
                t.CreatedBy))
            .ToList();

        return ReturnBatch.Rehydrate(
            batchId,
            organizationId,
            record.SaleId is null ? null : SaleId.From(record.SaleId.Value),
            record.BranchId is null ? null : PosBranchId.From(record.BranchId.Value),
            record.BatchNumber,
            ReturnBatchStatuses.Parse(record.Status),
            ReturnBatchRefundStatuses.Parse(record.RefundStatus),
            record.AcceptedReturnValue,
            record.RefundDueAmount,
            record.RefundedAmount,
            record.SaleReturnId is null ? null : SaleReturnId.From(record.SaleReturnId.Value),
            record.CreatedAtUtc,
            record.CreatedBy,
            record.UpdatedAtUtc,
            record.FinalizedAtUtc,
            record.FinalizedBy,
            record.Reason,
            record.Notes,
            lines,
            refunds,
            timeline,
            ReturnBatchSourceTypes.Parse(record.SourceType),
            record.ConnectedPurchaseOrderId is null
                ? null
                : Domain.ConnectedSuppliers.ConnectedPurchaseOrderId.From(record.ConnectedPurchaseOrderId.Value),
            record.PurchaseOrderId is null ? null : Domain.Purchasing.PurchaseOrderId.From(record.PurchaseOrderId.Value),
            record.BuyerOrganizationId is null ? null : PosOrganizationId.From(record.BuyerOrganizationId.Value),
            record.SellerOrganizationId is null ? null : PosOrganizationId.From(record.SellerOrganizationId.Value),
            record.BuyerBranchId is null ? null : PosBranchId.From(record.BuyerBranchId.Value),
            record.SellerBranchId is null ? null : PosBranchId.From(record.SellerBranchId.Value),
            ParsePaymentTiming(record.PaymentTiming),
            record.PoNumberSnapshot,
            record.SellerReceivedAtUtc,
            record.SellerReceivedBy);
    }

    public static ReturnBatchRecord ToRecord(ReturnBatch batch) =>
        new()
        {
            Id = batch.Id.Value,
            OrganizationId = batch.OrganizationId.Value,
            SourceType = ReturnBatchSourceTypes.ToCode(batch.SourceType),
            SaleId = batch.SaleId?.Value,
            BranchId = batch.BranchId?.Value,
            ConnectedPurchaseOrderId = batch.ConnectedPurchaseOrderId?.Value,
            PurchaseOrderId = batch.PurchaseOrderId?.Value,
            BuyerOrganizationId = batch.BuyerOrganizationId?.Value,
            SellerOrganizationId = batch.SellerOrganizationId?.Value,
            BuyerBranchId = batch.BuyerBranchId?.Value,
            SellerBranchId = batch.SellerBranchId?.Value,
            PaymentTiming = batch.PaymentTimingSnapshot?.ToString(),
            PoNumberSnapshot = batch.PoNumberSnapshot,
            BatchNumber = batch.BatchNumber,
            Status = ReturnBatchStatuses.ToCode(batch.Status),
            RefundStatus = ReturnBatchRefundStatuses.ToCode(batch.RefundStatus),
            AcceptedReturnValue = batch.AcceptedReturnValue,
            RefundDueAmount = batch.RefundDueAmount,
            RefundedAmount = batch.RefundedAmount,
            SaleReturnId = batch.SaleReturnId?.Value,
            Reason = batch.Reason,
            Notes = batch.Notes,
            CreatedAtUtc = batch.CreatedAtUtc,
            CreatedBy = batch.CreatedBy,
            UpdatedAtUtc = batch.UpdatedAtUtc,
            SellerReceivedAtUtc = batch.SellerReceivedAtUtc,
            SellerReceivedBy = batch.SellerReceivedBy,
            FinalizedAtUtc = batch.FinalizedAtUtc,
            FinalizedBy = batch.FinalizedBy
        };

    public static ReturnBatchLineRecord ToRecord(ReturnBatchLine line) =>
        new()
        {
            Id = line.Id.Value,
            ReturnBatchId = line.ReturnBatchId.Value,
            OrganizationId = line.OrganizationId.Value,
            SaleLineId = line.SaleLineId?.Value,
            PurchaseOrderLineId = line.PurchaseOrderLineId?.Value,
            ProductId = line.ProductId.Value,
            SupplierProductId = line.SupplierProductId?.Value,
            ProductNameSnapshot = line.ProductNameSnapshot,
            UomSnapshot = UnitOfMeasures.ToCode(line.UomSnapshot),
            UnitPriceSnapshot = line.UnitPriceSnapshot,
            LineTotalSnapshot = line.LineTotalSnapshot,
            AcceptedQuantity = line.AcceptedQuantity,
            RefundAmountSnapshot = line.RefundAmountSnapshot,
            SellableQuantity = line.SellableQuantity,
            DamagedQuantity = line.DamagedQuantity,
            InspectionNote = line.InspectionNote,
            ClassifiedAtUtc = line.ClassifiedAtUtc,
            ClassifiedBy = line.ClassifiedBy
        };

    public static ReturnBatchRefundRecord ToRecord(ReturnBatchRefund refund) =>
        new()
        {
            Id = refund.Id.Value,
            ReturnBatchId = refund.ReturnBatchId.Value,
            OrganizationId = Guid.Empty, // set by repository from aggregate owner
            Amount = refund.Amount,
            Method = SalePaymentMethods.ToCode(refund.Method),
            Reference = refund.Reference,
            Note = refund.Note,
            ClientRefundId = refund.ClientRefundId,
            CreatedAtUtc = refund.CreatedAtUtc,
            CreatedBy = refund.CreatedBy
        };

    public static ReturnBatchAuditEventRecord ToRecord(ReturnBatchAuditEvent auditEvent) =>
        new()
        {
            Id = auditEvent.Id,
            ReturnBatchId = auditEvent.ReturnBatchId.Value,
            OrganizationId = Guid.Empty, // set by repository from aggregate owner
            EventType = ReturnBatchAuditEventTypes.ToCode(auditEvent.EventType),
            PayloadJson = auditEvent.PayloadJson,
            CreatedAtUtc = auditEvent.CreatedAtUtc,
            CreatedBy = auditEvent.CreatedBy
        };

    public static void ApplyToRecord(ReturnBatch batch, ReturnBatchRecord record)
    {
        record.Status = ReturnBatchStatuses.ToCode(batch.Status);
        record.RefundStatus = ReturnBatchRefundStatuses.ToCode(batch.RefundStatus);
        record.RefundDueAmount = batch.RefundDueAmount;
        record.RefundedAmount = batch.RefundedAmount;
        record.SaleReturnId = batch.SaleReturnId?.Value;
        record.UpdatedAtUtc = batch.UpdatedAtUtc;
        record.SellerReceivedAtUtc = batch.SellerReceivedAtUtc;
        record.SellerReceivedBy = batch.SellerReceivedBy;
        record.FinalizedAtUtc = batch.FinalizedAtUtc;
        record.FinalizedBy = batch.FinalizedBy;
    }

    public static void ApplyLineToRecord(ReturnBatchLine line, ReturnBatchLineRecord record)
    {
        record.SellableQuantity = line.SellableQuantity;
        record.DamagedQuantity = line.DamagedQuantity;
        record.InspectionNote = line.InspectionNote;
        record.ClassifiedAtUtc = line.ClassifiedAtUtc;
        record.ClassifiedBy = line.ClassifiedBy;
    }

    private static ConnectedPoPaymentTiming? ParsePaymentTiming(string? code) =>
        string.IsNullOrWhiteSpace(code)
            ? null
            : Enum.TryParse<ConnectedPoPaymentTiming>(code.Trim(), ignoreCase: true, out var timing)
                ? timing
                : null;
}
