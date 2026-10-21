import { describe, expect, it } from "vitest";
import {
  TABLE_FOOTER_GLOBAL_CARET_SIZE,
  TABLE_FOOTER_GLOBAL_DEFAULT_APPEARANCE,
  TABLE_FOOTER_GLOBAL_DEFAULT_INTENT,
  TABLE_FOOTER_GLOBAL_INCLUDE_ALL,
  TABLE_FOOTER_GLOBAL_OPTION_FONT_SIZE,
  TABLE_FOOTER_GLOBAL_OPTION_MIN_HEIGHT,
  TABLE_FOOTER_GLOBAL_OPTION_MIN_WIDTH,
  TABLE_FOOTER_GLOBAL_OPTION_PADDING,
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
});
