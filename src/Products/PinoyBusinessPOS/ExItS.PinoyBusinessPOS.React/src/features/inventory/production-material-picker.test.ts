import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));

describe("POS-PRODUCTION-SETUP-MATERIAL-PICKER-UX-V1", () => {
  it("shows materials without requiring search and uses ingredient filter", () => {
    const source = readFileSync(resolve(here, "ProductionDefinitionFormPage.tsx"), "utf8");
    expect(source).toContain("canBeUsedAsIngredient: true");
    expect(source).toContain('data-testid="production-setup-material-browser"');
    expect(source).toContain("debouncedMaterial || undefined");
    expect(source).toMatch(/search:\s*debouncedMaterial\s*\|\|\s*undefined/);
    expect(source).toContain("ProductionMaterialQuantitySheet");
    expect(source).toContain('data-testid="production-setup-selected-materials"');
    expect(source).toContain("production.setups.selectedCount");
    expect(source).toContain("production.setups.editMaterial");
    expect(source).toContain("production.setups.removeMaterial");
    expect(source).toContain("production.setups.duplicateMaterial");
    expect(source).toContain("production.setups.materialAsOutputForbidden");
    expect(source).toContain("production.setups.needMaterials");
    expect(source).not.toContain("setMaterialQty");
  });

  it("quantity sheet supports weight kg/g and pack/piece UOM", () => {
    const sheet = readFileSync(resolve(here, "ProductionMaterialQuantitySheet.tsx"), "utf8");
    expect(sheet).toContain("production-material-weight-unit");
    expect(sheet).toContain('option value="kg"');
    expect(sheet).toContain('option value="g"');
    expect(sheet).toContain("production-material-unit");
    expect(sheet).toContain("production-material-base-uom");
    expect(sheet).toContain("transfer.available");
  });

  it("catalog client and API expose canBeUsedAsIngredient list filter", () => {
    const client = readFileSync(
      resolve(here, "../../api/pos/pos-catalog-client.ts"),
      "utf8",
    );
    expect(client).toContain("canBeUsedAsIngredient?: boolean");
    expect(client).toContain("canBeUsedAsIngredient: options.canBeUsedAsIngredient");
  });
});
