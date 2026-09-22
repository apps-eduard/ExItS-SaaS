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
  shouldSkipReceiveSettlementPayload,
  validateLockedReceivePaymentMatrix,
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
    expect(utang.prepaidSettled).toBe(false);
    const cash = resolveLockedReceivePaymentFromPo("Cash", 100);
    expect(cash.paidNow).toBe(100);
    expect(cash.paymentMethod).toBe("Cash");
    const check = resolveLockedReceivePaymentFromPo("Check", 250);
    expect(check.paidNow).toBe(0);
    expect(check.paymentMethod).toBe("Check");
  });

  it("treats settled PayBefore as prepaid — no receipt settlement required", () => {
    const prepaid = resolveLockedReceivePaymentFromPo({
      paymentTerm: "ManualGCash",
      paymentTiming: "PayBeforeFulfillment",
      estimatedTotal: 850,
      amountPaidSnapshot: 850,
      confirmedTotalAmount: 850,
    });
    expect(prepaid.prepaidSettled).toBe(true);
    expect(prepaid.requiresSettlement).toBe(false);
    expect(prepaid.paidNow).toBe(0);
    expect(prepaid.paymentMethod).toBe("GCash");
    expect(
      validateLockedReceivePaymentMatrix(
        prepaid,
        {
          gCashReference: "",
          bankName: "",
          transferOrDepositReference: "",
          settlementDate: "",
          checkNumber: "",
          checkDate: "",
          settlementNotes: "",
        },
        850,
      ),
    ).toBeNull();
  });

  it("flags PayBefore without settlement snapshot as integrity missing", () => {
    const missing = resolveLockedReceivePaymentFromPo({
      paymentTerm: "ManualGCash",
      paymentTiming: "PayBeforeFulfillment",
      estimatedTotal: 850,
      amountPaidSnapshot: 0,
      confirmedTotalAmount: 850,
    });
    expect(missing.prepaidSettled).toBe(false);
    expect(missing.prepaidIntegrityMissing).toBe(true);
    expect(
      validateLockedReceivePaymentMatrix(
        missing,
        {
          gCashReference: "",
          bankName: "",
          transferOrDepositReference: "",
          settlementDate: "",
          checkNumber: "",
          checkDate: "",
          settlementNotes: "",
        },
        850,
      ),
    ).toBe("purchasing.prepaidSettlementMissing");
  });

  it("keeps PayOnDelivery GCash settlement required and Cash without GCash", () => {
    const gcash = resolveLockedReceivePaymentFromPo({
      paymentTerm: "ManualGCash",
      paymentTiming: "PayOnDeliveryOrReceipt",
      estimatedTotal: 200,
      amountPaidSnapshot: 0,
    });
    expect(gcash.prepaidSettled).toBe(false);
    expect(gcash.requiresSettlement).toBe(true);
    expect(gcash.paymentMethod).toBe("GCash");
    expect(
      validateLockedReceivePaymentMatrix(
        gcash,
        {
          gCashReference: "",
          bankName: "",
          transferOrDepositReference: "",
          settlementDate: "",
          checkNumber: "",
          checkDate: "",
          settlementNotes: "",
        },
        200,
      ),
    ).toBe("purchasing.gcashReferenceRequired");

    const cash = resolveLockedReceivePaymentFromPo({
      paymentTerm: "Cash",
      paymentTiming: "PayOnDeliveryOrReceipt",
      estimatedTotal: 200,
    });
    expect(cash.requiresSettlement).toBe(false);
    expect(cash.paymentMethod).toBe("Cash");
    expect(
      validateLockedReceivePaymentMatrix(
        cash,
        {
          gCashReference: "",
          bankName: "",
          transferOrDepositReference: "",
          settlementDate: "",
          checkNumber: "",
          checkDate: "",
          settlementNotes: "",
        },
        200,
      ),
    ).toBeNull();
  });

  it("locks SupplierCredit without settlement fields", () => {
    const credit = resolveLockedReceivePaymentFromPo({
      paymentTerm: "Utang",
      paymentTiming: "SupplierCredit",
      estimatedTotal: 500,
    });
    expect(credit.mode).toBe("supplierCredit");
    expect(credit.requiresSettlement).toBe(false);
    expect(credit.paymentMethod).toBeNull();
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

  it("treats PayBefore without snapshot as integrity missing and never requires GCash", () => {
    const unpaid = resolveLockedReceivePaymentFromPo({
      paymentTerm: "ManualGCash",
      paymentTiming: "PayBeforeFulfillment",
      estimatedTotal: 850,
      amountPaidSnapshot: 0,
      confirmedTotalAmount: 850,
    });
    expect(unpaid.requiresSettlement).toBe(false);
    expect(unpaid.paidNow).toBe(0);
    expect(unpaid.prepaidSettled).toBe(false);
    expect(unpaid.prepaidIntegrityMissing).toBe(true);
    expect(shouldSkipReceiveSettlementPayload(unpaid)).toBe(true);
  });

  it("treats PayBefore financialSettlementStatus Settled as prepaid", () => {
    const prepaid = resolveLockedReceivePaymentFromPo({
      paymentTerm: "ManualGCash",
      paymentTiming: "PayBeforeFulfillment",
      estimatedTotal: 850,
      amountPaidSnapshot: 0,
      confirmedTotalAmount: 850,
      financialSettlementStatus: "Settled",
    });
    expect(prepaid.prepaidSettled).toBe(true);
    expect(prepaid.requiresSettlement).toBe(false);
  });

  it("treats PayOnDelivery Settled as already settled — no duplicate GCash", () => {
    const settled = resolveLockedReceivePaymentFromPo({
      paymentTerm: "ManualGCash",
      paymentTiming: "PayOnDeliveryOrReceipt",
      estimatedTotal: 200,
      amountPaidSnapshot: 200,
      financialSettlementStatus: "Settled",
    });
    expect(settled.alreadySettledAtReceipt).toBe(true);
    expect(settled.requiresSettlement).toBe(false);
    expect(
      validateLockedReceivePaymentMatrix(
        settled,
        {
          gCashReference: "",
          bankName: "",
          transferOrDepositReference: "",
          settlementDate: "",
          checkNumber: "",
          checkDate: "",
          settlementNotes: "",
        },
        200,
      ),
    ).toBeNull();
  });

  it("keeps pending Check as requiring settlement fields and not settled", () => {
    const check = resolveLockedReceivePaymentFromPo({
      paymentTerm: "Check",
      paymentTiming: "PayOnDeliveryOrReceipt",
      estimatedTotal: 250,
      financialSettlementStatus: "AwaitingPayment",
    });
    expect(check.alreadySettledAtReceipt).toBe(false);
    expect(check.requiresSettlement).toBe(true);
    expect(check.paidNow).toBe(0);
  });
});
