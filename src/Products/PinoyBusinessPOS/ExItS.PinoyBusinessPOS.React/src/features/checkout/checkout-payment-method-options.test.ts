import { describe, expect, it } from "vitest";
import {
  filterCheckoutUiChoices,
  isDebtCreatingPaymentChoice,
  isManualReferencePaymentChoice,
  toApiPaymentMethod,
  toUiPaymentChoice,
} from "@/features/checkout/checkout-payment-method-options";

describe("checkout-payment-method-options", () => {
  it("maps ManualGCash UI alias and Pro manual methods", () => {
    expect(toUiPaymentChoice("ManualGCash")).toBe("GCash");
    expect(toApiPaymentMethod("GCash")).toBe("ManualGCash");
    expect(toApiPaymentMethod("BankTransfer")).toBe("BankTransfer");
    expect(isManualReferencePaymentChoice("Check")).toBe(true);
  });

  it("classifies only Utang as debt-creating (Check is deferred settlement, not credit)", () => {
    expect(isDebtCreatingPaymentChoice("Utang")).toBe(true);
    expect(isDebtCreatingPaymentChoice("Cash")).toBe(false);
    expect(isDebtCreatingPaymentChoice("GCash")).toBe(false);
    expect(isDebtCreatingPaymentChoice("BankTransfer")).toBe(false);
    expect(isDebtCreatingPaymentChoice("Check")).toBe(false);
    expect(isDebtCreatingPaymentChoice("ManualMaya")).toBe(false);
  });

  it("filters Starter-like lists to basic methods only", () => {
    expect(filterCheckoutUiChoices(["Cash", "ManualGCash", "Utang"])).toEqual([
      "Cash",
      "GCash",
      "Utang",
    ]);
  });

  it("includes Pro manual methods when entitled", () => {
    expect(
      filterCheckoutUiChoices([
        "Cash",
        "ManualGCash",
        "Utang",
        "BankTransfer",
        "Check",
        "ManualMaya",
      ]),
    ).toEqual(["Cash", "GCash", "Utang", "BankTransfer", "Check", "ManualMaya"]);
  });

  it("drops online/coming-soon codes", () => {
    expect(filterCheckoutUiChoices(["Cash", "OnlineGCash", "Card"])).toEqual(["Cash"]);
  });
});
