/** Shared receive-at-receipt payment helpers (ADR-023 supplier credit). */

export type ReceivePaymentMode = "paidInFull" | "supplierCredit";

export const RECEIVE_PAYMENT_METHODS = ["Cash", "BankTransfer", "GCash", "Other"] as const;
export type ReceivePaymentMethodCode = (typeof RECEIVE_PAYMENT_METHODS)[number];

export function roundMoney(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}

export function remainingCredit(total: number, paidNow: number): number {
  return roundMoney(Math.max(0, total - paidNow));
}

/** Later posted payments only (excludes paid-at-receipt snapshot). */
export function laterPaymentsAmount(paidAmount: number, paidAtReceiptAmount: number): number {
  return roundMoney(Math.max(0, paidAmount - paidAtReceiptAmount));
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

export function validateReceivePaidNow(
  total: number,
  paidNow: number | null,
): "purchasing.invalidPaidNow" | "purchasing.paidNowExceedsTotal" | null {
  if (paidNow === null) {
    return "purchasing.invalidPaidNow";
  }
  if (paidNow > total) {
    return "purchasing.paidNowExceedsTotal";
  }
  return null;
}

/** Friendly reverse conflict when supplier payments block receipt void. */
export function receiptReverseErrorMessage(
  err: unknown,
  fallback: string,
  blockedByPayments: string,
): string {
  if (
    err &&
    typeof err === "object" &&
    "problem" in err &&
    err.problem &&
    typeof err.problem === "object" &&
    "errorCode" in err.problem &&
    err.problem.errorCode === "pos.supplier_payable.void.blocked_by_payments"
  ) {
    return blockedByPayments;
  }
  if (
    err &&
    typeof err === "object" &&
    "problem" in err &&
    err.problem &&
    typeof err.problem === "object" &&
    "detail" in err.problem &&
    typeof err.problem.detail === "string" &&
    err.problem.detail.trim()
  ) {
    return err.problem.detail;
  }
  return fallback;
}
