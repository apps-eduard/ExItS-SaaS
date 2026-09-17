/** Shared money amount entry: thousand commas + exactly 2 decimal places (e.g. 5,000.00). */

export function roundMoneyAmount(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}

/** Parse a non-negative money string (commas allowed). Empty → null. */
export function parseMoneyAmountInput(text: string): number | null {
  const trimmed = text.trim();
  if (!trimmed) {
    return null;
  }
  const normalized = trimmed.replace(/,/g, "");
  if (!/^\d+(\.\d{1,4})?$/.test(normalized)) {
    return null;
  }
  const value = Number(normalized);
  if (!Number.isFinite(value) || value < 0) {
    return null;
  }
  return roundMoneyAmount(value);
}

/** Canonical display for a money amount in an input: 5,000.00 / 255,500.50 */
export function formatMoneyAmountInput(value: number): string {
  if (!Number.isFinite(value)) {
    return "0.00";
  }
  return new Intl.NumberFormat("en-PH", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
    useGrouping: true,
  }).format(roundMoneyAmount(value));
}

/**
 * Live typing normalize: digits + optional decimals (max 2), with thousand commas.
 * Preserves a trailing "." while the user is still typing cents.
 */
export function normalizeMoneyAmountTyping(raw: string): string {
  const cleaned = raw.replace(/[^\d.]/g, "");
  if (!cleaned) {
    return "";
  }

  const firstDot = cleaned.indexOf(".");
  let intDigits: string;
  let fracDigits: string | null = null;
  let trailingDot = false;

  if (firstDot === -1) {
    intDigits = cleaned.replace(/\D/g, "");
  } else {
    intDigits = cleaned.slice(0, firstDot).replace(/\D/g, "");
    const after = cleaned.slice(firstDot + 1).replace(/\D/g, "").slice(0, 2);
    trailingDot = cleaned.endsWith(".") && after.length === 0;
    fracDigits = after;
  }

  // Drop leading zeros unless the integer part is only zeros before a decimal.
  intDigits = intDigits.replace(/^0+(?=\d)/, "");
  if (!intDigits) {
    intDigits = "0";
  }

  const grouped = intDigits.replace(/\B(?=(\d{3})+(?!\d))/g, ",");

  if (trailingDot) {
    return `${grouped}.`;
  }
  if (fracDigits !== null) {
    return `${grouped}.${fracDigits}`;
  }
  return grouped;
}
