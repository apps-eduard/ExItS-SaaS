import { describe, expect, it } from "vitest";
import {
  formatMoneyAmountInput,
  normalizeMoneyAmountTyping,
  parseMoneyAmountInput,
  roundMoneyAmount,
} from "@/lib/money-input";

describe("money-input", () => {
  it("rounds to two decimals", () => {
    expect(roundMoneyAmount(1.005)).toBe(1.01);
    expect(roundMoneyAmount(10.999)).toBe(11);
  });

  it("formats with thousand commas and two decimals", () => {
    expect(formatMoneyAmountInput(5000)).toBe("5,000.00");
    expect(formatMoneyAmountInput(255500.5)).toBe("255,500.50");
    expect(formatMoneyAmountInput(0)).toBe("0.00");
  });

  it("parses comma-grouped amounts", () => {
    expect(parseMoneyAmountInput("5,000.00")).toBe(5000);
    expect(parseMoneyAmountInput("255,500.50")).toBe(255500.5);
    expect(parseMoneyAmountInput("100")).toBe(100);
    expect(parseMoneyAmountInput("")).toBeNull();
    expect(parseMoneyAmountInput("-5")).toBeNull();
    expect(parseMoneyAmountInput("abc")).toBeNull();
  });

  it("normalizes typing with commas and at most two fraction digits", () => {
    expect(normalizeMoneyAmountTyping("5000")).toBe("5,000");
    expect(normalizeMoneyAmountTyping("5000.")).toBe("5,000.");
    expect(normalizeMoneyAmountTyping("5000.5")).toBe("5,000.5");
    expect(normalizeMoneyAmountTyping("5000.55")).toBe("5,000.55");
    expect(normalizeMoneyAmountTyping("5000.559")).toBe("5,000.55");
    expect(normalizeMoneyAmountTyping("255500.50")).toBe("255,500.50");
    expect(normalizeMoneyAmountTyping("abc")).toBe("");
  });
});
