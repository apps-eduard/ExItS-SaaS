import { mkdirSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";
import { mockAnonymousSession, mockAuthenticatedSession, passwordField } from "./helpers";

const screenshotDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../Docs/Reports/impl-gate-d1-sign-in",
);

test.describe("D1 screenshots", () => {
  test.beforeAll(() => {
    mkdirSync(screenshotDir, { recursive: true });
  });

  test("capture sign-in and authenticated landing", async ({ page }) => {
    await mockAnonymousSession(page);
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/sign-in");
    await page.getByRole("radio", { name: "Light" }).click();
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "01-sign-in-375x812-light.png"),
      fullPage: true,
    });
    await page.getByRole("radio", { name: "Dark" }).click();
    await page.screenshot({
      path: path.join(screenshotDir, "02-sign-in-375x812-dark.png"),
      fullPage: true,
    });
    await page.getByRole("radio", { name: "Light" }).click();
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.screenshot({
      path: path.join(screenshotDir, "03-sign-in-1440x900.png"),
      fullPage: true,
    });
  });

  test.describe("development host", () => {
    test.use({ baseURL: "http://127.0.0.1:5176" });

    test("capture local validation selector", async ({ page }) => {
      await page.route("**/platform-api/api/v1/platform/auth/me", (route) =>
        route.fulfill({
          status: 401,
          contentType: "application/json",
          body: JSON.stringify({ errorCode: "application.auth.session_invalid" }),
        }),
      );
      await page.route("**/platform-api/api/v1/platform/local-validation/enabled", (route) =>
        route.fulfill({ status: 200, contentType: "application/json", body: "true" }),
      );
      await page.route(
        "**/platform-api/api/v1/platform/local-validation/quick-login-identities",
        (route) =>
          route.fulfill({
            status: 200,
            contentType: "application/json",
            body: JSON.stringify([
              {
                key: "olivia",
                username: "olivia",
                email: "olivia.mendoza@exits.local",
                listLabel: "Olivia Mendoza",
              },
            ]),
          }),
      );
      await page.setViewportSize({ width: 375, height: 812 });
      await page.goto("/sign-in");
      await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible({ timeout: 20_000 });
      await page.getByRole("radio", { name: "Light" }).click();
      await expect(page.getByLabel("Test User")).toBeVisible();
      await page.screenshot({
        path: path.join(screenshotDir, "04-local-validation-selector.png"),
        fullPage: true,
      });
    });
  });

  test("capture authenticated mobile landing", async ({ page }) => {
    await mockAuthenticatedSession(page);
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/sign-in");
    await page.getByLabel("Username or email").fill("olivia");
    await passwordField(page).fill("local-only");
    await page.getByRole("button", { name: "Sign in" }).click();
    await expect(page.getByRole("heading", { name: "Pinoy Loan Manager" })).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "05-authenticated-mobile.png"),
      fullPage: true,
    });
  });
});
