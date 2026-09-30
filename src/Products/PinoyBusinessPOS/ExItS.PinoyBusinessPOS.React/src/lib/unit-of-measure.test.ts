import { describe, expect, it } from "vitest";
import { formatUnitOfMeasureSymbol } from "@/lib/unit-of-measure";

describe("formatUnitOfMeasureSymbol", () => {
  it("maps catalog codes to abbreviations / unit symbols", () => {
    expect(formatUnitOfMeasureSymbol("Kilogram")).toBe("kg");
    expect(formatUnitOfMeasureSymbol("kg")).toBe("kg");
    expect(formatUnitOfMeasureSymbol("Gram")).toBe("g");
    expect(formatUnitOfMeasureSymbol("Liter")).toBe("L");
    expect(formatUnitOfMeasureSymbol("Milliliter")).toBe("mL");
    expect(formatUnitOfMeasureSymbol("Meter")).toBe("m");
    expect(formatUnitOfMeasureSymbol("Piece")).toBe("pc");
    expect(formatUnitOfMeasureSymbol("Pack")).toBe("pack");
    expect(formatUnitOfMeasureSymbol("")).toBe("pc");
  });
});
