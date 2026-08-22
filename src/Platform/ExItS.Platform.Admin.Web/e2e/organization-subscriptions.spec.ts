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

const subscription = {
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: organization.id,
  productCode: "POS",
  planId: "22222222-2222-2222-2222-222222222222",
  status: "Active",
  productDisplayName: "Pinoy Business POS",
  planDisplayName: "Starter",
};

async function mockCore(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route("**/api/v1/platform/admin/organizations/*/commercial-summary", async (route) => {
    await route.fulfill({ json: { subscriptions: [], payments: [], latestEntitlements: [] } });
  });
  await page.route("**/api/v1/platform/subscriptions*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 1 } });
  });
  await page.route("**/api/v1/platform/catalog/**", async (route) => {
    await route.fulfill({ json: [] });
  });
  await page.route("**/api/v1/platform/antiforgery/token", async (route) => {
    await route.fulfill({ json: { headerName: "X-XSRF-TOKEN", token: "csrf-token" } });
  });
  await page.route("**/api/v1/platform/audit*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 8 } });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test("subscription navigation, filters, and no Activate control", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockCore(page);
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/subscriptions")) {
      expect(url).toMatch(/page=/);
      expect(url).toMatch(/pageSize=20/);
      await route.fulfill({
        json: { items: [subscription], totalCount: 1, page: 1, pageSize: 20 },
      });
      return;
    }
    if (url.includes("/branches") || url.includes("/members") || url.includes("/invitations")) {
      await route.fulfill({
        json: url.includes("/branches") ? [] : { items: [], totalCount: 0, page: 1, pageSize: 20 },
      });
      return;
    }
    await route.fulfill({ json: organization });
  });
  await page.goto(`/admin/organizations/${organization.id}/subscription`);
  await expect(page.getByRole("heading", { name: "Subscription", exact: true, level: 1 })).toBeVisible();
  await expect(page.getByText("Pinoy Business POS").first()).toBeVisible();
  await expect(page.getByRole("button", { name: /activate/i })).toHaveCount(0);
  await expect(page.getByRole("button", { name: "Suspend subscription" })).toBeVisible();
  await page.locator("#org-sub-status").selectOption("Active");
  await expect(page).toHaveURL(/status=Active/);
  await page.setViewportSize({ width: 375, height: 812 });
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});

test("empty, zero-result, error, forbidden, i18n, axe", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockCore(page);
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/subscriptions")) {
      await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
      return;
    }
    await route.fulfill({ json: organization });
  });
  await page.goto(`/admin/organizations/${organization.id}/subscription`);
  await expect(page.getByText("No subscriptions")).toBeVisible();
  await expect(page.getByText("No Pinoy Business POS subscription", { exact: true })).toBeVisible();
  await page.locator("#org-sub-status").selectOption("Active");
  await expect(page.getByText("No subscriptions match your filters.")).toBeVisible();
  await page.getByRole("button", { name: "Reset filters" }).click();
  await expect(page).not.toHaveURL(/status=/);
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Filipino/ }).click();
  await expect(
    page.getByRole("heading", { name: "Subskripsyon", exact: true, level: 1 }),
  ).toBeVisible();
  const results = await new AxeBuilder({ page }).analyze();
  const serious = results.violations.filter(
    (violation) => violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);
});

test("subscription error retry and forbidden fail-closed", async ({ page }) => {
  await mockCore(page);
  let fail = true;
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/subscriptions")) {
      if (fail) {
        await route.fulfill({ status: 500, json: { title: "Error", status: 500, detail: "boom" } });
        return;
      }
      await route.fulfill({
        json: { items: [subscription], totalCount: 1, page: 1, pageSize: 20 },
      });
      return;
    }
    await route.fulfill({ json: organization });
  });
  await page.goto(`/admin/organizations/${organization.id}/subscription`);
  await expect(page.getByRole("heading", { name: "Unable to load subscriptions." })).toBeVisible();
  await expect(page.getByRole("button", { name: "Copy diagnostics" })).toBeVisible();
  fail = false;
  await page.getByRole("button", { name: "Retry" }).click();
  await expect(page.getByText("Pinoy Business POS").first()).toBeVisible();
});
