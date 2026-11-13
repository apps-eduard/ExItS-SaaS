import { describe, expect, it } from "vitest";
import {
  REFERENCE_PALETTES,
  REFERENCE_SHADE_STEPS,
  countReferenceSwatches,
  filterReferencePalettes,
  referenceShadeLabel,
  referenceShadeName,
} from "./reference-palettes";

const EXPECTED_IDS = [
  "slate",
  "gray",
  "zinc",
  "neutral",
  "stone",
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
  "charcoal",
  "ivory",
  "coral",
  "peach",
  "terracotta",
  "burgundy",
  "gold",
  "sage",
  "forest",
  "turquoise",
  "azure",
  "navy",
  "lavender",
  "periwinkle",
  "magenta",
] as const;

describe("reference-palettes", () => {
  it("exposes 40 families with 400 curated hex swatches", () => {
    expect(REFERENCE_PALETTES).toHaveLength(40);
    expect(countReferenceSwatches()).toBe(400);

    for (const family of REFERENCE_PALETTES) {
      expect(Object.keys(family.shades)).toHaveLength(10);
      for (const step of REFERENCE_SHADE_STEPS) {
        expect(family.shades[step]).toMatch(/^#[0-9a-f]{6}$/i);
      }
    }
  });

  it("includes the required family ids in order", () => {
    expect(REFERENCE_PALETTES.map((family) => family.id)).toEqual([...EXPECTED_IDS]);
  });

  it("filters by name, shade, and hex", () => {
    expect(filterReferencePalettes("sage").map((f) => f.id)).toEqual(["sage"]);
    expect(filterReferencePalettes("500").every((f) => f.matchingSteps.includes(500))).toBe(true);
    expect(filterReferencePalettes("#22c55e").map((f) => f.id)).toEqual(["green"]);
    expect(filterReferencePalettes("mint-500").map((f) => f.id)).toEqual(["mint"]);
    expect(referenceShadeName("sage", 500)).toBe("sage-500");
    expect(referenceShadeLabel("Sage", 500)).toBe("Sage 500");
  });
});
