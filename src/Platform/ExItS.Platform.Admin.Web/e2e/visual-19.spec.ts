import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

const screenshotDir = resolve(
  process.cwd(),
  "../../../docs/Platform-Admin-Web/Reports/impl-19-product-detail-plans",
);

const session = {
  sessionId: "11111111-1111-1111-1111-111111111111",
  userId: "22222222-2222-2222-2222-222222222222",
  username: "olivia",
  displayName: "Olivia Mendoza",
  email: "olivia@example.test",
  expiresAtUtc: "2026-08-19T12:00:00Z",
  absoluteExpiresAtUtc: "2026-08-20T12:00:00Z",
  selectedOrganizationId: null,
  selectedOrganizationDisplayName: null,
  organizationSelectionState: "None",
  activeOrganizationCount: 0,
  accountClass: "Platform",
};

const authorization = {
  actorIdentifier: "olivia@example.test",
  actorType: "PlatformUser",
  platformUserId: session.userId,
  organizationId: null,
  permissions: ["platform.permission.view_portfolio", "platform.permission.manage_organizations"],
};

const productId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const planId = "dddddddd-dddd-dddd-dddd-dddddddddddd";

const productDetail = {
  id: productId,
  code: "future-product-x",
  displayName: "Future Product X",
  status: "Active",
  createdAtUtc: "2026-01-01T08:00:00Z",
  updatedAtUtc: "2026-08-01T08:00:00Z",
};

const planDetail = {
  id: planId,
  productCode: "future-product-x",
  productId,
  productDisplayName: "Future Product X",
  code: "starter",
  displayName: "Starter",
  status: "Active",
  monthlyPrice: 999,
  annualPrice: 9990,
  currencyCode: "PHP",
};

async function mockShell(page: Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route(`**/api/v1/platform/catalog/products/${productId}`, async (route) => {
    await route.fulfill({ json: productDetail });
  });
  await page.route("**/api/v1/platform/catalog/products/future-product-x/plans", async (route) => {
    await route.fulfill({ json: [planDetail] });
  });
  await page.route("**/api/v1/platform/catalog/products*", async (route) => {
    const url = route.request().url();
    if (url.includes(`/catalog/products/${productId}`) || url.includes("/plans")) {
      return;
    }
    await route.fulfill({
      json: { items: [productDetail], totalCount: 1, page: 1, pageSize: 100 },
    });
  });
  await page.route(`**/api/v1/platform/catalog/plans/${planId}`, async (route) => {
    await route.fulfill({ json: planDetail });
  });
  await page.route("**/api/v1/platform/catalog/plans*", async (route) => {
    const url = route.request().url();
    if (url.includes(`/catalog/plans/${planId}`)) {
      return;
    }
    await route.fulfill({
      json: { items: [planDetail], totalCount: 1, page: 1, pageSize: 20 },
    });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test.beforeAll(() => {
  mkdirSync(screenshotDir, { recursive: true });
});

test("19 product detail plans screenshots and axe", async ({ page }) => {
  await mockShell(page);
  await page.emulateMedia({ colorScheme: "dark" });

  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/admin/products/${productId}`);
  await expect(page.getByRole("heading", { name: "Future Product X" })).toBeVisible();
  await page.screenshot({
    path: resolve(screenshotDir, "01-product-detail-1440x900-dark.png"),
    fullPage: true,
  });

  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto(`/admin/products/${productId}`);
  await expect(page.getByRole("link", { name: "Starter" })).toBeVisible();
  await page.screenshot({
    path: resolve(screenshotDir, "02-product-detail-375x812-dark.png"),
    fullPage: true,
  });

  await page.emulateMedia({ colorScheme: "light" });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/plans");
  await expect(page.getByRole("heading", { name: "Plans & Pricing", exact: true })).toBeVisible();
  await page.screenshot({
    path: resolve(screenshotDir, "03-plans-1440x900.png"),
    fullPage: true,
  });

  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto("/admin/plans");
  await page.screenshot({
    path: resolve(screenshotDir, "04-plans-375x812.png"),
    fullPage: true,
  });

  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/admin/plans/${planId}`);
  await expect(page.getByRole("heading", { name: "Starter" })).toBeVisible();
  await page.screenshot({
    path: resolve(screenshotDir, "05-plan-detail-1440x900.png"),
    fullPage: true,
  });

  const accessibility = await new AxeBuilder({ page }).analyze();
  expect(accessibility.violations).toEqual([]);
});
