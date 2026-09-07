import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));

describe("POS-PRODUCTION-INGREDIENT-CAPABILITY-AND-SETUP-UX-V2", () => {
  it("catalog form exposes overlapping capability checkboxes and sends flags", () => {
    const form = readFileSync(resolve(here, "CatalogProductFormPage.tsx"), "utf8");
    expect(form).toContain("ProductCapabilitySelector");
    expect(form).toContain("canBeUsedAsIngredient: capabilities.canBeUsedAsIngredient");
    expect(form).toContain("isProduced: capabilities.isProduced");
    expect(form).toContain("canBeSold: capabilities.canBeSold");
    expect(form).not.toMatch(/businessUsage,\s*$/m);
  });

  it("capability selector uses checkboxes not exclusive radios", () => {
    const selector = readFileSync(resolve(here, "ProductCapabilitySelector.tsx"), "utf8");
    expect(selector).toContain('type="checkbox"');
    expect(selector).toContain("catalog-capability-ingredient");
    expect(selector).not.toContain('type="radio"');
  });
});
