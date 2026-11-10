import { describe, expect, it } from "vitest";
import { toColorText } from "./color";

describe("color text", () => {
  it("turns opaque rgb into hex and keeps translucent scrims", () => {
    expect(toColorText("rgb(15, 118, 110)")).toBe("#0f766e");
    expect(toColorText("rgba(15, 23, 42, 0.46)")).toBe("rgba(15, 23, 42, 0.46)");
  });
});
