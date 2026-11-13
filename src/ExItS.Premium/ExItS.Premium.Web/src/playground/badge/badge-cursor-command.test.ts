import { describe, expect, it } from "vitest";
import { badgeCursorCommand } from "./badge-cursor-command";

describe("badgeCursorCommand", () => {
  it("omits the default neutral tone", () => {
    expect(badgeCursorCommand({ tone: "neutral" })).toBe("ext badge");
  });

  it("includes non-default tones", () => {
    expect(badgeCursorCommand({ tone: "success" })).toBe("ext badge success");
    expect(badgeCursorCommand({ tone: "warning" })).toBe("ext badge warning");
  });
});
