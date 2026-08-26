import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
import { expect, test, type Page } from "@playwright/test";

const screenshotDir = resolve(
  process.cwd(),
  "../../../docs/Platform-Admin-Web/Reports/impl-14a-organization-readonly-polish",
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

const statusEntitlements = [
  {
    id: "11111111-1111-1111-1111-111111111111",
    productCode: "POS",
    productDisplayName: "Pinoy Business POS",
    subscriptionStatus: "Active",
  },
  {
    id: "22222222-2222-2222-2222-222222222222",
    productCode: "PLM",
    productDisplayName: "Platform License",
    subscriptionStatus: "Cancelled",
  },
  {
    id: "33333333-3333-3333-3333-333333333333",
    productCode: "CRM",
    productDisplayName: "Customer CRM",
    subscriptionStatus: "Expired",
  },
  {
    id: "44444444-4444-4444-4444-444444444444",
    productCode: "INV",
    productDisplayName: "Inventory Plus",
    subscriptionStatus: "GracePeriod",
  },
];

const grantSnapshot = {
  id: "55555555-5555-5555-5555-555555555555",
  organizationId: organization.id,
  productCode: "POS",
  subscriptionId: "66666666-6666-6666-6666-666666666666",
  planCode: "starter",
  planVersionNumber: 2,
  snapshotVersion: 4,
  schemaVersion: 1,
  subscriptionStatus: "Active",
  inGracePeriod: false,
  generatedAtUtc: "2026-08-01T08:00:00Z",
  grants: [
    { featureCode: "pos.checkout", enabled: true, numericLimit: 5 },
    { featureCode: "pos.reports", enabled: false },
    { featureCode: "pos.inventory", enabled: true },
  ],
};

const subscriptions = [
  {
    id: "77777777-7777-7777-7777-777777777777",
    organizationId: organization.id,
    productCode: "POS",
    planId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    productDisplayName: "Pinoy Business POS",
    planDisplayName: "Starter",
    status: "Cancelled",
    paidPeriodEndUtc: "2026-07-01T00:00:00Z",
  },
  {
    id: "88888888-8888-8888-8888-888888888888",
    organizationId: organization.id,
    productCode: "PLM",
    planId: "bbbbbbbb-cccc-dddd-eeee-ffffffffffff",
    productDisplayName: "Platform License",
    planDisplayName: "Pro",
    status: "Expired",
    paidPeriodEndUtc: "2026-06-01T00:00:00Z",
  },
];

async function mockCore(page: Page, summary?: unknown) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route("**/api/v1/platform/admin/organizations/*/commercial-summary", async (route) => {
    await route.fulfill({
      json: summary ?? { subscriptions: [], payments: [], latestEntitlements: statusEntitlements },
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
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

async function mockOrganizationRoutes(page: Page) {
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/entitlements/snapshots")) {
      await route.fulfill({
        json: { items: [grantSnapshot], totalCount: 1, page: 1, pageSize: 20 },
      });
      return;
    }
    if (url.includes("/subscriptions")) {
      await route.fulfill({
        json: { items: subscriptions, totalCount: subscriptions.length, page: 1, pageSize: 20 },
      });
      return;
    }
    if (
      url.includes("/branches") ||
      url.includes("/members") ||
      url.includes("/invitations") ||
      url.includes("/payments")
    ) {
      await route.fulfill({
        json: url.includes("/branches") ? [] : { items: [], totalCount: 0, page: 1, pageSize: 20 },
      });
      return;
    }
    await route.fulfill({ json: organization });
  });
}

test.beforeAll(() => {
  mkdirSync(screenshotDir, { recursive: true });
});

test("14A polish screenshots for products, entitlements, and subscriptions", async ({ page }) => {
  await mockCore(page);
  await mockOrganizationRoutes(page);

  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/admin/organizations/${organization.id}/products`);
  await expect(
    page.getByRole("heading", { name: "Products", exact: true, level: 1 }),
  ).toBeVisible();
  await expect(page.getByText("Cancelled")).toBeVisible();
  await expect(page.getByText("Expired")).toBeVisible();
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Light/ }).click();
  await page.screenshot({
    path: resolve(screenshotDir, "01-products-statuses-1440x900-light.png"),
    fullPage: true,
  });
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Dark/ }).click();
  await page.screenshot({
    path: resolve(screenshotDir, "02-products-statuses-1440x900-dark.png"),
    fullPage: true,
  });

  await page.goto(`/admin/organizations/${organization.id}/entitlements?product=POS`);
  await expect(
    page.getByRole("heading", { name: "Entitlements", exact: true, level: 1 }),
  ).toBeVisible();
  await expect(page.getByText("2 enabled · 1 disabled")).toBeVisible();
  await page.getByRole("button", { name: "Show grants" }).click();
  await expect(page.getByText("pos.checkout")).toBeVisible();
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Light/ }).click();
  await page.screenshot({
    path: resolve(screenshotDir, "03-entitlements-grants-1440x900-light.png"),
    fullPage: true,
  });
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Dark/ }).click();
  await page.screenshot({
    path: resolve(screenshotDir, "04-entitlements-grants-1440x900-dark.png"),
    fullPage: true,
  });
  await page.setViewportSize({ width: 375, height: 812 });
  await page.screenshot({
    path: resolve(screenshotDir, "05-entitlements-grants-375x812.png"),
    fullPage: true,
  });
  await page.setViewportSize({ width: 320, height: 800 });
  await page.screenshot({
    path: resolve(screenshotDir, "06-entitlements-grants-320x800.png"),
    fullPage: true,
  });

  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/admin/organizations/${organization.id}/subscription`);
  await expect(
    page.getByRole("heading", { name: "Subscription", exact: true, level: 1 }),
  ).toBeVisible();
  await expect(page.getByRole("cell", { name: "Cancelled" })).toBeVisible();
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Filipino/ }).click();
  await expect(
    page.getByRole("heading", { name: "Subskripsyon", exact: true, level: 1 }),
  ).toBeVisible();
  await expect(page.getByRole("cell", { name: "Nakansela" })).toBeVisible();
  await expect(page.getByRole("cell", { name: "Nag-expire" })).toBeVisible();
  await page.screenshot({
    path: resolve(screenshotDir, "07-subscriptions-statuses-fil-PH.png"),
    fullPage: true,
  });
});
