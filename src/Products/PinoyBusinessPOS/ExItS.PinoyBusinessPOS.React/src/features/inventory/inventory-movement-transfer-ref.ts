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
    movement.transactionType === "InventoryTransfer" ||
    movement.sourceType === "InventoryTransfer" ||
    TRANSFER_MOVEMENT_TYPES.has(movement.movementType)
  );
}

/**
 * Authoritative inventory transfer id for navigation / drawer loading.
 * Prefer server-resolved <c>transactionId</c>; never guess from reason text.
 * Do not fall back to <c>sourceId</c> — TransferIn / damage movements use child ids.
 */
export function resolveInventoryTransferTransactionId(
  movement: PosStockMovementDto,
): string | null {
  const id = movement.transactionId?.trim();
  return id && id.length > 0 ? id : null;
}

/**
 * Transfer document number: prefer server <c>transactionReference</c>,
 * otherwise parse the reason prefix for display only.
 */
export function extractTransferReferenceNumber(
  movement: PosStockMovementDto,
): string | null {
  if (!isInventoryTransferMovement(movement)) {
    return null;
  }

  const fromServer = movement.transactionReference?.trim();
  if (fromServer) {
    return fromServer;
  }

  const reason = movement.reason?.trim() ?? "";
  if (!reason) {
    return null;
  }

  for (const prefix of TRANSFER_REASON_PREFIXES) {
    if (reason.startsWith(prefix)) {
      const rest = reason.slice(prefix.length).trim();
      // Damage-hold reasons may append " · Return to source · Replacement requested"
      const number = rest.split(" · ")[0]?.trim() ?? "";
      return number.length > 0 ? number : null;
    }
  }

  return null;
}

export function inventoryTransferDetailPath(transferId: string): string {
  return `/inventory/transfers/${transferId}`;
}
