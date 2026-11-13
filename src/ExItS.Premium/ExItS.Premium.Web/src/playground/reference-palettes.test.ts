import { describe, expect, it } from "vitest";
import {
  REFERENCE_PALETTES,
  REFERENCE_SHADE_STEPS,
  countReferenceSwatches,
  filterReferencePalettes,
  referenceShadeName,
} from "./reference-palettes";

describe("reference-palettes", () => {
  it("exposes 25 families with 250 curated hex swatches", () => {
    expect(REFERENCE_PALETTES).toHaveLength(25);
    expect(countReferenceSwatches()).toBe(250);

    for (const family of REFERENCE_PALETTES) {
      expect(Object.keys(family.shades)).toHaveLength(10);
      for (const step of REFERENCE_SHADE_STEPS) {
        expect(family.shades[step]).toMatch(/^#[0-9a-f]{6}$/i);
      }
    }
  });

  it("includes the required family ids in order", () => {
    expect(REFERENCE_PALETTES.map((family) => family.id)).toEqual([
      "slate",
      "gray",
      "zinc",
      "stone",
      "neutral",
      "red",
      "rose",
      "pink",
      "fuchsia",
      "purple",
      "violet",
      "indigo",
      "blue",
      "sky",
      "cyan",
      "teal",
      "emerald",
      "green",
      "lime",
      "yellow",
      "amber",
      "orange",
      "brown",
      "olive",
      "mint",
    ]);
  });

  it("filters by name, shade, and hex", () => {
    expect(filterReferencePalettes("mint-500").map((f) => f.id)).toEqual(["mint"]);
    expect(filterReferencePalettes("700").every((f) => f.matchingSteps.includes(700))).toBe(true);
    expect(filterReferencePalettes("8f5a3c").map((f) => f.id)).toEqual(["brown"]);
    expect(referenceShadeName("brown", 700)).toBe("brown-700");
  });
});
