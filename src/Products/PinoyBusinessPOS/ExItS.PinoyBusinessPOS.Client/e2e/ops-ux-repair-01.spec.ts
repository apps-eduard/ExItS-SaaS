import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  mockBoundCashierSession,
  mockBoundOwnerSession,
  signInAndBindCashier,
  signInAndBindOwner,
  chooseOwnerManageBusiness,
  clientNavigate,
} from "./mock-bound-session";
import {
  MOCK_CHIPS_PRODUCT_ID,
  MOCK_COKE_PRODUCT_ID,
  MOCK_DRINKS_CATEGORY_ID,
  MOCK_SNACKS_CATEGORY_ID,
} from "./mock-pos-catalog";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";
import { mockPosRegisterShiftApi } from "./mock-pos-register-shift-route";
import { mockAuthorizedPosDevice, prepareSellReady, seedInstallationId } from "./mock-sell-ready";
import { E2E_BRANCH_ID, E2E_ORG_ID } from "./mock-bound-session";

const DEVICE_ID = "ffffffff-ffff-4fff-8fff-ffffffffffff";
const FIXED_INSTALL_ID = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";

test.describe("POS OPERATIONS UX REPAIR 01", () => {
  test.use({ serviceWorkers: "block" });

  test("cashier unregistered → device readiness, no admin controls", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    await signInAndBindCashier(page);

    await expect(page.getByTestId("sell-readiness-device")).toBeVisible();
    await expect(page.getByTestId("sell-floor")).toHaveCount(0);
    await expect(page.getByTestId("sell-readiness-register")).toBeVisible();
    await expect(page.getByTestId("sell-readiness-register")).toContainText(
      "Register this browser",
    );
    await expect(page.getByTestId("sell-readiness-manage-devices")).toHaveCount(0);
    await expect(page.getByTestId("devices-create-code")).toHaveCount(0);
    await expect(page.getByText("Revoke", { exact: false })).toHaveCount(0);

    await page.getByTestId("sell-readiness-register").click();
    await expect(page.getByTestId("device-register-page")).toBeVisible();
    await expect(page.getByTestId("device-redeem-branch-locked")).toBeVisible();
    await expect(page.getByText("Revoke", { exact: false })).toHaveCount(0);
    await expect(page.getByTestId("devices-create-code")).toHaveCount(0);
  });

  test("authorized device + no shift → shift readiness", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await seedInstallationId(page);
    await mockAuthorizedPosDevice(page);
    await mockPosRegisterShiftApi(page, { openShift: false });
    await signInAndBindCashier(page);

    await expect(page.getByTestId("sell-readiness-shift")).toBeVisible();
    await expect(page.getByTestId("sell-floor")).toHaveCount(0);
    await expect(page.getByTestId("sell-readiness-open-shift")).toBeVisible();

    await clientNavigate(page, "/sell");
    await expect(page.getByTestId("sell-readiness-shift")).toBeVisible();
  });

  test("authorized + open shift → Sell opens", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await prepareSellReady(page);
    await signInAndBindCashier(page);
    await expect(page.getByTestId("sell-floor")).toBeVisible();
    await expect(page.getByTestId("checkout-readiness")).toHaveCount(0);
  });

  test("mobile floating cart appears with items and stays while scrolling", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await prepareSellReady(page);
    await page.setViewportSize({ width: 375, height: 812 });
    await signInAndBindCashier(page);

    await expect(page.getByTestId("sell-cart-bar")).toBeHidden();
    await page.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`).click();
    const bar = page.getByTestId("sell-cart-bar");
    await expect(bar).toBeVisible();
    await expect(bar).toContainText("View cart");

    const before = await bar.boundingBox();
    expect(before).toBeTruthy();
    await page.evaluate(() => {
      const products = document.querySelector('[data-testid="sell-products"]');
      products?.scrollIntoView({ block: "end" });
      window.scrollBy(0, 400);
    });
    const after = await bar.boundingBox();
    expect(after).toBeTruthy();
    expect(Math.abs((after!.y ?? 0) - (before!.y ?? 0))).toBeLessThan(8);
    expect((after!.y ?? 0) + (after!.height ?? 0)).toBeLessThanOrEqual(812 + 1);

    await bar.click();
    await expect(page.getByTestId("sell-cart-sheet")).toBeVisible();
    await expect(page.getByTestId("checkout-readiness")).toHaveCount(0);

    await page.getByTestId("sell-cart-sheet").getByTestId("sell-cart-clear").click();
    await page
      .getByTestId("sell-cart-clear-confirm")
      .getByRole("button", { name: "Clear cart" })
      .click();
    await expect(page.getByTestId("sell-cart-bar")).toBeHidden();
  });

  test("desktop sticky landscape cart remains visible", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await prepareSellReady(page);
    await page.setViewportSize({ width: 1440, height: 900 });
    await signInAndBindCashier(page);

    const cart = page.getByTestId("sell-cart-landscape");
    await expect(cart).toBeVisible();
    await page.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`).click();
    await expect(cart.getByTestId("sell-pay")).toBeVisible();
    await assertNoHorizontalOverflow(page);
  });

  test("few-product category keeps bounded card height", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await prepareSellReady(page);
    await page.setViewportSize({ width: 1024, height: 768 });
    await signInAndBindCashier(page);

    await page.getByTestId(`sell-category-${MOCK_SNACKS_CATEGORY_ID}`).click();
    const chip = page.getByTestId(`sell-product-${MOCK_CHIPS_PRODUCT_ID}`);
    await expect(chip).toBeVisible();
    const box = await chip.boundingBox();
    expect(box).toBeTruthy();
    expect(box!.height).toBeLessThan(280);

    await page.getByTestId(`sell-category-${MOCK_DRINKS_CATEGORY_ID}`).click();
    const coke = page.getByTestId(`sell-product-${MOCK_COKE_PRODUCT_ID}`);
    await expect(coke).toBeVisible();
    const cokeBox = await coke.boundingBox();
    expect(cokeBox).toBeTruthy();
    expect(cokeBox!.height).toBeLessThan(280);
  });

  test("org essentials action cards have icons and responsive grid", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await signInAndBindOwner(page);
    await chooseOwnerManageBusiness(page);
    await expect(page.getByTestId("org-essentials-page")).toBeVisible();
    await expect(page.getByTestId("org-group-operations")).toBeVisible();
    await expect(page.getByTestId("open-start-selling")).toBeVisible();
    await expect(page.getByTestId("open-org-devices")).toBeVisible();
    await expect(page.getByTestId("open-start-selling").locator("svg").first()).toBeVisible();
    await expect(page.getByTestId("open-org-devices").locator("svg").first()).toBeVisible();

    for (const size of [
      { width: 375, height: 812 },
      { width: 768, height: 1024 },
      { width: 1440, height: 900 },
    ]) {
      await page.setViewportSize(size);
      await assertNoHorizontalOverflow(page);
      const card = page.getByTestId("open-start-selling");
      await expect(card).toBeVisible();
      const box = await card.boundingBox();
      expect(box!.height).toBeGreaterThanOrEqual(44);
    }
  });

  test("owner devices show authoritative capacity", async ({ page }) => {
    await seedInstallationId(page);
    await mockBoundOwnerSession(page);
    await page.route("**/platform-api/**/pos-devices**", async (route) => {
      const url = route.request().url();
      const method = route.request().method();
      if (url.includes("/pos-devices/capacity") && method === "GET") {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ used: 3, allowed: 5 }),
        });
      }
      if (url.includes("/pos-devices/authorize") && method === "POST") {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            posDeviceId: DEVICE_ID,
            branchId: E2E_BRANCH_ID,
            installationDeviceId: FIXED_INSTALL_ID,
          }),
        });
      }
      if (url.includes("/pos-devices") && method === "GET" && !url.includes("capacity")) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify([
            {
              id: DEVICE_ID,
              organizationId: E2E_ORG_ID,
              branchId: E2E_BRANCH_ID,
              installationDeviceId: FIXED_INSTALL_ID,
              friendlyName: "Front browser",
              status: "Active",
              registeredAtUtc: "2026-08-21T01:00:00Z",
              lastSeenAtUtc: "2026-08-21T01:00:00Z",
            },
          ]),
        });
      }
      return route.fallback();
    });
    await signInAndBindOwner(page);
    await chooseOwnerManageBusiness(page);
    await page.getByTestId("open-org-devices").click();
    await expect(page.getByTestId("org-devices-page")).toBeVisible();
    await expect(page.getByTestId("devices-capacity")).toContainText("3 of 5 active");
    await expect(page.getByTestId("devices-capacity")).toContainText("2 available");
    await expect(page.getByTestId("devices-capacity-bar")).toBeVisible();
  });
});
