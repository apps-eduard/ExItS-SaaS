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

const organization = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  displayName: "Northwind Market",
  slug: "northwind-market",
  status: "Active",
};

const entitlements = [
  {
    id: "11111111-1111-1111-1111-111111111111",
    productCode: "POS",
    productDisplayName: "Pinoy Business POS",
    subscriptionStatus: "Active",
    snapshotVersion: 3,
    generatedAtUtc: "2026-08-01T08:00:00Z",
  },
];

async function mockCore(page: import("@playwright/test").Page, summary?: unknown) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route("**/api/v1/platform/admin/organizations/*/commercial-summary", async (route) => {
    await route.fulfill({
      json: summary ?? { subscriptions: [], payments: [], latestEntitlements: entitlements },
    });
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
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/branches") || url.includes("/members") || url.includes("/invitations")) {
      await route.fulfill({
        json: url.includes("/branches") ? [] : { items: [], totalCount: 0, page: 1, pageSize: 20 },
      });
      return;
    }
    if (url.includes(organization.id) && !url.includes("?")) {
      await route.fulfill({ json: organization });
      return;
    }
    await route.fulfill({ json: { items: [organization], totalCount: 1, page: 1, pageSize: 20 } });
  });
}

test("Overview and Products navigation and mapping work", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockCore(page);
  await page.goto(`/admin/organizations/${organization.id}`);
  const workspaceNav = page.getByRole("navigation", { name: "Organization workspace" });
  await workspaceNav.getByRole("link", { name: "Products", exact: true }).click();
  await expect(page).toHaveURL(/\/products$/);
  await expect(
    page.getByRole("heading", { name: "Products", exact: true, level: 1 }),
  ).toBeVisible();
  await expect(page.getByText("Pinoy Business POS")).toBeVisible();
  await expect(page.getByText("POS")).toBeVisible();
  await expect(page.getByRole("button", { name: /grant/i })).toHaveCount(0);
  await workspaceNav.getByRole("link", { name: "Overview" }).click();
  await expect(page.getByRole("heading", { name: "Northwind Market" })).toBeVisible();
});

test("direct products deep link, tablet, and 375 have no overflow", async ({ page }) => {
  await mockCore(page);
  await page.setViewportSize({ width: 768, height: 1024 });
  await page.goto(`/admin/organizations/${organization.id}/products`);
  await expect(
    page.getByRole("heading", { name: "Products", exact: true, level: 1 }),
  ).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Breadcrumb" })).toContainText("Products");
  await page.setViewportSize({ width: 375, height: 812 });
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});

test("empty product access is truthful", async ({ page }) => {
  await mockCore(page, { subscriptions: [], payments: [], latestEntitlements: [] });
  await page.goto(`/admin/organizations/${organization.id}/products`);
  await expect(page.getByText("No product access records")).toBeVisible();
});

test("product access error stays in region with retry and copy diagnostics", async ({ page }) => {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  let fail = true;
  await page.route("**/api/v1/platform/admin/organizations/*/commercial-summary", async (route) => {
    if (fail) {
      await route.fulfill({ status: 500, json: { title: "Error", status: 500, detail: "boom" } });
      return;
    }
    await route.fulfill({
      json: { subscriptions: [], payments: [], latestEntitlements: entitlements },
    });
  });
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    await route.fulfill({ json: organization });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
  await page.goto(`/admin/organizations/${organization.id}/products`);
  await expect(page.getByRole("heading", { name: "Unable to load product access." })).toBeVisible();
  await expect(page.getByRole("button", { name: "Copy diagnostics" })).toBeVisible();
  fail = false;
  await page.getByRole("button", { name: "Retry" }).click();
  await expect(page.getByText("Pinoy Business POS")).toBeVisible();
});

test("forbidden product access fail-closes without leaking payload", async ({ page }) => {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route("**/api/v1/platform/admin/organizations/*/commercial-summary", async (route) => {
    await route.fulfill({
      status: 403,
      json: { title: "Forbidden", status: 403, detail: "product-secret" },
    });
  });
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    await route.fulfill({ json: organization });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
  await page.goto(`/admin/organizations/${organization.id}/products`);
  await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
  await expect(page.getByText("product-secret")).toHaveCount(0);
});

test("products localize, theme, density, and axe", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockCore(page);
  await page.goto(`/admin/organizations/${organization.id}/products`);
  await expect(
    page.getByRole("heading", { name: "Products", exact: true, level: 1 }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Filipino/ }).click();
  await expect(
    page.getByRole("heading", { name: "Mga Produkto", exact: true, level: 1 }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Mga kagustuhan" }).click();
  await page.getByRole("menuitem", { name: /^Madilim/ }).click();
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  await expect(page.locator("html")).toHaveAttribute("data-density", "balanced");
  const results = await new AxeBuilder({ page }).analyze();
  const serious = results.violations.filter(
    (violation) => violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);
});
