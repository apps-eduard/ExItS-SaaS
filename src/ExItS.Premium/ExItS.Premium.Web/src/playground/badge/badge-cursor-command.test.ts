import { describe, expect, it } from "vitest";
import { badgeCursorCommand } from "./badge-cursor-command";

describe("badgeCursorCommand", () => {
  it("omits default tone and soft appearance", () => {
    expect(badgeCursorCommand({ tone: "neutral", appearance: "soft" })).toBe("ext badge");
  });

  it("includes tone and non-default appearance", () => {
    expect(badgeCursorCommand({ tone: "success", appearance: "soft" })).toBe("ext badge success");
    expect(badgeCursorCommand({ tone: "warning", appearance: "solid" })).toBe("ext badge warning solid");
    expect(badgeCursorCommand({ tone: "danger", appearance: "outline" })).toBe("ext badge danger outline");
  });
});
