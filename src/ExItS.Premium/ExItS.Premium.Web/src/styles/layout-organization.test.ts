import { describe, expect, it } from "vitest";
import globalCss from "../styles/global.scss?raw";
import playgroundCss from "../playground/Playground.module.scss?raw";
import dialogCss from "../components/dialog/Dialog.module.scss?raw";
import sheetCss from "../components/sheet/BottomSheet.module.scss?raw";
import wheelCss from "../components/time/TimeWheel.module.scss?raw";
import indexHtml from "../../index.html?raw";

describe("layout organization refinements", () => {
  it("lets informational text be selected and keeps controls and the time wheel from selecting", () => {
    expect(globalCss).toMatch(/button\s*\{[^}]*user-select:\s*none/);
    expect(globalCss).toMatch(/\.type-numeric\s*\{[^}]*user-select:\s*text/);
    expect(playgroundCss).toMatch(/\.txAmt\s*\{[^}]*user-select:\s*text/);
    expect(wheelCss).toMatch(/touch-action:\s*pan-y;\s*user-select:\s*none/);
  });

  it("insets the shell once and gives the mobile drawer a 44px row target", () => {
    expect(indexHtml).toContain("viewport-fit=cover");
    expect(playgroundCss).toMatch(/\.shell\s*\{[^}]*padding:\s*env\(safe-area-inset-top\)/);
    expect(playgroundCss).toMatch(/\.drawer\[open\]\s*\{[^}]*env\(safe-area-inset-top\)/);
    expect(playgroundCss).toMatch(/@media \(max-width: 767px\)\s*\{[^}]*\.drawer \.navChild\s*\{[^}]*min-height:\s*44px/);
    expect(sheetCss).toContain("env(safe-area-inset-bottom)");
    expect(dialogCss).not.toMatch(/0\.9rem \+ env\(safe-area-inset-bottom/);
  });
});
