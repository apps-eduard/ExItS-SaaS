import type { InventoryTransferDto } from "@/api/pos/pos-inventory-transfer-client";

export type TransferActivityKind = "created" | "dispatched" | "received" | "cancelled";

export type TransferActivityEvent = {
  id: string;
  kind: TransferActivityKind;
  atUtc: string;
  actorId?: string | null;
};

/** Chronological transfer lifecycle from authoritative DTO timestamps only. */
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

  if (transfer.receivedAtUtc) {
    events.push({
      id: `${transfer.transferId}-received`,
      kind: "received",
      atUtc: transfer.receivedAtUtc,
      actorId: transfer.receivedBy,
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
