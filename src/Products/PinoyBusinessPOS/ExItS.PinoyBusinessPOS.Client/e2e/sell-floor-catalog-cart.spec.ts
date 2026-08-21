import { mkdirSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import { mockBoundCashierSession, signInAndBindCashier } from "./mock-bound-session";
import {
  MOCK_CHIPS_PRODUCT_ID,
  MOCK_COKE_PRODUCT_ID,
  MOCK_SNACKS_CATEGORY_ID,
} from "./mock-pos-catalog";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";

const screenshotDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../../../../docs/Mobile-React/Reports/impl-pos-react-05-catalog-session-cart",
);

test.describe("sell floor catalog and session cart", () => {
  test.beforeAll(() => {
    mkdirSync(screenshotDir, { recursive: true });
  });

  test.beforeEach(async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await signInAndBindCashier(page);
    await page.getByRole("button", { name: "Open sell floor" }).click();
    await expect(page.getByTestId("sell-floor")).toBeVisible();
    await expect(page.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeVisible();
  });

  test("tablet landscape adds to cart, keeps lines on category change, pay stays disabled", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1024, height: 768 });
    const landscapeCart = page.getByTestId("sell-cart-landscape");
    await expect(landscapeCart).toBeVisible();
    await page.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`).click();
    await expect(
      landscapeCart.getByTestId(`sell-cart-line-${MOCK_COKE_PRODUCT_ID}::base`),
    ).toBeVisible();
    await expect(landscapeCart.getByTestId("sell-cart-subtotal")).toContainText("25");

    await page.getByTestId(`sell-category-${MOCK_SNACKS_CATEGORY_ID}`).click();
    await expect(page.getByTestId(`sell-product-${MOCK_CHIPS_PRODUCT_ID}`)).toBeVisible();
    await expect(
      landscapeCart.getByTestId(`sell-cart-line-${MOCK_COKE_PRODUCT_ID}::base`),
    ).toBeVisible();
    await expect(landscapeCart.getByTestId("sell-pay")).toBeDisabled();
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "01-sell-floor-cart-tablet-1024x768.png"),
      fullPage: true,
    });
  });

  test("unknown barcode shows error without creating a product", async ({ page }) => {
    await page.getByTestId("sell-search").fill("4006381333930");
    await expect(page.getByTestId("sell-search-error")).toBeVisible();
    await expect(page.getByTestId("sell-search-error")).toContainText("barcode");
    await expect(page.getByTestId(`sell-cart-line-${MOCK_COKE_PRODUCT_ID}::base`)).toHaveCount(0);
  });
});
