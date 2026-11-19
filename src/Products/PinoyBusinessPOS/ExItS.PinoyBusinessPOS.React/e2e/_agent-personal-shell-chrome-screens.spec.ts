/**
 * One-off visual capture for Personal shell chrome / atmosphere.
 * Not part of the permanent e2e suite.
 */
import { expect, test, type Page } from "@playwright/test";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { clientNavigate, mockPersonalSession, signInAsPersonal } from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const OUT_DIR = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "_agent-screenshots");

function json(route: { fulfill: (r: object) => Promise<void> }, body: unknown, status = 200) {
  return route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

async function installHomeMocks(page: Page) {
  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/api/v1/personal/dashboard") && method === "GET") {
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

async function capture(
  page: Page,
  width: number,
  height: number,
  theme: "light" | "dark",
  primary: string,
) {
  await page.setViewportSize({ width, height });
  await setThemePrimary(page, theme, primary);
  await expect(page.getByTestId("personal-shell-atmosphere")).toBeVisible();
  await expect(page.getByTestId("personal-top-bar")).toBeVisible();
  const shotPath = path.join(OUT_DIR, `personal-shell-${width}-${theme}-${primary}.png`);
  await page.screenshot({ path: shotPath, fullPage: false });
  return shotPath;
}

test("capture Personal shell chrome at desktop/tablet/mobile", async ({ page }) => {
  await mockPersonalSession(page);
  await installHomeMocks(page);
  await signInAsPersonal(page);
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 20000 });
  await clientNavigate(page, "/personal");
  await expect(page.getByTestId("personal-home-page")).toBeVisible({ timeout: 15000 });

  const shots = [
    await capture(page, 1440, 900, "light", "green"),
    await capture(page, 1440, 900, "dark", "green"),
    await capture(page, 768, 1024, "light", "blue"),
    await capture(page, 390, 844, "light", "violet"),
    await capture(page, 390, 844, "dark", "orange"),
    await capture(page, 390, 844, "light", "rose"),
  ];

  for (const shot of shots) {
    expect(shot.length).toBeGreaterThan(10);
  }
});
