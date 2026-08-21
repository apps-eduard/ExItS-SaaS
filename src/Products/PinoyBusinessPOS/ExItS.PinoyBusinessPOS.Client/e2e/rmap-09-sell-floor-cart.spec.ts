import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import { mockBoundCashierSession, signInAndBindCashier } from "./mock-bound-session";
import {
  MOCK_COKE_PRODUCT_ID,
  MOCK_MEAT_PRODUCT_ID,
  MOCK_RICE_PRODUCT_ID,
  MOCK_RICE_SACK_UNIT_ID,
} from "./mock-pos-catalog";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";

const VIEWPORTS = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

test.describe("RMAP-09 sell floor and session cart parity", () => {
  test.use({ serviceWorkers: "block" });

  test.beforeEach(async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await signInAndBindCashier(page);
    await expect(page.getByTestId("sell-floor")).toBeVisible();
  });

  test("multi-UOM rice opens unit picker and adds sack at catalog unit price", async ({ page }) => {
    await page.setViewportSize({ width: 1024, height: 768 });
    await page.getByTestId(`sell-product-${MOCK_RICE_PRODUCT_ID}`).click();
    await expect(page.getByTestId("sell-unit-entry")).toBeVisible();
    await expect(page.getByTestId("sell-stock-hint")).toContainText("advisory");
    await page.getByTestId(`sell-unit-option-${MOCK_RICE_SACK_UNIT_ID}`).click();
    await page.getByTestId("sell-unit-add").click();

    const cart = page.getByTestId("sell-cart-landscape");
    await expect(
      cart.getByTestId(`sell-cart-line-${MOCK_RICE_PRODUCT_ID}::${MOCK_RICE_SACK_UNIT_ID}`),
    ).toBeVisible();
    await expect(cart.getByTestId("sell-cart-subtotal")).toContainText("2,600");
    await expect(cart.getByTestId("sell-pay")).toBeDisabled();
  });

  test("ByWeight meat opens weight entry with kg preview and sellable advisory", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1024, height: 768 });
    await page.getByTestId(`sell-product-${MOCK_MEAT_PRODUCT_ID}`).click();
    await expect(page.getByTestId("sell-weight-entry")).toBeVisible();
    await expect(page.getByTestId("sell-stock-hint")).toContainText("Sellable");
    await page.getByTestId("sell-weight-input").fill("2");
    await expect(page.getByTestId("sell-weight-preview")).toContainText("120.00");
    await page.getByTestId("sell-weight-confirm").click();

    const cart = page.getByTestId("sell-cart-landscape");
    await expect(cart.getByTestId(`sell-cart-line-${MOCK_MEAT_PRODUCT_ID}::base`)).toBeVisible();
    await expect(cart.getByTestId("sell-cart-subtotal")).toContainText("120");
  });

  test("barcode auto-add, quantity edit, and clear with confirmation", async ({ page }) => {
    await page.setViewportSize({ width: 1024, height: 768 });
    await page.getByTestId("sell-search").fill("4006381333931");
    const cart = page.getByTestId("sell-cart-landscape");
    await expect(cart.getByTestId(`sell-cart-line-${MOCK_COKE_PRODUCT_ID}::base`)).toBeVisible();

    await cart.getByTestId(`sell-cart-qty-input-${MOCK_COKE_PRODUCT_ID}::base`).fill("3");
    await expect(cart.getByTestId("sell-cart-subtotal")).toContainText("75");

    await cart.getByTestId("sell-cart-clear").click();
    await page
      .getByTestId("sell-cart-clear-confirm")
      .getByRole("button", { name: "Clear cart" })
      .click();
    await expect(cart.getByTestId(`sell-cart-line-${MOCK_COKE_PRODUCT_ID}::base`)).toHaveCount(0);
  });

  for (const viewport of VIEWPORTS) {
    test(`responsive sell floor ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      await expect(page.getByTestId("sell-floor")).toBeVisible();
      await expect(page.getByTestId("sell-search")).toBeVisible();
      await expect(page.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeVisible();

      if (viewport.width >= 900) {
        await expect(page.getByTestId("sell-cart-landscape")).toBeVisible();
      } else {
        await page.getByTestId("sell-cart-bar").click();
        await expect(page.getByTestId("sell-cart-sheet")).toBeVisible();
      }

      await assertNoHorizontalOverflow(page);
      await expect(page.getByTestId("sell-pay").first()).toBeDisabled();
    });
  }
});
