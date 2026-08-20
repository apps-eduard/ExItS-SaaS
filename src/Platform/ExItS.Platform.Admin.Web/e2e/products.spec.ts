import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

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

async function mockProducts(
  page: import("@playwright/test").Page,
  permissions = authorization.permissions,
) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: { ...authorization, permissions } });
  });
  await page.route("**/api/v1/platform/catalog/products*", async (route) => {
    await route.fulfill({ json: products });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test("authorized product catalog is read-only", async ({ page }) => {
  await mockProducts(page);
  await page.goto("/admin/products");
  await expect(page.getByRole("heading", { name: "Products", exact: true })).toBeVisible();
  await expect(
    page.getByRole("table").getByRole("link", { name: "Future Product X" }),
  ).toBeVisible();
  await expect(page.getByRole("button", { name: /create/i })).toHaveCount(0);
});

test("unauthorized product catalog fail-closes", async ({ page }) => {
  await mockProducts(page, []);
  await page.goto("/admin/products");
  await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
});

test("product catalog mobile and axe", async ({ page }) => {
  await mockProducts(page);
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto("/admin/products");
  await expect(page.getByRole("main").getByRole("link", { name: "Future Product X" })).toBeVisible();
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
  const accessibility = await new AxeBuilder({ page }).analyze();
  expect(accessibility.violations).toEqual([]);
});
