import { mkdirSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import { mockBoundCashierSession, signInAndBindCashier } from "./mock-bound-session";
import { MOCK_COKE_PRODUCT_ID } from "./mock-pos-catalog";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";
import { prepareSellReady } from "./mock-sell-ready";

const screenshotDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../../../../docs/Mobile-React/Reports/impl-pos-react-04-sell-floor-shell",
);

test.describe("sell floor shell", () => {
  test.beforeAll(() => {
    mkdirSync(screenshotDir, { recursive: true });
  });

  test.beforeEach(async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await prepareSellReady(page);
    await signInAndBindCashier(page);
    await expect(page.getByTestId("sell-floor")).toBeVisible();
  });

  test("phone 375 portrait hides bar until cart has items", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await expect(page.getByTestId("sell-search")).toBeVisible();
    await expect(page.getByTestId("sell-categories")).toBeVisible();
    await expect(page.getByTestId("sell-products")).toBeVisible();
    await expect(page.getByTestId("sell-cart-bar")).toBeHidden();
    await expect(page.getByTestId("sell-pay").first()).toBeDisabled();

    await page.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`).click();
    await expect(page.getByTestId("sell-cart-bar")).toBeVisible();
    await page.getByTestId("sell-cart-bar").click();
    await expect(page.getByTestId("sell-cart-sheet")).toBeVisible();
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "01-sell-floor-phone-375x812.png"),
      fullPage: true,
    });
  });

  test("tablet landscape 1024x768 shows split browse and cart", async ({ page }) => {
    await page.setViewportSize({ width: 1024, height: 768 });
    await expect(page.getByTestId("sell-cart-landscape")).toBeVisible();
    await expect(page.getByTestId("sell-cart-bar")).toBeHidden();
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "02-sell-floor-tablet-1024x768.png"),
      fullPage: true,
    });
  });

  test("desktop 1440 shows operational sell floor layout", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await expect(page.getByTestId("sell-floor")).toBeVisible();
    await expect(page.getByTestId("sell-search")).toBeVisible();
    await expect(page.getByTestId("sell-cart-landscape")).toBeVisible();
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "03-sell-floor-desktop-1440x900.png"),
      fullPage: true,
    });
  });
});
