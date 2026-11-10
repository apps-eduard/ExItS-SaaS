import { describe, expect, it } from "vitest";
import calendarCss from "./CalendarMonth.module.scss?raw";
import rangeCss from "./DateRangePicker.module.scss?raw";
import surfaceCss from "./surface.module.scss?raw";

describe("date picker responsive styles", () => {
  it("covers phone, tablet, and desktop widths and reduced motion", () => {
    expect(calendarCss).toContain("max-width: 390px");
    expect(calendarCss).toContain("max-width: 767px");
    expect(calendarCss).toContain("min-width: 768px");
    expect(calendarCss).toContain("min-width: 1024px");
    expect(calendarCss).toContain("prefers-reduced-motion: reduce");
    expect(rangeCss).toContain("min-width: 768px");
    expect(rangeCss).toContain("max-width: 767px");
    expect(rangeCss).toContain("min-width: 1024px");
    expect(surfaceCss).toContain("min-width: 768px");
    expect(surfaceCss).toContain("min-width: 1024px");
    expect(surfaceCss).toContain("min-width: 1440px");
    expect(surfaceCss).toContain("max-width: 767px");
    expect(surfaceCss).toContain("prefers-reduced-motion: reduce");
    expect(surfaceCss).not.toMatch(/#[0-9a-fA-F]{3,8}/);
    expect(calendarCss).not.toMatch(/#[0-9a-fA-F]{3,8}/);
  });
});
