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

async function mockAuthenticatedSession(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route("**/api/v1/platform/catalog/products*", async (route) => {
    await route.fulfill({
      json: {
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
      },
    });
  });
  await page.route("**/api/v1/platform/organizations*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 1 } });
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
  await page.route("**/health/ready", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
  await page.route("**/health", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test.describe("development frontend mode", () => {
  test.use({ baseURL: "http://127.0.0.1:4174" });

  test("Development section shows DEV_TEST_ONLY Test Payments as implemented", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await mockAuthenticatedSession(page);
    await page.goto("/admin");
    await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
    await expect(page.getByText("Development", { exact: true })).toBeVisible();
    await expect(page.getByRole("link", { name: "All Organizations" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Test Payments" })).toBeVisible();
    await expect(page.getByLabel("Test Payments. Under development")).toHaveCount(0);
    await expect(page.getByRole("link", { name: "All Accounts" })).toBeVisible();
    await expect(page.getByLabel("Event Delivery. Planned")).toBeVisible();
    await expect(page.getByRole("link", { name: "Event Delivery" })).toHaveCount(0);
  });

  test("known DEV_TEST_ONLY Test Payments route is implemented", async ({ page }) => {
    await mockAuthenticatedSession(page);
    await page.goto("/admin/local-validation/test-payments");
    await expect(page.getByRole("heading", { name: "Test Payments" })).toBeVisible();
    await expect(page.getByText("TEST / LOCAL VALIDATION ONLY")).toBeVisible();
    await expect(page.getByRole("heading", { name: "Under development" })).toHaveCount(0);
  });

  test("Test Payments page has no horizontal overflow at 375px", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await mockAuthenticatedSession(page);
    await page.goto("/admin/local-validation/test-payments");
    await expect(page.getByRole("heading", { name: "Test Payments" })).toBeVisible();
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
    );
    expect(overflow).toBe(false);
  });

  test("Test Payments page has no serious accessibility violations", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await mockAuthenticatedSession(page);
    await page.goto("/admin/local-validation/test-payments");
    await expect(page.getByRole("heading", { name: "Test Payments" })).toBeVisible();

    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(
      (violation) => violation.impact === "serious" || violation.impact === "critical",
    );
    expect(serious).toEqual([]);
  });
});

test("unknown platform route remains page not found", async ({ page }) => {
  await mockAuthenticatedSession(page);
  await page.goto("/admin/not-a-real-platform-route");
  await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Under development" })).toHaveCount(0);
});

test("production preview shows blueprint and hides Development-only tools", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockAuthenticatedSession(page);
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Overview" })).toBeVisible();
  await expect(page.getByRole("link", { name: "All Organizations" })).toBeVisible();
  await expect(page.getByText("Development", { exact: true })).toHaveCount(0);
  await expect(page.getByRole("link", { name: "All Accounts" })).toBeVisible();
  await expect(page.getByLabel("Event Delivery. Planned")).toBeVisible();
  await expect(page.getByText("Test Payments")).toHaveCount(0);
  await expect(page.getByRole("link", { name: "Future Product X" })).toBeVisible();
  await page.getByRole("link", { name: "Future Product X" }).click();
  await expect(page).toHaveURL(/product=future-product-x/);
});

test("memberships and personal features are implemented links in production preview", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockAuthenticatedSession(page);
  await page.goto("/admin");
  await expect(page.getByRole("link", { name: "Memberships" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Personal Features" })).toBeVisible();
  await expect(page.getByLabel("Memberships. Under development")).toHaveCount(0);
  await expect(page.getByLabel("Personal Features. Under development")).toHaveCount(0);
});
