import { describe, expect, it } from "vitest";
import { formatSellLinePreview } from "./format-sell-line-preview";

describe("formatSellLinePreview", () => {
  it("substitutes all placeholders including currency-formatted amounts", () => {
    const text = formatSellLinePreview("{qty} {unit} × {price} = {amount}", {
      qty: "10",
      unit: "kg",
      price: "₱220.00 / kg",
      amount: "₱2,200.00",
    });
    expect(text).toBe("10 kg × ₱220.00 / kg = ₱2,200.00");
    expect(text).not.toContain("{");
    expect(text).not.toContain("?");
  });

  it("replaces every {unit} occurrence when the template repeats it", () => {
    const text = formatSellLinePreview("{qty} {unit} / {unit}", {
      qty: "2",
      unit: "kg",
      price: "x",
      amount: "y",
    });
    expect(text).toBe("2 kg / kg");
  });
});
