import { describe, expect, it } from "vitest";
import {
  hasReceiveCostMarginWarning,
  receiveCostMarginKind,
  resolveReceiveEffectiveSellingPrice,
} from "@/features/purchasing/receive-cost-margin";

describe("receive-cost-margin", () => {
  it("prefers branch-effective selling price over organization default", () => {
    expect(
      resolveReceiveEffectiveSellingPrice({
        sellingPrice: 200,
        effectiveSellingPrice: 180,
        hasBranchPriceOverride: true,
      }),
    ).toBe(180);
    expect(
      resolveReceiveEffectiveSellingPrice({
        sellingPrice: 200,
        effectiveSellingPrice: null,
      }),
    ).toBe(200);
  });

  it("classifies cost vs effective selling price", () => {
    expect(receiveCostMarginKind(150, 200)).toBe("none");
    expect(receiveCostMarginKind(200, 200)).toBe("zeroMargin");
    expect(receiveCostMarginKind(300, 200)).toBe("negativeMargin");
    expect(hasReceiveCostMarginWarning(150, 200)).toBe(false);
    expect(hasReceiveCostMarginWarning(200, 200)).toBe(true);
    expect(hasReceiveCostMarginWarning(300, 200)).toBe(true);
  });
});
