import type { PosInventoryLotDto } from "@/api/pos/pos-inventory-client";

/** Eligible source lot for transfer FEFO / manual allocation. */
export type TransferEligibleLot = {
  lotId: string;
  lotNumber: string | null;
  expirationDate: string;
  quantityOnHand: number;
};

export type TransferLotAllocationSlice = {
  lotId: string;
  lotNumber: string | null;
  expirationDate: string;
  quantity: number;
  lotAvailableQuantity: number;
};

export type TransferFefoAllocateResult =
  | { ok: true; allocations: TransferLotAllocationSlice[] }
  | { ok: false; reason: "invalid_qty" | "insufficient" };

/** Matches API / domain Expired status for normal transfer eligibility. */
export function isTransferLotExpired(lot: {
  expiryStatus?: string | null;
}): boolean {
  const status = lot.expiryStatus?.trim() ?? "";
  return status.toLowerCase() === "expired";
}

/** Positive on-hand lots (includes expired — for Change lots visibility). */
export function selectTransferPositiveOnHandLots(
  lots: readonly PosInventoryLotDto[],
): PosInventoryLotDto[] {
  return lots.filter((lot) => lot.quantityOnHand > 0);
}

/** Positive on-hand lots that are not expired (matches domain IsSellable). */
export function selectTransferEligibleLots(
  lots: readonly PosInventoryLotDto[],
): TransferEligibleLot[] {
  return selectTransferPositiveOnHandLots(lots)
    .filter((lot) => !isTransferLotExpired(lot))
    .map((lot) => ({
      lotId: lot.lotId,
      lotNumber: lot.lotNumber ?? null,
      expirationDate: lot.expirationDate,
      quantityOnHand: lot.quantityOnHand,
    }));
}

export function sumTransferEligibleLotQuantity(
  lots: readonly PosInventoryLotDto[],
): number {
  return selectTransferEligibleLots(lots).reduce(
    (sum, lot) => sum + lot.quantityOnHand,
    0,
  );
}

export function sumTransferExpiredLotQuantity(
  lots: readonly PosInventoryLotDto[],
): number {
  return selectTransferPositiveOnHandLots(lots)
    .filter((lot) => isTransferLotExpired(lot))
    .reduce((sum, lot) => sum + lot.quantityOnHand, 0);
}

/** All positive on-hand lots (expired + non-expired) — physical transferable stock. */
export function sumTransferPhysicalLotQuantity(
  lots: readonly PosInventoryLotDto[],
): number {
  return selectTransferPositiveOnHandLots(lots).reduce(
    (sum, lot) => sum + lot.quantityOnHand,
    0,
  );
}

/**
 * Cap branch available by non-expired lot stock for TracksExpiration products.
 * Non-expiry products use branch available only.
 * Used by the product picker (default replenishment capacity).
 */
export function resolveTransferableAvailableQuantity(args: {
  branchAvailable: number;
  tracksExpiration: boolean;
  lots: readonly PosInventoryLotDto[] | null | undefined;
}): number {
  const branch = Math.max(0, args.branchAvailable);
  if (!args.tracksExpiration) {
    return branch;
  }
  if (args.lots == null) {
    return branch;
  }
  return Math.max(0, Math.min(branch, sumTransferEligibleLotQuantity(args.lots)));
}

/**
 * Change-lots allocation cap for expiry-tracked products: sum of lot on-hand.
 * Do not undercut with branch "available" (on-hand − reserved/holds) — Use max fills
 * lot quantityOnHand, and the server validates lot + branch on-hand (not reserved).
 */
export function resolveChangeLotsMaxQuantity(args: {
  branchAvailable: number;
  tracksExpiration: boolean;
  lots: readonly PosInventoryLotDto[] | null | undefined;
}): number {
  const branch = Math.max(0, args.branchAvailable);
  if (!args.tracksExpiration) {
    return branch;
  }
  if (args.lots == null) {
    return branch;
  }
  return Math.max(0, sumTransferPhysicalLotQuantity(args.lots));
}

/**
 * Canonical FEFO order: ExpirationDate ASC, then lotId for stability
 * (mirrors Domain InventoryLotFefo.AllocateSellable ordering intent).
 */
export function sortLotsForTransferFefo(
  lots: readonly TransferEligibleLot[],
): TransferEligibleLot[] {
  return [...lots].sort((a, b) => {
    const byDate = a.expirationDate.localeCompare(b.expirationDate);
    if (byDate !== 0) {
      return byDate;
    }
    return a.lotId.localeCompare(b.lotId);
  });
}

/**
 * Allocate transfer quantity across eligible (non-expired) lots using FEFO.
 * Does not exceed each lot's quantityOnHand.
 */
export function allocateTransferLotsFefo(
  lots: readonly TransferEligibleLot[],
  quantity: number,
): TransferFefoAllocateResult {
  if (!(quantity > 0) || !Number.isFinite(quantity)) {
    return { ok: false, reason: "invalid_qty" };
  }

  const ordered = sortLotsForTransferFefo(lots);
  let remaining = quantity;
  const allocations: TransferLotAllocationSlice[] = [];

  for (const lot of ordered) {
    if (remaining <= 0) {
      break;
    }
    const take = Math.min(lot.quantityOnHand, remaining);
    if (!(take > 0)) {
      continue;
    }
    allocations.push({
      lotId: lot.lotId,
      lotNumber: lot.lotNumber,
      expirationDate: lot.expirationDate,
      quantity: take,
      lotAvailableQuantity: lot.quantityOnHand,
    });
    remaining -= take;
  }

  if (remaining > 0) {
    return { ok: false, reason: "insufficient" };
  }

  return { ok: true, allocations };
}

export function sumTransferLotAllocationQty(
  allocations: readonly { quantity: number }[],
): number {
  return allocations.reduce(
    (sum, row) => sum + (Number.isFinite(row.quantity) ? row.quantity : 0),
    0,
  );
}
