import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  clientNavigate,
  mockBoundCashierSession,
  mockBoundManagerSession,
  mockBoundOwnerSession,
  signInAndBindCashier,
  signInAndBindManager,
  signInAndBindOwner,
} from "./mock-bound-session";
import { MOCK_COKE_PRODUCT_ID, mockPosCatalogAdminApi } from "./mock-pos-catalog-admin-route";

const VIEWPORTS = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

test.describe("RMAP-04 catalog admin parity", () => {
  test.use({ serviceWorkers: "block" });

  test("manager can open catalog, create product, and manage categories", async ({ page }) => {
    await mockBoundManagerSession(page);
    await mockPosCatalogAdminApi(page);
    await signInAndBindManager(page);
    await expect(page.getByTestId("open-catalog")).toBeVisible();
    await page.getByTestId("open-catalog").click();
    await expect(page.getByTestId("catalog-products-page")).toBeVisible();
    await expect(page.getByText("Coke 330ml")).toBeVisible();

    await page.getByRole("link", { name: "New product" }).click();
    await expect(page.getByTestId("catalog-product-form")).toBeVisible();
    await page.getByRole("textbox", { name: "Name", exact: true }).fill("New Juice");
    await page.getByRole("textbox", { name: "SKU", exact: true }).fill("JUICE-01");
    await page.getByRole("button", { name: "Save" }).click();
    await expect(page.getByTestId("catalog-product-form")).toBeVisible();
    await expect(page.getByRole("textbox", { name: "Name", exact: true })).toHaveValue("New Juice");

    await page.getByRole("link", { name: "Back to products" }).click();
    await page.getByRole("link", { name: "Categories" }).click();
    await expect(page.getByTestId("catalog-categories-page")).toBeVisible();
    await page.getByRole("textbox", { name: "New category name" }).fill("Bakery");
    await page.getByRole("button", { name: "Add category" }).click();
    await expect(page.getByText("Bakery")).toBeVisible();
  });

  test("cashier is denied catalog admin", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogAdminApi(page);
    await signInAndBindCashier(page);
    await expect(page.getByRole("heading", { name: "Sell floor" })).toBeVisible();
    await expect(page.getByTestId("open-catalog")).toHaveCount(0);
    // Keep SPA session/workspace bind — full page.goto remounts and drops in-memory bind.
    await clientNavigate(page, "/catalog");
    await expect(page).toHaveURL(/\/catalog$/);
    await expect(page.getByTestId("catalog-manage-denied")).toBeVisible();
  });

  test("owner can edit product and sees conflict on stale concurrency", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockPosCatalogAdminApi(page);
    await signInAndBindOwner(page);
    await page.getByTestId("workspace-destination-operations").click();
    await page.getByTestId("open-catalog").click();
    await page.getByText("Coke 330ml").click();
    await expect(page.getByTestId("catalog-product-form")).toBeVisible();

    await page.route(
      `**/pos-api/api/v1/pos/catalog/products/${MOCK_COKE_PRODUCT_ID}`,
      async (route) => {
        if (route.request().method() === "PUT") {
          return route.fulfill({
            status: 409,
            contentType: "application/json",
            body: JSON.stringify({ detail: "Product was modified by another user." }),
          });
        }
        return route.fallback();
      },
    );

    await page.getByRole("textbox", { name: "Name", exact: true }).fill("Coke Updated");
    await page.getByRole("button", { name: "Save" }).click();
    await expect(page.getByText(/changed elsewhere|modified by another/i)).toBeVisible();
  });

  for (const viewport of VIEWPORTS) {
    test(`catalog products responsive ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      await mockBoundManagerSession(page);
      await mockPosCatalogAdminApi(page);
      await signInAndBindManager(page);
      await page.getByTestId("open-catalog").click();
      await expect(page.getByTestId("catalog-products-page")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    });
  }
});
