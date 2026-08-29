import { describe, expect, it } from "vitest";
import {
  isSellFloorBusinessUsage,
  resolveBusinessUsage,
} from "@/features/catalog/product-business-usage";

describe("resolveBusinessUsage", () => {
  it("prefers explicit businessUsage when valid", () => {
    expect(resolveBusinessUsage({ businessUsage: "Ingredient", canBeSold: true })).toBe(
      "Ingredient",
    );
    expect(resolveBusinessUsage({ businessUsage: "InternalUse" })).toBe("InternalUse");
    expect(resolveBusinessUsage({ businessUsage: "Resale" })).toBe("Resale");
    expect(resolveBusinessUsage({ businessUsage: "ProducedItem" })).toBe("ProducedItem");
    expect(resolveBusinessUsage({ businessUsage: "MadeProduct" })).toBe("ProducedItem");
  });

  it("classifies produced flags before resale", () => {
    expect(resolveBusinessUsage({ isProduced: true, canBeSold: true })).toBe("ProducedItem");
    expect(resolveBusinessUsage({ usagePreset: "MadeProduct" })).toBe("ProducedItem");
    expect(resolveBusinessUsage({ usagePreset: "ProducedItem" })).toBe("ProducedItem");
  });

  it("falls back to canBeSold / ingredient flags when businessUsage missing", () => {
    expect(resolveBusinessUsage({ canBeSold: false, canBeUsedAsIngredient: true })).toBe(
      "Ingredient",
    );
    expect(resolveBusinessUsage({ canBeSold: false, usagePreset: "Ingredient" })).toBe(
      "Ingredient",
    );
    expect(resolveBusinessUsage({ canBeSold: false })).toBe("InternalUse");
    expect(resolveBusinessUsage({ canBeSold: true })).toBe("Resale");
    expect(resolveBusinessUsage({})).toBe("Resale");
  });

  it("ignores unknown businessUsage strings", () => {
    expect(resolveBusinessUsage({ businessUsage: "Other", canBeSold: false })).toBe("InternalUse");
  });

  it("marks resale and produced items as sell-floor usages", () => {
    expect(isSellFloorBusinessUsage("Resale")).toBe(true);
    expect(isSellFloorBusinessUsage("ProducedItem")).toBe(true);
    expect(isSellFloorBusinessUsage("Ingredient")).toBe(false);
    expect(isSellFloorBusinessUsage("InternalUse")).toBe(false);
  });
});
