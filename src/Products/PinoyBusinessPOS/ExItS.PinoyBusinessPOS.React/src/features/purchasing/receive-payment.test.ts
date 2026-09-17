import { describe, expect, it } from "vitest";
import {
  buildReceiveSettlementPayload,
  clearStaleSettlementFields,
  defaultPaidNowForMode,
  defaultReceivePaymentMode,
  directPurchaseCreditValidationKey,
  formatMoneyInput,
  isConnectedUtangPaymentTerm,
  laterPaymentsAmount,
  mapPoPaymentTermToReceiveMethod,
  parseMoneyInput,
  receiptReverseErrorMessage,
  remainingCredit,
  resolveLockedReceivePaymentFromPo,
  roundMoney,
  validateLockedSettlementFields,
  validateReceivePaidNow,
} from "@/features/purchasing/receive-payment";

describe("receive-payment helpers", () => {
  it("computes remaining credit from total and paid now", () => {
    expect(remainingCredit(100, 100)).toBe(0);
    expect(remainingCredit(100, 40)).toBe(60);
    expect(remainingCredit(100.1, 40)).toBe(60.1);
    expect(remainingCredit(50, 80)).toBe(0);
  });

  it("defaults paidNow formatting for whole and fractional amounts", () => {
    expect(formatMoneyInput(120)).toBe("120.00");
    expect(formatMoneyInput(120.5)).toBe("120.50");
    expect(formatMoneyInput(5000)).toBe("5,000.00");
    expect(roundMoney(10.005)).toBe(10.01);
  });

  it("parses valid money input and rejects invalid", () => {
    expect(parseMoneyInput("100")).toBe(100);
    expect(parseMoneyInput("100.25")).toBe(100.25);
    expect(parseMoneyInput("1,000.50")).toBe(1000.5);
    expect(parseMoneyInput("")).toBeNull();
    expect(parseMoneyInput("-5")).toBeNull();
    expect(parseMoneyInput("abc")).toBeNull();
  });

  it("requires supplier when direct purchase paidNow is less than total", () => {
    expect(directPurchaseCreditValidationKey(null, 100, 50)).toBe(
      "purchasing.supplierRequiredForCredit",
    );
    expect(directPurchaseCreditValidationKey("", 100, 50)).toBe(
      "purchasing.supplierRequiredForCredit",
    );
    expect(directPurchaseCreditValidationKey("sup-1", 100, 50)).toBeNull();
    expect(directPurchaseCreditValidationKey(null, 100, 100)).toBeNull();
  });

  it("validates PaidNow against receipt total", () => {
    expect(validateReceivePaidNow(100, null)).toBe("purchasing.invalidPaidNow");
    expect(validateReceivePaidNow(100, 101)).toBe("purchasing.paidNowExceedsTotal");
    expect(validateReceivePaidNow(100, 0)).toBeNull();
    expect(validateReceivePaidNow(100, 100)).toBeNull();
  });

  it("computes later payments excluding paid-at-receipt", () => {
    expect(laterPaymentsAmount(500, 200)).toBe(300);
    expect(laterPaymentsAmount(200, 200)).toBe(0);
  });

  it("detects connected Utang payment terms and defaults receive mode to credit", () => {
    expect(isConnectedUtangPaymentTerm("Utang")).toBe(true);
    expect(isConnectedUtangPaymentTerm("Utang / Credit")).toBe(true);
    expect(isConnectedUtangPaymentTerm("Cash")).toBe(false);
    expect(
      defaultReceivePaymentMode({
        connectedPurchaseOrderId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        paymentTerm: "Utang",
      }),
    ).toBe("supplierCredit");
    expect(
      defaultReceivePaymentMode({
        connectedPurchaseOrderId: null,
        paymentTerm: "Utang",
      }),
    ).toBe("paidInFull");
    expect(defaultPaidNowForMode("supplierCredit", 643)).toBe(0);
    expect(defaultPaidNowForMode("paidInFull", 643)).toBe(643);
  });

  it("locks receive payment from PO payment term", () => {
    expect(mapPoPaymentTermToReceiveMethod("ManualGCash")).toBe("GCash");
    expect(mapPoPaymentTermToReceiveMethod("BankDeposit")).toBe("BankDeposit");
    const utang = resolveLockedReceivePaymentFromPo("Utang", 643);
    expect(utang.paidNow).toBe(0);
    expect(utang.paymentMethod).toBeNull();
    const cash = resolveLockedReceivePaymentFromPo("Cash", 100);
    expect(cash.paidNow).toBe(100);
    expect(cash.paymentMethod).toBe("Cash");
    const check = resolveLockedReceivePaymentFromPo("Check", 250);
    expect(check.paidNow).toBe(0);
    expect(check.paymentMethod).toBe("Check");
  });

  it("clears stale settlement fields and validates locked settlement", () => {
    const fields = {
      gCashReference: "GCASH-1",
      bankName: "BDO",
      transferOrDepositReference: "REF",
      settlementDate: "2026-09-17",
      checkNumber: "100",
      checkDate: "2026-09-17",
      settlementNotes: "note",
    };
    const gcashOnly = clearStaleSettlementFields("GCash", fields);
    expect(gcashOnly.gCashReference).toBe("GCASH-1");
    expect(gcashOnly.bankName).toBe("");
    expect(
      validateLockedSettlementFields("GCash", { ...gcashOnly, gCashReference: "" }),
    ).toBe("purchasing.gcashReferenceRequired");
    const payload = buildReceiveSettlementPayload("Check", fields);
    expect(payload.checkClearingStatus).toBe("PendingClearing");
  });

  it("maps receipt reverse blocked-by-payments to friendly message", () => {
    const friendly =
      "This receipt cannot be reversed because supplier payments have already been recorded.";
    expect(
      receiptReverseErrorMessage(
        {
          problem: {
            errorCode: "pos.supplier_payable.void.blocked_by_payments",
            detail: "raw",
          },
        },
        "fallback",
        friendly,
      ),
    ).toBe(friendly);
  });
});
