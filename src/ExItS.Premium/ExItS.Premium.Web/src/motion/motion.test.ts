import { describe, expect, it } from "vitest";
import tokens from "../styles/tokens.scss?raw";
import dialogCss from "../components/dialog/Dialog.module.scss?raw";
import sheetCss from "../components/sheet/BottomSheet.module.scss?raw";
import skeletonCss from "../components/skeleton/Skeleton.module.scss?raw";
import {
  DIALOG_DESKTOP_SPRING,
  DIALOG_DESKTOP_VARIANTS,
  DIALOG_SHEET_SPRING,
  DIALOG_SHEET_VARIANTS,
  MOTION_SPRING,
} from "./motion";

describe("motion tokens", () => {
  it("uses the shared durations and disables them for reduced motion", () => {
    expect(tokens).toContain("--motion-fast: 120ms");
    expect(tokens).toContain("--motion-base: 180ms");
    expect(tokens).toContain("--motion-slow: 280ms");
    expect(tokens).toMatch(/@media \(prefers-reduced-motion: reduce\)[\s\S]*--motion-fast: 1ms/);
    expect(dialogCss).toContain("prefers-reduced-motion: reduce");
    expect(sheetCss).toContain("prefers-reduced-motion: reduce");
    expect(skeletonCss).toContain("prefers-reduced-motion: reduce");
  });

  it("keeps dialog motion on shared spring infrastructure", () => {
    expect(MOTION_SPRING.type).toBe("spring");
    expect(DIALOG_DESKTOP_SPRING.type).toBe("spring");
    expect(DIALOG_SHEET_SPRING.type).toBe("spring");
    expect(DIALOG_DESKTOP_SPRING.stiffness).toBeLessThan(MOTION_SPRING.stiffness);
    expect(DIALOG_SHEET_SPRING.stiffness).toBeGreaterThanOrEqual(DIALOG_DESKTOP_SPRING.stiffness);
    expect(DIALOG_SHEET_SPRING.damping).toBeLessThanOrEqual(DIALOG_DESKTOP_SPRING.damping + 1);
  });

  it("matches Inspira Animated Modal desktop 3D and sheet slide variants", () => {
    expect(DIALOG_DESKTOP_SPRING).toMatchObject({ stiffness: 260, damping: 15 });
    expect(DIALOG_DESKTOP_VARIANTS.initial).toMatchObject({
      opacity: 0,
      scale: 0.5,
      rotateX: 80,
      y: 40,
    });
    expect(DIALOG_DESKTOP_VARIANTS.open).toMatchObject({
      opacity: 1,
      scale: 1,
      rotateX: 0,
      y: 0,
    });
    expect(DIALOG_DESKTOP_VARIANTS.exit).toMatchObject({
      opacity: 0,
      scale: 0.8,
      rotateX: 10,
    });
    expect(DIALOG_SHEET_VARIANTS.initial).toMatchObject({ y: "100%", scale: 1, rotateX: 0 });
    expect(DIALOG_SHEET_VARIANTS.open).toMatchObject({ y: 0, scale: 1, rotateX: 0 });
    expect(DIALOG_SHEET_VARIANTS.exit).toMatchObject({ y: "100%", scale: 1, rotateX: 0 });
  });
});
