import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

const screenshotDir = resolve(
  process.cwd(),
  "../../../docs/Platform-Admin-Web/Reports/impl-06c-final-polish",
);

const session = {
  sessionId: "11111111-1111-1111-1111-111111111111",
  userId: "22222222-2222-2222-2222-222222222222",
  username: "olivia",
  displayName: "Olivia Mendoza",
  email: "olivia.mendoza@exits.local",
  expiresAtUtc: "2026-08-19T12:00:00Z",
  absoluteExpiresAtUtc: "2026-08-20T12:00:00Z",
  selectedOrganizationId: null,
  selectedOrganizationDisplayName: null,
  organizationSelectionState: "None",
  activeOrganizationCount: 0,
  accountClass: "Platform",
};

const authorization = {
  actorIdentifier: "olivia.mendoza@exits.local",
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

async function mockShell(page: Page) {
  let authenticated = true;
  await page.route("**/api/v1/platform/auth/logout", async (route) => {
    authenticated = false;
    await route.fulfill({ status: 204, body: "" });
  });
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    if (!authenticated) {
      await route.fulfill({
        status: 401,
        json: { status: 401, errorCode: "application.auth.session_invalid" },
      });
      return;
    }
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route("**/api/v1/platform/organizations*", async (route) => {
    const url = new URL(route.request().url());
    if (url.searchParams.get("status") === "Suspended") {
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
    await route.fulfill({ json: paged([], 6) });
  });
  await page.route("**/api/v1/platform/subscriptions*", async (route) => {
    await route.fulfill({ json: paged([], 5) });
  });
  await page.route("**/api/v1/platform/users*", async (route) => {
    await route.fulfill({ json: paged([], 0) });
  });
  await page.route("**/api/v1/platform/audit*", async (route) => {
    await route.fulfill({
      json: paged(
        [
          {
            id: "audit-1",
            occurredAtUtc: "2026-08-19T08:00:00Z",
            actorIdentifier: "platform-user:89535ae2-1234-5678-9abc-def0123e987a",
            actionCode: "platform.auth.login_succeeded",
            targetType: "PlatformAuthSession",
            outcome: "Succeeded",
            summary: "Signed in",
          },
          {
            id: "audit-2",
            occurredAtUtc: "2026-08-19T08:01:00Z",
            actorIdentifier: "olivia.mendoza@exits.local",
            actionCode: "platform.access.checked",
            targetType: "Organization",
            outcome: "Succeeded",
            summary: "Access checked",
          },
        ],
        2,
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

async function setLight(
  page: Page,
  extra?: { theme?: "light" | "dark"; language?: "en" | "fil-PH" },
) {
  await page.addInitScript((value) => {
    window.localStorage.setItem(
      "exits.platform-admin-web.ui-preferences.v1",
      JSON.stringify({
        theme: "light",
        language: "en",
        density: "balanced",
        sidebarCollapsed: false,
        ...value,
      }),
    );
  }, extra ?? {});
}

test.beforeAll(() => {
  mkdirSync(screenshotDir, { recursive: true });
});

test("shell polish screenshots, account menu, logout, and axe", async ({ page }) => {
  await mockShell(page);
  await setLight(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Collapse sidebar" })).toBeVisible();
  await expect(page.getByText("OM", { exact: true })).toBeVisible();
  await expect(page.getByText("Signed in", { exact: true })).toBeVisible();
  await expect(page.locator('[title="platform.auth.login_succeeded"]')).toBeVisible();
  await expect(page.getByText("platform.auth.login_succeeded")).toHaveCount(1);
  await expect(page.getByText("Authentication session")).toBeVisible();
  await expect(page.getByText("Platform user")).toBeVisible();
  await expect(page.getByText("89535ae2…e987a")).toBeVisible();
  await expect(page.getByRole("heading", { name: "Platform readiness" })).toBeVisible();
  await page.screenshot({ path: resolve(screenshotDir, "01-dashboard-expanded-1440x900.png") });
  await page.screenshot({ path: resolve(screenshotDir, "06-audit-polish.png") });

  await page.getByRole("button", { name: "Collapse sidebar" }).click();
  await expect(page.getByRole("button", { name: "Expand sidebar" })).toBeVisible();
  await page.screenshot({ path: resolve(screenshotDir, "02-dashboard-collapsed-1440x900.png") });
  await page.getByRole("button", { name: "Expand sidebar" }).click();

  await page.getByRole("button", { name: "Account menu" }).click();
  await expect(page.getByRole("menuitem", { name: /Sign out/i })).toBeVisible();
  await expect(page.getByRole("menu").locator(".break-all")).toHaveText(
    "olivia.mendoza@exits.local",
  );
  await page.screenshot({ path: resolve(screenshotDir, "03-account-menu-open.png") });
  await page.keyboard.press("Escape");

  const results = await new AxeBuilder({ page }).analyze();
  const serious = results.violations.filter(
    (violation) => violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);

  await page.getByRole("button", { name: "Account menu" }).click();
  await page.getByRole("menuitem", { name: /Sign out/i }).click();
  await expect(page).toHaveURL(/\/admin\/login/);
  await page.reload();
  await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
});

test("dark and phone polish screenshots", async ({ page }) => {
  await mockShell(page);
  await setLight(page, { theme: "dark" });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin");
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  await page.screenshot({ path: resolve(screenshotDir, "04-dashboard-dark.png") });

  await setLight(page);
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto("/admin");
  await expect(page.getByRole("button", { name: "Open navigation" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Collapse sidebar" })).toHaveCount(0);
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
  await page.screenshot({ path: resolve(screenshotDir, "05-dashboard-375x812.png") });
});
