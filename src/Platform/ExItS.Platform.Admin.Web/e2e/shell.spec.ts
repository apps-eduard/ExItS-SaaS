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

async function mockAuthenticatedSession(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: authorization });
  });
}

test("unauthenticated /admin redirects to sign-in", async ({ page }) => {
  await page.goto("/admin");
  await expect(page).toHaveURL(/\/admin\/login/);
});

test("authenticated shell is usable on desktop and has no serious accessibility violations", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockAuthenticatedSession(page);
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Overview" })).toBeVisible();
  await expect(page.getByText("Development Tools")).toHaveCount(0);
  await expect(page.getByRole("button", { name: "Sign Out" })).toHaveCount(0);

  const results = await new AxeBuilder({ page }).analyze();
  const serious = results.violations.filter(
    (violation) => violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);
});

test("authenticated shell remains usable at 1280x800", async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 800 });
  await mockAuthenticatedSession(page);
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Overview" })).toBeVisible();
});

test("tablet drawer navigation works at 768x1024", async ({ page }) => {
  await page.setViewportSize({ width: 768, height: 1024 });
  await mockAuthenticatedSession(page);
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
  await page.getByRole("button", { name: "Open navigation" }).click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog")).toHaveCount(0);
});

test("phone drawer navigation works at 375x812", async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 812 });
  await mockAuthenticatedSession(page);
  await page.goto("/admin");
  await page.getByRole("button", { name: "Open navigation" }).click();
  await expect(page.getByRole("dialog").getByRole("link", { name: "Overview" })).toBeVisible();
});

test("authenticated shell has no horizontal overflow at 320px", async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 568 });
  await mockAuthenticatedSession(page);
  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});
