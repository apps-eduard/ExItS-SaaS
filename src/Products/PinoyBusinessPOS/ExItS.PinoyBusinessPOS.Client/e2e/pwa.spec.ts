import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { mockAuthenticatedSession } from "./helpers/session";

async function assertNoHorizontalOverflow(page: import("@playwright/test").Page) {
  const overflow = await page.evaluate(() => {
    const root = document.scrollingElement ?? document.documentElement;
    return root.scrollWidth - root.clientWidth;
  });
  expect(overflow).toBeLessThanOrEqual(1);
}

test.describe("PWA foundation", () => {
  test("serves a valid installable manifest and required icons", async ({ request }) => {
    const response = await request.get("/manifest.webmanifest");
    expect(response.ok()).toBeTruthy();
    const manifest = (await response.json()) as {
      name: string;
      short_name: string;
      start_url: string;
      display: string;
      icons: Array<{ src: string; sizes: string; purpose?: string }>;
    };
    expect(manifest.name).toBe("ExItS Mobile");
    expect(manifest.short_name).toBe("ExItS Mobile");
    expect(manifest.start_url).toBe("/");
    expect(manifest.display).toBe("standalone");
    expect(manifest.icons.some((icon) => icon.sizes === "192x192")).toBe(true);
    expect(manifest.icons.some((icon) => icon.sizes === "512x512")).toBe(true);
    expect(manifest.icons.some((icon) => icon.purpose?.includes("maskable"))).toBe(true);
    for (const icon of manifest.icons) {
      const iconResponse = await request.get(icon.src.startsWith("/") ? icon.src : `/${icon.src}`);
      expect(iconResponse.ok(), icon.src).toBeTruthy();
    }
  });

  test("enables a production service worker that does not cache API data", async ({ request }) => {
    const response = await request.get("/sw.js");
    expect(response.ok()).toBeTruthy();
    const source = await response.text();
    expect(source).toContain("NetworkOnly");
    expect(source).toMatch(/\\\/api\\\//);
    expect(source).toMatch(/platform-api/);
    expect(source).not.toMatch(/BackgroundSyncPlugin|workbox-background-sync/);
    expect(source).not.toMatch(/CacheFirst[\s\S]{0,180}\/api\/|\/api\/[\s\S]{0,180}CacheFirst/);
    expect(source).toMatch(/assets\/index-[A-Za-z0-9_-]+\.(js|css)/);
  });

  test("standalone phone tablet and desktop shell do not overflow", async ({ page }) => {
    await mockAuthenticatedSession(page);
    await page.addInitScript(() => {
      const original = window.matchMedia.bind(window);
      window.matchMedia = ((query: string) => {
        if (query.includes("display-mode: standalone")) {
          return {
            matches: true,
            media: query,
            onchange: null,
            addEventListener: () => undefined,
            removeEventListener: () => undefined,
            addListener: () => undefined,
            removeListener: () => undefined,
            dispatchEvent: () => true,
          } as MediaQueryList;
        }
        return original(query);
      }) as typeof window.matchMedia;
    });

    for (const viewport of [
      { width: 375, height: 812 },
      { width: 768, height: 1024 },
      { width: 1280, height: 800 },
    ] as const) {
      await page.setViewportSize(viewport);
      await page.goto("/");
      await expect(page.getByRole("heading", { name: "ExItS Mobile" })).toBeVisible();
      await expect(page.getByRole("banner")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    }
  });

  test("axe has no serious or critical violations with the PWA shell", async ({ page }) => {
    await mockAuthenticatedSession(page);
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/");
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(
      (violation) => violation.impact === "serious" || violation.impact === "critical",
    );
    expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
  });
});
