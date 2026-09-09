import {
  formatQuantityDisplay,
  isByWeightSellingMode,
  normalizeWeightToKilograms,
  roundQuantity,
  type WeightInputUnit,
} from "@/cart/sell-cart-helpers";

export type { WeightInputUnit };

export function isStockUseWeightProduct(input: {
  unitOfMeasure?: string | null;
  sellingMode?: string | null;
}): boolean {
  if (isByWeightSellingMode(input.sellingMode)) {
    return true;
  }
  const uom = input.unitOfMeasure?.trim().toLowerCase() ?? "";
  return uom === "kilogram" || uom === "kg" || uom === "g" || uom === "gram" || uom === "grams";
}

export type StockUseQtyParseResult =
  | { ok: true; quantity: number }
  | { ok: false; reason: "blank" | "invalid" | "zero" | "precision" };

/** Parse operator entry to canonical inventory quantity (kg for weight products). */
export function parseStockUseEntryQuantity(input: {
  raw: string;
  isWeight: boolean;
  weightUnit: WeightInputUnit;
}): StockUseQtyParseResult {
  const trimmed = input.raw.trim();
  if (trimmed === "") {
    return { ok: false, reason: "blank" };
  }

  const rawValue = Number(trimmed);
  if (!Number.isFinite(rawValue)) {
    return { ok: false, reason: "invalid" };
  }

  if (input.isWeight) {
    const parsed = normalizeWeightToKilograms(rawValue, input.weightUnit);
    if ("error" in parsed) {
      if (parsed.error === "precision") {
        return { ok: false, reason: "precision" };
      }
      if (parsed.error === "zero") {
        return { ok: false, reason: "zero" };
      }
      return { ok: false, reason: "invalid" };
    }
    return { ok: true, quantity: parsed.kilograms };
  }

  if (rawValue <= 0) {
    return { ok: false, reason: "zero" };
  }

  return { ok: true, quantity: rawValue };
}

export function canAddStockUseQuantity(input: {
  raw: string;
  isWeight: boolean;
  weightUnit: WeightInputUnit;
  available: number;
}): boolean {
  const parsed = parseStockUseEntryQuantity(input);
  if (!parsed.ok) {
    return false;
  }
  return parsed.quantity > 0 && parsed.quantity <= input.available;
}

/** Display string for a canonical quantity in the operator's chosen unit. */
export function displayQtyFromCanonical(input: {
  quantity: number;
  isWeight: boolean;
  weightUnit: WeightInputUnit;
}): string {
  if (input.isWeight && input.weightUnit === "g") {
    return formatQuantityDisplay(roundQuantity(input.quantity * 1000));
  }
  return formatQuantityDisplay(input.quantity);
}

export function convertDisplayBetweenWeightUnits(
  rawDisplay: string,
  from: WeightInputUnit,
  to: WeightInputUnit,
): string {
  if (from === to) {
    return rawDisplay;
  }
  const trimmed = rawDisplay.trim();
  if (trimmed === "") {
    return rawDisplay;
  }
  const n = Number(trimmed);
  if (!Number.isFinite(n)) {
    return rawDisplay;
  }
  const asKg = from === "g" ? n / 1000 : n;
  if (to === "g") {
    return formatQuantityDisplay(roundQuantity(asKg * 1000));
  }
  return formatQuantityDisplay(roundQuantity(asKg));
}

export function entryUnitLabel(input: {
  isWeight: boolean;
  weightUnit: WeightInputUnit;
  uom: string;
}): string {
  if (input.isWeight) {
    return input.weightUnit;
  }
  return input.uom;
}
