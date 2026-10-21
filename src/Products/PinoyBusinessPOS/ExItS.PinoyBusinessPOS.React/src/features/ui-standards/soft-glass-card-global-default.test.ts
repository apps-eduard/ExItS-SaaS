import { describe, expect, it } from "vitest";
import {
  SOFT_GLASS_BORDER_SLATE,
  SOFT_GLASS_BORDER_SLATE_PCT,
  SOFT_GLASS_DARK,
  SOFT_GLASS_FOCUS,
  SOFT_GLASS_LABEL_SIZE,
  SOFT_GLASS_LABEL_WEIGHT,
  SOFT_GLASS_LIGHT,
  SOFT_GLASS_SHADOW_ACCENT_PCT,
  SOFT_GLASS_TRANSITION_MS,
} from "@/features/ui-standards/soft-glass-card-global-default";

describe("Soft Glass card global default", () => {
  it("locks type, focus, and motion", () => {
    expect(SOFT_GLASS_LABEL_SIZE).toBe("0.875rem");
    expect(SOFT_GLASS_LABEL_WEIGHT).toBe(600);
    expect(SOFT_GLASS_FOCUS).toBe("#8b5cf6");
    expect(SOFT_GLASS_TRANSITION_MS).toBe(120);
    expect(SOFT_GLASS_SHADOW_ACCENT_PCT).toBe(8);
    expect(SOFT_GLASS_BORDER_SLATE).toBe("#e2e8f0");
    expect(SOFT_GLASS_BORDER_SLATE_PCT).toBe(62);
  });

  it("locks light Quick Action color identities", () => {
    expect(SOFT_GLASS_LIGHT.lent.chip).toBe("#10b981");
    expect(SOFT_GLASS_LIGHT.owe.chip).toBe("#3b82f6");
    expect(SOFT_GLASS_LIGHT.owe.ink).toBe("#1e40af");
    expect(SOFT_GLASS_LIGHT.people.chip).toBe("#4169e1");
    expect(SOFT_GLASS_LIGHT.info.chip).toBe("#0ea5e9");
  });

  it("locks dark Quick Action color identities without neon cyan labels", () => {
    expect(SOFT_GLASS_DARK.lent.chip).toBe("#047857");
    expect(SOFT_GLASS_DARK.owe.chip).toBe("#1d4ed8");
    expect(SOFT_GLASS_DARK.label).toBe("#e2e8f0");
    expect(SOFT_GLASS_DARK.surfaceMid).toBe("#1e293b");
  });
});
