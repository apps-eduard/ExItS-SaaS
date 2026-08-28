import { requiresOpeningExpirationDate } from "@/features/inventory/inventory-lot-status";

export type OpeningStockFormState = {
  trackStockQuantity: boolean;
  addOpeningStock: boolean;
  openingQuantity: string;
  unitCost: string;
  expiryDate: string;
  batchLot: string;
  tracksExpiration: boolean;
};

export function computeOpeningStockValue(quantity: number, unitCost: number): number | null {
  if (Number.isNaN(quantity) || Number.isNaN(unitCost) || quantity <= 0 || unitCost < 0) {
    return null;
  }

  return Math.round(quantity * unitCost * 100) / 100;
}

export type OpeningStockValidationKey =
  | "openingStock.quantityRequired"
  | "openingStock.quantityInvalid"
  | "openingStock.unitCostRequired"
  | "openingStock.unitCostInvalid"
  | "openingStock.expiryRequired";

export function validateOpeningStockInput(
  state: OpeningStockFormState,
): OpeningStockValidationKey | null {
  if (!state.trackStockQuantity || !state.addOpeningStock) {
    return null;
  }

  const quantity = Number(state.openingQuantity);
  if (!state.openingQuantity.trim()) {
    return "openingStock.quantityRequired";
  }

  if (Number.isNaN(quantity) || quantity <= 0) {
    return "openingStock.quantityInvalid";
  }

  const unitCost = Number(state.unitCost);
  if (!state.unitCost.trim()) {
    return "openingStock.unitCostRequired";
  }

  if (Number.isNaN(unitCost) || unitCost <= 0) {
    return "openingStock.unitCostInvalid";
  }

  if (
    requiresOpeningExpirationDate(state.tracksExpiration, quantity) &&
    !state.expiryDate.trim()
  ) {
    return "openingStock.expiryRequired";
  }

  return null;
}

export function buildEnableInventoryBody(state: OpeningStockFormState): {
  openingQuantity?: number;
  unitCost?: number;
  expirationDate?: string;
  lotNumber?: string;
} {
  if (!state.trackStockQuantity) {
    return {};
  }

  if (!state.addOpeningStock) {
    return { openingQuantity: 0 };
  }

  const quantity = Number(state.openingQuantity);
  const unitCost = Number(state.unitCost);
  const body: {
    openingQuantity: number;
    unitCost: number;
    expirationDate?: string;
    lotNumber?: string;
  } = {
    openingQuantity: quantity,
    unitCost,
  };

  if (state.tracksExpiration && state.expiryDate.trim()) {
    body.expirationDate = state.expiryDate.trim();
  }

  const lot = state.batchLot.trim();
  if (lot) {
    body.lotNumber = lot;
  }

  return body;
}
