import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import { mockBoundManagerSession, signInAndBindManager } from "./mock-bound-session";
import { mockPosCatalogAdminApi } from "./mock-pos-catalog-admin-route";

const VIEWPORTS = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

test.describe("RMAP-05 product units and selling mode", () => {
  test.use({ serviceWorkers: "block" });

  test("manager configures base UOM, PerItem, and sell package multiplier", async ({ page }) => {
    await mockBoundManagerSession(page);
    await mockPosCatalogAdminApi(page);
    await signInAndBindManager(page);
    await page.getByTestId("open-catalog").click();
    await page.getByRole("link", { name: "New product" }).click();

    await page.getByRole("textbox", { name: "Name", exact: true }).fill("Rice");
    await page.getByTestId("catalog-base-uom").selectOption("Kilogram");
    await page.getByTestId("catalog-selling-mode").selectOption("PerItem");
    await page.getByRole("textbox", { name: "Base selling price" }).fill("55");
    await page.getByTestId("catalog-configure-packages").check();
    await expect(page.getByTestId("catalog-unit-editor")).toBeVisible();

    const sellCards = page.getByTestId("catalog-unit-editor").locator(".flex.flex-col.gap-2");
    // Fill second card (Sell) after Purchase defaults
    await page.getByRole("button", { name: "Add sell package" }).click();
    const sellName = page.getByRole("textbox", { name: "Package name" }).last();
    await sellName.fill("Sack 50kg");
    await page.getByRole("textbox", { name: "Short label" }).last().fill("sack");
    await page.getByRole("textbox", { name: "Multiplier to base" }).last().fill("50");
    await page.getByRole("textbox", { name: "Sell unit price" }).last().fill("2600");

    await page.getByRole("button", { name: "Save" }).click();
    await expect(page.getByTestId("catalog-product-form")).toBeVisible();
    await expect(page.getByTestId("catalog-base-uom")).toHaveValue("Kilogram");
  });

  test("ByWeight locks base unit to Kilogram", async ({ page }) => {
    await mockBoundManagerSession(page);
    await mockPosCatalogAdminApi(page);
    await signInAndBindManager(page);
    await page.getByTestId("open-catalog").click();
    await page.getByRole("link", { name: "New product" }).click();
    await page.getByTestId("catalog-selling-mode").selectOption("ByWeight");
    await expect(page.getByTestId("catalog-base-uom")).toHaveValue("Kilogram");
    await expect(page.getByTestId("catalog-base-uom")).toBeDisabled();
  });

  for (const viewport of VIEWPORTS) {
    test(`unit form responsive ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      await mockBoundManagerSession(page);
      await mockPosCatalogAdminApi(page);
      await signInAndBindManager(page);
      await page.getByTestId("open-catalog").click();
      await page.getByRole("link", { name: "New product" }).click();
      await page.getByTestId("catalog-configure-packages").check();
      await assertNoHorizontalOverflow(page);
    });
  }
});
