import { describe, expect, it } from "vitest";
import {
  formatStockUseOccurredDate,
  isStockUseReasonCode,
  stockUseReasonLabelKey,
  stockUseStatusLabelKey,
  sumStockUseLineCosts,
} from "@/features/inventory/stock-use-labels";

describe("stock-use-labels", () => {
  it("maps reason codes to i18n keys", () => {
    expect(stockUseReasonLabelKey("InternalOperations")).toBe(
      "stockUse.reason.internalOperations",
    );
    expect(stockUseReasonLabelKey("StaffUse")).toBe("stockUse.reason.staffUse");
    expect(stockUseReasonLabelKey("SampleOrTesting")).toBe(
      "stockUse.reason.sampleOrTesting",
    );
    expect(stockUseReasonLabelKey("Other")).toBe("stockUse.reason.other");
    expect(stockUseReasonLabelKey("Unknown")).toBe("stockUse.reason.other");
  });

  it("maps status codes", () => {
    expect(stockUseStatusLabelKey("Posted")).toBe("stockUse.status.posted");
    expect(stockUseStatusLabelKey("Voided")).toBe("stockUse.status.voided");
  });

  it("validates reason codes", () => {
    expect(isStockUseReasonCode("InternalOperations")).toBe(true);
    expect(isStockUseReasonCode("Nope")).toBe(false);
  });

  it("sums line costs only when all authoritative", () => {
    expect(sumStockUseLineCosts([{ lineCostSnapshot: 10 }, { lineCostSnapshot: 5 }])).toBe(15);
    expect(sumStockUseLineCosts([{ lineCostSnapshot: 10 }, { lineCostSnapshot: null }])).toBe(
      null,
    );
    expect(sumStockUseLineCosts([])).toBe(null);
  });

  it("formats occurred dates", () => {
    const formatted = formatStockUseOccurredDate("2026-08-29T12:00:00.000Z");
    expect(formatted.length).toBeGreaterThan(0);
    expect(formatStockUseOccurredDate("not-a-date")).toBe("not-a-date");
  });
});
