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

async function mockSession(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({ json: session });
  });
}

test("authorization 500 stays fail-closed and exposes copyable diagnostics", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await mockSession(page);
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({
      status: 500,
      contentType: "application/problem+json",
      headers: { "X-Correlation-Id": "7f9c2f2e-1111-1111-1111-111111111111" },
      body: JSON.stringify({
        title: "An unexpected error occurred.",
        status: 500,
        detail: "An unexpected error occurred.",
        errorCode: "platform.internal_error",
        traceId: "00-server-trace",
      }),
    });
  });

  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
  await expect(page.getByRole("link", { name: "All Organizations" })).toHaveCount(0);
  await expect(page.getByRole("alert")).toHaveCount(1);
  await expect(page.getByRole("button", { name: "Copy diagnostics" })).toBeVisible();

  await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
  await page.getByRole("button", { name: "Copy diagnostics" }).click();
  await expect(page.getByText("Copied")).toBeVisible();
  const copied = await page.evaluate(() => navigator.clipboard.readText());
  expect(copied).toContain("EXITS ERROR DIAGNOSTICS");
  expect(copied).toContain("Load authorization");
  expect(copied).not.toContain("olivia@example.test");

  const results = await new AxeBuilder({ page }).analyze();
  const serious = results.violations.filter(
    (violation) => violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);
});

test("diagnostic notice does not overflow at 375px", async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 812 });
  await mockSession(page);
  await page.route("**/api/v1/platform/authorization/me", async (route) => {
    await route.fulfill({
      status: 500,
      contentType: "application/problem+json",
      body: JSON.stringify({
        title: "An unexpected error occurred.",
        status: 500,
        errorCode: "platform.internal_error",
        traceId: "00-server-trace",
      }),
    });
  });
  await page.goto("/admin");
  await expect(page.getByRole("alert")).toBeVisible();
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});
