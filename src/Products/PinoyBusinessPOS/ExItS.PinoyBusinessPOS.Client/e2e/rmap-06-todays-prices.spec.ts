import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  mockBoundCashierSession,
  mockBoundManagerSession,
  signInAndBindCashier,
  signInAndBindManager,
} from "./mock-bound-session";
import { MOCK_COKE_PRODUCT_ID, mockPosCatalogAdminApi } from "./mock-pos-catalog-admin-route";

test.describe("RMAP-06 today's prices", () => {
  test.use({ serviceWorkers: "block" });

  test("manager updates a dirty price row", async ({ page }) => {
    await mockBoundManagerSession(page);
    await mockPosCatalogAdminApi(page);
    await page.route("**/pos-api/api/v1/pos/catalog/products/prices", async (route) => {
      if (route.request().method() !== "POST") {
        return route.fallback();
      }
      const body = route.request().postDataJSON() as {
        items: Array<{ productId: string; sellingPrice: number; expectedUpdatedAtUtc: string }>;
      };
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          results: body.items.map((item) => ({
            productId: item.productId,
            succeeded: true,
            changed: true,
            product: {
              productId: item.productId,
              organizationId: "11111111-1111-1111-1111-111111111111",
              name: "Coke 330ml",
              unitOfMeasure: "Piece",
              sellingMode: "PerItem",
              sellingPrice: item.sellingPrice,
              status: "Active",
              createdAtUtc: "2026-01-01T00:00:00Z",
              updatedAtUtc: new Date().toISOString(),
            },
          })),
          succeededCount: body.items.length,
          failedCount: 0,
          changedCount: body.items.length,
        }),
      });
    });
    await signInAndBindManager(page);
    await page.getByTestId("open-catalog").click();
    await page.getByRole("link", { name: "Today's Prices" }).click();
    await expect(page.getByTestId("todays-prices-page")).toBeVisible();
    await expect(page.getByText("Coke 330ml")).toBeVisible();
    await page.getByRole("textbox", { name: "New price" }).first().fill("30");
    await page.getByTestId("prices-save").click();
    await expect(page.getByText(/Saved|Na-save/i)).toBeVisible();
  });

  test("cashier denied today's prices via catalog gate", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogAdminApi(page);
    await signInAndBindCashier(page);
    await page.goto("/catalog/todays-prices");
    await expect(page.getByTestId("catalog-manage-denied")).toBeVisible();
  });

  test("todays prices usable at 375x812", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await mockBoundManagerSession(page);
    await mockPosCatalogAdminApi(page);
    await signInAndBindManager(page);
    await page.getByTestId("open-catalog").click();
    await page.getByRole("link", { name: "Today's Prices" }).click();
    await assertNoHorizontalOverflow(page);
  });
});
