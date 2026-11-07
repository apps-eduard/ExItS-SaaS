import { describe, expect, it } from "vitest";
import { formatCurrency } from "./format";

describe("formatCurrency", () => {
  it("preserves two-decimal precision for large Personal balances", () => {
    expect(formatCurrency(7_705_000)).toBe("7,705,000.00");
    expect(formatCurrency(77_050_000.5)).toBe("77,050,000.50");
    expect(formatCurrency(0)).toBe("0.00");
  });

  it("does not invent currency symbols when no code is provided", () => {
    expect(formatCurrency(1_234.5)).toBe("1,234.50");
  });
});
