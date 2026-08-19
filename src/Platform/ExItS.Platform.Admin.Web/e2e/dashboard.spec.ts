import { expect, test, type Page } from "@playwright/test";
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

const fullAuthorization = {
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

function paged(items: unknown[] = [], totalCount = items.length) {
  return { items, totalCount, page: 1, pageSize: items.length || 1 };
}

async function mockSession(page: Page, authorization = fullAuthorization) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
}

async function mockDashboardData(page: Page, options?: { failOrganizations?: boolean }) {
  await page.route("**/api/v1/platform/organizations*", async (route) => {
    if (options?.failOrganizations) {
      await route.fulfill({
        status: 500,
        json: { title: "Error", status: 500, detail: "Organization list failed." },
      });
      return;
    }
    const url = new URL(route.request().url());
    const status = url.searchParams.get("status");
    if (status === "Suspended") {
      await route.fulfill({
        json: paged(
          [
            {
              id: "org-1",
              displayName: "Harbor Market",
              slug: "harbor-market",
              status: "Suspended",
            },
          ],
          1,
        ),
      });
      return;
    }
    await route.fulfill({ json: paged([], status === "Active" ? 4 : status === "Closed" ? 1 : 6) });
  });
  await page.route("**/api/v1/platform/subscriptions*", async (route) => {
    const url = new URL(route.request().url());
    const status = url.searchParams.get("status");
    const totals: Record<string, number> = { Trialing: 1, Active: 3, PastDue: 0, GracePeriod: 1 };
    await route.fulfill({ json: paged([], status ? (totals[status] ?? 0) : 5) });
  });
  await page.route("**/api/v1/platform/users*", async (route) => {
    const url = new URL(route.request().url());
    if (url.searchParams.get("directory") === "Unassigned") {
      await route.fulfill({
        json: paged(
          [
            {
              id: "user-1",
              displayName: "Unassigned Staff",
              username: "unassigned.staff",
              email: "unassigned@example.test",
              status: "Active",
            },
          ],
          1,
        ),
      });
      return;
    }
    await route.fulfill({ json: paged([], 2) });
  });
  await page.route("**/api/v1/platform/audit*", async (route) => {
    await route.fulfill({
      json: paged(
        [
          {
            id: "audit-1",
            occurredAtUtc: "2026-08-19T08:00:00Z",
            actorIdentifier: "olivia@example.test",
            actionCode: "platform.access.checked",
            targetType: "Organization",
            outcome: "Succeeded",
            summary: "Access checked",
          },
        ],
        1,
      ),
    });
  });
  await page.route("**/health/ready", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
  await page.route("**/health", async (route) => {
    if (route.request().url().includes("/health/ready")) {
      await route.fallback();
      return;
    }
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

async function expectNoHorizontalOverflow(page: Page) {
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
}

test("full-permission dashboard renders real summaries", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockSession(page);
  await mockDashboardData(page);
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Organizations", exact: true })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Organizations needing attention" }),
  ).toBeVisible();
  await expect(page.getByRole("heading", { name: "Subscriptions" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Accounts needing review" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Recent Platform activity" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Platform readiness" })).toBeVisible();
  await expect(page.getByText("Harbor Market")).toBeVisible();
  await expect(page.getByText("Platform access checked")).toBeVisible();
  await expect(page.locator('[title="platform.access.checked"]')).toBeVisible();
  await expect(page.getByRole("link", { name: "View organizations" })).toHaveCount(0);
});

test("limited-permission dashboard hides unauthorized widgets", async ({ page }) => {
  await mockSession(page, {
    ...fullAuthorization,
    permissions: ["platform.permission.view_audit_records"],
  });
  await mockDashboardData(page);
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Recent Platform activity" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Organizations", exact: true })).toHaveCount(0);
  await expect(page.getByRole("heading", { name: "Subscriptions" })).toHaveCount(0);
  await expect(page.getByRole("heading", { name: "Accounts needing review" })).toHaveCount(0);
});

test("one widget error does not blank the dashboard", async ({ page }) => {
  await mockSession(page);
  await mockDashboardData(page, { failOrganizations: true });
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Subscriptions" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Recent Platform activity" })).toBeVisible();
  await expect(page.getByText("Unable to load this summary.").first()).toBeVisible();
  await expect(page.getByRole("button", { name: "Retry" }).first()).toBeVisible();
});

test("dashboard has no horizontal overflow at 375px", async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 812 });
  await mockSession(page);
  await mockDashboardData(page);
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Organizations", exact: true })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Organizations needing attention" }),
  ).toBeVisible();
  await expectNoHorizontalOverflow(page);
});

test("dashboard has no horizontal overflow at 320px", async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 568 });
  await mockSession(page);
  await mockDashboardData(page);
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Organizations", exact: true })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Organizations needing attention" }),
  ).toBeVisible();
  await expectNoHorizontalOverflow(page);
});

test("dashboard localizes to Filipino", async ({ page }) => {
  await mockSession(page);
  await mockDashboardData(page);
  await page.goto("/admin");
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /Filipino/ }).click();
  await expect(page.getByRole("heading", { name: "Mga Organisasyon", exact: true })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Mga Subskripsyon" })).toBeVisible();
});

test("dashboard Dark theme remains usable", async ({ page }) => {
  await mockSession(page);
  await mockDashboardData(page);
  await page.goto("/admin");
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /Dark/ }).click();
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  await expect(page.getByRole("heading", { name: "Organizations", exact: true })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Organizations needing attention" }),
  ).toBeVisible();
});

test("authenticated dashboard has no serious accessibility violations", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockSession(page);
  await mockDashboardData(page);
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Organizations", exact: true })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Organizations needing attention" }),
  ).toBeVisible();
  const results = await new AxeBuilder({ page }).analyze();
  const serious = results.violations.filter(
    (violation) => violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);
});
