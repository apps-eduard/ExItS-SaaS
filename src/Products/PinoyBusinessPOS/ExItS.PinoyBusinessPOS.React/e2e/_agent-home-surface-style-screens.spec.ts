/**
 * One-off visual capture for Personal Home surface style toggle.
 * Not part of the permanent e2e suite.
 */
import { expect, test, type Page } from "@playwright/test";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { clientNavigate, mockPersonalSession, signInAsPersonal } from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const OUT_DIR = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "_agent-screenshots");
const STORAGE_KEY = "exits.personal.home.surfaceStyle";

function json(route: { fulfill: (r: object) => Promise<void> }, body: unknown) {
  return route.fulfill({
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

async function installHomeMocks(page: Page) {
  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    if (url.includes("/api/v1/personal/dashboard")) {
      return json(route, {
        userIdentityId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        accountProfileId: "11111111-1111-4111-8111-111111111111",
        accountClass: "Personal",
        utangAvailable: true,
        contactCount: 12,
        activeRelationshipCount: 5,
        totalLentBalance: 128450.75,
        totalBorrowedBalance: 98765.5,
        pendingConfirmationCount: 1,
        sharedWithMeLentBalance: 15420.25,
        sharedWithMeBorrowedBalance: 8320,
        sharedWithMeActiveCount: 3,
      });
    }
    if (url.includes("/api/v1/personal/")) {
      return json(route, []);
    }
    return route.fallback();
  });
}

async function setThemePrimary(page: Page, theme: "light" | "dark", primary: string) {
  await page.evaluate(
    ({ nextTheme, nextPrimary }) => {
      document.documentElement.setAttribute("data-theme", nextTheme);
      document.documentElement.setAttribute("data-primary", nextPrimary);
      document.documentElement.setAttribute("data-accent", nextPrimary);
    },
    { nextTheme: theme, nextPrimary: primary },
  );
}

async function setSurfaceStyle(page: Page, style: "modern-solid" | "ehg-glass") {
  const current = await page.getByTestId("personal-home-page").getAttribute("data-surface-style");
  if (current === style) {
    return;
  }
  await page.getByTestId("personal-home-surface-style").click();
  await page.getByTestId(`personal-home-surface-style-option-${style}`).click();
  await expect(page.getByTestId("personal-home-page")).toHaveAttribute("data-surface-style", style);
  await expect
    .poll(() => page.evaluate((key) => window.localStorage.getItem(key), STORAGE_KEY))
    .toBe(style);
  await expect(page.getByTestId(`personal-home-surface-style-option-${style}`)).toHaveCount(0);
  await page.getByTestId("personal-home-page").click({ position: { x: 8, y: 8 } });
}

test("capture Personal Home surface styles", async ({ page }) => {
  await mockPersonalSession(page);
  await installHomeMocks(page);
  await signInAsPersonal(page);
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 20000 });
  await clientNavigate(page, "/personal");
  await expect(page.getByTestId("personal-home-page")).toBeVisible({ timeout: 15000 });
  await expect(page.getByTestId("personal-home-surface-style")).toBeVisible();

  const shots: Array<
    [number, number, "light" | "dark", string, "modern-solid" | "ehg-glass"]
  > = [
    [1440, 900, "light", "green", "modern-solid"],
    [1440, 900, "light", "green", "ehg-glass"],
    [1440, 900, "dark", "green", "modern-solid"],
    [1440, 900, "dark", "green", "ehg-glass"],
    [390, 844, "light", "blue", "modern-solid"],
    [390, 844, "light", "blue", "ehg-glass"],
    [390, 844, "dark", "violet", "modern-solid"],
    [390, 844, "dark", "violet", "ehg-glass"],
    [1440, 900, "light", "orange", "modern-solid"],
    [1440, 900, "light", "rose", "ehg-glass"],
  ];

  for (const [width, height, theme, primary, style] of shots) {
    await page.setViewportSize({ width, height });
    await setThemePrimary(page, theme, primary);
    await setSurfaceStyle(page, style);
    await expect(page.getByTestId("personal-home-page")).toBeVisible();
    await page.screenshot({
      path: path.join(
        OUT_DIR,
        `home-surface-${style}-${width}-${theme}-${primary}.png`,
      ),
      fullPage: false,
    });
  }
});
