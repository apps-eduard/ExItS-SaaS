import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { assertNoHorizontalOverflow } from "./helpers";

const viewports = [
  { name: "320", width: 320, height: 568 },
  { name: "375", width: 375, height: 812 },
  { name: "768", width: 768, height: 1024 },
  { name: "1440", width: 1440, height: 900 },
] as const;

async function mockAuthenticatedSession(page: import("@playwright/test").Page) {
  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    if (url.includes("/api/v1/platform/auth/me")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          sessionId: "11111111-1111-1111-1111-111111111111",
          username: "owner",
          displayName: "Owner User",
        }),
      });
    }
    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      return route.fulfill({ status: 200, contentType: "application/json", body: "[]" });
    }
    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ headerName: "X-XSRF-TOKEN", token: "e2e-csrf" }),
      });
    }
    return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });
}

test.describe("POS React foundation", () => {
  test("sign-in loads in English by default", async ({ page }) => {
    await page.route("**/platform-api/**", async (route) => {
      if (route.request().url().includes("/auth/me")) {
        return route.fulfill({ status: 401, contentType: "application/json", body: "{}" });
      }
      return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
    });
    await page.goto("/sign-in");
    await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();
    await expect(page.locator("html")).toHaveAttribute("lang", "en");
    await expect(page.locator("html")).toHaveAttribute("data-theme", "system");
  });

  for (const viewport of viewports) {
    test(`${viewport.name} has no horizontal overflow on sign-in`, async ({ page }) => {
      await page.route("**/platform-api/**", async (route) => {
        if (route.request().url().includes("/auth/me")) {
          return route.fulfill({ status: 401, contentType: "application/json", body: "{}" });
        }
        return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
      });
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await page.goto("/sign-in");
      await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();
      await assertNoHorizontalOverflow(page);
    });
  }

  test("theme switch is global from preferences", async ({ page }) => {
    await mockAuthenticatedSession(page);
    await page.goto("/settings/preferences");
    await expect(page.getByRole("heading", { name: "Preferences" })).toBeVisible();
    await page.getByRole("button", { name: "Theme: System" }).click();
    await page.getByRole("menuitem", { name: /Dark/ }).click();
    await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
    await page.getByRole("button", { name: "Theme: Dark" }).click();
    await page.getByRole("menuitem", { name: /Light/ }).click();
    await expect(page.locator("html")).toHaveAttribute("data-theme", "light");
    await page.getByRole("button", { name: "Theme: Light" }).click();
    await page.getByRole("menuitem", { name: /System/ }).click();
    await expect(page.locator("html")).toHaveAttribute("data-theme", "system");
  });

  test("locale switch proves English and Filipino from preferences", async ({ page }) => {
    await mockAuthenticatedSession(page);
    await page.goto("/settings/preferences");
    await expect(page.getByRole("heading", { name: "Preferences" })).toBeVisible();
    await page.getByRole("button", { name: "Language: English" }).click();
    await page.getByRole("menuitem", { name: /Filipino/ }).click();
    await expect(page.locator("html")).toHaveAttribute("lang", "fil-PH");
    await expect(page.getByRole("heading", { name: "Preferences" })).toBeVisible();
    await page.getByRole("button", { name: /Language: Filipino|Wika: Filipino/ }).click();
    await page.getByRole("menuitem", { name: /English/ }).click();
    await expect(page.locator("html")).toHaveAttribute("lang", "en");
    await expect(page.getByRole("heading", { name: "Preferences" })).toBeVisible();
  });

  test("unknown routes render 404", async ({ page }) => {
    await mockAuthenticatedSession(page);
    await page.goto("/this-route-does-not-exist");
    await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
    await page.getByRole("link", { name: "Back to home" }).click();
    await expect(page).toHaveURL("/personal");
  });

  test("axe has no serious or critical violations on sign-in", async ({ page }) => {
    await page.route("**/platform-api/**", async (route) => {
      if (route.request().url().includes("/auth/me")) {
        return route.fulfill({ status: 401, contentType: "application/json", body: "{}" });
      }
      return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
    });
    await page.goto("/sign-in");
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(
      (violation) => violation.impact === "serious" || violation.impact === "critical",
    );
    expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
  });
});
