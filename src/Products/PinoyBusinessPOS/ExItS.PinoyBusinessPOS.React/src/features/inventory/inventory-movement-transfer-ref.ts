import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";

const TRANSFER_REASON_PREFIXES = [
  "Transfer out ",
  "Transfer in ",
  "Transfer cancelled ",
  "Transfer damage hold ",
  "Transfer damage recovery ",
  "Transfer damage return out ",
  "Transfer damage return in ",
  "Transfer damage write-off ",
] as const;

const TRANSFER_MOVEMENT_TYPES = new Set([
  "TransferOut",
  "TransferIn",
  "TransferCancelRestore",
  "TransferDamageHold",
  "TransferDamageRecovery",
  "TransferDamageReturnOut",
  "TransferDamageReturnIn",
  "TransferDamageWriteOff",
]);

/** True when the movement is transfer-sourced (any transfer movement type). */
export function isInventoryTransferMovement(movement: PosStockMovementDto): boolean {
  return (
    movement.sourceType === "InventoryTransfer" ||
    TRANSFER_MOVEMENT_TYPES.has(movement.movementType)
  );
}

/**
 * Transfer document number embedded in stock-movement reason
 * (e.g. "Transfer out TR-260922-001" → "TR-260922-001").
 */
export function extractTransferReferenceNumber(
  movement: PosStockMovementDto,
): string | null {
  if (!isInventoryTransferMovement(movement)) {
    return null;
  }

  const reason = movement.reason?.trim() ?? "";
  if (!reason) {
    return null;
  }

  for (const prefix of TRANSFER_REASON_PREFIXES) {
    if (reason.startsWith(prefix)) {
      const number = reason.slice(prefix.length).trim();
      return number.length > 0 ? number : null;
    }
  }

  return null;
}

export function inventoryTransferDetailPath(transferId: string): string {
  return `/inventory/transfers/${transferId}`;
}
