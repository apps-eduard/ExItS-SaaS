/** Shared receive-at-receipt payment helpers (ADR-023 supplier credit). */

import {
  formatMoneyAmountInput,
  parseMoneyAmountInput,
  roundMoneyAmount,
} from "@/lib/money-input";

export type ReceivePaymentMode = "paidInFull" | "supplierCredit";

export const RECEIVE_PAYMENT_METHODS = [
  "Cash",
  "BankTransfer",
  "BankDeposit",
  "GCash",
  "Check",
  "Other",
] as const;
export type ReceivePaymentMethodCode = (typeof RECEIVE_PAYMENT_METHODS)[number];

export type ReceiveSettlementFields = {
  gCashReference: string;
  bankName: string;
  transferOrDepositReference: string;
  settlementDate: string;
  checkNumber: string;
  checkDate: string;
  settlementNotes: string;
};

export const EMPTY_RECEIVE_SETTLEMENT: ReceiveSettlementFields = {
  gCashReference: "",
  bankName: "",
  transferOrDepositReference: "",
  settlementDate: "",
  checkNumber: "",
  checkDate: "",
  settlementNotes: "",
};

export type LockedReceivePaymentConfig = {
  lockedFromPo: true;
  mode: ReceivePaymentMode;
  paymentMethod: ReceivePaymentMethodCode | null;
  paymentTerm: string;
  paymentTiming: string;
  paidNow: number;
  requiresSettlement: boolean;
  /** PayBefore prepayment already confirmed — show read-only settlement, skip receipt payment entry. */
  prepaidSettled: boolean;
  prepaidIntegrityMissing: boolean;
};

export type ResolveLockedReceivePaymentInput = {
  paymentTerm?: string | null;
  paymentTiming?: string | null;
  estimatedTotal: number;
  amountPaidSnapshot?: number | null;
  confirmedTotalAmount?: number | null;
  financialSettlementStatus?: string | null;
};

export function roundMoney(value: number): number {
  return roundMoneyAmount(value);
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
  return parseMoneyAmountInput(text);
}

export function formatMoneyInput(value: number): string {
  return formatMoneyAmountInput(value);
}

function normalizePaymentTerm(paymentTerm?: string | null): string {
  return (paymentTerm ?? "Cash").trim();
}

export function isConnectedUtangPaymentTerm(paymentTerm?: string | null): boolean {
  const normalized = normalizePaymentTerm(paymentTerm).toLowerCase();
  return (
    normalized === "utang" ||
    normalized === "utang / credit" ||
    normalized === "utang/credit"
  );
}

export function mapPoPaymentTermToReceiveMethod(
  paymentTerm?: string | null,
): ReceivePaymentMethodCode | null {
  const normalized = normalizePaymentTerm(paymentTerm);
  switch (normalized) {
    case "Cash":
    case "COD":
      return "Cash";
    case "BankTransfer":
      return "BankTransfer";
    case "BankDeposit":
      return "BankDeposit";
    case "ManualGCash":
    case "GCash":
      return "GCash";
    case "Check":
      return "Check";
    case "Utang":
      return null;
    default:
      return "Cash";
  }
}

/**
 * Locked receive payment derived from confirmed PO payment term + timing.
 * Overload: (paymentTerm, estimatedTotal) kept for existing call sites.
 */
export function resolveLockedReceivePaymentFromPo(
  paymentTermOrInput: string | null | undefined | ResolveLockedReceivePaymentInput,
  estimatedTotalMaybe?: number,
): LockedReceivePaymentConfig {
  const input: ResolveLockedReceivePaymentInput =
    typeof paymentTermOrInput === "object" && paymentTermOrInput !== null
      ? paymentTermOrInput
      : {
          paymentTerm: paymentTermOrInput,
          estimatedTotal: estimatedTotalMaybe ?? 0,
        };

  const term = normalizePaymentTerm(input.paymentTerm);
  const timing = (input.paymentTiming ?? "").trim();
  const estimatedTotal = roundMoney(input.estimatedTotal);
  const paidSnapshot = roundMoney(input.amountPaidSnapshot ?? 0);
  const required =
    input.confirmedTotalAmount != null && input.confirmedTotalAmount > 0
      ? roundMoney(input.confirmedTotalAmount)
      : estimatedTotal;
  const payBefore = timing === "PayBeforeFulfillment";
  const financiallySettled =
    (input.financialSettlementStatus ?? "").trim().toLowerCase() === "settled";
  const prepaidSettled =
    payBefore &&
    (paidSnapshot + 0.0000001 >= Math.max(required, 0.01) || financiallySettled);
  // Pay-before never collects settlement at receipt (payment is pre-fulfillment only).
  const prepaidIntegrityMissing =
    payBefore && !prepaidSettled && paidSnapshot <= 0 && !financiallySettled;

  if (timing === "SupplierCredit" || isConnectedUtangPaymentTerm(term)) {
    return {
      lockedFromPo: true,
      mode: "supplierCredit",
      paymentMethod: null,
      paymentTerm: term,
      paymentTiming: timing,
      paidNow: 0,
      requiresSettlement: false,
      prepaidSettled: false,
      prepaidIntegrityMissing: false,
    };
  }

  if (payBefore) {
    return {
      lockedFromPo: true,
      mode: "paidInFull",
      paymentMethod: mapPoPaymentTermToReceiveMethod(term),
      paymentTerm: term,
      paymentTiming: timing,
      paidNow: 0,
      requiresSettlement: false,
      prepaidSettled,
      prepaidIntegrityMissing,
    };
  }

  if (term === "Check") {
    return {
      lockedFromPo: true,
      mode: "supplierCredit",
      paymentMethod: "Check",
      paymentTerm: term,
      paymentTiming: timing,
      paidNow: 0,
      requiresSettlement: true,
      prepaidSettled: false,
      prepaidIntegrityMissing,
    };
  }

  const method = mapPoPaymentTermToReceiveMethod(term);
  const requiresSettlement =
    method === "GCash" || method === "BankTransfer" || method === "BankDeposit";
  return {
    lockedFromPo: true,
    mode: "paidInFull",
    paymentMethod: method,
    paymentTerm: term,
    paymentTiming: timing,
    paidNow: estimatedTotal,
    requiresSettlement,
    prepaidSettled: false,
    prepaidIntegrityMissing,
  };
}

export function clearStaleSettlementFields(
  method: ReceivePaymentMethodCode | null,
  fields: ReceiveSettlementFields,
): ReceiveSettlementFields {
  if (method === "GCash") {
    return {
      ...EMPTY_RECEIVE_SETTLEMENT,
      gCashReference: fields.gCashReference,
      settlementNotes: fields.settlementNotes,
    };
  }
  if (method === "BankTransfer" || method === "BankDeposit") {
    return {
      ...EMPTY_RECEIVE_SETTLEMENT,
      bankName: fields.bankName,
      transferOrDepositReference: fields.transferOrDepositReference,
      settlementDate: fields.settlementDate,
      settlementNotes: fields.settlementNotes,
    };
  }
  if (method === "Check") {
    return {
      ...EMPTY_RECEIVE_SETTLEMENT,
      bankName: fields.bankName,
      checkNumber: fields.checkNumber,
      checkDate: fields.checkDate,
      settlementNotes: fields.settlementNotes,
    };
  }
  return { ...EMPTY_RECEIVE_SETTLEMENT };
}

export function buildReceiveSettlementPayload(
  method: ReceivePaymentMethodCode | null,
  fields: ReceiveSettlementFields,
  options?: { skipSettlement?: boolean },
): {
  gCashReference?: string | null;
  bankName?: string | null;
  transferOrDepositReference?: string | null;
  settlementDate?: string | null;
  checkNumber?: string | null;
  checkDate?: string | null;
  settlementNotes?: string | null;
  checkClearingStatus?: string | null;
} {
  if (options?.skipSettlement) {
    return {};
  }
  const trimmedNotes = fields.settlementNotes.trim();
  const notes = trimmedNotes ? trimmedNotes : null;
  if (method === "GCash") {
    return {
      gCashReference: fields.gCashReference.trim() || null,
      settlementNotes: notes,
    };
  }
  if (method === "BankTransfer" || method === "BankDeposit") {
    return {
      bankName: fields.bankName.trim() || null,
      transferOrDepositReference: fields.transferOrDepositReference.trim() || null,
      settlementDate: fields.settlementDate.trim() || null,
      settlementNotes: notes,
    };
  }
  if (method === "Check") {
    return {
      bankName: fields.bankName.trim() || null,
      checkNumber: fields.checkNumber.trim() || null,
      checkDate: fields.checkDate.trim() || null,
      settlementNotes: notes,
      checkClearingStatus: "PendingClearing",
    };
  }
  return {};
}

export function validateLockedSettlementFields(
  method: ReceivePaymentMethodCode | null,
  fields: ReceiveSettlementFields,
  options?: { skipSettlement?: boolean },
): string | null {
  if (options?.skipSettlement) {
    return null;
  }
  if (method === "GCash" && !fields.gCashReference.trim()) {
    return "purchasing.gcashReferenceRequired";
  }
  if (method === "BankTransfer" || method === "BankDeposit") {
    if (!fields.bankName.trim()) {
      return "purchasing.bankNameRequired";
    }
    if (!fields.transferOrDepositReference.trim()) {
      return "purchasing.transferReferenceRequired";
    }
    if (!fields.settlementDate.trim()) {
      return "purchasing.settlementDateRequired";
    }
  }
  if (method === "Check") {
    if (!fields.checkNumber.trim()) {
      return "purchasing.checkNumberRequired";
    }
    if (!fields.bankName.trim()) {
      return "purchasing.bankNameRequired";
    }
    if (!fields.checkDate.trim()) {
      return "purchasing.checkDateRequired";
    }
  }
  return null;
}

/**
 * Default receive payment mode for a PO. Connected Utang → supplier credit; otherwise paid in full.
 */
export function defaultReceivePaymentMode(options: {
  connectedPurchaseOrderId?: string | null;
  paymentTerm?: string | null;
}): ReceivePaymentMode {
  if (
    options.connectedPurchaseOrderId &&
    isConnectedUtangPaymentTerm(options.paymentTerm)
  ) {
    return "supplierCredit";
  }
  return "paidInFull";
}

/**
 * Default PaidNow for the selected mode. Utang/credit starts at 0 so buyer payable
 * and seller receivable match the received obligation.
 */
export function defaultPaidNowForMode(mode: ReceivePaymentMode, estimatedTotal: number): number {
  return mode === "paidInFull" ? estimatedTotal : 0;
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
