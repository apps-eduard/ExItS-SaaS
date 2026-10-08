import { describe, expect, it } from "vitest";
import { parseCustomerListTab, parseKindForTest } from "./customers-kind";

describe("customers kind filter", () => {
  it("defaults unknown values to all", () => {
    expect(parseKindForTest(null)).toBe("all");
    expect(parseKindForTest("nope")).toBe("all");
    expect(parseKindForTest("businesses")).toBe("businesses");
    expect(parseKindForTest("people")).toBe("people");
    expect(parseKindForTest("deactivated")).toBe("all");
  });

  it("keeps the deactivated customer tab", () => {
    expect(parseCustomerListTab("deactivated")).toBe("deactivated");
    expect(parseCustomerListTab("people")).toBe("people");
    expect(parseCustomerListTab(null)).toBe("all");
  });
});
