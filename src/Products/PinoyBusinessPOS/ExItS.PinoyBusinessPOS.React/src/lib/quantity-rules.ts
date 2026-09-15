/**
 * Quantity precision rules mirroring SaleMoney / PosSaleOptions.
 * Do not invent a separate precision model for UI.
 */

export const MEASURED_QUANTITY_DECIMALS = 3;

const WHOLE_UNIT_CODES = new Set([
  "piece",
  "pack",
  "box",
  "bottle",
  "can",
  "sachet",
]);

export function isByWeightSellingMode(sellingMode?: string | null): boolean {
  return (sellingMode ?? "").trim().toLowerCase() === "byweight";
}

export function isWholeUnitOfMeasure(unitOfMeasure?: string | null): boolean {
  return WHOLE_UNIT_CODES.has((unitOfMeasure ?? "").trim().toLowerCase());
}

/**
 * Max decimal places for a quantity.
 * ByWeight → 3; whole UOM + PerItem → 0; otherwise measured (3), including unknown UOM.
 */
export function maxQuantityDecimals(
  unitOfMeasure?: string | null,
  sellingMode?: string | null,
): number {
  if (isByWeightSellingMode(sellingMode)) {
    return MEASURED_QUANTITY_DECIMALS;
  }
  if (isWholeUnitOfMeasure(unitOfMeasure)) {
    return 0;
  }
  return MEASURED_QUANTITY_DECIMALS;
}

export function requiresWholeQuantity(
  unitOfMeasure?: string | null,
  sellingMode?: string | null,
): boolean {
  return maxQuantityDecimals(unitOfMeasure, sellingMode) === 0;
}

/** Smallest UI / domain increment implied by precision (1 or 0.001). */
export function quantityStepForPrecision(precision: number): number {
  if (precision <= 0) {
    return 1;
  }
  return Number((10 ** -precision).toFixed(precision));
}

export function minPositiveQuantity(precision: number): number {
  return quantityStepForPrecision(precision);
}

export function hasAtMostDecimals(value: number, decimals: number): boolean {
  if (!Number.isFinite(value)) {
    return false;
  }
  if (decimals <= 0) {
    return Math.abs(value - Math.trunc(value)) < 1e-9;
  }
  const factor = 10 ** decimals;
  return Math.abs(value * factor - Math.round(value * factor)) < 1e-6;
}

export function clampQuantityToPrecision(value: number, precision: number): number {
  if (!Number.isFinite(value)) {
    return Number.NaN;
  }
  if (precision <= 0) {
    return Math.trunc(value);
  }
  const factor = 10 ** precision;
  return Math.round(value * factor) / factor;
}

export function formatQuantityValue(value: number, precision: number): string {
  if (!Number.isFinite(value)) {
    return "";
  }
  const clamped = clampQuantityToPrecision(value, precision);
  if (precision <= 0) {
    return String(Math.trunc(clamped));
  }
  // Whole values render without a decimal point (1 not 1.000 / 1.).
  if (Math.abs(clamped - Math.trunc(clamped)) < 1e-9) {
    return String(Math.trunc(clamped));
  }
  const fixed = clamped.toFixed(precision);
  return fixed.replace(/(\.\d*?[1-9])0+$/u, "$1").replace(/\.0+$/u, "");
}

export function isValidQuantity(
  value: number,
  unitOfMeasure?: string | null,
  sellingMode?: string | null,
): boolean {
  if (!Number.isFinite(value) || value <= 0) {
    return false;
  }
  return hasAtMostDecimals(value, maxQuantityDecimals(unitOfMeasure, sellingMode));
}

export type QuantityTypingParse =
  | { kind: "empty" }
  | { kind: "incomplete" }
  | { kind: "invalid" }
  | { kind: "value"; value: number };

/** Lenient parse while typing; rejects negatives and excess precision. */
export function parseQuantityTyping(raw: string, precision: number): QuantityTypingParse {
  const trimmed = raw.trim();
  if (trimmed === "") {
    return { kind: "empty" };
  }
  if (!/^\d*([.]\d*)?$/u.test(trimmed)) {
    return { kind: "invalid" };
  }
  if (trimmed === "." || trimmed.endsWith(".")) {
    return { kind: "incomplete" };
  }
  if (trimmed.startsWith(".") && trimmed.length > 1) {
    // ".5" → treat as value once complete
  }
  const value = Number(trimmed);
  if (!Number.isFinite(value) || value < 0) {
    return { kind: "invalid" };
  }
  const decimalPart = trimmed.includes(".") ? trimmed.split(".")[1] ?? "" : "";
  if (precision <= 0) {
    if (decimalPart.length > 0) {
      return { kind: "invalid" };
    }
    return { kind: "value", value };
  }
  if (decimalPart.length > precision) {
    return { kind: "invalid" };
  }
  return { kind: "value", value };
}

export function stepQuantity(input: {
  value: number;
  direction: 1 | -1;
  step: number;
  precision: number;
  min: number;
  max?: number;
}): number {
  const precision = Math.max(0, input.precision);
  const step = input.step > 0 ? input.step : 1;
  const factor = 10 ** precision;
  const stepScaled = Math.max(1, Math.round(step * factor));

  let next: number;
  // Whole-unit steps (e.g. step=1): snap off fractional leftovers so 1.004 + → 2.
  if (stepScaled >= factor) {
    const wholeStep = stepScaled / factor;
    if (input.direction > 0) {
      next = Math.floor(input.value / wholeStep + 1e-9) * wholeStep + wholeStep;
    } else {
      const grid = input.value / wholeStep;
      const onGrid = Math.abs(grid - Math.round(grid)) < 1e-9;
      next = onGrid
        ? Math.round(grid) * wholeStep - wholeStep
        : Math.floor(grid + 1e-9) * wholeStep;
    }
  } else {
    const valueScaled = Math.round(input.value * factor);
    next = (valueScaled + input.direction * stepScaled) / factor;
  }

  if (!Number.isFinite(next)) {
    return input.value;
  }
  if (next < input.min) {
    return input.min;
  }
  if (input.max != null && next > input.max) {
    return input.max;
  }
  return clampQuantityToPrecision(next, precision);
}
