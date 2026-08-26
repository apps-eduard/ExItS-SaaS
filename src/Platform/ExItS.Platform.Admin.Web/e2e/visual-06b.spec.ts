import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

const screenshotDir = resolve(
  process.cwd(),
  "../../../docs/Platform-Admin-Web/Reports/impl-06b-uniform-visual-system",
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

const olivia = {
  key: "olivia",
  username: "olivia",
  displayName: "Olivia Mendoza",
  email: "olivia.mendoza@exits.local",
  listLabel: "Olivia Mendoza — Platform Administration",
};

function paged(items: unknown[] = [], totalCount = items.length) {
  return { items, totalCount, page: 1, pageSize: items.length || 1 };
}

async function mockUnauthenticated(page: Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({
      status: 401,
      json: { status: 401, errorCode: "application.auth.session_invalid" },
    });
  });
}

async function mockDashboard(page: Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route("**/api/v1/platform/organizations*", async (route) => {
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
    await route.fulfill({
      json: paged([], status === "Active" ? 18 : status === "Closed" ? 2 : 24),
    });
  });
  await page.route("**/api/v1/platform/subscriptions*", async (route) => {
    const url = new URL(route.request().url());
    const status = url.searchParams.get("status");
    const totals: Record<string, number> = { Trialing: 4, Active: 22, PastDue: 3, GracePeriod: 2 };
    await route.fulfill({ json: paged([], status ? (totals[status] ?? 0) : 31) });
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
          3,
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

async function setPreferences(
  page: Page,
  prefs: { theme?: "light" | "dark" | "system"; language?: "en" | "fil-PH" },
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
  }, prefs);
}

async function expectNoOverflow(page: Page) {
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
}

async function expectNoSeriousAxe(page: Page) {
  const results = await new AxeBuilder({ page }).analyze();
  const serious = results.violations.filter(
    (violation) => violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);
}

test.beforeAll(() => {
  mkdirSync(screenshotDir, { recursive: true });
});

test("login screenshots and accessibility", async ({ page }) => {
  await mockUnauthenticated(page);
  await setPreferences(page, { theme: "light", language: "en" });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/login");
  await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
  await expect(page.locator("#dev-test-user")).toHaveCount(0);
  await page.screenshot({ path: resolve(screenshotDir, "01-login-1440x900-en-light.png") });
  await expectNoSeriousAxe(page);

  await setPreferences(page, { theme: "dark", language: "en" });
  await page.goto("/admin/login");
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  await page.screenshot({ path: resolve(screenshotDir, "02-login-1440x900-en-dark.png") });

  await setPreferences(page, { theme: "light", language: "en" });
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto("/admin/login");
  await expectNoOverflow(page);
  await page.screenshot({ path: resolve(screenshotDir, "03-login-375x812-en-light.png") });

  await setPreferences(page, { theme: "light", language: "fil-PH" });
  await page.goto("/admin/login");
  await expect(page.getByRole("heading", { name: "Mag-sign In" })).toBeVisible();
  await page.screenshot({ path: resolve(screenshotDir, "04-login-375x812-fil-PH.png") });
});

test("local validation screenshots fill email only", async ({ page }) => {
  await page.route("**/config.js", async (route) => {
    await route.fulfill({
      contentType: "application/javascript",
      body: 'window.__EXITS_PLATFORM_ADMIN_WEB__={platformApiBaseUrl:"http://127.0.0.1:8091",localValidationToolsEnabled:true};',
    });
  });
  await mockUnauthenticated(page);
  await page.route("**/api/v1/platform/local-validation/enabled", async (route) => {
    await route.fulfill({ json: true });
  });
  await page.route("**/api/v1/platform/local-validation/quick-login-identities", async (route) => {
    await route.fulfill({ json: [olivia] });
  });
  await setPreferences(page, { theme: "light", language: "en" });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/login");
  await expect(page.getByText("Local Validation", { exact: true })).toBeVisible();
  await expect(page.getByLabel("Test User — Local Validation")).toBeVisible();
  await page.screenshot({
    path: resolve(screenshotDir, "05-login-local-validation-1440x900.png"),
  });

  await page.getByLabel("Test User — Local Validation").selectOption("olivia");
  await expect(page.getByLabel("Email")).toHaveValue(olivia.email);
  await expect(page.locator("#sign-in-password")).toHaveValue("");
  await page.screenshot({
    path: resolve(screenshotDir, "06-login-local-validation-user-selected.png"),
  });
});

test("dashboard screenshots, density, and accessibility", async ({ page }) => {
  await mockDashboard(page);
  await setPreferences(page, { theme: "light", language: "en" });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Organizations", exact: true })).toBeVisible();
  await expect(page.getByRole("columnheader", { name: "Action" })).toBeVisible();
  await page.screenshot({ path: resolve(screenshotDir, "07-dashboard-1440x900-en-light.png") });
  await expectNoSeriousAxe(page);

  await page.getByRole("button", { name: "Collapse sidebar" }).click();
  await expect(page.getByRole("button", { name: "Expand sidebar" })).toBeVisible();
  await page.getByRole("button", { name: "Expand sidebar" }).click();

  await setPreferences(page, { theme: "dark", language: "en" });
  await page.goto("/admin");
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  await page.screenshot({ path: resolve(screenshotDir, "08-dashboard-1440x900-en-dark.png") });

  await setPreferences(page, { theme: "light", language: "en" });
  await page.setViewportSize({ width: 1280, height: 800 });
  await page.goto("/admin");
  await page.screenshot({ path: resolve(screenshotDir, "09-dashboard-1280x800-en-light.png") });

  await page.setViewportSize({ width: 768, height: 1024 });
  await page.goto("/admin");
  await page.getByRole("button", { name: "Open navigation" }).click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.keyboard.press("Escape");
  await page.screenshot({ path: resolve(screenshotDir, "10-dashboard-768x1024-en-light.png") });

  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto("/admin");
  await expectNoOverflow(page);
  await page.screenshot({ path: resolve(screenshotDir, "11-dashboard-375x812-en-light.png") });

  await setPreferences(page, { theme: "light", language: "fil-PH" });
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Pangkalahatang-tanaw" })).toBeVisible();
  await page.screenshot({ path: resolve(screenshotDir, "12-dashboard-375x812-fil-PH.png") });

  await setPreferences(page, { theme: "light", language: "en" });
  await page.setViewportSize({ width: 320, height: 568 });
  await page.goto("/admin");
  await expectNoOverflow(page);
  await page.screenshot({ path: resolve(screenshotDir, "13-dashboard-320x568-en-light.png") });
});
