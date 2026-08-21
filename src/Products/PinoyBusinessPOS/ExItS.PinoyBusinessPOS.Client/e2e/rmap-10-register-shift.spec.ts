import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  mockBoundCashierSession,
  mockBoundOrgAdminSession,
  signInAndBindCashier,
  signInAndBindOrgAdmin,
  clientNavigate,
} from "./mock-bound-session";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";
import { mockPosRegisterShiftApi } from "./mock-pos-register-shift-route";

const VIEWPORTS = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

test.describe("RMAP-10 register and open shift gate", () => {
  test.use({ serviceWorkers: "block" });

  test("no open shift blocks checkout readiness and guides to open shift", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: false });
    await signInAndBindCashier(page);

    await expect(page.getByTestId("sell-floor")).toBeVisible();
    await expect(page.getByTestId("sell-shift-banner")).toContainText("No open shift");
    await expect(page.getByTestId("checkout-readiness").first()).toHaveAttribute(
      "data-readiness",
      "blocked_no_shift",
    );
    await expect(page.getByTestId("sell-pay").first()).toBeDisabled();
    await page.getByTestId("sell-banner-open-shift").click();
    await expect(page.getByTestId("shift-open-page")).toBeVisible();
  });

  test("open shift marks readiness ready without inventing device money-post", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    const shiftApi = await mockPosRegisterShiftApi(page, { openShift: false });
    await signInAndBindCashier(page);

    await page.getByTestId("sell-banner-open-shift").click();
    await expect(page.getByTestId("shift-open-page")).toBeVisible();
    await page.getByTestId("shift-opening-cash").fill("500");
    await page.getByTestId("shift-open-confirm").click();
    await expect(page.getByTestId("shift-detail-page")).toBeVisible();
    shiftApi.setState({ openShift: true });

    await clientNavigate(page, "/sell");
    await expect(page.getByTestId("sell-floor")).toBeVisible();
    await expect(page.getByTestId("checkout-readiness").first()).toHaveAttribute(
      "data-readiness",
      "ready",
    );
    await expect(page.getByTestId("checkout-readiness-detail").first()).toContainText("ready");
    await expect(page.getByTestId("sell-pay").first()).toBeDisabled();
  });

  test("closed shift is not ready", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true, closedShift: true });
    await signInAndBindCashier(page);
    await expect(page.getByTestId("checkout-readiness").first()).toHaveAttribute(
      "data-readiness",
      "blocked_closed",
    );
  });

  test("denied shift capability fails closed", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: false, denyShifts: true });
    await signInAndBindCashier(page);
    await expect(page.getByTestId("checkout-readiness").first()).toHaveAttribute(
      "data-readiness",
      "blocked_denied",
    );
  });

  test("wrong branch header on open is rejected", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: false, wrongBranchOnOpen: true });
    await signInAndBindCashier(page);
    await page.getByTestId("sell-banner-open-shift").click();
    await page.getByTestId("shift-opening-cash").fill("100");
    await page.getByTestId("shift-open-confirm").click();
    await expect(page.getByTestId("shift-open-error")).toBeVisible();
    await expect(page.getByTestId("shift-open-error")).toContainText("branch");
  });

  test("wrong organization header on open is rejected", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: false, wrongOrgOnOpen: true });
    await signInAndBindCashier(page);
    await page.getByTestId("sell-banner-open-shift").click();
    await page.getByTestId("shift-opening-cash").fill("100");
    await page.getByTestId("shift-open-confirm").click();
    await expect(page.getByTestId("shift-open-error")).toBeVisible();
    await expect(page.getByTestId("shift-open-error")).toContainText("Organization");
  });

  test("server shift state is re-read when tab becomes visible", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    const shiftApi = await mockPosRegisterShiftApi(page, { openShift: true });
    await signInAndBindCashier(page);
    await expect(page.getByTestId("checkout-readiness").first()).toHaveAttribute(
      "data-readiness",
      "ready",
    );

    shiftApi.setState({ openShift: false, closedShift: false });
    await page.evaluate(() => {
      Object.defineProperty(document, "visibilityState", {
        configurable: true,
        get: () => "hidden",
      });
      document.dispatchEvent(new Event("visibilitychange"));
      Object.defineProperty(document, "visibilityState", {
        configurable: true,
        get: () => "visible",
      });
      document.dispatchEvent(new Event("visibilitychange"));
    });

    await expect(page.getByTestId("checkout-readiness").first()).toHaveAttribute(
      "data-readiness",
      "blocked_no_shift",
      { timeout: 10000 },
    );
    await expect(page.getByTestId("sell-shift-banner")).toContainText("No open shift");
  });

  test("close shift returns readiness to blocked", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: true });
    await signInAndBindCashier(page);
    await clientNavigate(page, `/shifts/${"cccccccc-cccc-cccc-cccc-cccccccccccc"}`);
    await expect(page.getByTestId("shift-detail-page")).toBeVisible();
    await page.getByTestId("shift-closing-cash").fill("500");
    await page.getByTestId("shift-close-confirm").click();
    await expect(page.getByTestId("shift-status-chip")).toContainText("Closed");
    await clientNavigate(page, "/sell");
    await expect(page.getByTestId("checkout-readiness").first()).toHaveAttribute(
      "data-readiness",
      "blocked_no_shift",
    );
  });

  test("cashier registers list is view-only without manage", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: false });
    await signInAndBindCashier(page);
    await clientNavigate(page, "/registers");
    await expect(page.getByTestId("registers-list-page")).toBeVisible();
    await expect(page.getByTestId("register-view-only")).toBeVisible();
    await expect(page.getByTestId("registers-list")).toBeVisible();
  });

  test("cashier can open shift without admin experience", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: false });
    await signInAndBindCashier(page);
    await clientNavigate(page, "/role/cashier");
    await expect(page.getByTestId("open-shift-open")).toBeVisible();
    await expect(page.getByTestId("open-registers")).toBeVisible();
    await page.getByTestId("open-shift-open").click();
    await expect(page.getByTestId("shift-open-page")).toBeVisible();
  });

  test("org admin without POS sell role is denied shifts", async ({ page }) => {
    await mockBoundOrgAdminSession(page);
    await mockPosRegisterShiftApi(page, { openShift: false });
    await signInAndBindOrgAdmin(page);
    await clientNavigate(page, "/shifts/open");
    await expect(page.getByTestId("shifts-view-denied")).toBeVisible();
  });

  for (const viewport of VIEWPORTS) {
    test(`responsive shift hub ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await mockBoundCashierSession(page);
      await mockPosCatalogApi(page);
      await mockPosRegisterShiftApi(page, { openShift: false });
      await signInAndBindCashier(page);
      await page.setViewportSize(viewport);
      await clientNavigate(page, "/shifts");
      await expect(page.getByTestId("shifts-hub-page")).toBeVisible();
      await expect(page.getByTestId("shift-go-open")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    });
  }
});
