import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

const screenshotDir = resolve(
  process.cwd(),
  "../../../docs/Platform-Admin-Web/Reports/impl-14b-entitlements-compact-grants",
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

const entitlements = [
  {
    id: "11111111-1111-1111-1111-111111111111",
    productCode: "POS",
    productDisplayName: "Pinoy Business POS",
    subscriptionStatus: "Active",
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
    { featureCode: "pos.staff", enabled: true },
    { featureCode: "pos.refunds", enabled: false },
  ],
};

async function mockCore(page: Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route("**/api/v1/platform/admin/organizations/*/commercial-summary", async (route) => {
    await route.fulfill({
      json: { subscriptions: [], payments: [], latestEntitlements: entitlements },
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
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/entitlements/snapshots")) {
      await route.fulfill({
        json: { items: [grantSnapshot], totalCount: 1, page: 1, pageSize: 20 },
      });
      return;
    }
    await route.fulfill({ json: organization });
  });
}

test.beforeAll(() => {
  mkdirSync(screenshotDir, { recursive: true });
});

test("14B compact grant disclosure screenshots and axe", async ({ page }) => {
  await mockCore(page);

  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/admin/organizations/${organization.id}/entitlements?product=POS`);
  await expect(
    page.getByRole("heading", { name: "Entitlements", exact: true, level: 1 }),
  ).toBeVisible();
  await expect(page.getByText("3 enabled · 2 disabled")).toBeVisible();
  await expect(page.getByRole("button", { name: "Show grants" })).toBeVisible();
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Light/ }).click();
  await page.screenshot({
    path: resolve(screenshotDir, "01-entitlements-collapsed-1440x900-light.png"),
    fullPage: true,
  });
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Dark/ }).click();
  await page.screenshot({
    path: resolve(screenshotDir, "02-entitlements-collapsed-1440x900-dark.png"),
    fullPage: true,
  });

  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Light/ }).click();
  await page.getByRole("button", { name: "Show grants" }).click();
  await expect(page.getByText("pos.checkout")).toBeVisible();
  await page.screenshot({
    path: resolve(screenshotDir, "03-entitlements-expanded-1440x900-light.png"),
    fullPage: true,
  });

  await page.setViewportSize({ width: 375, height: 812 });
  await page.getByRole("button", { name: "Hide grants" }).click();
  await expect(page.getByRole("button", { name: "Show grants" })).toBeVisible();
  let overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
  await page.screenshot({
    path: resolve(screenshotDir, "04-entitlements-collapsed-375x812.png"),
    fullPage: true,
  });
  await page.getByRole("button", { name: "Show grants" }).click();
  await page.screenshot({
    path: resolve(screenshotDir, "05-entitlements-expanded-375x812.png"),
    fullPage: true,
  });

  await page.setViewportSize({ width: 320, height: 800 });
  overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
  await page.screenshot({
    path: resolve(screenshotDir, "06-entitlements-expanded-320x800.png"),
    fullPage: true,
  });

  const results = await new AxeBuilder({ page }).analyze();
  const serious = results.violations.filter(
    (violation) => violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);
});
