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
    "platform.permission.manage_platform_users",
  ],
};

const users = {
  items: [
    {
      id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      displayName: "Olivia Mendoza",
      username: "olivia",
      email: "olivia@example.test",
      status: "Active",
      accountClasses: ["Platform"],
      organizationNames: [],
    },
  ],
  totalCount: 21,
  page: 1,
  pageSize: 20,
};

async function mockUsers(
  page: import("@playwright/test").Page,
  permissions = authorization.permissions,
) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: { ...authorization, permissions } });
  });
  await page.route("**/api/v1/platform/catalog/products*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 100 } });
  });
  await page.route("**/api/v1/platform/users*", async (route) => {
    await route.fulfill({ json: users });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test("authorized users directory is implemented and has no mutation controls", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockUsers(page);
  await page.goto("/admin/users");
  await expect(page.getByRole("heading", { name: "All Accounts", exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: "Olivia Mendoza" })).toBeVisible();
  await expect(page.getByRole("button", { name: /create/i })).toHaveCount(0);
  await expect(page.getByRole("link", { name: "Olivia Mendoza" })).toHaveAttribute(
    "href",
    "/admin/users/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  );
});

test("directory views use API enum values", async ({ page }) => {
  await mockUsers(page);
  await page.goto("/admin/users?directory=PlatformStaff");
  await expect(page.getByRole("heading", { name: "All Accounts / Platform Staff" })).toBeVisible();
  await page.goto("/admin/users?directory=Organization");
  await expect(
    page.getByRole("heading", { name: "All Accounts / Organization Accounts" }),
  ).toBeVisible();
  await page.goto("/admin/users?directory=Personal");
  await expect(
    page.getByRole("heading", { name: "All Accounts / Personal Accounts" }),
  ).toBeVisible();
  await page.goto("/admin/users?directory=Unassigned");
  await expect(page.getByRole("heading", { name: "All Accounts / Needs Review" })).toBeVisible();
});

test("unauthorized users route fail-closes", async ({ page }) => {
  await mockUsers(page, ["platform.permission.view_portfolio"]);
  await page.goto("/admin/users");
  await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "All Accounts", exact: true })).toHaveCount(0);
});

test("users directory has no overflow at 375 and no serious axe issues", async ({ page }) => {
  await mockUsers(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/users");
  await expect(page.getByRole("link", { name: "Olivia Mendoza" })).toBeVisible();
  const accessibility = await new AxeBuilder({ page }).analyze();
  expect(accessibility.violations).toEqual([]);
  await page.setViewportSize({ width: 375, height: 812 });
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});
