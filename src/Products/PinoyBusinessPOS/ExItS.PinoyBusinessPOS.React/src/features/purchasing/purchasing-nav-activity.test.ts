import { describe, expect, it } from "vitest";
import {
  derivePurchasingNavigationCount,
  formatPurchasingNavBadgeCount,
} from "@/features/purchasing/purchasing-nav-activity";

describe("derivePurchasingNavigationCount", () => {
  it("sums non-overlapping incoming + receivable activity", () => {
    expect(
      derivePurchasingNavigationCount({
        incomingPendingCount: 2,
        receivableCount: 0,
      }),
    ).toBe(2);
    expect(
      derivePurchasingNavigationCount({
        incomingPendingCount: 2,
        receivableCount: 3,
      }),
    ).toBe(5);
  });

  it("does not invent counts from excluded master-data buckets", () => {
    // Suppliers / PO totals / direct history are intentionally omitted from the API.
    expect(
      derivePurchasingNavigationCount({
        incomingPendingCount: 0,
        receivableCount: 0,
      }),
    ).toBe(0);
  });

  it("clamps negative inputs", () => {
    expect(
      derivePurchasingNavigationCount({
        incomingPendingCount: -1,
        receivableCount: 4,
      }),
    ).toBe(4);
  });
});

describe("formatPurchasingNavBadgeCount", () => {
  it("caps display at 99+", () => {
    expect(formatPurchasingNavBadgeCount(1)).toBe("1");
    expect(formatPurchasingNavBadgeCount(99)).toBe("99");
    expect(formatPurchasingNavBadgeCount(100)).toBe("99+");
  });
});
