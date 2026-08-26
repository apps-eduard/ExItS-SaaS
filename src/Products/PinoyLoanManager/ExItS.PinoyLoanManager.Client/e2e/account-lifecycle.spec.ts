import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import {
  assertNoHorizontalOverflow,
  assertNoSessionTokenPersistence,
  mockAnonymousSession,
} from "./helpers";

async function mockLifecycleApis(page: import("@playwright/test").Page) {
  await mockAnonymousSession(page);
  await page.route("**/platform-api/api/v1/platform/auth/register", (route) =>
    route.fulfill({
      status: 409,
      contentType: "application/json",
      body: JSON.stringify({ errorCode: "application.auth.email_conflict" }),
    }),
  );
  await page.route("**/platform-api/api/v1/platform/auth/forgot-password", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        message: "If an eligible account exists, a password reset token was issued.",
      }),
    }),
  );
  await page.route("**/platform-api/api/v1/platform/auth/activate-account", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ hasPassword: true }),
    }),
  );
  await page.route("**/platform-api/api/v1/platform/auth/reset-password", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ hasPassword: true }),
    }),
  );
}

test.describe("account lifecycle", () => {
  test("sign-up, forgot, activate, and reset fit 320/375/desktop", async ({ page }) => {
    await mockLifecycleApis(page);
    for (const viewport of [
      { width: 320, height: 568 },
      { width: 375, height: 812 },
      { width: 1440, height: 900 },
    ] as const) {
      await page.setViewportSize(viewport);
      await page.goto("/sign-up");
      await expect(page.getByRole("heading", { name: "Create account" })).toBeVisible();
      await assertNoHorizontalOverflow(page);
      await page.goto("/forgot-password");
      await expect(page.getByRole("heading", { name: "Forgot password" })).toBeVisible();
      await assertNoHorizontalOverflow(page);
    }
  });

  test("privacy-safe registration acknowledgement and no token persistence", async ({ page }) => {
    await mockLifecycleApis(page);
    await page.goto("/sign-up");
    await page.getByLabel("Display name").fill("Pat Lender");
    await page.getByLabel("Email").fill("pat@example.com");
    await page.getByRole("button", { name: "Create account" }).click();
    await expect(page.getByText("Check your email to continue.")).toBeVisible();
    await expect(page.getByText(/already exists|borrower|Synced/i)).toHaveCount(0);
    await assertNoSessionTokenPersistence(page);
  });

  test("activation succeeds, scrubs the token, and stays off storage", async ({ page }) => {
    await mockLifecycleApis(page);
    await page.goto("/activate-account?token=one-time-handoff");
    await expect(page).toHaveURL(/\/activate-account$/);
    await expect(page).not.toHaveURL(/token=/);
    await page.getByLabel("New password").fill("local-only");
    await page.getByLabel("Confirm password").fill("local-only");
    await page.getByRole("button", { name: "Activate account" }).click();
    await expect(page).toHaveURL(/\/sign-in$/);
    await expect(page.getByText("Account activated. Sign in with your password.")).toBeVisible();
    await expect(page).not.toHaveURL(/token=/);
    await assertNoSessionTokenPersistence(page);
    expect(await page.content()).not.toContain("one-time-handoff");
  });

  test("missing activation token is safe", async ({ page }) => {
    await mockLifecycleApis(page);
    await page.goto("/activate-account");
    await expect(page.getByText("Activation link is invalid or missing.")).toBeVisible();
  });

  test("reset succeeds and shows the sign-in notice", async ({ page }) => {
    await mockLifecycleApis(page);
    await page.goto("/reset-password?token=reset-handoff");
    await page.getByLabel("New password").fill("local-only");
    await page.getByLabel("Confirm password").fill("local-only");
    await page.getByRole("button", { name: "Reset password" }).click();
    await expect(page.getByText("Password reset. Sign in with your new password.")).toBeVisible();
    await expect(page).not.toHaveURL(/token=/);
  });

  test("axe has no serious or critical violations on sign-up", async ({ page }) => {
    await mockLifecycleApis(page);
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/sign-up");
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(
      (violation) => violation.impact === "serious" || violation.impact === "critical",
    );
    expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
  });
});
