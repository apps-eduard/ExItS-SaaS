import { describe, expect, it } from "vitest";
import {
  directPurchaseCreditValidationKey,
  formatMoneyInput,
  parseMoneyInput,
  remainingCredit,
  roundMoney,
} from "@/features/purchasing/receive-payment";

describe("receive-payment helpers", () => {
  it("computes remaining credit from total and paid now", () => {
    expect(remainingCredit(100, 100)).toBe(0);
    expect(remainingCredit(100, 40)).toBe(60);
    expect(remainingCredit(100.1, 40)).toBe(60.1);
    expect(remainingCredit(50, 80)).toBe(0);
  });

  it("defaults paidNow formatting for whole and fractional amounts", () => {
    expect(formatMoneyInput(120)).toBe("120");
    expect(formatMoneyInput(120.5)).toBe("120.50");
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
});
