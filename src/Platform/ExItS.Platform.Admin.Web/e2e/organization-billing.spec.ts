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

const payment = {
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: organization.id,
  productCode: "POS",
  amount: 1500,
  currencyCode: "PHP",
  method: "GCash",
  status: "Confirmed",
  paidAtUtc: "2026-08-01T08:00:00Z",
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

test("billing navigation, status filter, and no mutations", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  const paymentUrls: string[] = [];
  await mockCore(page);
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/payments")) {
      paymentUrls.push(url);
      expect(url).toMatch(/page=/);
      expect(url).toMatch(/pageSize=20/);
      await route.fulfill({
        json: { items: [payment], totalCount: 1, page: 1, pageSize: 20 },
      });
      return;
    }
    if (
      url.includes("/branches") ||
      url.includes("/members") ||
      url.includes("/invitations") ||
      url.includes("/subscriptions") ||
      url.includes("/entitlements")
    ) {
      await route.fulfill({
        json: url.includes("/branches") ? [] : { items: [], totalCount: 0, page: 1, pageSize: 20 },
      });
      return;
    }
    await route.fulfill({ json: organization });
  });
  await page.goto(`/admin/organizations/${organization.id}`);
  const workspaceNav = page.getByRole("navigation", { name: "Organization workspace" });
  await workspaceNav.getByRole("link", { name: "Billing" }).click();
  await expect(page).toHaveURL(/\/billing/);
  await expect(page.getByRole("heading", { name: "Billing", exact: true, level: 1 })).toBeVisible();
  await expect(page.getByText("1500 PHP")).toBeVisible();
  await expect(page.getByRole("button", { name: /record/i })).toHaveCount(0);
  await expect(page.getByRole("button", { name: /confirm/i })).toHaveCount(0);
  await expect(page.getByRole("button", { name: /reject/i })).toHaveCount(0);
  await expect(page.getByRole("button", { name: /void/i })).toHaveCount(0);
  const filtered = page.waitForRequest(
    (request) => request.url().includes("/payments") && request.url().includes("status=Confirmed"),
  );
  await page.locator("#org-billing-status").selectOption("Confirmed");
  await expect(page).toHaveURL(/status=Confirmed/);
  expect((await filtered).url()).toContain("status=Confirmed");
});

test("empty billing is truthful", async ({ page }) => {
  await mockCore(page);
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/payments")) {
      await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
      return;
    }
    await route.fulfill({ json: organization });
  });
  await page.goto(`/admin/organizations/${organization.id}/billing`);
  await expect(page.getByText("No SaaS payments")).toBeVisible();
});

test("billing error retry and forbidden fail-closed without amount leak", async ({ page }) => {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route("**/api/v1/platform/admin/organizations/*/commercial-summary", async (route) => {
    await route.fulfill({ json: { subscriptions: [], payments: [], latestEntitlements: [] } });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
  let fail = true;
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/payments")) {
      if (fail) {
        await route.fulfill({ status: 500, json: { title: "Error", status: 500, detail: "boom" } });
        return;
      }
      await route.fulfill({
        json: { items: [payment], totalCount: 1, page: 1, pageSize: 20 },
      });
      return;
    }
    await route.fulfill({ json: organization });
  });
  await page.goto(`/admin/organizations/${organization.id}/billing`);
  await expect(page.getByRole("heading", { name: "Unable to load billing." })).toBeVisible();
  fail = false;
  await page.getByRole("button", { name: "Retry" }).click();
  await expect(page.getByText("1500 PHP")).toBeVisible();

  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/payments")) {
      await route.fulfill({
        status: 403,
        json: { title: "Forbidden", status: 403, detail: "payment-secret", amount: 9999.99 },
      });
      return;
    }
    await route.fulfill({ json: organization });
  });
  await page.reload();
  await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
  await expect(page.getByText("payment-secret")).toHaveCount(0);
  await expect(page.getByText("9999.99")).toHaveCount(0);
});

test("direct billing deep link, tablet, and 375 have no overflow", async ({ page }) => {
  await mockCore(page);
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/payments")) {
      await route.fulfill({
        json: { items: [payment], totalCount: 1, page: 1, pageSize: 20 },
      });
      return;
    }
    await route.fulfill({ json: organization });
  });
  await page.setViewportSize({ width: 768, height: 1024 });
  await page.goto(`/admin/organizations/${organization.id}/billing`);
  await expect(page.getByRole("heading", { name: "Billing", exact: true, level: 1 })).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Breadcrumb" })).toContainText("Billing");
  await page.setViewportSize({ width: 375, height: 812 });
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});

test("billing localize, theme, density, and axe", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockCore(page);
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/payments")) {
      await route.fulfill({
        json: { items: [payment], totalCount: 1, page: 1, pageSize: 20 },
      });
      return;
    }
    await route.fulfill({ json: organization });
  });
  await page.goto(`/admin/organizations/${organization.id}/billing`);
  await expect(page.getByRole("heading", { name: "Billing", exact: true, level: 1 })).toBeVisible();
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Filipino/ }).click();
  await expect(
    page.getByRole("heading", { name: "Pagsingil", exact: true, level: 1 }),
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
