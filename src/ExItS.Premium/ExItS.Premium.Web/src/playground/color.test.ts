import { describe, expect, it } from "vitest";
import { inkForHex, relativeLuminance, toColorText } from "./color";

describe("color text", () => {
  it("turns opaque rgb into hex and keeps translucent scrims", () => {
    expect(toColorText("rgb(15, 118, 110)")).toBe("#0f766e");
    expect(toColorText("rgba(15, 23, 42, 0.46)")).toBe("rgba(15, 23, 42, 0.46)");
  });

  it("picks dark ink on light fills and light ink on dark fills", () => {
    expect(relativeLuminance("#f5f6f8")).toBeGreaterThan(0.55);
    expect(inkForHex("#f5f6f8")).toBe("#111827");
    expect(inkForHex("#111827")).toBe("#f8fafc");
    expect(inkForHex("#ff5722")).toBe("#f8fafc");
  });
});
