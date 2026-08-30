/** Shared receive-at-receipt payment helpers (ADR-023 supplier credit). */

export function roundMoney(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}

export function remainingCredit(total: number, paidNow: number): number {
  return roundMoney(Math.max(0, total - paidNow));
}

/** Parse a non-negative money input; empty → null. */
export function parseMoneyInput(text: string): number | null {
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
  return roundMoney(value);
}

export function formatMoneyInput(value: number): string {
  if (!Number.isFinite(value)) {
    return "0";
  }
  const rounded = roundMoney(value);
  return Number.isInteger(rounded) ? String(rounded) : rounded.toFixed(2);
}

/**
 * Direct purchase: credit (paidNow < total) requires a supplier.
 * Returns an i18n message key when invalid, otherwise null.
 */
export function directPurchaseCreditValidationKey(
  supplierId: string | null | undefined,
  total: number,
  paidNow: number,
): "purchasing.supplierRequiredForCredit" | null {
  if (total <= 0) {
    return null;
  }
  if (paidNow < total && !supplierId?.trim()) {
    return "purchasing.supplierRequiredForCredit";
  }
  return null;
}
