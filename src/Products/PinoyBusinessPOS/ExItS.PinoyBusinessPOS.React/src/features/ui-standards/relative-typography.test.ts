import { describe, expect, it } from "vitest";
import {
  adjustFontSize,
  adjustFontWeight,
  applyTypographyToken,
  SIDENAV_BASE_TYPOGRAPHY,
  typographyCursorCommand,
  typographyFromOriginal,
} from "@/features/ui-standards/relative-typography";

describe("relative typography adjustments", () => {
  it("raises font-weight by a percent and keeps a minimum step of 10", () => {
    expect(adjustFontWeight(400, 1)).toBe(410);
    expect(adjustFontWeight(400, 5)).toBe(420);
    expect(adjustFontWeight(500, 1)).toBe(510);
  });

  it("lowers font-weight by a percent, rounds, and clamps between 100 and 900", () => {
    expect(adjustFontWeight(400, -1)).toBe(390);
    expect(adjustFontWeight(400, 25)).toBe(500);
    expect(adjustFontWeight(400, 2.6)).toBe(410);
    expect(adjustFontWeight(890, 20)).toBe(900);
    expect(adjustFontWeight(105, -1)).toBe(100);
  });

  it("scales font-size by a pure percent and keeps the unit", () => {
    expect(adjustFontSize("14px", 1)).toBe("14.14px");
    expect(adjustFontSize("0.875rem", 1)).toBe("0.88375rem");
    expect(adjustFontSize("16px", -10)).toBe("14.4px");
    expect(adjustFontSize("1.25em", 0)).toBe("1.25em");
  });

  it("adds repeated presses into one total measured from the original", () => {
    expect(typographyCursorCommand(26, 20)).toBe("ext typography FW+26 FZ+20");
    expect(typographyCursorCommand(0, 0, 26)).toBe("ext typography FWZ+26");
    expect(typographyFromOriginal(0, 0, 26)).toEqual({ fontWeight: 630, fontSize: "1.1025rem" });
    expect(typographyCursorCommand(0, 0)).toBe("ext typography");
    expect(typographyFromOriginal(26, 20)).toEqual({ fontWeight: 630, fontSize: "1.05rem" });
    expect(typographyFromOriginal(13, 0).fontWeight).toBe(565);
  });

  it("applies FW and FZ tokens to the current snapshot", () => {
    const heavier = applyTypographyToken(SIDENAV_BASE_TYPOGRAPHY, "FW+1");
    expect(heavier.fontWeight).toBe(510);
    expect(heavier.fontSize).toBe("0.875rem");

    const smaller = applyTypographyToken({ fontWeight: 400, fontSize: "14px" }, "FZ-1");
    expect(smaller.fontWeight).toBe(400);
    expect(smaller.fontSize).toBe("13.86px");
  });
});
