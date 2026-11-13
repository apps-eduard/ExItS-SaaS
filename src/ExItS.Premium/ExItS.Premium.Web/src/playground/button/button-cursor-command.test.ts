import { describe, expect, it } from "vitest";
import { buttonCursorCommand } from "./button-cursor-command";

const base = {
  variant: "primary" as const,
  appearance: "solid" as const,
  icon: "none",
  iconOnly: false,
  fullWidth: false,
  loading: false,
  disabled: false,
  ripple: true,
};

describe("buttonCursorCommand", () => {
  it("omits primary solid defaults and keeps ripple", () => {
    expect(buttonCursorCommand(base)).toBe("ext btn ripple");
  });

  it("includes tone, appearance, icon, and states", () => {
    expect(
      buttonCursorCommand({
        ...base,
        variant: "danger",
        appearance: "soft",
        icon: "check",
        iconOnly: true,
        fullWidth: true,
        loading: true,
        disabled: true,
        ripple: true,
      }),
    ).toBe("ext btn icon danger soft check full-width loading ripple disabled");
  });

  it("drops ripple when off", () => {
    expect(buttonCursorCommand({ ...base, ripple: false })).toBe("ext btn");
  });
});
