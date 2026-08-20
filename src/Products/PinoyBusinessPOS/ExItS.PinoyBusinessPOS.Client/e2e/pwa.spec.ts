import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import {
  assertNoApiOrAuthTrafficInCaches,
  assertNoHorizontalOverflow,
  inspectServiceWorkerCaches,
  waitForServiceWorker,
} from "./helpers";

test.describe("PWA static shell", () => {
  test("serves a valid installable manifest and required icons", async ({ request }) => {
    const response = await request.get("/manifest.webmanifest");
    expect(response.ok()).toBeTruthy();
    const manifest = (await response.json()) as {
      name: string;
      short_name: string;
      start_url: string;
      display: string;
      theme_color: string;
      icons: Array<{ src: string; sizes: string; purpose?: string }>;
    };
    expect(manifest.name).toBe("Pinoy Business POS");
    expect(manifest.short_name).toBe("ExItS POS");
    expect(manifest.start_url).toBe("/");
    expect(manifest.display).toBe("standalone");
    expect(manifest.theme_color).toBe("#166534");
    expect(manifest.icons.some((icon) => icon.sizes === "192x192")).toBe(true);
    expect(manifest.icons.some((icon) => icon.sizes === "512x512")).toBe(true);
    expect(manifest.icons.some((icon) => icon.purpose?.includes("maskable"))).toBe(true);
    for (const icon of manifest.icons) {
      const iconResponse = await request.get(icon.src.startsWith("/") ? icon.src : `/${icon.src}`);
      expect(iconResponse.ok(), icon.src).toBeTruthy();
    }
  });

  test("production service worker is NetworkOnly for APIs and has no Background Sync", async ({
    request,
  }) => {
    const response = await request.get("/sw.js");
    expect(response.ok()).toBeTruthy();
    const source = await response.text();
    expect(source).toContain("NetworkOnly");
    expect(source).toMatch(/\/api\//);
    const apiHandler = source.match(
      /startsWith\("\/api\/"\)[\s\S]{0,180}?new e\.(NetworkOnly|CacheFirst|StaleWhileRevalidate)/,
    );
    expect(apiHandler?.[1]).toBe("NetworkOnly");
    expect(source).not.toMatch(/BackgroundSyncPlugin|workbox-background-sync/);
    expect(source).toMatch(/auth[\s\S]{0,160}NetworkOnly|\/\(auth\|session\)\//);
    expect(source).toMatch(/assets\/index-[A-Za-z0-9_-]+\.(js|css)/);
    expect(source).not.toMatch(/platform-api/);
  });

  test("runtime Cache Storage holds the shell and never caches API or auth traffic", async ({
    page,
  }) => {
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Pinoy Business POS" })).toBeVisible();
    const caches = await inspectServiceWorkerCaches(page);
    expect(caches.urls.length).toBeGreaterThan(0);
    assertNoApiOrAuthTrafficInCaches(caches.urls);
    expect(caches.indexedDbNames.join(" ")).not.toMatch(
      /sale|payment|customer|credit|sessionToken|LocalStore/i,
    );
  });

  test("registers a service worker and keeps the foundation shell", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Pinoy Business POS" })).toBeVisible();
    await waitForServiceWorker(page);
    const registered = await page.evaluate(async () => {
      const registration = await navigator.serviceWorker.ready;
      return Boolean(registration.active || registration.waiting || registration.installing);
    });
    expect(registered).toBe(true);
    const storageKeys = await page.evaluate(() => Object.keys(window.localStorage));
    expect(storageKeys.every((key) => key.startsWith("exits.pos-client.ui-preferences"))).toBe(
      true,
    );
    await assertNoHorizontalOverflow(page);
  });

  test("axe has no serious or critical violations with the PWA shell", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Pinoy Business POS" })).toBeVisible();
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(
      (violation) => violation.impact === "serious" || violation.impact === "critical",
    );
    expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
  });
});
