import { describe, expect, it } from "vitest";
import { badgeCursorCommand } from "./badge-cursor-command";

const base = {
  tone: "neutral" as const,
  appearance: "soft" as const,
  allCaps: false,
  indicator: false,
};

describe("badgeCursorCommand", () => {
  it("omits default tone and soft appearance", () => {
    expect(badgeCursorCommand(base)).toBe("ext badge");
  });

  it("includes tone, appearance, and switches", () => {
    expect(badgeCursorCommand({ ...base, tone: "success" })).toBe("ext badge success");
    expect(badgeCursorCommand({ ...base, tone: "warning", appearance: "solid" })).toBe("ext badge warning solid");
    expect(badgeCursorCommand({ ...base, tone: "danger", appearance: "outline" })).toBe("ext badge danger outline");
    expect(badgeCursorCommand({ ...base, tone: "success", appearance: "emphasis" })).toBe("ext badge success emphasis");
    expect(badgeCursorCommand({ ...base, tone: "help" })).toBe("ext badge help");
    expect(badgeCursorCommand({ ...base, tone: "contrast", appearance: "outline" })).toBe("ext badge contrast outline");
    expect(badgeCursorCommand({ ...base, tone: "success", allCaps: true, indicator: true })).toBe(
      "ext badge success all-caps indicator",
    );
  });
});
