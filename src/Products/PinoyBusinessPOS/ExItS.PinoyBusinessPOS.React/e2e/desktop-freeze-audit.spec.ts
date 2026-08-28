import { expect, test } from "@playwright/test";
import { mockBoundManagerSession, signInAndBindManager } from "./mock-bound-session";
import { mockPosCatalogApi } from "./mock-pos-catalog-route";

/**
 * POS-REACT-DESKTOP-FREEZE-AUDIT-01
 * Stress navigation at desktop vs phone and assert click latency stays bounded
 * while DOM node count stabilizes (no continuous leak).
 */
async function measureNavRound(page: import("@playwright/test").Page, paths: string[]) {
  const clickDurations: number[] = [];
  for (const path of paths) {
    const started = Date.now();
    await page.evaluate((next) => {
      window.history.pushState({}, "", next);
      window.dispatchEvent(new PopStateEvent("popstate"));
    }, path);
    await page.waitForTimeout(50);
    clickDurations.push(Date.now() - started);
  }
  const nodes = await page.evaluate(() => document.getElementsByTagName("*").length);
  return { clickDurations, nodes };
}

const NAV_CYCLE = [
  "/role/manager",
  "/sell",
  "/inventory",
  "/catalog",
  "/customers",
  "/orders",
  "/purchasing",
  "/more",
];

test.describe("Desktop freeze audit stress", () => {
  test.use({ serviceWorkers: "block" });

  test.beforeEach(async ({ page }) => {
    await mockBoundManagerSession(page);
    await mockPosCatalogApi(page);
    await signInAndBindManager(page);
  });

  test("desktop 1440: 40 navigations stay responsive and DOM stabilizes", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await expect(page.getByTestId("org-bottom-nav")).toBeVisible();

    const before = await page.evaluate(() => document.getElementsByTagName("*").length);
    const rounds: number[] = [];
    let lastNodes = before;

    for (let i = 0; i < 5; i++) {
      const { clickDurations, nodes } = await measureNavRound(page, NAV_CYCLE);
      rounds.push(...clickDurations);
      // Allow modest churn; fail on continuous growth > 25% per full cycle after warm-up.
      if (i >= 2) {
        expect(nodes).toBeLessThan(lastNodes * 1.25 + 200);
      }
      lastNodes = nodes;
    }

    const p95 = [...rounds].sort((a, b) => a - b)[Math.floor(rounds.length * 0.95)] ?? 0;
    // Client-side pushState + popstate should stay snappy even under stress.
    expect(p95).toBeLessThan(1500);

    const after = await page.evaluate(() => document.getElementsByTagName("*").length);
    expect(after).toBeLessThan(before * 2 + 500);

    // Shell must still accept clicks even if a page query failed (no blocking overlay).
    await page.getByTestId("org-nav-home").click({ force: false });
    await expect(page.getByTestId("org-bottom-nav")).toBeVisible();
    await expect(page.getByTestId("client-error-overlay")).toHaveCount(0);
    await expect(page.getByTestId("workspace-transition-overlay")).toHaveCount(0);
  });

  test("phone 390: same navigation sequence remains responsive", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await expect(page.getByTestId("org-bottom-nav")).toBeVisible();

    const before = await page.evaluate(() => document.getElementsByTagName("*").length);
    const rounds: number[] = [];

    for (let i = 0; i < 5; i++) {
      const { clickDurations } = await measureNavRound(page, NAV_CYCLE);
      rounds.push(...clickDurations);
    }

    const p95 = [...rounds].sort((a, b) => a - b)[Math.floor(rounds.length * 0.95)] ?? 0;
    expect(p95).toBeLessThan(1500);

    const after = await page.evaluate(() => document.getElementsByTagName("*").length);
    expect(after).toBeLessThan(before * 2 + 500);
    await page.getByTestId("org-nav-home").click();
    await expect(page.getByTestId("client-error-overlay")).toHaveCount(0);
  });
});
