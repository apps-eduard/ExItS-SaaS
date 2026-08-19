import { mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";

const enabled = process.env.PWEB_CONTAINER_SMOKE === "1";
const password = process.env.LOCAL_VALIDATION_SHARED_PASSWORD ?? "";
const screenshotDir = resolve(
  dirname(fileURLToPath(import.meta.url)),
  "../../../../docs/Platform-Admin-Web/Reports/impl-06a-local-validation",
);

test.describe("local-validation React container smoke", () => {
  test.skip(
    !enabled,
    "Set PWEB_CONTAINER_SMOKE=1 after the React Admin container is listening on 8095.",
  );
  test.use({ baseURL: process.env.PWEB_CONTAINER_BASE_URL ?? "http://localhost:8095" });

  test("serves /admin and SPA fallback for a known /admin route", async ({ page }) => {
    await page.goto("/admin");
    await expect(page.locator("#root")).toBeVisible();
    await expect(page).toHaveURL(/\/admin/);

    const response = await page.goto("/admin/organizations");
    expect(response?.ok()).toBeTruthy();
    await expect(page.locator("#root")).toBeVisible();
  });

  test("exposes the runtime Local Validation tools flag without secrets", async ({ request }) => {
    const response = await request.get("/config.js");
    expect(response.ok()).toBeTruthy();
    const body = await response.text();
    expect(body).toContain('platformApiBaseUrl:"http://localhost:8091"');
    expect(body).toContain("localValidationToolsEnabled:true");
    expect(body).not.toMatch(/password|secret|token/i);
  });

  test("Test User selector fills email only and cookie login reaches the dashboard", async ({
    page,
  }) => {
    test.skip(!password, "LOCAL_VALIDATION_SHARED_PASSWORD is required for the 8095 login path.");
    mkdirSync(screenshotDir, { recursive: true });

    await page.goto("/admin/login");
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
    await expect(page.getByText("Local Validation", { exact: true })).toBeVisible();
    const selector = page.getByLabel("Test User — Local Validation");
    await expect(selector).toBeVisible();

    await page.getByRole("button", { name: "Preferences" }).click();
    await page.getByRole("menuitem", { name: /^Light/ }).click();
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.screenshot({
      path: resolve(screenshotDir, "01-login-local-validation-1440x900-light.png"),
      fullPage: true,
    });

    await page.getByRole("button", { name: "Preferences" }).click();
    await page.getByRole("menuitem", { name: /^Dark/ }).click();
    await page.screenshot({
      path: resolve(screenshotDir, "02-login-local-validation-1440x900-dark.png"),
      fullPage: true,
    });

    await page.getByRole("button", { name: "Preferences" }).click();
    await page.getByRole("menuitem", { name: /^Light/ }).click();
    await page.setViewportSize({ width: 375, height: 812 });
    await page.screenshot({
      path: resolve(screenshotDir, "03-login-local-validation-375x812-light.png"),
      fullPage: true,
    });

    await page.setViewportSize({ width: 1440, height: 900 });
    const option = page
      .locator("#dev-test-user option")
      .filter({ hasText: "Olivia Mendoza" })
      .first();
    const value = await option.getAttribute("value");
    expect(value).toBeTruthy();
    await selector.selectOption(value!);
    await expect(page.getByLabel("Email")).toHaveValue(/olivia\.mendoza@exits\.local/i);
    await expect(page.locator("#sign-in-password")).toHaveValue("");
    await page.screenshot({
      path: resolve(screenshotDir, "04-login-after-test-user-selected.png"),
      fullPage: true,
    });

    await page.locator("#sign-in-password").fill(password);
    await page.getByRole("button", { name: "Sign In" }).click();
    await expect(page).toHaveURL(/\/admin$/);
    await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
    const storage = await page.evaluate(() => ({
      local: { ...window.localStorage },
      session: { ...window.sessionStorage },
    }));
    expect(JSON.stringify(storage)).not.toMatch(/sessionToken|accessToken|bearer/i);
    await page.screenshot({
      path: resolve(screenshotDir, "05-dashboard-after-test-login.png"),
      fullPage: true,
    });
  });
});
