import { describe, expect, it } from "vitest";
import {
  exitsTableShouldShowPaginationFooter,
  exitsTableShouldShowRowCount,
  TABLE_FOOTER_GLOBAL_CARET_SIZE,
  TABLE_FOOTER_GLOBAL_DEFAULT_APPEARANCE,
  TABLE_FOOTER_GLOBAL_DEFAULT_INTENT,
  TABLE_FOOTER_GLOBAL_HIDE_CONTROLS_AT_OR_BELOW,
  TABLE_FOOTER_GLOBAL_INCLUDE_ALL,
  TABLE_FOOTER_GLOBAL_OPTION_FONT_SIZE,
  TABLE_FOOTER_GLOBAL_OPTION_MIN_HEIGHT,
  TABLE_FOOTER_GLOBAL_OPTION_MIN_WIDTH,
  TABLE_FOOTER_GLOBAL_OPTION_PADDING,
  TABLE_FOOTER_GLOBAL_ROW_COUNT_ALIGN,
  TABLE_FOOTER_GLOBAL_ROW_COUNT_MESSAGE_KEY,
  TABLE_FOOTER_GLOBAL_ROW_COUNT_WEIGHT,
} from "@/features/ui-standards/table-footer-global-default";

describe("Table footer global default", () => {
  it("locks footer intent and appearance", () => {
    expect(TABLE_FOOTER_GLOBAL_DEFAULT_INTENT).toBe("standard");
    expect(TABLE_FOOTER_GLOBAL_DEFAULT_APPEARANCE).toBe("glossy-raise");
  });

  it("locks page-size option density", () => {
    expect(TABLE_FOOTER_GLOBAL_OPTION_MIN_HEIGHT).toBe("32px");
    expect(TABLE_FOOTER_GLOBAL_OPTION_PADDING).toBe("2px 8px");
    expect(TABLE_FOOTER_GLOBAL_OPTION_MIN_WIDTH).toBe("2.5rem");
    expect(TABLE_FOOTER_GLOBAL_OPTION_FONT_SIZE).toBe("14px");
  });

  it("locks nav caret size and All option", () => {
    expect(TABLE_FOOTER_GLOBAL_CARET_SIZE).toBe("1.5rem");
    expect(TABLE_FOOTER_GLOBAL_INCLUDE_ALL).toBe(true);
  });

  it("locks ≤10 row-count: hide pagination, start-aligned bold count", () => {
    expect(TABLE_FOOTER_GLOBAL_HIDE_CONTROLS_AT_OR_BELOW).toBe(10);
    expect(TABLE_FOOTER_GLOBAL_ROW_COUNT_ALIGN).toBe("start");
    expect(TABLE_FOOTER_GLOBAL_ROW_COUNT_WEIGHT).toBe(700);
    expect(TABLE_FOOTER_GLOBAL_ROW_COUNT_MESSAGE_KEY).toBe("exitsTable.rowCount");
    expect(exitsTableShouldShowPaginationFooter(0)).toBe(false);
    expect(exitsTableShouldShowPaginationFooter(10)).toBe(false);
    expect(exitsTableShouldShowPaginationFooter(11)).toBe(true);
    expect(exitsTableShouldShowRowCount(0)).toBe(false);
    expect(exitsTableShouldShowRowCount(1)).toBe(true);
    expect(exitsTableShouldShowRowCount(10)).toBe(true);
    expect(exitsTableShouldShowRowCount(11)).toBe(false);
  });
});
