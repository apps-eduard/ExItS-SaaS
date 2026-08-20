import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

const screenshotDir = resolve(
  process.cwd(),
  "../../../docs/Platform-Admin-Web/Reports/impl-15-organization-activity-audit",
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

const auditItems = [
  {
    id: "11111111-1111-1111-1111-111111111111",
    occurredAtUtc: "2026-08-01T08:00:00Z",
    actorIdentifier: "olivia@example.test",
    actorType: "PlatformUser",
    actionCode: "platform.auth.login_succeeded",
    targetType: "PlatformAuthSession",
    targetId: "22222222-2222-2222-2222-222222222222",
    outcome: "Succeeded",
    summary: "Signed in successfully",
  },
  {
    id: "33333333-3333-3333-3333-333333333333",
    occurredAtUtc: "2026-08-02T09:00:00Z",
    actorIdentifier: "platform-user:44444444-4444-4444-4444-444444444444",
    actorType: "PlatformUser",
    actionCode: "org.branch.update.very.long.action.code.that.must.wrap.safely",
    targetType: "OrganizationBranch",
    targetId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    outcome: "Denied",
    reason: "Not permitted",
  },
  {
    id: "55555555-5555-5555-5555-555555555555",
    occurredAtUtc: "2026-08-03T10:00:00Z",
    actorIdentifier: "cashier@ORG000001",
    actorType: "OrganizationMember",
    actionCode: "custom.unknown.event",
    targetType: "UnknownTargetType",
    targetId: "zzzz",
    outcome: "Failed",
    summary: "Failure recorded",
  },
];

async function mockShell(page: Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route(`**/api/v1/platform/organizations/${organization.id}`, async (route) => {
    await route.fulfill({ json: organization });
  });
  await page.route(`**/api/v1/platform/organizations/${organization.id}/audit*`, async (route) => {
    await route.fulfill({
      json: { items: auditItems, totalCount: auditItems.length, page: 1, pageSize: 20 },
    });
  });
  await page.route("**/api/v1/platform/admin/organizations/*/commercial-summary", async (route) => {
    await route.fulfill({ json: { subscriptions: [], payments: [], latestEntitlements: [] } });
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

test.beforeAll(() => {
  mkdirSync(screenshotDir, { recursive: true });
});

test("15 organization activity audit screenshots and axe", async ({ page }) => {
  await mockShell(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/admin/organizations/${organization.id}/activity`);
  await expect(page.getByRole("heading", { name: "Activity / Audit", level: 1 })).toBeVisible();
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Light/ }).click();
  await page.screenshot({
    path: resolve(screenshotDir, "01-activity-table-1440x900-light.png"),
    fullPage: true,
  });

  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Dark/ }).click();
  await page.screenshot({
    path: resolve(screenshotDir, "02-activity-table-1440x900-dark.png"),
    fullPage: true,
  });

  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Light/ }).click();
  const accessibility = await new AxeBuilder({ page }).analyze();
  expect(accessibility.violations).toEqual([]);

  await page.setViewportSize({ width: 375, height: 812 });
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
    ),
  ).toBe(false);
  await page.screenshot({
    path: resolve(screenshotDir, "03-activity-cards-375x812.png"),
    fullPage: true,
  });

  await page.setViewportSize({ width: 320, height: 800 });
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
    ),
  ).toBe(false);
  await page.screenshot({
    path: resolve(screenshotDir, "04-activity-cards-320x800.png"),
    fullPage: true,
  });
});
