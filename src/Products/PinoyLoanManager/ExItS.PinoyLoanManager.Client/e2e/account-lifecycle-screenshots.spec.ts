import { mkdirSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";
import { mockAnonymousSession } from "./helpers";

const screenshotDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../Docs/Reports/impl-gate-d2-account-lifecycle",
);

async function mockLifecycle(page: import("@playwright/test").Page) {
  await mockAnonymousSession(page);
  await page.route("**/platform-api/api/v1/platform/auth/register", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        message: "If the email is eligible, a verification message was sent.",
      }),
    }),
  );
}

test.describe("D2 screenshots", () => {
  test.beforeAll(() => {
    mkdirSync(screenshotDir, { recursive: true });
  });

  test("capture account lifecycle screens without secrets", async ({ page }) => {
    await mockLifecycle(page);
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/sign-up");
    await page.getByRole("radio", { name: "Light" }).click();
    await expect(page.getByRole("heading", { name: "Create account" })).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "01-sign-up-375x812.png"),
      fullPage: true,
    });

    await page.setViewportSize({ width: 1440, height: 900 });
    await page.screenshot({
      path: path.join(screenshotDir, "02-sign-up-1440x900.png"),
      fullPage: true,
    });

    await page.setViewportSize({ width: 375, height: 812 });
    await page.getByLabel("Display name").fill("Pat Lender");
    await page.getByLabel("Email").fill("pat@example.com");
    await page.getByRole("button", { name: "Create account" }).click();
    await expect(page.getByText("Check your email to continue.")).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "03-check-email.png"),
      fullPage: true,
    });

    await page.goto("/activate-account?token=screenshot-handoff");
    await expect(page).not.toHaveURL(/token=/);
    await expect(page.getByRole("heading", { name: "Activate account" })).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "04-activate-account.png"),
      fullPage: true,
    });

    await page.goto("/forgot-password");
    await expect(page.getByRole("heading", { name: "Forgot password" })).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "05-forgot-password.png"),
      fullPage: true,
    });

    await page.goto("/reset-password?token=screenshot-handoff");
    await expect(page).not.toHaveURL(/token=/);
    await expect(page.getByRole("heading", { name: "Reset password" })).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "06-reset-password.png"),
      fullPage: true,
    });
  });
});
