import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import {
  assertNoApiOrAuthTrafficInCaches,
  assertNoHorizontalOverflow,
  inspectServiceWorkerCaches,
  mockAnonymousSession,
  mockAuthenticatedSession,
} from "./helpers";

test.describe("connectivity fail-closed", () => {
  test("shows an advisory offline notice and recovers without mutation replay", async ({
    page,
  }) => {
    await mockAnonymousSession(page);
    await page.setViewportSize({ width: 375, height: 812 });
    const mutating: string[] = [];
    page.on("request", (request) => {
      if (["POST", "PUT", "PATCH", "DELETE"].includes(request.method())) {
        mutating.push(`${request.method()} ${request.url()}`);
      }
    });

    await page.goto("/sign-in");
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
    await expect(page.getByTestId("connectivity-notice")).toHaveCount(0);

    await page.context().setOffline(true);
    await expect(page.getByTestId("connectivity-notice")).toContainText("You're offline");
    await expect(page.getByTestId("connectivity-notice")).toContainText("Reconnect to continue.");
    await expect(page.getByTestId("connectivity-notice")).not.toContainText(/offline mode/i);
    await assertNoHorizontalOverflow(page);

    mutating.length = 0;
    await page.context().setOffline(false);
    await expect(page.getByTestId("connectivity-notice")).toHaveCount(0);
    await expect.poll(() => mutating.length).toBe(0);

    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(
      (violation) => violation.impact === "serious" || violation.impact === "critical",
    );
    expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
  });

  test("offline notice uses Filipino copy", async ({ page }) => {
    await mockAnonymousSession(page);
    await page.addInitScript(() => {
      window.localStorage.setItem(
        "exits.plm-client.ui-preferences.v1",
        JSON.stringify({ theme: "dark", locale: "fil-PH" }),
      );
    });
    await page.goto("/sign-in");
    await page.context().setOffline(true);
    await expect(page.getByTestId("connectivity-notice")).toContainText("Wala kang koneksyon");
    await page.context().setOffline(false);
  });

  test("fits 320, tablet, and desktop without overflow", async ({ page }) => {
    await mockAnonymousSession(page);
    await page.goto("/sign-in");
    await page.context().setOffline(true);
    await expect(page.getByTestId("connectivity-notice")).toBeVisible();
    for (const size of [
      { width: 320, height: 800 },
      { width: 768, height: 1024 },
      { width: 1440, height: 900 },
    ]) {
      await page.setViewportSize(size);
      await assertNoHorizontalOverflow(page);
    }
    await page.context().setOffline(false);
  });

  test("offline reload does not restore a stale allowed workspace from cache", async ({ page }) => {
    await mockAuthenticatedSession(page, { startSignedIn: true });
    await page.goto("/");
    await expect(page.getByText(/Workspace is ready/i)).toBeVisible();
    const caches = await inspectServiceWorkerCaches(page);
    assertNoApiOrAuthTrafficInCaches(caches.urls);
    expect(caches.indexedDbNames.join(",")).not.toMatch(/loan|payment|borrower|collection/i);

    await page.unroute("**/platform-api/api/v1/platform/**").catch(() => undefined);
    await page.route("**/platform-api/**", (route) => route.abort());
    await page.context().setOffline(true);
    await page.reload({ waitUntil: "domcontentloaded" });

    await expect(page.getByText(/Workspace is ready/i)).toHaveCount(0);
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
    await page.context().setOffline(true);
    await page.evaluate(() => window.dispatchEvent(new Event("offline")));
    await expect(page.getByTestId("connectivity-notice")).toBeVisible();
    await expect(page.getByText(/allowed=true/i)).toHaveCount(0);
    const after = await inspectServiceWorkerCaches(page);
    assertNoApiOrAuthTrafficInCaches(after.urls);
  });
});
