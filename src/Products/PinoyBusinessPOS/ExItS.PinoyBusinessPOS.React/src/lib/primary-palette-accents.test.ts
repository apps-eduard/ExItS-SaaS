import { describe, expect, it } from "vitest";
import {
  AMBIENT_PRIMARY_COLORS,
  pickNextAmbientPrimary,
  PRIMARY_PALETTE_ACCENTS_LIGHT,
  resolveAmbientPrimaryAccent,
} from "@/lib/primary-palette-accents";

describe("primary palette accents (decorative)", () => {
  it("covers every canonical Primary option", () => {
    expect(AMBIENT_PRIMARY_COLORS).toHaveLength(9);
    for (const color of AMBIENT_PRIMARY_COLORS) {
      expect(PRIMARY_PALETTE_ACCENTS_LIGHT[color]).toMatch(/^#[0-9a-f]{6}$/i);
      expect(resolveAmbientPrimaryAccent(color, false)).toBe(PRIMARY_PALETTE_ACCENTS_LIGHT[color]);
      expect(resolveAmbientPrimaryAccent(color, true)).not.toBe(PRIMARY_PALETTE_ACCENTS_LIGHT[color]);
    }
  });

  it("never picks the same Primary consecutively when alternatives exist", () => {
    for (let i = 0; i < 40; i += 1) {
      const next = pickNextAmbientPrimary("blue");
      expect(next).not.toBe("blue");
      expect(AMBIENT_PRIMARY_COLORS).toContain(next);
    }
  });

  it("returns the only option when the palette is a singleton", () => {
    expect(pickNextAmbientPrimary("green", ["green"])).toBe("green");
  });
});
