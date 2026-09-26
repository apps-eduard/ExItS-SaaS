import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";

const DIRECT_PURCHASE_MOVEMENT_TYPES = new Set([
  "DirectPurchaseReceipt",
  "DirectPurchaseReceiptReversal",
]);

/** Matches POS document numbers like DP-260926-001 (not a raw GUID). */
const DIRECT_PURCHASE_NUMBER_PATTERN = /^DP-\d{6}-\d{3,}$/i;

const GUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function isGuidLike(value: string): boolean {
  return GUID_PATTERN.test(value);
}

function isDirectPurchaseDocumentNumber(value: string): boolean {
  return DIRECT_PURCHASE_NUMBER_PATTERN.test(value);
}

/** True when the movement is a direct-buy receipt (or its reversal). */
export function isDirectPurchaseMovement(movement: PosStockMovementDto): boolean {
  return (
    movement.transactionType === "DirectPurchase" ||
    movement.sourceType === "DirectPurchase" ||
    DIRECT_PURCHASE_MOVEMENT_TYPES.has(movement.movementType)
  );
}

/**
 * Authoritative direct-purchase receipt id for navigation.
 * Prefer server-resolved <c>transactionId</c>; fall back to sourceId.
 */
export function resolveDirectPurchaseReceiptId(
  movement: PosStockMovementDto,
): string | null {
  if (!isDirectPurchaseMovement(movement)) {
    return null;
  }

  const fromTransaction = movement.transactionId?.trim();
  if (fromTransaction) {
    return fromTransaction;
  }

  const fromSource = movement.sourceId?.trim();
  return fromSource && fromSource.length > 0 ? fromSource : null;
}

/**
 * Human-readable receipt number (e.g. DP-260926-001).
 * Never returns a raw GUID — those are navigation ids only.
 */
export function extractDirectPurchaseReferenceNumber(
  movement: PosStockMovementDto,
): string | null {
  if (!isDirectPurchaseMovement(movement)) {
    return null;
  }

  const fromServer = movement.transactionReference?.trim();
  if (fromServer && !isGuidLike(fromServer)) {
    return fromServer;
  }

  const reason = movement.reason?.trim() ?? "";
  if (reason) {
    const match = reason.match(/\b(DP-\d{6}-\d{3,})\b/i);
    if (match?.[1]) {
      return match[1].toUpperCase();
    }
  }

  return null;
}

/** Label for the clickable Direct Buy ref: receipt number only (never GUID). */
export function resolveDirectPurchaseDisplayLabel(
  movement: PosStockMovementDto,
): string | null {
  const number = extractDirectPurchaseReferenceNumber(movement);
  if (!number) {
    return null;
  }
  return isDirectPurchaseDocumentNumber(number) || !isGuidLike(number)
    ? number
    : null;
}

export function directPurchaseDetailPath(receiptId: string): string {
  return `/purchasing/direct-purchases/${receiptId}`;
}
