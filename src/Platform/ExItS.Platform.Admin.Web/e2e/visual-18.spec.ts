import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

const screenshotDir = resolve(
  process.cwd(),
  "../../../docs/Platform-Admin-Web/Reports/impl-18-product-catalog",
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

const products = {
  items: [
    {
      id: "11111111-1111-1111-1111-111111111111",
      code: "future-product-x",
      displayName: "Future Product X",
      status: "Active",
      updatedAtUtc: "2026-08-01T08:00:00Z",
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 20,
};

async function mockShell(page: Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route("**/api/v1/platform/catalog/products*", async (route) => {
    await route.fulfill({ json: products });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test.beforeAll(() => {
  mkdirSync(screenshotDir, { recursive: true });
});

test("18 product catalog screenshots and axe", async ({ page }) => {
  await mockShell(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/products");
  await expect(page.getByRole("heading", { name: "Products", exact: true })).toBeVisible();
  await page.screenshot({
    path: resolve(screenshotDir, "01-products-1440x900.png"),
    fullPage: true,
  });
  const accessibility = await new AxeBuilder({ page }).analyze();
  expect(accessibility.violations).toEqual([]);
  await page.setViewportSize({ width: 375, height: 812 });
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
  await page.screenshot({
    path: resolve(screenshotDir, "02-products-375x812.png"),
    fullPage: true,
  });
});
