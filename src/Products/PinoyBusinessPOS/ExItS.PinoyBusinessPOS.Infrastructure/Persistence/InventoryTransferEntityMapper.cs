using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class InventoryTransferEntityMapper
{
    public static InventoryTransfer ToDomain(
        InventoryTransferRecord record,
        IReadOnlyList<InventoryTransferLineRecord> lines,
        IReadOnlyList<InventoryTransferReceiptRecord>? receipts = null,
        IReadOnlyList<InventoryTransferReceiptLineRecord>? receiptLines = null)
    {
        var receiptDomains = BuildReceipts(receipts, receiptLines);
        return InventoryTransfer.Rehydrate(
            InventoryTransferId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.StockRequestId is null ? null : StockRequestId.From(record.StockRequestId.Value),
            record.TransferNumber,
            PosBranchId.From(record.SourceBranchId),
            PosBranchId.From(record.DestinationBranchId),
            InventoryTransferStatuses.Parse(record.Status),
            record.Notes,
            record.CreatedBy,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.DispatchedAtUtc,
            record.DispatchedBy,
            record.ReceivedAtUtc,
            record.ReceivedBy,
            record.CancelledAtUtc,
            record.CancelledBy,
            record.ClosedAtUtc,
            record.ClosedBy,
            lines.OrderBy(l => l.LineNumber).Select(ToDomain).ToList(),
            receiptDomains);
    }

    private static IReadOnlyList<InventoryTransferReceipt> BuildReceipts(
        IReadOnlyList<InventoryTransferReceiptRecord>? receipts,
        IReadOnlyList<InventoryTransferReceiptLineRecord>? receiptLines)
    {
        if (receipts is null || receipts.Count == 0)
        {
            return [];
        }

        var linesByReceipt = (receiptLines ?? [])
            .GroupBy(l => l.ReceiptId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return receipts
            .OrderBy(r => r.Sequence)
            .Select(r => InventoryTransferReceipt.Rehydrate(
                InventoryTransferReceiptId.From(r.Id),
                PosOrganizationId.From(r.OrganizationId),
                InventoryTransferId.From(r.TransferId),
                r.Sequence,
                r.ReceivedAtUtc,
                r.ReceivedBy,
                linesByReceipt.TryGetValue(r.Id, out var lines)
                    ? lines.Select(ToDomain).ToList()
                    : []))
            .ToList();
    }

    public static InventoryTransferLine ToDomain(InventoryTransferLineRecord record) =>
        InventoryTransferLine.Rehydrate(
            InventoryTransferLineId.From(record.Id),
            InventoryTransferId.From(record.TransferId),
            PosOrganizationId.From(record.OrganizationId),
            CatalogProductId.From(record.ProductId),
            record.LineNumber,
            record.NameSnapshot,
            UnitOfMeasures.Parse(record.UnitOfMeasure),
            record.SentQty,
            record.ReceivedQty,
            string.IsNullOrWhiteSpace(record.DiscrepancyReason)
                ? null
                : InventoryTransferDiscrepancyReasons.Parse(record.DiscrepancyReason),
            record.DiscrepancyNote,
            record.SourceLotId is null ? null : InventoryLotId.From(record.SourceLotId.Value),
            record.LotNumber,
            record.ExpirationDate,
            record.UnitCostSnapshot,
            record.ClosedQty,
            record.WaivedQty);

    public static InventoryTransferReceiptLine ToDomain(InventoryTransferReceiptLineRecord record) =>
        InventoryTransferReceiptLine.Rehydrate(
            InventoryTransferReceiptLineId.From(record.Id),
            InventoryTransferReceiptId.From(record.ReceiptId),
            InventoryTransferLineId.From(record.TransferLineId),
            CatalogProductId.From(record.ProductId),
            record.QuantityReceived,
            record.QuantityDamaged,
            record.QuantityMissing,
            record.QuantityOther,
            record.OtherReasonCode,
            record.OtherReasonNote,
            string.IsNullOrWhiteSpace(record.MissingDisposition)
                ? null
                : InventoryTransferMissingDispositions.Parse(record.MissingDisposition),
            string.IsNullOrWhiteSpace(record.DamagedFollowUp)
                ? null
                : InventoryTransferDiscrepancyFollowUps.Parse(record.DamagedFollowUp),
            string.IsNullOrWhiteSpace(record.OtherFollowUp)
                ? null
                : InventoryTransferDiscrepancyFollowUps.Parse(record.OtherFollowUp),
            record.QuantityWaived,
            record.Note);

    public static InventoryTransferRecord ToRecord(InventoryTransfer transfer) =>
        new()
        {
            Id = transfer.Id.Value,
            OrganizationId = transfer.OrganizationId.Value,
            StockRequestId = transfer.StockRequestId?.Value,
            TransferNumber = transfer.TransferNumber,
            SourceBranchId = transfer.SourceBranchId.Value,
            DestinationBranchId = transfer.DestinationBranchId.Value,
            Status = InventoryTransferStatuses.ToCode(transfer.Status),
            Notes = transfer.Notes,
            CreatedBy = transfer.CreatedBy,
            CreatedAtUtc = transfer.CreatedAtUtc,
            UpdatedAtUtc = transfer.UpdatedAtUtc,
            DispatchedAtUtc = transfer.DispatchedAtUtc,
            DispatchedBy = transfer.DispatchedBy,
            ReceivedAtUtc = transfer.ReceivedAtUtc,
            ReceivedBy = transfer.ReceivedBy,
            CancelledAtUtc = transfer.CancelledAtUtc,
            CancelledBy = transfer.CancelledBy,
            ClosedAtUtc = transfer.ClosedAtUtc,
            ClosedBy = transfer.ClosedBy
        };

    public static void ApplyToRecord(InventoryTransfer transfer, InventoryTransferRecord record)
    {
        record.TransferNumber = transfer.TransferNumber;
        record.StockRequestId = transfer.StockRequestId?.Value;
        record.Status = InventoryTransferStatuses.ToCode(transfer.Status);
        record.Notes = transfer.Notes;
        record.UpdatedAtUtc = transfer.UpdatedAtUtc;
        record.DispatchedAtUtc = transfer.DispatchedAtUtc;
        record.DispatchedBy = transfer.DispatchedBy;
        record.ReceivedAtUtc = transfer.ReceivedAtUtc;
        record.ReceivedBy = transfer.ReceivedBy;
        record.CancelledAtUtc = transfer.CancelledAtUtc;
        record.CancelledBy = transfer.CancelledBy;
        record.ClosedAtUtc = transfer.ClosedAtUtc;
        record.ClosedBy = transfer.ClosedBy;
    }

    public static InventoryTransferLineRecord ToRecord(InventoryTransferLine line) =>
        new()
        {
            Id = line.Id.Value,
            TransferId = line.TransferId.Value,
            OrganizationId = line.OrganizationId.Value,
            ProductId = line.ProductId.Value,
            LineNumber = line.LineNumber,
            NameSnapshot = line.NameSnapshot,
            UnitOfMeasure = line.UnitOfMeasure.ToString(),
            SentQty = line.SentQty,
            ReceivedQty = line.ReceivedQty,
            ClosedQty = line.ClosedQty,
            WaivedQty = line.WaivedQty,
            DiscrepancyReason = line.DiscrepancyReason is null
                ? null
                : InventoryTransferDiscrepancyReasons.ToCode(line.DiscrepancyReason.Value),
            DiscrepancyNote = line.DiscrepancyNote,
            SourceLotId = line.SourceLotId?.Value,
            LotNumber = line.LotNumber,
            ExpirationDate = line.ExpirationDate,
            UnitCostSnapshot = line.UnitCostSnapshot
        };

    public static InventoryTransferReceiptRecord ToRecord(InventoryTransferReceipt receipt) =>
        new()
        {
            Id = receipt.Id.Value,
            OrganizationId = receipt.OrganizationId.Value,
            TransferId = receipt.TransferId.Value,
            Sequence = receipt.Sequence,
            ReceivedAtUtc = receipt.ReceivedAtUtc,
            ReceivedBy = receipt.ReceivedBy
        };

    public static InventoryTransferReceiptLineRecord ToRecord(InventoryTransferReceiptLine line) =>
        new()
        {
            Id = line.Id.Value,
            ReceiptId = line.ReceiptId.Value,
            TransferLineId = line.TransferLineId.Value,
            ProductId = line.ProductId.Value,
            QuantityReceived = line.QuantityReceived,
            QuantityDamaged = line.QuantityDamaged,
            QuantityMissing = line.QuantityMissing,
            QuantityOther = line.QuantityOther,
            OtherReasonCode = line.OtherReasonCode,
            OtherReasonNote = line.OtherReasonNote,
            MissingDisposition = line.MissingDisposition is null
                ? null
                : InventoryTransferMissingDispositions.ToCode(line.MissingDisposition.Value),
            DamagedFollowUp = line.DamagedFollowUp is null
                ? null
                : InventoryTransferDiscrepancyFollowUps.ToCode(line.DamagedFollowUp.Value),
            OtherFollowUp = line.OtherFollowUp is null
                ? null
                : InventoryTransferDiscrepancyFollowUps.ToCode(line.OtherFollowUp.Value),
            QuantityWaived = line.QuantityWaived,
            Note = line.Note
        };

    public static InventoryBranchBalance ToDomain(InventoryBranchBalanceRecord record) =>
        InventoryBranchBalance.Rehydrate(
            PosOrganizationId.From(record.OrganizationId),
            PosBranchId.From(record.BranchId),
            CatalogProductId.From(record.ProductId),
            record.OnHandQuantity,
            record.UpdatedAtUtc,
            record.ReservedQuantity,
            record.PendingReturnQuantity);

    public static InventoryBranchBalanceRecord ToRecord(InventoryBranchBalance balance) =>
        new()
        {
            OrganizationId = balance.OrganizationId.Value,
            BranchId = balance.BranchId.Value,
            ProductId = balance.ProductId.Value,
            OnHandQuantity = balance.OnHandQuantity,
            ReservedQuantity = balance.ReservedQuantity,
            PendingReturnQuantity = balance.PendingReturnQuantity,
            UpdatedAtUtc = balance.UpdatedAtUtc
        };

    public static void ApplyToRecord(InventoryBranchBalance balance, InventoryBranchBalanceRecord record)
    {
        record.OnHandQuantity = balance.OnHandQuantity;
        record.ReservedQuantity = balance.ReservedQuantity;
        record.PendingReturnQuantity = balance.PendingReturnQuantity;
        record.UpdatedAtUtc = balance.UpdatedAtUtc;
    }

    public static InventoryBranchReorderSetting ToDomain(InventoryBranchReorderSettingRecord record) =>
        InventoryBranchReorderSetting.Rehydrate(
            PosOrganizationId.From(record.OrganizationId),
            PosBranchId.From(record.BranchId),
            CatalogProductId.From(record.ProductId),
            record.ReorderLevel,
            record.ReorderQuantity,
            record.UpdatedAtUtc,
            record.UpdatedBy);

    public static InventoryBranchReorderSettingRecord ToRecord(InventoryBranchReorderSetting setting) =>
        new()
        {
            OrganizationId = setting.OrganizationId.Value,
            BranchId = setting.BranchId.Value,
            ProductId = setting.ProductId.Value,
            ReorderLevel = setting.ReorderLevel,
            ReorderQuantity = setting.ReorderQuantity,
            UpdatedAtUtc = setting.UpdatedAtUtc,
            UpdatedBy = setting.UpdatedBy
        };

    public static InventoryBranchReorderDefault ToDomain(InventoryBranchReorderDefaultRecord record) =>
        InventoryBranchReorderDefault.Rehydrate(
            PosOrganizationId.From(record.OrganizationId),
            PosBranchId.From(record.BranchId),
            record.ReorderLevel,
            record.ReorderQuantity,
            record.UpdatedAtUtc,
            record.UpdatedBy);

    public static InventoryBranchReorderDefaultRecord ToRecord(InventoryBranchReorderDefault setting) =>
        new()
        {
            OrganizationId = setting.OrganizationId.Value,
            BranchId = setting.BranchId.Value,
            ReorderLevel = setting.ReorderLevel,
            ReorderQuantity = setting.ReorderQuantity,
            UpdatedAtUtc = setting.UpdatedAtUtc,
            UpdatedBy = setting.UpdatedBy
        };
}
