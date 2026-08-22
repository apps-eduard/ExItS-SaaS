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
  permissions: ["platform.permission.view_portfolio", "platform.permission.manage_organizations"],
};

const checkedAt = "2026-08-22T11:00:00Z";

const healthy = {
  overallStatus: "Healthy",
  host: {
    cpuPercent: 12.4,
    memoryUsedBytes: 6657199309,
    memoryTotalBytes: 17179869184,
    storageUsedBytes: 141733920768,
    storageFreeBytes: 126701535232,
    storageTotalBytes: 268435456000,
    uptimeSeconds: 18 * 86400 + 7 * 3600,
  },
  services: [
    { name: "platform-api", status: "Healthy", latencyMs: 21, checkedAtUtc: checkedAt },
    { name: "pos-api", status: "Healthy", latencyMs: 18, checkedAtUtc: checkedAt },
    { name: "platform-database", status: "Healthy", latencyMs: 9, checkedAtUtc: checkedAt },
    { name: "pos-database", status: "Healthy", latencyMs: 11, checkedAtUtc: checkedAt },
  ],
  build: {
    environment: "Testing",
    applicationVersion: "1.0.0",
    commitSha: "abcdef123456",
  },
  backup: { status: "NotAvailable", lastSuccessfulAtUtc: null, ageSeconds: null },
};

const degraded = {
  ...healthy,
  overallStatus: "Degraded",
  services: [
    { name: "platform-api", status: "Healthy", latencyMs: 8, checkedAtUtc: checkedAt },
    { name: "pos-api", status: "Degraded", latencyMs: 80, checkedAtUtc: checkedAt },
    { name: "platform-database", status: "Healthy", latencyMs: 7, checkedAtUtc: checkedAt },
    { name: "pos-database", status: "Healthy", latencyMs: 9, checkedAtUtc: checkedAt },
  ],
};

async function mockSystemHealth(
  page: import("@playwright/test").Page,
  snapshot: typeof healthy,
  permissions = authorization.permissions,
) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({ json: { ...authorization, permissions } });
  });
  await page.route("**/api/v1/platform/operations/system-health", async (route) => {
    await route.fulfill({ json: snapshot });
  });
  await page.route("**/api/v1/platform/catalog/products*", async (route) => {
    await route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 100 } });
  });
  await page.route("**/health/**", async (route) => {
    await route.fulfill({ contentType: "text/plain", body: "Healthy" });
  });
}

test("system-health page opens in a healthy scenario", async ({ page }) => {
  await mockSystemHealth(page, healthy);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/system-health");
  await expect(page.getByRole("heading", { name: "System Health" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Overall" })).toBeVisible();
  await expect(page.getByText("6.2 GB / 16 GB")).toBeVisible();
  await expect(page.getByRole("table").getByText("Platform API")).toBeVisible();
  await expect(page.getByText("abcdef123456")).toBeVisible();
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});

test("system-health degraded service scenario", async ({ page }) => {
  await mockSystemHealth(page, degraded);
  await page.goto("/admin/system-health");
  await expect(page.getByRole("heading", { name: "System Health" })).toBeVisible();
  await expect(page.getByText("Degraded").first()).toBeVisible();
  await expect(page.getByText("80 ms")).toBeVisible();
});

test("system-health has no serious axe violations at 1440x900", async ({ page }) => {
  await mockSystemHealth(page, healthy);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/admin/system-health");
  await expect(page.getByRole("heading", { name: "System Health" })).toBeVisible();
  const results = await new AxeBuilder({ page }).analyze();
  const serious = results.violations.filter(
    (violation) => violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);
});

test("system-health tablet 768x1024 has no horizontal overflow", async ({ page }) => {
  await mockSystemHealth(page, healthy);
  await page.setViewportSize({ width: 768, height: 1024 });
  await page.goto("/admin/system-health");
  await expect(page.getByRole("heading", { name: "System Health" })).toBeVisible();
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});
