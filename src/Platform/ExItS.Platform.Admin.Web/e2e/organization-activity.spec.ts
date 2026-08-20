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
];

async function mockCore(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route(`**/api/v1/platform/organizations/${organization.id}`, async (route) => {
    if (route.request().method() !== "GET") {
      await route.fulfill({ status: 405, json: { title: "Method Not Allowed", status: 405 } });
      return;
    }
    await route.fulfill({ json: organization });
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

test("activity navigation, filters, paging, and no mutations", async ({ page }) => {
  await mockCore(page);
  const auditUrls: string[] = [];
  await page.route(`**/api/v1/platform/organizations/${organization.id}/audit*`, async (route) => {
    const url = route.request().url();
    auditUrls.push(url);
    expect(route.request().method()).toBe("GET");
    const pageParam = new URL(url).searchParams.get("page") ?? "1";
    await route.fulfill({
      json: {
        items: auditItems,
        totalCount: 21,
        page: Number(pageParam),
        pageSize: 20,
      },
    });
  });

  await page.goto(`/admin/organizations/${organization.id}`);
  await page
    .getByRole("navigation", { name: "Organization workspace" })
    .getByRole("link", { name: "Activity / Audit" })
    .click();
  await expect(page).toHaveURL(new RegExp(`/admin/organizations/${organization.id}/activity`));
  await expect(page.getByRole("heading", { name: "Activity / Audit", level: 1 })).toBeVisible();
  await expect(page.getByText("Signed in", { exact: true })).toBeVisible();
  await expect(page.getByRole("table").getByText("Succeeded", { exact: true })).toBeVisible();
  await expect(page.getByRole("table").getByText("Denied", { exact: true })).toBeVisible();
  await expect(page.getByText("Signed in successfully")).toBeVisible();
  await expect(page.getByRole("button", { name: /export/i })).toHaveCount(0);
  await expect(page.getByRole("button", { name: /delete/i })).toHaveCount(0);

  await page.getByLabel("Actor").fill("olivia");
  await page.getByLabel("Outcome").selectOption("Denied");
  await page.getByRole("button", { name: "Apply filters" }).click();
  await expect(page).toHaveURL(/actor=olivia/);
  await expect(page).toHaveURL(/outcome=Denied/);
  await expect(page.getByRole("button", { name: "Next" })).toBeEnabled();
  await page.getByRole("button", { name: "Next" }).click();
  await expect(page).toHaveURL(/page=2/);
  await expect
    .poll(() =>
      auditUrls.some((url) => {
        const params = new URL(url).searchParams;
        return params.get("page") === "2" && params.get("pageSize") === "20";
      }),
    )
    .toBe(true);
  expect(
    auditUrls.some((url) => {
      const params = new URL(url).searchParams;
      return params.get("actor") === "olivia" && params.get("outcome") === "Denied";
    }),
  ).toBe(true);
});

test("empty activity is truthful", async ({ page }) => {
  await mockCore(page);
  await page.route(`**/api/v1/platform/organizations/${organization.id}/audit*`, async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
  });
  await page.goto(`/admin/organizations/${organization.id}/activity`);
  await expect(page.getByText("No audit records")).toBeVisible();
});

test("forbidden activity fail-closes without leaking payload", async ({ page }) => {
  await mockCore(page);
  await page.route(`**/api/v1/platform/organizations/${organization.id}/audit*`, async (route) => {
    await route.fulfill({
      status: 403,
      json: { title: "Forbidden", status: 403, detail: "audit-secret" },
    });
  });
  await page.goto(`/admin/organizations/${organization.id}/activity`);
  await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
  await expect(page.getByText("audit-secret")).toHaveCount(0);
});

test("direct activity deep link, 375, and 320 have no overflow", async ({ page }) => {
  await mockCore(page);
  await page.route(`**/api/v1/platform/organizations/${organization.id}/audit*`, async (route) => {
    await route.fulfill({
      json: { items: auditItems, totalCount: 2, page: 1, pageSize: 20 },
    });
  });
  await page.goto(`/admin/organizations/${organization.id}/activity`);
  await expect(page.getByRole("heading", { name: "Activity / Audit", level: 1 })).toBeVisible();

  await page.setViewportSize({ width: 375, height: 812 });
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
    ),
  ).toBe(false);

  await page.setViewportSize({ width: 320, height: 800 });
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
    ),
  ).toBe(false);
});

test("activity localize, theme, density, and axe", async ({ page }) => {
  await mockCore(page);
  await page.route(`**/api/v1/platform/organizations/${organization.id}/audit*`, async (route) => {
    await route.fulfill({
      json: { items: auditItems, totalCount: 2, page: 1, pageSize: 20 },
    });
  });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/admin/organizations/${organization.id}/activity`);
  await expect(page.getByRole("heading", { name: "Activity / Audit", level: 1 })).toBeVisible();

  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Filipino/ }).click();
  await expect(page.getByRole("heading", { name: "Aktibidad / Audit", level: 1 })).toBeVisible();

  await page.getByRole("button", { name: "Mga kagustuhan" }).click();
  await page.getByRole("menuitem", { name: /^Madilim/ }).click();
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  await expect(page.locator("html")).toHaveAttribute("data-density", "balanced");

  await page.getByRole("button", { name: "Mga kagustuhan" }).click();
  await page.getByRole("menuitem", { name: /^Maliwanag/ }).click();
  await expect(page.locator("html")).toHaveAttribute("data-theme", "light");

  const accessibility = await new AxeBuilder({ page }).analyze();
  const serious = accessibility.violations.filter(
    (violation) => violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);
});
