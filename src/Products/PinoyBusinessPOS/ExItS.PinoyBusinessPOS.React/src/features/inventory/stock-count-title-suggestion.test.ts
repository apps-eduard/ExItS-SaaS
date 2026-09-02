import { describe, expect, it } from "vitest";
import type { CreateStockCountRequest } from "@/api/pos/pos-stock-count-client";
import {
  nextTitleAfterSuggestionInputsChange,
  suggestStockCountTitle,
  weekOfMonth,
} from "@/features/inventory/stock-count-title-suggestion";

const SEP_02 = "2026-09-02";
const SEP_15 = "2026-09-15";

describe("stock count title suggestions", () => {
  it("SC-TITLE-01 Weekly generates suggested weekly title", () => {
    expect(suggestStockCountTitle("Weekly", SEP_02)).toBe("September 2026 - Week 1");
    expect(suggestStockCountTitle("Weekly", SEP_15)).toBe("September 2026 - Week 3");
    expect(weekOfMonth(1)).toBe(1);
    expect(weekOfMonth(8)).toBe(2);
    expect(weekOfMonth(30)).toBe(5);
  });

  it("SC-TITLE-02 Monthly generates month/year", () => {
    expect(suggestStockCountTitle("Monthly", SEP_02)).toBe("September 2026");
  });

  it("SC-TITLE-03 Quarterly generates Qx year", () => {
    expect(suggestStockCountTitle("Quarterly", SEP_02)).toBe("Q3 2026");
    expect(suggestStockCountTitle("Quarterly", "2026-01-10")).toBe("Q1 2026");
    expect(suggestStockCountTitle("Quarterly", "2026-12-01")).toBe("Q4 2026");
  });

  it("SC-TITLE-04 Annual generates annual title", () => {
    expect(suggestStockCountTitle("Annual", SEP_02)).toBe("2026 Annual Stock Count");
  });

  it("SC-TITLE-05 user can edit suggested title", () => {
    const suggested = suggestStockCountTitle("Weekly", SEP_02);
    const edited = "09 Week 1";
    expect(suggested).toBe("September 2026 - Week 1");
    expect(edited).not.toBe(suggested);
    expect(edited.length).toBeGreaterThan(0);
  });

  it("SC-TITLE-06 user custom edit survives rerender / date refresh", () => {
    const custom = "September Week 1 - Main Shelf";
    const afterDateChange = nextTitleAfterSuggestionInputsChange({
      period: "Weekly",
      countDate: SEP_15,
      currentTitle: custom,
      titleDirty: true,
    });
    expect(afterDateChange).toBe(custom);

    const afterPeriodChange = nextTitleAfterSuggestionInputsChange({
      period: "Monthly",
      countDate: SEP_02,
      currentTitle: custom,
      titleDirty: true,
    });
    expect(afterPeriodChange).toBe(custom);
  });

  it("SC-TITLE-07 Custom is not forced to generated title", () => {
    expect(suggestStockCountTitle("Custom", SEP_02)).toBe("");
    const keptBlank = nextTitleAfterSuggestionInputsChange({
      period: "Custom",
      countDate: SEP_02,
      currentTitle: "",
      titleDirty: false,
    });
    expect(keptBlank).toBe("");

    const customTyped = nextTitleAfterSuggestionInputsChange({
      period: "Custom",
      countDate: SEP_15,
      currentTitle: "Cycle Count A",
      titleDirty: true,
    });
    expect(customTyped).toBe("Cycle Count A");
  });

  it("SC-TITLE-08 no inventory/count quantity behavior changed", () => {
    const body: CreateStockCountRequest = {
      title: "September 2026 - Week 1",
      lines: [{ productId: "11111111-1111-1111-1111-111111111111" }],
      countDate: SEP_02,
      notes: null,
    };
    expect(body).not.toHaveProperty("period");
    expect(body).not.toHaveProperty("periodType");
    expect(Object.keys(body).sort()).toEqual(["countDate", "lines", "notes", "title"]);
    expect(body.lines[0]).toEqual({
      productId: "11111111-1111-1111-1111-111111111111",
    });
    expect(body.lines[0]).not.toHaveProperty("countedQuantity");
  });

  it("refreshes suggestion when title is not dirty", () => {
    expect(
      nextTitleAfterSuggestionInputsChange({
        period: "Monthly",
        countDate: SEP_02,
        currentTitle: "stale",
        titleDirty: false,
      }),
    ).toBe("September 2026");
  });
});
