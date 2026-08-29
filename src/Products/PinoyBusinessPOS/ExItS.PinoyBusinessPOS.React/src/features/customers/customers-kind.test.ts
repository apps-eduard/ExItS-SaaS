import { describe, expect, it } from "vitest";
import { parseKindForTest } from "./customers-kind";

describe("customers kind filter", () => {
  it("defaults unknown values to all", () => {
    expect(parseKindForTest(null)).toBe("all");
    expect(parseKindForTest("nope")).toBe("all");
    expect(parseKindForTest("businesses")).toBe("businesses");
    expect(parseKindForTest("people")).toBe("people");
  });
});
