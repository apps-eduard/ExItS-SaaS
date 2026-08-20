import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

const screenshotDir = resolve(
  process.cwd(),
  "../../../docs/Platform-Admin-Web/Reports/impl-17-platform-user-detail",
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
    "platform.permission.manage_platform_users",
  ],
};

const userDetail = {
  id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  displayName: "Olivia Mendoza",
  username: "olivia",
  email: "olivia@example.test",
  status: "Active",
  accountClasses: ["Platform"],
  organizationNames: ["Northwind"],
  organizations: [{ name: "Northwind", roleDisplay: "Owner" }],
  firstName: "Olivia",
  lastName: "Mendoza",
  createdAtUtc: "2026-01-01T08:00:00Z",
  updatedAtUtc: "2026-08-01T08:00:00Z",
};

const assignments = {
  items: [
    {
      id: "11111111-1111-1111-1111-111111111111",
      platformUserId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      role: "PlatformAdministrator",
      status: "Active",
      grantedByActor: "admin@example.test",
      grantedAtUtc: "2026-08-01T08:00:00Z",
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 10,
};

async function mockShell(page: Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
  await page.route("**/api/v1/platform/catalog/products*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 100 } });
  });
  await page.route("**/api/v1/platform/users/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", async (route) => {
    await route.fulfill({ json: userDetail });
  });
  await page.route("**/api/v1/platform/authorization/assignments*", async (route) => {
    await route.fulfill({ json: assignments });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test.beforeAll(() => {
  mkdirSync(screenshotDir, { recursive: true });
});

test("17 platform user detail screenshots and axe", async ({ page }) => {
  await mockShell(page);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/users/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
  await expect(page.getByRole("heading", { name: "Olivia Mendoza" })).toBeVisible();
  await expect(page.getByText("Platform administrator")).toBeVisible();
  await page.screenshot({
    path: resolve(screenshotDir, "01-user-detail-1440x900.png"),
    fullPage: true,
  });
  const accessibility = await new AxeBuilder({ page }).analyze();
  expect(accessibility.violations).toEqual([]);
  await page.setViewportSize({ width: 375, height: 812 });
  const overflow375 = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow375).toBe(false);
  await page.screenshot({
    path: resolve(screenshotDir, "02-user-detail-375x812.png"),
    fullPage: true,
  });
  await page.setViewportSize({ width: 320, height: 640 });
  const overflow320 = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow320).toBe(false);
  await page.screenshot({
    path: resolve(screenshotDir, "03-user-detail-320x640.png"),
    fullPage: true,
  });
});
