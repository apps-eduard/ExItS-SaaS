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

/** Positive on-hand lots that are not expired (matches domain FEFO sellable filter). */
export function selectTransferEligibleLots(
  lots: readonly PosInventoryLotDto[],
): TransferEligibleLot[] {
  return lots
    .filter((lot) => {
      if (!(lot.quantityOnHand > 0)) {
        return false;
      }
      const status = lot.expiryStatus?.trim() ?? "";
      return status.toLowerCase() !== "expired";
    })
    .map((lot) => ({
      lotId: lot.lotId,
      lotNumber: lot.lotNumber ?? null,
      expirationDate: lot.expirationDate,
      quantityOnHand: lot.quantityOnHand,
    }));
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
 * Allocate transfer quantity across eligible lots using FEFO.
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
