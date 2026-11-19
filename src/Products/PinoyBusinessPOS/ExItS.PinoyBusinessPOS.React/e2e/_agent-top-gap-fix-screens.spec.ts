/**
 * Visual proof: Personal shell flush to viewport top (Home + Utang).
 * One-off agent capture — not permanent suite.
 * Asserts DOM rects AND PNG pixel row 0 (prior agents trusted rects alone).
 */
import { expect, test, type Page } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { PNG } from "pngjs";
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

async function installPersonalMocks(page: Page) {
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

async function assertFlushTop(page: Page) {
  const tops = await page.evaluate(() => {
    const shell = document.querySelector('[data-testid="personal-shell"]') as HTMLElement | null;
    const topbar = document.querySelector('[data-testid="personal-top-bar"]') as HTMLElement | null;
    const crystal = document.querySelector(
      '[data-testid="personal-crystal-screen"]',
    ) as HTMLElement | null;
    const atmosphere = document.querySelector(
      '[data-testid="personal-shell-atmosphere"]',
    ) as HTMLElement | null;
    const sidebar = document.querySelector('[data-testid="personal-sidebar"]') as HTMLElement | null;
    const before = sidebar ? getComputedStyle(sidebar, "::before") : null;
    return {
      shellTop: shell?.getBoundingClientRect().top ?? 999,
      topbarTop: topbar?.getBoundingClientRect().top ?? 999,
      crystalTop: crystal?.getBoundingClientRect().top ?? 999,
      atmospherePosition: atmosphere ? getComputedStyle(atmosphere).position : null,
      atmosphereHeight: atmosphere ? getComputedStyle(atmosphere).height : null,
      sidebarRadius: sidebar ? getComputedStyle(sidebar).borderRadius : null,
      shellPaddingTop: shell ? getComputedStyle(shell).paddingTop : null,
      sidebarBeforeContent: before?.content ?? null,
      sidebarBeforeDisplay: before?.display ?? null,
    };
  });

  expect(tops.shellTop).toBeLessThanOrEqual(2);
  expect(tops.topbarTop).toBeLessThanOrEqual(2);
  expect(tops.crystalTop).toBeLessThanOrEqual(2);
  expect(tops.atmospherePosition).toBe("absolute");
  expect(tops.shellPaddingTop).toBe("0px");
  expect(tops.sidebarRadius === "0px" || tops.sidebarRadius === "").toBeTruthy();
  // Soft Glass side lips must be off — they read as a floating top gap.
  expect(tops.sidebarBeforeContent === "none" || tops.sidebarBeforeDisplay === "none").toBeTruthy();
  return tops;
}

/** Row 0 must contain chrome (sidebar / atmosphere) — not a full-width empty body band. */
function assertPngRow0HasChrome(filePath: string, widthHint: number, theme: "light" | "dark") {
  const png = PNG.sync.read(fs.readFileSync(filePath));
  expect(png.width).toBe(widthHint);
  const sample = (x: number, y: number) => {
    const i = (png.width * y + x) * 4;
    return [png.data[i], png.data[i + 1], png.data[i + 2]] as const;
  };
  const left = sample(Math.min(24, png.width - 1), 0);
  const mid = sample(Math.floor(png.width * 0.5), 0);
  const farLeft = sample(0, 0);

  if (widthHint >= 1024) {
    if (theme === "light") {
      // Desktop light: sidebar near-white at row 0 (not Soft Glass teal / body gray).
      expect(left[0]).toBeGreaterThan(240);
      expect(left[1]).toBeGreaterThan(240);
      expect(left[2]).toBeGreaterThan(240);
      // x=0 must also be chrome after Soft Glass lips removed (was 244,246,248 canvas lip).
      expect(farLeft[0]).toBeGreaterThan(240);
    } else {
      // Desktop dark: sidebar slate chrome at row 0 (not empty black letterbox).
      expect(left[0] + left[1] + left[2]).toBeGreaterThan(40);
      expect(left[0]).toBeLessThan(80);
    }
  }
  // Desktop content column: row 0 should include atmosphere wash (not empty letterbox).
  // Mobile mid can be near-canvas #eef1f4 — only assert no uniform pure letterbox band.
  if (widthHint >= 1024 && theme === "light") {
    // Mid should differ from sidebar white (atmosphere / wash present).
    const delta =
      Math.abs(mid[0] - left[0]) + Math.abs(mid[1] - left[1]) + Math.abs(mid[2] - left[2]);
    expect(delta).toBeGreaterThan(20);
  }
  // Any viewport: row 0 must not be pure black letterbox.
  expect(mid[0] + mid[1] + mid[2]).toBeGreaterThan(30);
}

test("capture Personal Home + Utang flush-top screens", async ({ page }) => {
  await mockPersonalSession(page);
  await installPersonalMocks(page);
  await signInAsPersonal(page);
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 20000 });

  const shots: Array<[string, number, number, "light" | "dark", string]> = [
    ["/personal", 1440, 900, "light", "green"],
    ["/personal", 1440, 900, "dark", "green"],
    ["/personal", 390, 844, "light", "blue"],
    ["/personal", 390, 844, "dark", "violet"],
    ["/personal/utang", 1440, 900, "light", "green"],
    ["/personal/utang", 390, 844, "dark", "violet"],
  ];

  for (const [route, width, height, theme, primary] of shots) {
    await page.setViewportSize({ width, height });
    await page.evaluate(
      ({ nextTheme, nextPrimary }) => {
        document.documentElement.setAttribute("data-theme", nextTheme);
        document.documentElement.setAttribute("data-primary", nextPrimary);
        document.documentElement.setAttribute("data-accent", nextPrimary);
      },
      { nextTheme: theme, nextPrimary: primary },
    );
    await clientNavigate(page, route);
    if (route === "/personal") {
      await expect(page.getByTestId("personal-home-page")).toBeVisible({ timeout: 15000 });
    } else {
      await expect(page.getByTestId("personal-shell")).toBeVisible();
    }

    const tops = await assertFlushTop(page);
    console.log(`FLUSH_${route}_${width}_${theme}`, JSON.stringify(tops));

    const label = route.includes("utang") ? "utang" : "home";
    const shotPath = path.join(OUT_DIR, `v1-${label}-top-gap-fix-${width}-${theme}-${primary}.png`);
    await page.screenshot({ path: shotPath, fullPage: false });
    assertPngRow0HasChrome(shotPath, width, theme);

    if (width === 1440 && theme === "light" && label === "home") {
      // Tight crop for vision inspection of row 0.
      const src = PNG.sync.read(fs.readFileSync(shotPath));
      const cropH = 48;
      const out = new PNG({ width: src.width, height: cropH });
      for (let y = 0; y < cropH; y++) {
        for (let x = 0; x < src.width; x++) {
          const i = (src.width * y + x) << 2;
          const j = (src.width * y + x) << 2;
          out.data[j] = src.data[i];
          out.data[j + 1] = src.data[i + 1];
          out.data[j + 2] = src.data[i + 2];
          out.data[j + 3] = src.data[i + 3];
        }
      }
      const cropPath = path.join(OUT_DIR, "v1-home-top-gap-fix-crop48.png");
      fs.writeFileSync(cropPath, PNG.sync.write(out));
    }
  }
});
