import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
import { expect, test, type Page } from "@playwright/test";

const screenshotDir = resolve(
  process.cwd(),
  "../../../docs/Platform-Admin-Web/Reports/impl-14c-final-navigation-blueprint",
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

async function mockShell(page: Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route("**/api/v1/platform/catalog/products*", async (route) => {
    await route.fulfill({
      json: {
        items: [
          {
            id: "11111111-1111-1111-1111-111111111111",
            code: "future-product-x",
            displayName: "Future Product X",
            status: "Active",
          },
          {
            id: "22222222-2222-2222-2222-222222222222",
            code: "pinoy-business-pos",
            displayName: "Pinoy Business POS",
            status: "Active",
          },
        ],
        totalCount: 2,
        page: 1,
        pageSize: 100,
      },
    });
  });
  await page.route("**/api/v1/platform/organizations*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
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

test("14C navigation blueprint screenshots", async ({ page }) => {
  await mockShell(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
  await expect(page.getByRole("link", { name: "All Organizations" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Future Product X" })).toBeVisible();
  await page.getByRole("button", { name: "Preferences" }).click();
  await page.getByRole("menuitem", { name: /^Light/ }).click();
  await page.screenshot({
    path: resolve(screenshotDir, "01-sidebar-expanded-full-1440x900.png"),
    fullPage: true,
  });

  // Keep Organizations + By Product visible for screenshot 02.
  const orgToggle = page.getByRole("button", { name: "Organizations", exact: true });
  if ((await orgToggle.getAttribute("aria-expanded")) !== "true") {
    await orgToggle.click();
  }
  const byProductToggle = page.getByRole("button", { name: "By Product", exact: true });
  if ((await byProductToggle.getAttribute("aria-expanded")) !== "true") {
    await byProductToggle.click();
  }
  await expect(page.getByRole("link", { name: "All Organizations" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Future Product X" })).toBeVisible();
  await page.screenshot({
    path: resolve(screenshotDir, "02-organizations-by-product-expanded.png"),
    fullPage: true,
  });

  // Ensure People & Access is expanded (toggle only if currently collapsed).
  const peopleToggle = page.getByRole("button", { name: "People & Access", exact: true });
  if ((await peopleToggle.getAttribute("aria-expanded")) !== "true") {
    await peopleToggle.click();
  }
  await expect(page.getByLabel("All Accounts. Under development")).toBeVisible();
  await page.screenshot({
    path: resolve(screenshotDir, "03-people-section-expanded.png"),
    fullPage: true,
  });

  await page.getByRole("button", { name: "Collapse sidebar" }).click();
  await expect(page.getByRole("button", { name: "Expand sidebar" })).toBeVisible();
  await page.screenshot({
    path: resolve(screenshotDir, "04-sidebar-icon-rail.png"),
    fullPage: true,
  });

  await page.getByRole("button", { name: "Expand sidebar" }).click();
  await page.setViewportSize({ width: 375, height: 812 });
  await page.getByRole("button", { name: "Open navigation" }).click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await expect(
    page.getByRole("dialog").getByRole("link", { name: "All Organizations" }),
  ).toBeVisible();
  await page.screenshot({
    path: resolve(screenshotDir, "05-mobile-drawer-375x812.png"),
    fullPage: true,
  });
});
