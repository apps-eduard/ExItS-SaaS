import { isByWeightSellingMode } from "@/cart/sell-cart-helpers";

const WHOLE_UOMS = new Set(["piece", "pack", "box", "bottle", "can", "sachet", "pc"]);

/** Match SaleMoney.MaxQuantityDecimals for return quantity controls. */
export function maxReturnQuantityDecimals(unitOfMeasure: string, sellingMode: string): number {
  if (isByWeightSellingMode(sellingMode)) {
    return 3;
  }
  const uom = unitOfMeasure.trim().toLowerCase();
  if (WHOLE_UOMS.has(uom)) {
    return 0;
  }
  return 3;
}

export function requiresWholeReturnQuantity(unitOfMeasure: string, sellingMode: string): boolean {
  return maxReturnQuantityDecimals(unitOfMeasure, sellingMode) === 0;
}

export function clampReturnQuantity(value: number, max: number, decimals: number): number {
  if (!Number.isFinite(value) || value <= 0) {
    return 0;
  }
  const capped = Math.min(value, max);
  if (decimals <= 0) {
    return Math.min(Math.floor(capped + 1e-9), Math.floor(max + 1e-9));
  }
  const factor = 10 ** decimals;
  return Math.min(Math.round(capped * factor) / factor, max);
}

export function formatReturnQuantityDisplay(
  quantity: number,
  unitOfMeasure: string,
  sellingMode: string,
): string {
  const decimals = maxReturnQuantityDecimals(unitOfMeasure, sellingMode);
  const formatted =
    decimals === 0 ? String(Math.trunc(quantity)) : quantity.toFixed(Math.min(decimals, 3));
  const unit = isByWeightSellingMode(sellingMode)
    ? "kg"
    : unitOfMeasure.trim().toLowerCase() === "piece"
      ? "pc"
      : unitOfMeasure;
  return `${formatted} ${unit}`;
}
