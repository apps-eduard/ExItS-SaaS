/**
 * Visual proof: Personal Home layout fix — no oversized black wave slab.
 * One-off agent capture — not permanent suite.
 */
import { expect, test, type Page } from "@playwright/test";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { clientNavigate, mockPersonalSession, signInAsPersonal } from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const OUT_DIR = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "_agent-screenshots");

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

test("capture Personal Home layout fix screens", async ({ page }) => {
  await mockPersonalSession(page);
  await installHomeMocks(page);
  await signInAsPersonal(page);
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 20000 });
  await clientNavigate(page, "/personal");
  await expect(page.getByTestId("personal-home-page")).toBeVisible({ timeout: 15000 });

  const shots: Array<[number, number, "light" | "dark", string]> = [
    [1440, 900, "light", "green"],
    [1440, 900, "dark", "green"],
    [768, 1024, "light", "blue"],
    [768, 1024, "dark", "violet"],
    [390, 844, "light", "blue"],
    [390, 844, "dark", "violet"],
  ];

  for (const [width, height, theme, primary] of shots) {
    await page.setViewportSize({ width, height });
    await setThemePrimary(page, theme, primary);
    await expect(page.getByTestId("personal-home-page")).toBeVisible();

    const metrics = await page.evaluate(() => {
      const waves = document.querySelector(".personal-shell-atmosphere__waves") as SVGElement | null;
      const atmosphere = document.querySelector(
        "[data-testid='personal-shell-atmosphere']",
      ) as HTMLElement | null;
      const pageEl = document.querySelector("[data-testid='personal-home-page']") as HTMLElement | null;
      const paths = Array.from(document.querySelectorAll(".personal-shell-atmosphere__wave")).map(
        (el) => {
          const r = (el as SVGGraphicsElement).getBoundingClientRect();
          return { h: r.height, y: r.y, d: el.getAttribute("d") ?? "" };
        },
      );
      return {
        atmospherePosition: atmosphere ? getComputedStyle(atmosphere).position : null,
        wavesHeight: waves ? getComputedStyle(waves).height : null,
        wavesFill: waves ? getComputedStyle(waves).fill : null,
        pageY: pageEl?.getBoundingClientRect().y ?? null,
        pathHeights: paths.map((p) => Math.round(p.h)),
        closesToFloor: paths.some((p) => /V\s*420|V420/i.test(p.d)),
      };
    });

    expect(metrics.atmospherePosition).toBe("absolute");
    expect(metrics.closesToFloor).toBe(false);
    expect(metrics.pageY).not.toBeNull();
    expect(metrics.pageY!).toBeLessThan(120);
    // Ribbons stay compact — no 400px+ black slabs.
    for (const h of metrics.pathHeights) {
      expect(h).toBeLessThan(160);
    }

    await page.screenshot({
      path: path.join(OUT_DIR, `v1-home-layout-fix-${width}-${theme}-${primary}.png`),
      fullPage: false,
    });
  }
});
