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

const manageCatalogPermissions = [
  "platform.permission.view_portfolio",
  "platform.permission.manage_catalog",
];

const productId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

let product = {
  id: productId,
  code: "pinoy-business-pos",
  displayName: "Pinoy Business POS",
  status: "Inactive",
  createdAtUtc: "2026-01-01T08:00:00Z",
  updatedAtUtc: "2026-08-01T08:00:00Z",
};

async function mockProductLifecycle(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({
      json: {
        actorIdentifier: session.email,
        actorType: "PlatformUser",
        platformUserId: session.userId,
        organizationId: null,
        permissions: manageCatalogPermissions,
      },
    });
  });
  await page.route("**/api/v1/platform/antiforgery/token", async (route) => {
    await route.fulfill({ json: { headerName: "X-XSRF-TOKEN", token: "test-token" } });
  });
  await page.route(`**/api/v1/platform/catalog/products/${productId}`, async (route) => {
    await route.fulfill({ json: product });
  });
  await page.route("**/api/v1/platform/catalog/products?**", async (route) => {
    await route.fulfill({
      json: { items: [product], totalCount: 1, page: 1, pageSize: 100 },
    });
  });
  await page.route(`**/api/v1/platform/catalog/products/pinoy-business-pos/plans`, async (route) => {
    await route.fulfill({ json: [] });
  });
  await page.route(`**/api/v1/platform/catalog/products/${productId}/rename`, async (route) => {
    if (route.request().method() !== "PATCH") {
      await route.fallback();
      return;
    }
    const body = route.request().postDataJSON() as typeof product;
    product = { ...product, displayName: body.displayName, updatedAtUtc: new Date().toISOString() };
    await route.fulfill({ json: product });
  });
  await page.route(`**/api/v1/platform/catalog/products/${productId}/activate`, async (route) => {
    product = { ...product, status: "Active", updatedAtUtc: new Date().toISOString() };
    await route.fulfill({ json: product });
  });
  await page.route(`**/api/v1/platform/catalog/products/${productId}/deactivate`, async (route) => {
    product = { ...product, status: "Inactive", updatedAtUtc: new Date().toISOString() };
    await route.fulfill({ json: product });
  });
  await page.route(`**/api/v1/platform/catalog/products/${productId}/retire`, async (route) => {
    product = { ...product, status: "Retired", updatedAtUtc: new Date().toISOString() };
    await route.fulfill({ json: product });
  });
}

test.describe("SaaS product lifecycle", () => {
  test.beforeEach(async () => {
    product = {
      id: productId,
      code: "pinoy-business-pos",
      displayName: "Pinoy Business POS",
      status: "Inactive",
      createdAtUtc: "2026-01-01T08:00:00Z",
      updatedAtUtc: "2026-08-01T08:00:00Z",
    };
  });

  test("rename display name persists after save", async ({ page }) => {
    await mockProductLifecycle(page);
    await page.goto(`/admin/products/${productId}`);
    await expect(page.getByRole("heading", { name: "Pinoy Business POS" })).toBeVisible();
    await page.getByLabel("Display name").fill("Pinoy Business POS (Test)");
    await page.getByRole("button", { name: "Save display name" }).click();
    await expect(page.getByText("Display name saved.")).toBeVisible();
    await expect(page.getByRole("heading", { name: "Pinoy Business POS (Test)" })).toBeVisible();
    await expect(page.locator("dd", { hasText: "pinoy-business-pos" })).toBeVisible();
  });

  test("inactive product can activate then deactivate", async ({ page }) => {
    await mockProductLifecycle(page);
    await page.goto(`/admin/products/${productId}`);
    await page.getByRole("button", { name: "Activate" }).click();
    await page.getByRole("button", { name: "Confirm" }).click();
    await expect(page.getByText("Product activated.")).toBeVisible();
    await page.getByRole("button", { name: "Deactivate" }).click();
    await page.getByRole("button", { name: "Confirm" }).click();
    await expect(page.getByText("Product deactivated.")).toBeVisible();
  });

  test("retire shows terminal state without outbound actions", async ({ page }) => {
    await mockProductLifecycle(page);
    await page.goto(`/admin/products/${productId}`);
    await page.getByRole("button", { name: "Activate" }).click();
    await page.getByRole("button", { name: "Confirm" }).click();
    await page.getByRole("button", { name: "Retire product" }).click();
    await page.getByRole("button", { name: "Confirm" }).click();
    await expect(page.getByText("Product retired.")).toBeVisible();
    await expect(page.getByText(/outbound lifecycle transitions are blocked/i)).toBeVisible();
    await expect(page.getByRole("button", { name: "Activate" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Deactivate" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Retire product" })).toHaveCount(0);
  });

  test("product detail passes axe on desktop", async ({ page }) => {
    await mockProductLifecycle(page);
    await page.setViewportSize({ width: 1280, height: 900 });
    await page.goto(`/admin/products/${productId}`);
    await expect(page.getByRole("heading", { name: "Pinoy Business POS" })).toBeVisible();
    const results = await new AxeBuilder({ page }).analyze();
    expect(results.violations).toEqual([]);
  });
});
