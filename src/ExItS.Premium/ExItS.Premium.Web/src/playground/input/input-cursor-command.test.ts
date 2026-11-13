import { describe, expect, it } from "vitest";
import { inputCursorCommand } from "./input-cursor-command";

const base = {
  appearance: "standard" as const,
  focusMotion: "border-reveal" as const,
  kind: "text" as const,
  status: "none" as const,
  disabled: false,
  readOnly: false,
  required: false,
};

describe("inputCursorCommand", () => {
  it("keeps standard appearance omitted and includes focus", () => {
    expect(inputCursorCommand(base)).toBe("ext input border-reveal");
  });

  it("includes field, appearance, status, and flags", () => {
    expect(
      inputCursorCommand({
        appearance: "outlined",
        focusMotion: "focus-draw",
        kind: "search",
        status: "error",
        disabled: true,
        readOnly: true,
        required: true,
      }),
    ).toBe("ext input search outlined focus-draw error required readonly disabled");
  });
});
