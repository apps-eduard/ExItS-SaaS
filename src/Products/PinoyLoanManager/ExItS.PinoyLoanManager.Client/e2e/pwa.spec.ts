import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { assertNoHorizontalOverflow, mockAnonymousSession } from "./helpers";

test.describe("PWA foundation", () => {
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
    expect(manifest.name).toBe("Pinoy Loan Manager");
    expect(manifest.short_name).toBe("PinoyLoan");
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
    expect(source).toMatch(/platform-api/);
    expect(source).not.toMatch(/BackgroundSyncPlugin|workbox-background-sync/);
    expect(source).toMatch(/startsWith\("\/api\/"\)[\s\S]{0,200}NetworkOnly/);
    expect(source).toMatch(/platform-api[\s\S]{0,200}NetworkOnly/);
    expect(source).not.toMatch(
      /startsWith\("\/api\/"\)[\s\S]{0,160}(?:CacheFirst|StaleWhileRevalidate)/,
    );
    expect(source).toMatch(/assets\/index-[A-Za-z0-9_-]+\.(js|css)/);
  });

  test("SPA preview fallback and refresh of / keep the product shell", async ({
    page,
    request,
  }) => {
    const response = await request.get("/not-a-route");
    expect(response.ok()).toBeTruthy();
    expect(await response.text()).toContain("Pinoy Loan Manager");
    await mockAnonymousSession(page);
    await page.goto("/");
    await page.reload();
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
  });

  test("registers a service worker and keeps the product shell", async ({ page }) => {
    await mockAnonymousSession(page);
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
    await expect(page.getByRole("status")).toHaveCount(0);
    const registered = await page.evaluate(async () => {
      const registration = await navigator.serviceWorker.ready;
      return Boolean(registration.active || registration.waiting || registration.installing);
    });
    expect(registered).toBe(true);
    const storageKeys = await page.evaluate(() => Object.keys(window.localStorage));
    expect(storageKeys.every((key) => key.startsWith("exits.plm-client.ui-preferences"))).toBe(
      true,
    );
    await assertNoHorizontalOverflow(page);
  });

  test("axe has no serious or critical violations with the PWA shell", async ({ page }) => {
    await mockAnonymousSession(page);
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(
      (violation) => violation.impact === "serious" || violation.impact === "critical",
    );
    expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
  });
});
