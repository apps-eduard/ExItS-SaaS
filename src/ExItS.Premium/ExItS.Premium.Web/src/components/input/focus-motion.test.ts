import { describe, expect, it } from "vitest";
import motionCss from "./InputMotion.module.scss?raw";
import {
  INPUT_FOCUS_DRAW_MS,
  INPUT_FOCUS_MOTION_DEFAULT,
  INPUT_FOCUS_MOTION_OPTIONS,
  INPUT_FOCUS_MOTIONS,
  INPUT_FOCUS_REVEAL_MS,
  INPUT_FOCUS_TRACE_MS,
  isInputFocusMotion,
} from "./focus-motion";

describe("input focus motion", () => {
  it("keeps the shipped treatment as the default", () => {
    expect(INPUT_FOCUS_MOTION_DEFAULT).toBe("none");
    expect(INPUT_FOCUS_MOTIONS).toEqual(["none", "border-reveal", "soft-glow", "running-accent", "focus-draw"]);
    expect(INPUT_FOCUS_MOTION_OPTIONS.map((option) => option.id)).toEqual(["border-reveal", "soft-glow", "focus-draw"]);
    expect(isInputFocusMotion("focus-draw")).toBe(true);
    expect(isInputFocusMotion("running-accent")).toBe(true);
    expect(isInputFocusMotion("pulse")).toBe(false);
  });

  it("uses one short CSS pass and keeps a steady focus stroke", () => {
    expect(INPUT_FOCUS_REVEAL_MS).toBe(INPUT_FOCUS_DRAW_MS);
    expect(motionCss).toContain(`${INPUT_FOCUS_REVEAL_MS}ms`);
    expect(motionCss).toContain(`${INPUT_FOCUS_TRACE_MS}ms`);
    expect(motionCss).toContain(`${INPUT_FOCUS_DRAW_MS}ms`);
    expect(motionCss).toContain("stroke: var(--input-accent)");
    expect(motionCss).toContain("linear 1 forwards");
    expect(motionCss).toContain("prefers-reduced-motion: reduce");
    expect(motionCss).not.toContain("infinite");
    expect(motionCss).not.toContain("backdrop-filter");
    expect(motionCss).toContain("inset 0 0 0 2px");
  });
});
