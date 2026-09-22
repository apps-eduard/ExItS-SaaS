import type {
  InventoryTransferDto,
  InventoryTransferReceiptLineDto,
} from "@/api/pos/pos-inventory-transfer-client";

export type TransferReceiptActivityLine = Pick<
  InventoryTransferReceiptLineDto,
  "quantityReceived" | "quantityDamaged" | "quantityMissing" | "missingDisposition" | "note"
>;

export type TransferActivityKind =
  | "created"
  | "dispatched"
  | "received"
  | "receipt"
  | "closedRemainder"
  | "cancelled";

export type TransferActivityEvent = {
  id: string;
  kind: TransferActivityKind;
  atUtc: string;
  actorId?: string | null;
  receiptSequence?: number;
  receiptLines?: TransferReceiptActivityLine[];
};

/** Chronological transfer lifecycle from authoritative DTO timestamps and receipt history. */
export function buildTransferActivityEvents(
  transfer: InventoryTransferDto,
): TransferActivityEvent[] {
  const events: TransferActivityEvent[] = [
    {
      id: `${transfer.transferId}-created`,
      kind: "created",
      atUtc: transfer.createdAtUtc,
      actorId: transfer.createdBy,
    },
  ];

  if (transfer.dispatchedAtUtc) {
    events.push({
      id: `${transfer.transferId}-dispatched`,
      kind: "dispatched",
      atUtc: transfer.dispatchedAtUtc,
      actorId: transfer.dispatchedBy,
    });
  }

  const receipts = transfer.receipts ?? [];
  if (receipts.length > 0) {
    for (const receipt of receipts) {
      const receiptLines = receipt.lines.map((line) => ({
        quantityReceived: line.quantityReceived,
        quantityDamaged: line.quantityDamaged ?? 0,
        quantityMissing: line.quantityMissing ?? 0,
        missingDisposition: line.missingDisposition ?? null,
        note: line.note ?? null,
      }));
      events.push({
        id: `${transfer.transferId}-receipt-${receipt.receiptId}`,
        kind: "receipt",
        atUtc: receipt.receivedAtUtc,
        actorId: receipt.receivedBy,
        receiptSequence: receipt.sequence,
        receiptLines,
      });
    }
  } else if (transfer.receivedAtUtc) {
    events.push({
      id: `${transfer.transferId}-received`,
      kind: "received",
      atUtc: transfer.receivedAtUtc,
      actorId: transfer.receivedBy,
    });
  }

  if (transfer.status === "ClosedWithDiscrepancy" && transfer.closedAtUtc) {
    events.push({
      id: `${transfer.transferId}-closed-remainder`,
      kind: "closedRemainder",
      atUtc: transfer.closedAtUtc,
      actorId: transfer.closedBy ?? null,
    });
  }

  if (transfer.cancelledAtUtc) {
    events.push({
      id: `${transfer.transferId}-cancelled`,
      kind: "cancelled",
      atUtc: transfer.cancelledAtUtc,
      actorId: transfer.cancelledBy,
    });
  }

  return events.sort((a, b) => a.atUtc.localeCompare(b.atUtc));
}
