import { describe, expect, it } from "vitest";
import {
  hasConfiguredInternalSource,
  pickPreferredSourceId,
  remainingRequestQty,
} from "@/features/replenishment/stock-request-helpers";

describe("stock-request-helpers", () => {
  it("prefers preferred active source and falls back to first active", () => {
    expect(
      pickPreferredSourceId([
        { sourceLocationId: "a", isPreferred: false, isActive: true },
        { sourceLocationId: "b", isPreferred: true, isActive: true },
      ]),
    ).toBe("b");
    expect(
      pickPreferredSourceId([{ sourceLocationId: "a", isPreferred: false, isActive: true }]),
    ).toBe("a");
    expect(pickPreferredSourceId([{ sourceLocationId: "a", isPreferred: true, isActive: false }])).toBe(
      null,
    );
  });

  it("computes remaining qty from fulfilled and in-progress", () => {
    expect(remainingRequestQty(10, 0, 6)).toBe(4);
    expect(remainingRequestQty(10, 6, 0)).toBe(4);
    expect(remainingRequestQty(10, 10, 0)).toBe(0);
  });

  it("detects no configured internal source", () => {
    expect(hasConfiguredInternalSource([])).toBe(false);
    expect(hasConfiguredInternalSource([{ isActive: false }])).toBe(false);
    expect(hasConfiguredInternalSource([{ isActive: true }])).toBe(true);
  });
});
