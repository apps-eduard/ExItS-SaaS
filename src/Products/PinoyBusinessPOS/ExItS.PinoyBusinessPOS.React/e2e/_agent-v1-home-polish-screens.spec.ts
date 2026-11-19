/**
 * One-off visual capture for Personal Home V1 surface polish.
 * Not part of the permanent e2e suite.
 */
import { expect, test, type Page } from "@playwright/test";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { clientNavigate, mockPersonalSession, signInAsPersonal } from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const OUT_DIR = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "_agent-screenshots");
const PHASE = process.env.HOME_POLISH_PHASE ?? "after";

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

test(`capture Personal Home polish ${PHASE}`, async ({ page }) => {
  await mockPersonalSession(page);
  await installHomeMocks(page);
  await signInAsPersonal(page);
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 20000 });
  await clientNavigate(page, "/personal");
  await expect(page.getByTestId("personal-home-page")).toBeVisible({ timeout: 15000 });

  const shots: Array<[number, number, "light" | "dark", string]> = [
    [1440, 900, "light", "green"],
    [1440, 900, "dark", "green"],
    [390, 844, "light", "blue"],
    [390, 844, "dark", "violet"],
    [1440, 900, "light", "orange"],
    [1440, 900, "light", "rose"],
  ];

  for (const [width, height, theme, primary] of shots) {
    await page.setViewportSize({ width, height });
    await setThemePrimary(page, theme, primary);
    await expect(page.getByTestId("personal-home-page")).toBeVisible();
    await page.screenshot({
      path: path.join(OUT_DIR, `v1-home-polish-${PHASE}-${width}-${theme}-${primary}.png`),
      fullPage: false,
    });
  }
});
