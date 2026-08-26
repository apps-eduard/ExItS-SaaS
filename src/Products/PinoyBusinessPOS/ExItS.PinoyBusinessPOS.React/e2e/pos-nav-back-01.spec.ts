import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  chooseOwnerManageBusiness,
  clientNavigate,
  completeOfflinePinSetupIfNeeded,
  E2E_ORG_ID,
  mockBoundManagerSession,
  mockBoundOwnerSession,
  signInAndBindManager,
  signInAndBindOwner,
} from "./mock-bound-session";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";
import { E2E_SHIFT_ID, mockPosRegisterShiftApi } from "./mock-pos-register-shift-route";

test.use({ serviceWorkers: "block" });

const VIEWPORTS = [
  { name: "phone", width: 375, height: 812 },
  { name: "tablet-portrait", width: 768, height: 1024 },
  { name: "tablet-landscape", width: 1024, height: 768 },
] as const;

const CUSTOMER_ID = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";

async function mockCustomersApi(page: import("@playwright/test").Page) {
  await page.route("**/pos-api/api/v1/pos/customers**", async (route) => {
    const url = route.request().url();
    const pathname = new URL(url).pathname.replace(/\/$/, "");
    const method = route.request().method();

    if (method !== "GET") {
      await route.fallback();
      return;
    }

    if (pathname.endsWith(`/customers/${CUSTOMER_ID}/credit-summary`)) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ outstandingAmount: 0 }),
      });
      return;
    }
    if (pathname.includes(`/customers/${CUSTOMER_ID}/credit-entries`)) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0 }),
      });
      return;
    }
    if (pathname.includes(`/customers/${CUSTOMER_ID}/repayments`)) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0 }),
      });
      return;
    }
    if (pathname.endsWith(`/customers/${CUSTOMER_ID}`)) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          customerId: CUSTOMER_ID,
          organizationId: E2E_ORG_ID,
          displayName: "Ana Reyes",
          mobileNumber: "09171234567",
          address: "",
          notes: null,
          status: "Active",
          createdAtUtc: "2026-08-01T00:00:00Z",
          updatedAtUtc: "2026-08-01T00:00:00Z",
          linkedPersonalPublicUserId: null,
        }),
      });
      return;
    }
    await route.fallback();
  });
}

async function mockSellerOrdersApi(page: import("@playwright/test").Page) {
  await page.route("**/pos-api/api/v1/pos/organizations/*/customer-orders**", async (route) => {
    if (route.request().method() !== "GET") {
      await route.fallback();
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 40 }),
    });
  });
}

async function bindManagerOperations(page: import("@playwright/test").Page) {
  await mockBoundManagerSession(page);
  await mockPosCatalogApi(page);
  await mockPosRegisterShiftApi(page, { openShift: false });
  await mockCustomersApi(page);
  await mockSellerOrdersApi(page);
  await signInAndBindManager(page);
  await expect(page.getByTestId("manager-home")).toBeVisible({ timeout: 15000 });
}

test.describe("POS-NAV-BACK-01 child page back navigation", () => {
  test("root bottom-nav pages have no header back control", async ({ page }) => {
    await bindManagerOperations(page);

    await clientNavigate(page, "/role/manager");
    await expect(page.getByTestId("manager-home")).toBeVisible();
    await expect(page.getByTestId("page-header-back")).toHaveCount(0);

    await clientNavigate(page, "/catalog");
    await expect(page.getByTestId("catalog-products-page")).toBeVisible();
    await expect(page.getByTestId("page-header-back")).toHaveCount(0);

    await clientNavigate(page, "/sell");
    await expect(
      page
        .getByTestId("sell-floor")
        .or(page.getByTestId("sell-readiness-device"))
        .or(page.getByTestId("sell-readiness-shift")),
    ).toBeVisible();
    await expect(page.getByTestId("page-header-back")).toHaveCount(0);

    await clientNavigate(page, "/orders");
    await expect(page.getByTestId("seller-orders-page")).toBeVisible();
    await expect(page.getByTestId("page-header-back")).toHaveCount(0);

    await clientNavigate(page, "/more");
    await expect(page.getByTestId("org-more-page")).toBeVisible();
    await expect(page.getByTestId("page-header-back")).toHaveCount(0);
  });

  test("child pages expose canonical header back links", async ({ page }) => {
    await bindManagerOperations(page);

    await clientNavigate(page, "/shifts");
    await expect(page.getByTestId("shifts-hub-page")).toBeVisible();
    const shiftsBack = page.getByTestId("page-header-back-shifts");
    await expect(shiftsBack).toBeVisible();
    await expect(shiftsBack).toHaveAttribute("href", "/role/manager");
    await expect(shiftsBack).toHaveAccessibleName("Back to Manager home");

    await clientNavigate(page, "/shifts/open");
    await expect(page.getByTestId("shift-open-page")).toBeVisible();
    await expect(page.getByTestId("page-header-back-shifts")).toHaveAttribute("href", "/shifts");

    await clientNavigate(page, `/shifts/${E2E_SHIFT_ID}`);
    await expect(page.getByTestId("shift-detail-page")).toBeVisible();
    await expect(page.getByTestId("page-header-back-shifts")).toHaveAttribute("href", "/shifts");

    await clientNavigate(page, "/registers");
    await expect(page.getByTestId("registers-list-page")).toBeVisible();
    await expect(page.getByTestId("page-header-back-registers")).toHaveAttribute(
      "href",
      "/role/manager",
    );

    await clientNavigate(page, `/customers/${CUSTOMER_ID}`);
    await expect(page.getByTestId("customer-detail-page")).toBeVisible();
    await expect(page.getByTestId("page-header-back-customers")).toHaveAttribute(
      "href",
      "/customers",
    );
  });

  test("direct refresh keeps canonical back destination", async ({ page }) => {
    await bindManagerOperations(page);
    await clientNavigate(page, `/customers/${CUSTOMER_ID}`);
    await expect(page.getByTestId("customer-detail-page")).toBeVisible({ timeout: 15000 });
    await expect(page.getByTestId("page-header-back-customers")).toHaveAttribute(
      "href",
      "/customers",
    );

    await page.reload();

    const ops = page.getByTestId("workspace-destination-operations");
    const customerDetail = page.getByTestId("customer-detail-page");
    await Promise.race([
      ops.waitFor({ state: "visible", timeout: 15000 }),
      customerDetail.waitFor({ state: "visible", timeout: 15000 }),
    ]);
    if (await ops.isVisible().catch(() => false)) {
      await ops.waitFor({ state: "visible", timeout: 15000 });
      await ops.click();
      await expect(page.getByTestId("manager-home")).toBeVisible({ timeout: 15000 });
      await clientNavigate(page, `/customers/${CUSTOMER_ID}`);
    }
    await expect(page.getByTestId("customer-detail-page")).toBeVisible({ timeout: 15000 });
    await expect(page.getByTestId("page-header-back-customers")).toHaveAttribute(
      "href",
      "/customers",
    );
  });

  test("cash handling removes duplicate bottom back link", async ({ page }) => {
    await mockBoundOwnerSession(page, { organizationManagementAuthority: true });
    await mockPosCatalogApi(page);
    await mockPosRegisterShiftApi(page, { openShift: false });
    await signInAndBindOwner(page);
    await Promise.race([
      page.getByTestId("offline-pin-setup-page").waitFor({ state: "visible", timeout: 15000 }),
      page.getByTestId("workspace-destination-manage_business").waitFor({ state: "visible", timeout: 15000 }),
      page.getByRole("heading", { name: "Choose workspace" }).waitFor({ state: "visible", timeout: 15000 }),
    ]);
    await completeOfflinePinSetupIfNeeded(page);
    await page
      .getByTestId("workspace-destination-manage_business")
      .waitFor({ state: "visible", timeout: 15000 });
    await chooseOwnerManageBusiness(page);
    await expect(page.getByTestId("org-essentials-page")).toBeVisible({ timeout: 15000 });
    await clientNavigate(page, "/org/cash-handling");
    await expect(page.getByTestId("cash-handling-page")).toBeVisible();
    await expect(page.getByTestId("page-header-back-org")).toHaveAttribute("href", "/org");
    await expect(page.getByRole("link", { name: "Back to home" })).toHaveCount(0);
  });

  test("responsive layouts keep accessible 44px back targets", async ({ page }) => {
    await bindManagerOperations(page);

    for (const viewport of VIEWPORTS) {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await clientNavigate(page, "/shifts");
      const back = page.getByTestId("page-header-back-shifts");
      await expect(back).toBeVisible();
      const box = await back.boundingBox();
      expect(box?.width ?? 0).toBeGreaterThanOrEqual(44);
      expect(box?.height ?? 0).toBeGreaterThanOrEqual(44);
      await assertNoHorizontalOverflow(page);
    }
  });
});
