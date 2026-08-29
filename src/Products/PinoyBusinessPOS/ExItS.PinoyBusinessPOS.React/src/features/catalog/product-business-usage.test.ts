import { describe, expect, it } from "vitest";
import { resolveBusinessUsage } from "@/features/catalog/product-business-usage";

describe("resolveBusinessUsage", () => {
  it("prefers explicit businessUsage when valid", () => {
    expect(resolveBusinessUsage({ businessUsage: "Ingredient", canBeSold: true })).toBe(
      "Ingredient",
    );
    expect(resolveBusinessUsage({ businessUsage: "InternalUse" })).toBe("InternalUse");
    expect(resolveBusinessUsage({ businessUsage: "Resale" })).toBe("Resale");
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
});
