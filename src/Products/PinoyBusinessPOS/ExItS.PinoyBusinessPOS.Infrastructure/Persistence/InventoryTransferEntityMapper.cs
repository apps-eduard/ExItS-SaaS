using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class InventoryTransferEntityMapper
{
    public static InventoryTransfer ToDomain(
        InventoryTransferRecord record,
        IReadOnlyList<InventoryTransferLineRecord> lines) =>
        InventoryTransfer.Rehydrate(
            InventoryTransferId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
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
            lines.OrderBy(l => l.LineNumber).Select(ToDomain).ToList());

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
            record.DiscrepancyNote);

    public static InventoryTransferRecord ToRecord(InventoryTransfer transfer) =>
        new()
        {
            Id = transfer.Id.Value,
            OrganizationId = transfer.OrganizationId.Value,
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
            CancelledBy = transfer.CancelledBy
        };

    public static void ApplyToRecord(InventoryTransfer transfer, InventoryTransferRecord record)
    {
        record.TransferNumber = transfer.TransferNumber;
        record.Status = InventoryTransferStatuses.ToCode(transfer.Status);
        record.Notes = transfer.Notes;
        record.UpdatedAtUtc = transfer.UpdatedAtUtc;
        record.DispatchedAtUtc = transfer.DispatchedAtUtc;
        record.DispatchedBy = transfer.DispatchedBy;
        record.ReceivedAtUtc = transfer.ReceivedAtUtc;
        record.ReceivedBy = transfer.ReceivedBy;
        record.CancelledAtUtc = transfer.CancelledAtUtc;
        record.CancelledBy = transfer.CancelledBy;
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
            DiscrepancyReason = line.DiscrepancyReason is null
                ? null
                : InventoryTransferDiscrepancyReasons.ToCode(line.DiscrepancyReason.Value),
            DiscrepancyNote = line.DiscrepancyNote
        };

    public static InventoryBranchBalance ToDomain(InventoryBranchBalanceRecord record) =>
        InventoryBranchBalance.Rehydrate(
            PosOrganizationId.From(record.OrganizationId),
            PosBranchId.From(record.BranchId),
            CatalogProductId.From(record.ProductId),
            record.OnHandQuantity,
            record.UpdatedAtUtc);

    public static InventoryBranchBalanceRecord ToRecord(InventoryBranchBalance balance) =>
        new()
        {
            OrganizationId = balance.OrganizationId.Value,
            BranchId = balance.BranchId.Value,
            ProductId = balance.ProductId.Value,
            OnHandQuantity = balance.OnHandQuantity,
            UpdatedAtUtc = balance.UpdatedAtUtc
        };
}
