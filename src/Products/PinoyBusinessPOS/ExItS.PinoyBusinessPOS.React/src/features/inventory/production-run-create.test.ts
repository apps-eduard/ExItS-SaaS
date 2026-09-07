import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));

describe("POS-PRODUCTION-RUN-RECIPE-SNAPSHOT-STOCK-GUARD-V1", () => {
  it("Produce starts from active setups and loads recipe with scaling + stock guard", () => {
    const source = readFileSync(resolve(here, "ProductionRunCreatePage.tsx"), "utf8");
    expect(source).toContain("listProductionDefinitions");
    expect(source).toContain("isActive: true");
    expect(source).toContain("canBeUsedAsIngredient: true");
    expect(source).toContain("productionScaleFactor");
    expect(source).toContain("scaleProductionQuantity");
    expect(source).toContain("findProduceShortages");
    expect(source).toContain("produceBlocked");
    expect(source).toContain('data-testid="production-run-shortage-summary"');
    expect(source).toContain("production-run-add-ingredient");
    expect(source).toContain("extraMaterials");
    expect(source).toContain("showToast");
    expect(source).toContain("listCatalogProducts");
    // Ingredient picker only — setups come from production definitions, not the full catalog.
    expect(source).toMatch(/canBeUsedAsIngredient:\s*true/);
  });

  it("client and API support run-only extra materials", () => {
    const client = readFileSync(resolve(here, "../../api/pos/pos-production-client.ts"), "utf8");
    expect(client).toContain("extraMaterials");
    expect(client).toContain("CreateProductionRunExtraMaterialRequest");
  });
});
