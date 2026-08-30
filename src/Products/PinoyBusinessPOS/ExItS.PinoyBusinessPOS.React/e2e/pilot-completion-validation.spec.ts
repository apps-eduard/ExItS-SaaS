import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  mockBoundCashierSession,
  mockBoundManagerSession,
  signInAndBindCashier,
  signInAndBindManager,
} from "./mock-bound-session";
import { MOCK_COKE_PRODUCT_ID } from "./mock-pos-catalog";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";
import { prepareSellReady } from "./mock-sell-ready";
import { clientNavigate } from "./mock-bound-session";

/**
 * POS-PILOT-COMPLETION-VALIDATION-01 — Scenario 21 viewport audit.
 * Renders real React routes (mocked session/API). Does not validate CSS source alone.
 */

const viewports = [
  { name: "360", width: 360, height: 740 },
  { name: "768", width: 768, height: 1024 },
  { name: "1440", width: 1440, height: 900 },
] as const;

const managerPages: Array<{ path: string; label: string }> = [
  { path: "/sell", label: "Sell" },
  { path: "/catalog", label: "Products" },
  { path: "/inventory", label: "Inventory" },
  { path: "/purchasing/direct-purchases", label: "Direct Purchase" },
  { path: "/purchasing", label: "Purchase Orders" },
  { path: "/purchasing/receive-stock", label: "Receive / GRN" },
  { path: "/purchasing/payables", label: "Supplier Credit / Payables" },
  { path: "/inventory/stock-counts", label: "Stock Count" },
  { path: "/inventory/stock-use", label: "Stock Use" },
  { path: "/inventory/waste-loss", label: "Waste/Loss" },
  { path: "/reports", label: "Reports" },
  { path: "/shifts", label: "Shift/Register" },
  { path: "/customers", label: "Customer/Utang" },
  { path: "/more", label: "Settings/More" },
];

test.describe("Pilot completion Scenario 21 responsive pages", () => {
  test.use({ serviceWorkers: "block" });

  for (const viewport of viewports) {
    test(`manager operational pages usable at ${viewport.name}px`, async ({ page }) => {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await mockBoundManagerSession(page);
      await page.route("**/pos-api/**", async (route) => {
        if (route.request().method() === "GET") {
          return route.fulfill({
            status: 200,
            contentType: "application/json",
            body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 50 }),
          });
        }
        return route.fallback();
      });
      await mockPosCatalogApi(page);
      await prepareSellReady(page);
      await signInAndBindManager(page);

      const issues: string[] = [];
      for (const entry of managerPages) {
        await clientNavigate(page, entry.path);
        await page.waitForTimeout(150);
        const overflow = await page.evaluate(() => {
          const root = document.scrollingElement ?? document.documentElement;
          return root.scrollWidth - root.clientWidth;
        });
        if (overflow > 1) {
          issues.push(`${entry.label}@${viewport.name}:overflow=${overflow}`);
        }
      }
      expect(issues, issues.join("; ")).toEqual([]);
    });
  }

  test("Sell mobile 360 cart primitives no overflow", async ({ page }) => {
    await page.setViewportSize({ width: 360, height: 740 });
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await prepareSellReady(page);
    await signInAndBindCashier(page);
    await expect(page.getByTestId("sell-floor")).toBeVisible();
    await expect(page.getByTestId("sell-search")).toBeVisible();
    await page.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`).click();
    await expect(page.getByTestId("sell-cart-bar")).toBeVisible();
    await page.getByTestId("sell-cart-bar").click();
    const sheet = page.getByTestId("sell-cart-sheet");
    await expect(sheet).toBeVisible();
    await expect(sheet.getByTestId("quantity-stepper")).toBeVisible();
    await assertNoHorizontalOverflow(page);
  });
});
