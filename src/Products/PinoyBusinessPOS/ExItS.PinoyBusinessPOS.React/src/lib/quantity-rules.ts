/**
 * Quantity precision rules mirroring SaleMoney / PosSaleOptions.
 * Do not invent a separate precision model for UI.
 */

/** Max digits after the decimal for divisible / measured quantities. */
export const MEASURED_QUANTITY_DECIMALS = 2;

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
 * ByWeight → 2; whole UOM + PerItem → 0; otherwise measured (2), including unknown UOM.
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

/** Smallest UI / domain increment implied by precision (1 or 0.01). */
export function quantityStepForPrecision(precision: number): number {
  if (precision <= 0) {
    return 1;
  }
  return Number((10 ** -precision).toFixed(precision));
}

export function minPositiveQuantity(precision: number): number {
  return quantityStepForPrecision(precision);
}

/**
 * Valid quantity floor for steppers/inputs: never 0.
 * Whole units → 1; measured/divisible → smallest positive domain quantum (e.g. 0.01).
 */
export function quantityInputMinimum(
  unitOfMeasure?: string | null,
  sellingMode?: string | null,
): number {
  return minPositiveQuantity(maxQuantityDecimals(unitOfMeasure, sellingMode));
}

/** Whole-unit +/- step for steppers; divisible units still allow decimal typing via precision. */
export function quantityStepperWholeStep(): number {
  return 1;
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

/** Insert thousand commas into an unsigned integer digit string (e.g. 1000 → 1,000). */
export function withThousandCommas(intDigits: string): string {
  const cleaned = intDigits.replace(/[^\d]/gu, "");
  if (cleaned === "") {
    return "0";
  }
  return cleaned.replace(/\B(?=(\d{3})+(?!\d))/gu, ",");
}

/** Strip thousand commas for parsing. */
export function stripQuantityGrouping(raw: string): string {
  return raw.replace(/,/gu, "");
}

export function formatQuantityValue(value: number, precision: number): string {
  if (!Number.isFinite(value)) {
    return "";
  }
  const clamped = clampQuantityToPrecision(value, precision);
  const abs = Math.abs(clamped);
  const intPart = withThousandCommas(String(Math.trunc(abs)));
  if (precision <= 0) {
    return intPart;
  }
  // Whole values render without a decimal point (1 not 1.00 / 1.).
  if (Math.abs(clamped - Math.trunc(clamped)) < 1e-9) {
    return intPart;
  }
  const fixed = abs.toFixed(precision);
  const frac = fixed
    .split(".")[1]
    ?.replace(/0+$/u, "")
    ?? "";
  if (frac === "") {
    return intPart;
  }
  return `${intPart}.${frac}`;
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

/** Lenient parse while typing; rejects negatives and excess precision. Allows thousand commas. */
export function parseQuantityTyping(raw: string, precision: number): QuantityTypingParse {
  const trimmed = raw.trim();
  if (trimmed === "") {
    return { kind: "empty" };
  }
  // Allow commas only as grouping characters; strip before numeric checks.
  if (!/^[,\d]*([.]\d*)?$/u.test(trimmed)) {
    return { kind: "invalid" };
  }
  const normalized = stripQuantityGrouping(trimmed);
  if (normalized === "" && trimmed.includes(",")) {
    return { kind: "incomplete" };
  }
  if (normalized === "." || normalized.endsWith(".")) {
    return { kind: "incomplete" };
  }
  const value = Number(normalized);
  if (!Number.isFinite(value) || value < 0) {
    return { kind: "invalid" };
  }
  const decimalPart = normalized.includes(".") ? normalized.split(".")[1] ?? "" : "";
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
  const valueScaled = Math.round(input.value * factor);
  // Preserve decimal remainder: 1.5 + 1 → 2.5 (never floor to 2).
  const next = (valueScaled + input.direction * stepScaled) / factor;

  if (!Number.isFinite(next)) {
    return input.value;
  }
  if (next < input.min || next <= 0) {
    return input.min > 0 ? input.min : minPositiveQuantity(precision);
  }
  if (input.max != null && next > input.max) {
    return input.max;
  }
  return clampQuantityToPrecision(next, precision);
}
