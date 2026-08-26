import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

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
  permissions: [
    "platform.permission.view_portfolio",
    "platform.permission.manage_organizations",
    "platform.permission.manage_subscriptions",
    "platform.permission.view_audit_records",
    "platform.permission.view_global_catalog",
    "platform.permission.view_privacy_compliance",
    "platform.permission.manage_platform_users",
    "platform.permission.manage_memberships",
    "platform.permission.manage_manual_payments",
    "platform.permission.manage_entitlement_overrides",
  ],
};

const catalog = {
  items: [
    {
      id: "11111111-1111-1111-1111-111111111111",
      code: "future-product-x",
      displayName: "Future Product X",
      status: "Active",
    },
    {
      id: "22222222-2222-2222-2222-222222222222",
      code: "pinoy-business-pos",
      displayName: "Pinoy Business POS",
      status: "Active",
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 100,
};

const organizations = {
  items: [
    {
      id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      displayName: "Northwind Market",
      slug: "northwind-market",
      status: "Active",
      createdAtUtc: "2026-01-15T08:00:00Z",
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
    await route.fulfill({ json: catalog });
  });
  await page.route("**/api/v1/platform/organizations*", async (route) => {
    await route.fulfill({ json: organizations });
  });
  await page.route("**/api/v1/platform/subscriptions*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 1 } });
  });
  await page.route("**/api/v1/platform/users*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 1 } });
  });
  await page.route("**/api/v1/platform/audit*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 8 } });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test("14D product organizations remains implemented after server filter", async ({ page }) => {
  await mockShell(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/organizations");
  await expect(page.getByRole("heading", { name: "Organizations", exact: true })).toBeVisible();
  await expect(page.getByText("Northwind Market")).toBeVisible();
  await expect(page.locator("#org-list-product")).toBeVisible();

  await page.goto("/admin/organizations?product=future-product-x");
  await expect(
    page.getByRole("heading", { name: "Organizations / Future Product X" }),
  ).toBeVisible();
  await expect(page.getByText("Northwind Market")).toBeVisible();
  await expect(page.locator("#org-list-product")).toHaveValue("future-product-x");
  await expect(page.getByTestId("product-org-filter-blocked")).toHaveCount(0);

  const accessibility = await new AxeBuilder({ page }).analyze();
  expect(accessibility.violations).toEqual([]);

  await page.setViewportSize({ width: 375, height: 812 });
  await expect(page.getByText("Northwind Market")).toBeVisible();
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});

test("14D invalid product stays safe and preserves filters when switching", async ({ page }) => {
  await mockShell(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/organizations?product=not-real&search=acme&status=Active");
  await expect(
    page.getByText("That product is not available in the authorized catalog."),
  ).toBeVisible();
  await expect(page.getByTestId("product-org-filter-blocked")).toHaveCount(0);

  await page.goto("/admin/organizations?search=acme&status=Active&sortBy=Slug&page=2");
  await page.locator("#org-list-product").selectOption("pinoy-business-pos");
  await expect(page).toHaveURL(/product=pinoy-business-pos/);
  await expect(page).toHaveURL(/search=acme/);
  await expect(page).toHaveURL(/status=Active/);
  await expect(page).toHaveURL(/sortBy=Slug/);
  await expect(page).not.toHaveURL(/page=2/);
  await expect(page.getByTestId("product-org-filter-results")).toBeVisible();
});
