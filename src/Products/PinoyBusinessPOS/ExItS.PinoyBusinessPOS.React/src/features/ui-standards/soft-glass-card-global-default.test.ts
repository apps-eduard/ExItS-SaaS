import { describe, expect, it } from "vitest";
import {
  SOFT_GLASS_BORDER_SLATE,
  SOFT_GLASS_BORDER_SLATE_PCT,
  UI_STANDARDS_CRYSTAL_BORDER_DARK_CHIP_PCT,
  UI_STANDARDS_CRYSTAL_BORDER_SLATE_PCT,
  UI_STANDARDS_CRYSTAL_SCREEN_BORDER_COMMAND,
  UI_STANDARDS_CRYSTAL_SCREEN_INSET,
  UI_STANDARDS_CRYSTAL_SCREEN_RADIUS,
  UI_STANDARDS_CRYSTAL_SHADOW_ACCENT_PCT,
  SOFT_GLASS_DARK,
  SOFT_GLASS_DARK_BORDER_CHIP_PCT,
  SOFT_GLASS_DARK_BORDER_HOVER_CHIP_PCT,
  SOFT_GLASS_DARK_BORDER_HOVER_SLATE,
  SOFT_GLASS_DARK_BORDER_SLATE,
  SOFT_GLASS_FIELD_DEFAULT_COMMAND,
  SOFT_GLASS_FIELD_RADIUS,
  SOFT_GLASS_FOCUS,
  SOFT_GLASS_LABEL_SIZE,
  SOFT_GLASS_LABEL_WEIGHT,
  SOFT_GLASS_LIGHT,
  SOFT_GLASS_QA_DEFAULT_COMMAND,
  SOFT_GLASS_QA_TRAILING,
  SOFT_GLASS_RADIUS,
  SOFT_GLASS_RIBBON_DEFAULT_COMMAND,
  SOFT_GLASS_RIBBON_RADIUS,
  SOFT_GLASS_SHADOW_ACCENT_PCT,
  SOFT_GLASS_TRANSITION_MS,
  softGlassQaTrailing,
} from "@/features/ui-standards/soft-glass-card-global-default";
import { TABLE_GLOBAL_SOFT_GLASS_RADIUS } from "@/features/ui-standards/table-global-default";

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

  it("locks Soft Glass shell and title ribbon radius", () => {
    expect(SOFT_GLASS_RADIUS).toBe("1.1rem");
    expect(SOFT_GLASS_RADIUS).toBe(TABLE_GLOBAL_SOFT_GLASS_RADIUS);
    expect(SOFT_GLASS_RIBBON_RADIUS).toBe("1.1rem 1.1rem 1.1rem 0");
    expect(SOFT_GLASS_RIBBON_DEFAULT_COMMAND).toBe("ext soft-glass ribbon radius");
  });

  it("locks Soft Glass input / dropdown field radius (Soft control)", () => {
    expect(SOFT_GLASS_FIELD_RADIUS).toBe("0.6875rem");
    expect(SOFT_GLASS_FIELD_DEFAULT_COMMAND).toBe("ext soft-glass field radius");
  });

  it("locks dark Soft Glass Quick Action border (home QA)", () => {
    expect(SOFT_GLASS_DARK_BORDER_SLATE).toBe("#475569");
    expect(SOFT_GLASS_DARK_BORDER_CHIP_PCT).toBe(16);
    expect(SOFT_GLASS_DARK_BORDER_HOVER_SLATE).toBe("#64748b");
    expect(SOFT_GLASS_DARK_BORDER_HOVER_CHIP_PCT).toBe(22);
  });

  it("locks Soft Glass screen-edge frame (Personal + UI Standards)", () => {
    expect(UI_STANDARDS_CRYSTAL_SCREEN_BORDER_COMMAND).toBe("ext ui-standards crystal screen-border");
    expect(UI_STANDARDS_CRYSTAL_BORDER_SLATE_PCT).toBe(SOFT_GLASS_BORDER_SLATE_PCT);
    expect(UI_STANDARDS_CRYSTAL_BORDER_DARK_CHIP_PCT).toBe(SOFT_GLASS_DARK_BORDER_CHIP_PCT);
    expect(UI_STANDARDS_CRYSTAL_SHADOW_ACCENT_PCT).toBe(SOFT_GLASS_SHADOW_ACCENT_PCT);
    expect(UI_STANDARDS_CRYSTAL_SCREEN_INSET).toBe("0.45rem");
    expect(UI_STANDARDS_CRYSTAL_SCREEN_RADIUS).toBe(SOFT_GLASS_RADIUS);
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

  it("locks Soft Glass Quick Action trailing ends", () => {
    expect(SOFT_GLASS_QA_TRAILING).toEqual(["count", "chevron", "add"]);
    expect(SOFT_GLASS_QA_DEFAULT_COMMAND).toBe("ext soft-glass qa trailing-auto");
    expect(softGlassQaTrailing({ count: 0 })).toBe("count");
    expect(softGlassQaTrailing({ count: 3 })).toBe("count");
    expect(softGlassQaTrailing({})).toBe("chevron");
    expect(softGlassQaTrailing({ trailing: "add" })).toBe("add");
  });
});
