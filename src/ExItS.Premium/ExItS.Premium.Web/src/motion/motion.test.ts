import { describe, expect, it } from "vitest";
import tokens from "../styles/tokens.scss?raw";
import dialogCss from "../components/dialog/Dialog.module.scss?raw";
import sheetCss from "../components/sheet/BottomSheet.module.scss?raw";
import skeletonCss from "../components/skeleton/Skeleton.module.scss?raw";

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
});
