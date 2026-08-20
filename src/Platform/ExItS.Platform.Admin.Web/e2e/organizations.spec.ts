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
  totalCount: 21,
  page: 1,
  pageSize: 20,
};

async function mockSession(
  page: import("@playwright/test").Page,
  permissions = authorization.permissions,
) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: { ...authorization, permissions } });
  });
  await page.route("**/api/v1/platform/admin/organizations/*/commercial-summary", async (route) => {
    await route.fulfill({
      json: {
        subscriptions: [{ id: "s1", productCode: "POS", status: "Active" }],
        payments: [],
        latestEntitlements: [],
      },
    });
  });
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    const url = route.request().url();
    const match = url.match(
      /\/organizations\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?:\?|$)/,
    );
    if (match) {
      const organization = organizations.items.find((item) => item.id === match[1]);
      if (!organization) {
        await route.fulfill({
          status: 404,
          json: {
            title: "Not Found",
            status: 404,
            errorCode: "application.organization.not_found",
          },
        });
        return;
      }
      await route.fulfill({ json: organization });
      return;
    }
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
  await page.route("**/health/ready", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
  await page.route("**/health", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test("authorized organizations list is implemented and has no mutation controls", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockSession(page);
  await page.goto("/admin");
  await expect(page.getByRole("link", { name: "All Organizations" })).toBeVisible();
  await page.getByRole("link", { name: "All Organizations" }).click();
  await expect(page).toHaveURL(/\/admin\/organizations/);
  await expect(
    page.getByRole("heading", { name: "Organizations", exact: true, level: 1 }),
  ).toBeVisible();
  await expect(page.getByText("Northwind Market")).toBeVisible();
  await expect(page.getByRole("button", { name: /create/i })).toHaveCount(0);
  await expect(page.getByRole("button", { name: /edit/i })).toHaveCount(0);
  await expect(page.getByRole("button", { name: /delete/i })).toHaveCount(0);
});

test("unauthorized organizations route fail-closes", async ({ page }) => {
  await mockSession(page, ["platform.permission.view_audit_records"]);
  await page.goto("/admin/organizations");
  await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Organizations", exact: true, level: 1 }),
  ).toHaveCount(0);
  await expect(page.getByRole("link", { name: "All Organizations" })).toHaveCount(0);
});

test("search, status, sort, and pagination update the server query and URL", async ({ page }) => {
  const seen: string[] = [];
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    seen.push(route.request().url());
    await route.fulfill({ json: organizations });
  });
  await page.goto("/admin/organizations");
  await expect(page.getByText("Northwind Market")).toBeVisible();
  await page.getByLabel("Search").fill("north");
  await page.getByRole("button", { name: "Search" }).click();
  await expect(page).toHaveURL(/search=north/);
  await page.locator("#org-list-status").selectOption("Active");
  await page.locator("#org-list-sort").selectOption("CreatedAtUtc");
  await page.locator("#org-list-order").selectOption("desc");
  await page.getByRole("button", { name: "Next" }).click();
  await expect(page).toHaveURL(/page=2/);
  await page.reload();
  await expect(page).toHaveURL(/search=north/);
  await expect(page).toHaveURL(/status=Active/);
  await expect(page).toHaveURL(/sortBy=CreatedAtUtc/);
  await expect(page).toHaveURL(/sortDesc=true/);
  await expect(page).toHaveURL(/page=2/);
  expect(seen.some((url) => url.includes("search=north"))).toBeTruthy();
  expect(seen.some((url) => url.includes("status=Active"))).toBeTruthy();
  expect(seen.some((url) => url.includes("sortBy=CreatedAtUtc"))).toBeTruthy();
  expect(seen.some((url) => url.includes("sortDesc=true"))).toBeTruthy();
  expect(seen.some((url) => url.includes("page=2") && url.includes("pageSize=20"))).toBeTruthy();
});

test("empty, zero-result, and error states stay in the list region", async ({ page }) => {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route(/\/api\/v1\/platform\/organizations(\/|\?|$)/, async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
  });
  await page.goto("/admin/organizations");
  await expect(page.getByText("No organizations")).toBeVisible();
  await page.getByLabel("Search").fill("zzz");
  await page.getByRole("button", { name: "Search" }).click();
  await expect(page.getByText("No organizations match your filters.")).toBeVisible();
  await expect(page.getByRole("button", { name: "Reset filters" }).first()).toBeVisible();
});

test("organizations list localizes, supports dark theme, and has no overflow at 375px", async ({
  page,
}) => {
  await mockSession(page);
  await page.goto("/admin/organizations");
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Filipino/ }).click();
  await expect(page.getByRole("heading", { name: "Mga Organisasyon" })).toBeVisible();
  await page.getByRole("button", { name: "Mga kagustuhan" }).click();
  await page.getByRole("menuitem", { name: /^Madilim/ }).click();
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  await page.setViewportSize({ width: 375, height: 812 });
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});

test("organizations list has no serious accessibility violations", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockSession(page);
  await page.goto("/admin/organizations");
  await expect(
    page.getByRole("heading", { name: "Organizations", exact: true, level: 1 }),
  ).toBeVisible();
  const results = await new AxeBuilder({ page }).analyze();
  const serious = results.violations.filter(
    (violation) => violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);
});
