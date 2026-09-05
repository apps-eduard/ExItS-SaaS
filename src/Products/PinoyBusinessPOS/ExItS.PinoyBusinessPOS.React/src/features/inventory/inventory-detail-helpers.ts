import type { PosInventoryAccountDto, PosInventoryLotDto } from "@/api/pos/pos-inventory-client";

/** Non-expired quantity beyond the near-expiry warning window (from API totals). */
export function computeGoodQuantity(account: PosInventoryAccountDto): number {
  const sellable = account.sellableQuantity ?? 0;
  const nearExpiry = account.nearExpiryQuantity ?? 0;
  return Math.max(0, sellable - nearExpiry);
}

export function sortLotsByExpiry(lots: PosInventoryLotDto[]): PosInventoryLotDto[] {
  return [...lots].sort((a, b) => a.expirationDate.localeCompare(b.expirationDate));
}

export function formatLotBatchLabel(lotNumber?: string | null): string {
  return lotNumber?.trim() ? lotNumber.trim() : "—";
}

export function canDisableExpirationTracking(account: PosInventoryAccountDto): boolean {
  return account.isTracked && account.onHandQuantity <= 0;
}

/**
 * Tracked product with zero on-hand at the current location and no opening
 * movement for this location yet (hasOpeningStock is branch-scoped from the API).
 */
export function canAddOpeningStock(account: PosInventoryAccountDto): boolean {
  return (
    account.isTracked &&
    account.hasOpeningStock !== true &&
    account.onHandQuantity <= 0
  );
}
