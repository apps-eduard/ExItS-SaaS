import {
  formatQuantityValue,
  isByWeightSellingMode,
  maxQuantityDecimals,
  requiresWholeQuantity,
} from "@/lib/quantity-rules";

export {
  maxQuantityDecimals as maxReturnQuantityDecimals,
  requiresWholeQuantity as requiresWholeReturnQuantity,
} from "@/lib/quantity-rules";

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
  const decimals = maxQuantityDecimals(unitOfMeasure, sellingMode);
  const formatted = formatQuantityValue(quantity, decimals);
  const unit = isByWeightSellingMode(sellingMode)
    ? "kg"
    : unitOfMeasure.trim().toLowerCase() === "piece"
      ? "pc"
      : unitOfMeasure;
  return `${formatted} ${unit}`;
}
