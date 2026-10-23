import { describe, expect, it } from "vitest";
import {
  TOOLTIP_GLOBAL_ARROW_PX,
  TOOLTIP_GLOBAL_DEFAULT_COMMAND,
  TOOLTIP_GLOBAL_DEFAULT_PLACEMENT,
  TOOLTIP_GLOBAL_DEFAULT_SHELL,
  TOOLTIP_GLOBAL_FOCUS_DELAY_MS,
  TOOLTIP_GLOBAL_GAP_PX,
  TOOLTIP_GLOBAL_HOVER_DELAY_MS,
} from "@/features/ui-standards/tooltip-global-default";

describe("Tooltip global default", () => {
  it("locks Soft Glass top placement command", () => {
    expect(TOOLTIP_GLOBAL_DEFAULT_COMMAND).toBe("ext tooltip soft-glass top");
  });

  it("locks Soft Glass shell and top placement for icon actions", () => {
    expect(TOOLTIP_GLOBAL_DEFAULT_SHELL).toBe("soft-glass");
    expect(TOOLTIP_GLOBAL_DEFAULT_PLACEMENT).toBe("top");
    expect(TOOLTIP_GLOBAL_HOVER_DELAY_MS).toBe(300);
    expect(TOOLTIP_GLOBAL_FOCUS_DELAY_MS).toBe(40);
    expect(TOOLTIP_GLOBAL_GAP_PX).toBe(10);
    expect(TOOLTIP_GLOBAL_ARROW_PX).toBe(8);
  });
});
