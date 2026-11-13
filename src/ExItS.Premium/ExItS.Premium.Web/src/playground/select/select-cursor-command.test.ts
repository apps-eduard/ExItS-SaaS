import { describe, expect, it } from "vitest";
import { selectCursorCommand } from "./select-cursor-command";

describe("selectCursorCommand", () => {
  it("omits stacked and scale defaults", () => {
    expect(
      selectCursorCommand({
        appearance: "stacked",
        focusMotion: "none",
        menuMotion: "scale",
        searchable: false,
        multiple: false,
      }),
    ).toBe("ext dropdown");
  });

  it("includes appearance, focus, transition, and flags", () => {
    expect(
      selectCursorCommand({
        appearance: "soft",
        focusMotion: "border-reveal",
        menuMotion: "flip-up",
        searchable: true,
        multiple: true,
      }),
    ).toBe("ext dropdown soft border-reveal flip-up searchable multi");
  });
});
