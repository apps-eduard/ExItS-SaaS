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
  createdAtUtc: "2026-01-15T08:00:00Z",
  updatedAtUtc: "2026-08-01T08:00:00Z",
  profile: { legalName: "Northwind LLC" },
};

async function mockCore(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
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

function mockOrganizationApi(
  page: import("@playwright/test").Page,
  handler: (route: import("@playwright/test").Route) => Promise<void>,
) {
  return page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, handler);
}

test("list navigation opens the organization workspace", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockCore(page);
  await page.route("**/api/v1/platform/admin/organizations/*/commercial-summary", async (route) => {
    await route.fulfill({
      json: {
        subscriptions: [{ id: "s1", productCode: "POS", status: "Active" }],
        payments: [],
        latestEntitlements: [],
      },
    });
  });
  await mockOrganizationApi(page, async (route) => {
    const url = route.request().url();
    if (url.includes(organization.id) && !url.includes("?")) {
      await route.fulfill({ json: organization });
      return;
    }
    await route.fulfill({
      json: { items: [organization], totalCount: 1, page: 1, pageSize: 20 },
    });
  });
  await page.goto("/admin/organizations?search=north");
  await page.getByRole("link", { name: "Northwind Market" }).click();
  await expect(page).toHaveURL(/\/admin\/organizations\/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa$/);
  await expect(page.getByRole("heading", { name: "Northwind Market" })).toBeVisible();
  await expect(page.getByText("POS", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: /edit/i })).toHaveCount(0);
  await page
    .getByRole("navigation", { name: "Breadcrumb" })
    .getByRole("link", { name: "Organizations" })
    .click();
  await expect(page).toHaveURL(/\/admin\/organizations/);
  await expect(page).toHaveURL(/search=north/);
});

test("direct deep link, tablet, and 375 mobile have no overflow", async ({ page }) => {
  await mockCore(page);
  await page.route("**/api/v1/platform/admin/organizations/*/commercial-summary", async (route) => {
    await route.fulfill({ json: { subscriptions: [], payments: [], latestEntitlements: [] } });
  });
  await mockOrganizationApi(page, async (route) => {
    await route.fulfill({ json: organization });
  });
  await page.setViewportSize({ width: 768, height: 1024 });
  await page.goto(`/admin/organizations/${organization.id}`);
  await expect(page.getByRole("heading", { name: "Northwind Market" })).toBeVisible();
  await page.setViewportSize({ width: 375, height: 812 });
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});

test("invalid id does not call the organization API", async ({ page }) => {
  const seen: string[] = [];
  await mockCore(page);
  await mockOrganizationApi(page, async (route) => {
    seen.push(route.request().url());
    await route.fulfill({ json: organization });
  });
  await page.goto("/admin/organizations/not-a-guid");
  await expect(page.getByRole("heading", { name: "Organization not found" })).toBeVisible();
  expect(seen.some((url) => url.includes("not-a-guid"))).toBeFalsy();
});

test("missing organization shows not found", async ({ page }) => {
  await mockCore(page);
  await mockOrganizationApi(page, async (route) => {
    await route.fulfill({
      status: 404,
      json: { title: "Not Found", status: 404, errorCode: "application.organization.not_found" },
    });
  });
  await page.goto(`/admin/organizations/${organization.id}`);
  await expect(page.getByRole("heading", { name: "Organization not found" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Back to Organizations" })).toBeVisible();
});

test("forbidden organization GET fail-closes without leaking payload", async ({ page }) => {
  await mockCore(page);
  await mockOrganizationApi(page, async (route) => {
    await route.fulfill({
      status: 403,
      json: { title: "Forbidden", status: 403, detail: "secret" },
    });
  });
  await page.goto(`/admin/organizations/${organization.id}`);
  await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
  await expect(page.getByText("secret")).toHaveCount(0);
  await expect(page.getByText("Northwind Market")).toHaveCount(0);
});

test("supplemental commercial failure stays isolated with retry and copy diagnostics", async ({
  page,
}) => {
  await mockCore(page);
  let fail = true;
  await page.route("**/api/v1/platform/admin/organizations/*/commercial-summary", async (route) => {
    if (fail) {
      await route.fulfill({ status: 500, json: { title: "Error", status: 500, detail: "boom" } });
      return;
    }
    await route.fulfill({
      json: {
        subscriptions: [{ id: "s1", productCode: "POS", status: "Active" }],
        payments: [],
        latestEntitlements: [],
      },
    });
  });
  await mockOrganizationApi(page, async (route) => {
    await route.fulfill({ json: organization });
  });
  await page.goto(`/admin/organizations/${organization.id}`);
  await expect(page.getByRole("heading", { name: "Northwind Market" })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Unable to load commercial records." }),
  ).toBeVisible();
  await expect(page.getByRole("button", { name: "Copy error details" })).toBeVisible();
  fail = false;
  await page.getByRole("button", { name: "Retry" }).click();
  await expect(page.getByText("POS", { exact: true })).toBeVisible();
});

test("workspace localizes, supports themes, preserves density, and has no serious axe issues", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockCore(page);
  await page.route("**/api/v1/platform/admin/organizations/*/commercial-summary", async (route) => {
    await route.fulfill({ json: { subscriptions: [], payments: [], latestEntitlements: [] } });
  });
  await mockOrganizationApi(page, async (route) => {
    await route.fulfill({ json: organization });
  });
  await page.goto(`/admin/organizations/${organization.id}`);
  await expect(page.getByRole("heading", { name: "Northwind Market" })).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("data-density", "balanced");
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Filipino/ }).click();
  await expect(page.getByText("Pagkakakilanlan")).toBeVisible();
  await page.getByRole("button", { name: "Mga kagustuhan" }).click();
  await page.getByRole("menuitem", { name: /^Madilim/ }).click();
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  await expect(page.locator("html")).toHaveAttribute("data-density", "balanced");
  await page.getByRole("button", { name: "Mga kagustuhan" }).click();
  await page.getByRole("menuitem", { name: /^Maliwanag/ }).click();
  await page.getByRole("button", { name: "Mga kagustuhan" }).click();
  await page.getByRole("menuitem", { name: /^Ingles/ }).click();
  const results = await new AxeBuilder({ page }).analyze();
  const serious = results.violations.filter(
    (violation) => violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);
});
