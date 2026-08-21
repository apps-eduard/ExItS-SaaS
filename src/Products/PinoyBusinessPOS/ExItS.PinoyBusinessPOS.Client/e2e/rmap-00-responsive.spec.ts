import { mkdirSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test, type Page } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import { mockBoundCashierSession, signInAndBindCashier } from "./mock-bound-session";
import { MOCK_COKE_PRODUCT_ID } from "./mock-pos-catalog";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";
import { prepareSellReady } from "./mock-sell-ready";

const screenshotDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../../../../docs/Mobile-React/Reports/impl-pos-react-rmap-00-responsive",
);

const viewports = [
  { name: "375x812", width: 375, height: 812, cart: "phone" as const },
  { name: "768x1024", width: 768, height: 1024, cart: "phone" as const },
  { name: "1024x768", width: 1024, height: 768, cart: "landscape" as const },
  { name: "1440x900", width: 1440, height: 900, cart: "landscape" as const },
];

async function openSellFloor(page: Page) {
  await signInAndBindCashier(page);
  await expect(page.getByTestId("sell-floor")).toBeVisible();
}

async function assertSearchFocus(page: Page) {
  const search = page.getByTestId("sell-search");
  await search.focus();
  await expect(search).toBeFocused();
}

async function assertCartPrimitives(page: Page, cart: "phone" | "landscape") {
  if (cart === "phone") {
    await expect(page.getByTestId("sell-cart-bar")).toBeVisible();
    await page.getByTestId("sell-cart-bar").click();
    const sheet = page.getByTestId("sell-cart-sheet");
    await expect(sheet).toBeVisible();
    await expect(sheet.getByTestId("quantity-stepper")).toBeVisible();
    await expect(sheet.getByTestId(`sell-cart-qty-${MOCK_COKE_PRODUCT_ID}::base`)).toBeVisible();
    await expect(sheet.getByTestId("sell-cart-subtotal")).toBeVisible();
    return;
  }

  const landscape = page.getByTestId("sell-cart-landscape");
  await expect(landscape).toBeVisible();
  await expect(page.getByTestId("sell-cart-bar")).toBeHidden();
  await expect(landscape.getByTestId("quantity-stepper")).toBeVisible();
  await expect(landscape.getByTestId(`sell-cart-qty-${MOCK_COKE_PRODUCT_ID}::base`)).toBeVisible();
  await expect(landscape.getByTestId("sell-cart-subtotal")).toBeVisible();
}

test.describe("RMAP-00 shared responsive sell surface", () => {
  test.beforeAll(() => {
    mkdirSync(screenshotDir, { recursive: true });
  });

  test("loading skeleton appears while catalog is delayed", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page, { productDelayMs: 1500 });
    await prepareSellReady(page);
    await page.setViewportSize({ width: 375, height: 812 });
    await openSellFloor(page);
    await expect(page.getByTestId("loading-skeleton")).toBeVisible();
    await expect(page.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeVisible({
      timeout: 10_000,
    });
  });

  for (const viewport of viewports) {
    test(`${viewport.name} SearchField QuantityStepper MoneyDisplay cart primitives no overflow`, async ({
      page,
    }) => {
      await mockBoundCashierSession(page);
      await mockPosCatalogApi(page);
      await prepareSellReady(page);
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await openSellFloor(page);

      await expect(page.getByTestId("sell-search")).toBeVisible();
      await assertSearchFocus(page);
      await expect(page.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`)).toBeVisible();
      await expect(page.getByTestId(`sell-product-price-${MOCK_COKE_PRODUCT_ID}`)).toBeVisible();
      await page.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`).click();

      await assertCartPrimitives(page, viewport.cart);
      await expect(page.getByTestId("sell-pay").first()).toBeEnabled();
      await assertNoHorizontalOverflow(page);

      await page.screenshot({
        path: path.join(screenshotDir, `${viewport.name}.png`),
        fullPage: true,
      });
    });
  }
});
