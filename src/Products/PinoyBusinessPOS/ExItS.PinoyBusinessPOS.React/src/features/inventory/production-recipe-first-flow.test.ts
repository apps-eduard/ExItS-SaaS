import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));

describe("POS-PRODUCTION-RECIPE-FIRST-TIME-SETUP-AND-REUSE-V1", () => {
  it("first-time recipe does not require output product before save", () => {
    const form = readFileSync(resolve(here, "ProductionDefinitionFormPage.tsx"), "utf8");
    expect(form).toContain('isEdit ? "1" : "100"');
    expect(form).toContain("production-recipe-yield-uom");
    expect(form).toContain("setOutputSheetOpen(true)");
    expect(form).toContain("ProductionRecipeOutputSheet");
    expect(form).toContain("production.recipes.saveAsRecipe");
    expect(form).toContain("outputCreatedOnSaveHint");
    // Create mode must not force catalog output picker upfront.
    expect(form).toContain("showOutputPicker = isEdit && !outputProductId");
    expect(form).toContain("browseCatalogIngredients");
    expect(form).toContain("ensureCanBeUsedAsIngredient");
  });

  it("save sheet creates or links output product", () => {
    const sheet = readFileSync(resolve(here, "ProductionRecipeOutputSheet.tsx"), "utf8");
    expect(sheet).toContain("createCatalogProduct");
    expect(sheet).toContain("isProduced: true");
    expect(sheet).toContain("canBeSold");
    expect(sheet).toContain("canBeUsedAsIngredient");
    expect(sheet).toContain("production-recipe-output-mode-create");
    expect(sheet).toContain("production-recipe-output-mode-existing");
    expect(sheet).toContain("estimatedMaterialCost");
    expect(sheet).toContain("enableInventoryTracking");
  });

  it("recipes list shows standard yield and Produce action", () => {
    const list = readFileSync(resolve(here, "ProductionDefinitionListPage.tsx"), "utf8");
    expect(list).toContain("production.recipes.standardYieldLabel");
    expect(list).toContain("production.recipes.produceAction");
    expect(list).toContain("/inventory/production/produce?definitionId=");
  });

  it("Produce still starts from recipes not full catalog", () => {
    const produce = readFileSync(resolve(here, "ProductionRunCreatePage.tsx"), "utf8");
    expect(produce).toContain("listProductionDefinitions");
    expect(produce).toContain("isActive: true");
    expect(produce).toContain("canBeUsedAsIngredient: true");
    expect(produce).toContain("produceBlocked");
  });
});
