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
  },
  {
    id: "22222222-2222-2222-2222-222222222222",
    productCode: "PLM",
    productDisplayName: "Platform License",
    subscriptionStatus: "Trialing",
  },
];

const snapshot = {
  id: "33333333-3333-3333-3333-333333333333",
  organizationId: organization.id,
  productCode: "POS",
  subscriptionId: "44444444-4444-4444-4444-444444444444",
  planCode: "starter",
  snapshotVersion: 4,
  subscriptionStatus: "Active",
  inGracePeriod: false,
  generatedAtUtc: "2026-08-01T08:00:00Z",
  grants: [{ featureCode: "pos.checkout", enabled: true }],
};

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
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test("entitlement navigation, product selector, and no mutations", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  const snapshotUrls: string[] = [];
  await mockCore(page);
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/entitlements/snapshots")) {
      snapshotUrls.push(url);
      expect(url).toMatch(/page=/);
      expect(url).toMatch(/pageSize=20/);
      expect(url).toMatch(/\/products\/(POS|PLM)\//);
      await route.fulfill({
        json: { items: [snapshot], totalCount: 1, page: 1, pageSize: 20 },
      });
      return;
    }
    if (
      url.includes("/branches") ||
      url.includes("/members") ||
      url.includes("/invitations") ||
      url.includes("/subscriptions")
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
  await workspaceNav.getByRole("link", { name: "Entitlements" }).click();
  await expect(page).toHaveURL(/\/entitlements/);
  await expect(
    page.getByRole("heading", { name: "Entitlements", exact: true, level: 1 }),
  ).toBeVisible();
  await expect(page).toHaveURL(/product=POS/);
  await expect(page.getByText("starter")).toBeVisible();
  await expect(page.getByText("1 enabled · 0 disabled")).toBeVisible();
  await expect(page.getByRole("button", { name: "Show grants" })).toBeVisible();
  await expect(page.getByText("pos.checkout")).toHaveCount(0);
  await page.getByRole("button", { name: "Show grants" }).click();
  await expect(page.getByRole("button", { name: "Hide grants" })).toHaveAttribute(
    "aria-expanded",
    "true",
  );
  await expect(page.getByText("pos.checkout")).toBeVisible();
  await expect(page.getByText("Enabled", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: /override/i })).toHaveCount(0);
  await expect(page.getByRole("button", { name: /reconcile/i })).toHaveCount(0);
  await page.locator("#org-entitlement-product").selectOption("PLM");
  await expect(page).toHaveURL(/product=PLM/);
  expect(snapshotUrls.some((url) => url.includes("/products/UNKNOWN/"))).toBe(false);
});

test("empty product access is truthful and does not invent a history call", async ({ page }) => {
  const snapshotHits: string[] = [];
  await mockCore(page, { subscriptions: [], payments: [], latestEntitlements: [] });
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/entitlements/snapshots")) {
      snapshotHits.push(url);
    }
    await route.fulfill({ json: organization });
  });
  await page.goto(`/admin/organizations/${organization.id}/entitlements`);
  await expect(page.getByText("No product access records")).toBeVisible();
  expect(snapshotHits).toEqual([]);
});

test("unsanitized product does not call snapshot history", async ({ page }) => {
  const snapshotHits: string[] = [];
  await mockCore(page);
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/entitlements/snapshots")) {
      snapshotHits.push(url);
      await route.fulfill({ json: { items: [snapshot], totalCount: 1, page: 1, pageSize: 20 } });
      return;
    }
    await route.fulfill({ json: organization });
  });
  await page.goto(`/admin/organizations/${organization.id}/entitlements?product=UNKNOWN`);
  await expect(
    page.getByText("This product is not in the authorized product access list."),
  ).toBeVisible();
  expect(snapshotHits).toEqual([]);
});

test("entitlement error retry and forbidden fail-closed", async ({ page }) => {
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
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
  let fail = true;
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/entitlements/snapshots")) {
      if (fail) {
        await route.fulfill({ status: 500, json: { title: "Error", status: 500, detail: "boom" } });
        return;
      }
      await route.fulfill({
        json: { items: [snapshot], totalCount: 1, page: 1, pageSize: 20 },
      });
      return;
    }
    await route.fulfill({ json: organization });
  });
  await page.goto(`/admin/organizations/${organization.id}/entitlements?product=POS`);
  await expect(
    page.getByRole("heading", { name: "Unable to load entitlement snapshots." }),
  ).toBeVisible();
  await expect(page.getByRole("button", { name: "Copy diagnostics" })).toBeVisible();
  fail = false;
  await page.getByRole("button", { name: "Retry" }).click();
  await expect(page.getByText("starter")).toBeVisible();

  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/entitlements/snapshots")) {
      await route.fulfill({
        status: 403,
        json: { title: "Forbidden", status: 403, detail: "entitlement-secret" },
      });
      return;
    }
    await route.fulfill({ json: organization });
  });
  await page.reload();
  await expect(page.getByText("This list is not available.")).toBeVisible();
  await expect(page.getByText("entitlement-secret")).toHaveCount(0);
});

test("direct entitlements deep link, tablet, and 375 have no overflow", async ({ page }) => {
  await mockCore(page);
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/entitlements/snapshots")) {
      await route.fulfill({
        json: { items: [snapshot], totalCount: 1, page: 1, pageSize: 20 },
      });
      return;
    }
    await route.fulfill({ json: organization });
  });
  await page.setViewportSize({ width: 768, height: 1024 });
  await page.goto(`/admin/organizations/${organization.id}/entitlements?product=POS`);
  await expect(
    page.getByRole("heading", { name: "Entitlements", exact: true, level: 1 }),
  ).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Breadcrumb" })).toContainText("Entitlements");
  await page.setViewportSize({ width: 375, height: 812 });
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});

test("entitlements localize, theme, density, and axe", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockCore(page);
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    if (url.includes("/entitlements/snapshots")) {
      await route.fulfill({
        json: { items: [snapshot], totalCount: 1, page: 1, pageSize: 20 },
      });
      return;
    }
    await route.fulfill({ json: organization });
  });
  await page.goto(`/admin/organizations/${organization.id}/entitlements?product=POS`);
  await expect(
    page.getByRole("heading", { name: "Entitlements", exact: true, level: 1 }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Filipino/ }).click();
  await expect(
    page.getByRole("heading", { name: "Mga Karapatan", exact: true, level: 1 }),
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
