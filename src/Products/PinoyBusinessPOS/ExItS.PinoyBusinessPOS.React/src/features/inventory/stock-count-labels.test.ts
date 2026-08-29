import { describe, expect, it } from "vitest";
import {
  differenceProductCount,
  formatVariance,
  lineMatchesFilter,
  parseCountedQuantity,
  previewVariance,
  stockCountStatusLabelKey,
  stockCountStatusTone,
  summarizeCountLines,
} from "@/features/inventory/stock-count-labels";
import type { StockCountLineDto } from "@/api/pos/pos-stock-count-client";

function line(partial: Partial<StockCountLineDto> & Pick<StockCountLineDto, "productId">): StockCountLineDto {
  return {
    lineId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    productName: "Coke",
    unitOfMeasure: "pcs",
    lineNumber: 1,
    systemOnHandSnapshot: 24,
    countedQuantity: null,
    variance: null,
    ...partial,
  };
}

describe("stock-count-labels", () => {
  it("maps status labels and tones", () => {
    expect(stockCountStatusLabelKey("InProgress")).toBe("stockCount.status.inProgress");
    expect(stockCountStatusTone("Completed")).toBe("success");
    expect(stockCountStatusTone("Cancelled")).toBe("danger");
  });

  it("formats variance with sign and treats zero distinctly", () => {
    expect(formatVariance(3)).toBe("+3");
    expect(formatVariance(-2)).toBe("-2");
    expect(formatVariance(0)).toBe("0");
    expect(formatVariance(null)).toBe("—");
  });

  it("previews variance from counted text without treating empty as zero", () => {
    expect(previewVariance(24, "")).toBeNull();
    expect(previewVariance(24, "0")).toBe(-24);
    expect(previewVariance(10.5, "9.75")).toBeCloseTo(-0.75);
  });

  it("parses counted quantity with explicit zero", () => {
    expect(parseCountedQuantity("")).toBeNull();
    expect(parseCountedQuantity("0")).toBe(0);
    expect(parseCountedQuantity("-1")).toBe("invalid");
    expect(parseCountedQuantity("8.5")).toBe(8.5);
  });

  it("filters lines for not counted / difference / matched", () => {
    const coke = line({ productId: "p1", countedQuantity: null });
    const rice = line({
      productId: "p2",
      countedQuantity: 10,
      variance: 0,
      systemOnHandSnapshot: 10,
    });
    const milk = line({
      productId: "p3",
      countedQuantity: 9,
      variance: 1,
      systemOnHandSnapshot: 8,
    });
    expect(lineMatchesFilter(coke, undefined, "notCounted")).toBe(true);
    expect(lineMatchesFilter(rice, undefined, "matched")).toBe(true);
    expect(lineMatchesFilter(milk, undefined, "hasDifference")).toBe(true);
  });

  it("summarizes progress without inventing aggregate mixed-unit totals", () => {
    const lines = [
      line({ productId: "p1", countedQuantity: 22, variance: -2, systemOnHandSnapshot: 24 }),
      line({ productId: "p2", countedQuantity: 10, variance: 0, systemOnHandSnapshot: 10 }),
      line({ productId: "p3", countedQuantity: null, variance: null }),
    ];
    const summary = summarizeCountLines(lines);
    expect(summary).toEqual({
      total: 3,
      counted: 2,
      remaining: 1,
      matched: 1,
      lower: 1,
      higher: 0,
    });
    expect(differenceProductCount(lines)).toBe(1);
  });
});
