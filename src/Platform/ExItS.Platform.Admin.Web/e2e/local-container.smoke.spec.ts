import { mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";

const enabled = process.env.PWEB_CONTAINER_SMOKE === "1";
const password = process.env.LOCAL_VALIDATION_SHARED_PASSWORD ?? "";
const branchesScreenshotDir = resolve(
  dirname(fileURLToPath(import.meta.url)),
  "../../../../docs/Platform-Admin-Web/Reports/impl-09-organization-branches",
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

    await page.goto("/admin/login");
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
    await expect(page.getByText("Local Validation", { exact: true })).toBeVisible();
    const selector = page.getByLabel("Test User — Local Validation");
    await expect(selector).toBeVisible();

    await page.getByRole("button", { name: "Preferences" }).click();
    await page.getByRole("menuitem", { name: /^Light/ }).click();
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

    await page.locator("#sign-in-password").fill(password);
    await page.getByRole("button", { name: "Sign In" }).click();
    await expect(page).toHaveURL(/\/admin$/);
    await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
    const storage = await page.evaluate(() => ({
      local: { ...window.localStorage },
      session: { ...window.sessionStorage },
    }));
    expect(JSON.stringify(storage)).not.toMatch(/sessionToken|accessToken|bearer/i);

    await expect(page.getByRole("button", { name: "Collapse sidebar" })).toBeVisible();
    await expect(page.getByText("OM", { exact: true })).toBeVisible();
    await page.getByRole("button", { name: "Account menu" }).click();
    await expect(page.getByRole("menuitem", { name: /Sign out/i })).toBeVisible();
    const logout = page.waitForResponse(
      (response) =>
        response.url().includes("/api/v1/platform/auth/logout") &&
        response.request().method() === "POST",
    );
    await page.getByRole("menuitem", { name: /Sign out/i }).click();
    expect((await logout).ok()).toBeTruthy();
    await expect(page).toHaveURL(/\/admin\/login/);
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
    await page.reload();
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Overview" })).toHaveCount(0);

    await selector.selectOption(value!);
    await page.locator("#sign-in-password").fill(password);
    await page.getByRole("button", { name: "Sign In" }).click();
    await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();

    await expect(
      page
        .getByRole("navigation", { name: "Primary" })
        .getByRole("link", { name: "Organizations" }),
    ).toBeVisible();
    await page
      .getByRole("navigation", { name: "Primary" })
      .getByRole("link", { name: "Organizations" })
      .click();
    await expect(page).toHaveURL(/\/admin\/organizations/);
    await expect(
      page.getByRole("heading", { name: "Organizations", exact: true, level: 1 }),
    ).toBeVisible();
    await expect(page.locator('[aria-busy="true"]')).toHaveCount(0);
    await expect(page.getByText("abc-sari-sari")).toBeVisible();
    await expect(page.getByRole("button", { name: /create/i })).toHaveCount(0);

    mkdirSync(branchesScreenshotDir, { recursive: true });
    await page.setViewportSize({ width: 1440, height: 900 });
    const organizationLink = page.locator('table a[href^="/admin/organizations/"]').first();
    await expect(organizationLink).toBeVisible();
    await organizationLink.click();
    await expect(page).toHaveURL(/\/admin\/organizations\/[0-9a-fA-F-]{36}$/);
    await expect(page.locator("h1")).toBeVisible();
    const workspaceNav = page.getByRole("navigation", { name: "Organization workspace" });
    await expect(workspaceNav.getByRole("link", { name: "Overview" })).toBeVisible();
    await expect(workspaceNav.getByRole("link", { name: "Branches" })).toBeVisible();
    await expect(workspaceNav.getByRole("link", { name: "People" })).toHaveCount(0);
    await page.screenshot({
      path: resolve(branchesScreenshotDir, "04-workspace-navigation.png"),
      fullPage: true,
    });
    await workspaceNav.getByRole("link", { name: "Branches" }).click();
    await expect(page).toHaveURL(/\/admin\/organizations\/[0-9a-fA-F-]{36}\/branches$/);
    await expect(
      page.getByRole("heading", { name: "Branches", exact: true, level: 1 }),
    ).toBeVisible();
    await expect(page.locator('[aria-busy="true"]')).toHaveCount(0);
    await expect(page.getByRole("button", { name: /create/i })).toHaveCount(0);
    await expect(page.getByRole("button", { name: /edit/i })).toHaveCount(0);
    await page.getByRole("button", { name: "Preferences" }).click();
    await page.getByRole("menuitem", { name: /^Light/ }).click();
    await page.screenshot({
      path: resolve(branchesScreenshotDir, "01-branches-1440x900-light.png"),
      fullPage: true,
    });
    await page.getByRole("button", { name: "Preferences" }).click();
    await page.getByRole("menuitem", { name: /^Dark/ }).click();
    await page.screenshot({
      path: resolve(branchesScreenshotDir, "02-branches-1440x900-dark.png"),
      fullPage: true,
    });
    await page.getByRole("button", { name: "Preferences" }).click();
    await page.getByRole("menuitem", { name: /^Light/ }).click();
    await page.setViewportSize({ width: 375, height: 812 });
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
    );
    expect(overflow).toBe(false);
    await page.screenshot({
      path: resolve(branchesScreenshotDir, "03-branches-375x812.png"),
      fullPage: true,
    });
  });
});
