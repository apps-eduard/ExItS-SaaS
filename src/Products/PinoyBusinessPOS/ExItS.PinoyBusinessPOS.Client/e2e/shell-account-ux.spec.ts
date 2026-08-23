import { mkdirSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import { mockBoundCashierSession, signInAndBindCashier } from "./mock-bound-session";

const screenshotDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../../../../docs/Mobile-React/Reports/impl-pos-react-shell-account-ux",
);

test.describe("shell account UX evidence", () => {
  test.beforeAll(() => {
    mkdirSync(screenshotDir, { recursive: true });
  });

  test("desktop shell account menu and preferences light", async ({ page }) => {
    await mockBoundCashierSession(page);
    await page.setViewportSize({ width: 1440, height: 900 });
    await signInAndBindCashier(page);
    await expect(page.getByRole("heading", { name: "New Sale" })).toBeVisible();
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "01-desktop-shell-1440x900-en-light.png"),
      fullPage: true,
    });

    await page.getByTestId("account-menu-trigger").click();
    await expect(page.getByRole("menu")).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "02-desktop-account-menu-1440x900-en-light.png"),
      fullPage: true,
    });

    await page.getByRole("menuitem", { name: "Preferences" }).click();
    await expect(page.getByRole("heading", { name: "Preferences" })).toBeVisible();
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "03-desktop-preferences-1440x900-en-light.png"),
      fullPage: true,
    });
  });

  test("desktop preferences dark and Filipino", async ({ page }) => {
    await mockBoundCashierSession(page);
    await page.setViewportSize({ width: 1440, height: 900 });
    await signInAndBindCashier(page);
    await page.getByTestId("account-menu-trigger").click();
    await page.getByRole("menuitem", { name: "Preferences" }).click();

    await page.getByRole("radio", { name: "Theme: Dark" }).click();
    await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
    await page.screenshot({
      path: path.join(screenshotDir, "04-desktop-preferences-1440x900-en-dark.png"),
      fullPage: true,
    });

    await page.getByRole("radio", { name: "Language: Filipino" }).click();
    await expect(page.locator("html")).toHaveAttribute("lang", "fil-PH");
    await page.screenshot({
      path: path.join(screenshotDir, "05-desktop-preferences-1440x900-fil-dark.png"),
      fullPage: true,
    });
  });

  test("tablet landscape shell", async ({ page }) => {
    await mockBoundCashierSession(page);
    await page.setViewportSize({ width: 1024, height: 768 });
    await signInAndBindCashier(page);
    await expect(page.getByTestId("sell-floor")).toBeVisible();
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "06-tablet-landscape-shell-1024x768-en-light.png"),
      fullPage: true,
    });
  });

  test("tablet portrait and phone shells", async ({ page }) => {
    await mockBoundCashierSession(page);

    await page.setViewportSize({ width: 768, height: 1024 });
    await signInAndBindCashier(page);
    await expect(page.getByRole("heading", { name: "New Sale" })).toBeVisible();
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "07-tablet-portrait-shell-768x1024-en-light.png"),
      fullPage: true,
    });

    await page.setViewportSize({ width: 375, height: 812 });
    await assertNoHorizontalOverflow(page);
    await page.getByTestId("account-menu-trigger").click();
    await expect(page.getByRole("menu")).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "08-phone-account-menu-375x812-en-light.png"),
      fullPage: true,
    });
    await page.keyboard.press("Escape");

    await page.setViewportSize({ width: 320, height: 568 });
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "09-phone-shell-320x568-en-light.png"),
      fullPage: true,
    });
  });

  test("sign-in brand consistency", async ({ page }) => {
    await page.route("**/platform-api/**", async (route) => {
      if (route.request().url().includes("/auth/me")) {
        return route.fulfill({ status: 401, contentType: "application/json", body: "{}" });
      }
      return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
    });
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto("/sign-in");
    await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "10-sign-in-1440x900-en-light.png"),
      fullPage: true,
    });
  });
});
