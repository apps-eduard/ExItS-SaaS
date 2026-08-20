import { mkdirSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import {
  assertNoApiOrAuthTrafficInCaches,
  assertNoHorizontalOverflow,
  inspectServiceWorkerCaches,
  mockAuthenticatedSession,
} from "./helpers";

const screenshotDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../Docs/Reports/impl-pwa-hardening",
);

test.describe("PWA hardening production-preview evidence", () => {
  test.beforeAll(() => {
    mkdirSync(screenshotDir, { recursive: true });
  });

  test("01-02 online workspace then offline fail-closed", async ({ page }) => {
    await mockAuthenticatedSession(page, { startSignedIn: true });
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/");
    await expect(page.getByText(/Workspace is ready/i)).toBeVisible();
    const caches = await inspectServiceWorkerCaches(page);
    assertNoApiOrAuthTrafficInCaches(caches.urls);
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "01-online-workspace-375x812.png"),
      fullPage: true,
    });

    await page.unroute("**/platform-api/api/v1/platform/**").catch(() => undefined);
    await page.route("**/platform-api/**", (route) => route.abort());
    await page.context().setOffline(true);
    await page.reload({ waitUntil: "domcontentloaded" });
    await expect(page.getByText(/Workspace is ready/i)).toHaveCount(0);
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
    await page.evaluate(() => window.dispatchEvent(new Event("offline")));
    await expect(page.getByTestId("connectivity-notice")).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "02-offline-fail-closed-375x812.png"),
      fullPage: true,
    });
  });

  test("03-04 back online with fresh mocked access and update notice", async ({ page }) => {
    await mockAuthenticatedSession(page, { startSignedIn: true });
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/");
    await expect(page.getByText(/Workspace is ready/i)).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "03-back-online-375x812.png"),
      fullPage: true,
    });

    await page.evaluate(() => window.dispatchEvent(new Event("plm:pwa-need-refresh")));
    await expect(page.getByRole("status")).toContainText("Update available");
    await page.screenshot({
      path: path.join(screenshotDir, "04-update-available-375x812.png"),
      fullPage: true,
    });

    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(
      (violation) => violation.impact === "serious" || violation.impact === "critical",
    );
    expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
  });

  test("05 offline desktop and 320 workspace", async ({ page }) => {
    await mockAuthenticatedSession(page, { startSignedIn: true });
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto("/");
    await expect(page.getByText(/Workspace is ready/i)).toBeVisible();
    await page.context().setOffline(true);
    await page.evaluate(() => window.dispatchEvent(new Event("offline")));
    await expect(page.getByTestId("connectivity-notice")).toBeVisible();
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "05-offline-desktop-1440x900.png"),
      fullPage: true,
    });
    await page.context().setOffline(false);
    await page.evaluate(() => window.dispatchEvent(new Event("online")));

    await page.setViewportSize({ width: 320, height: 800 });
    await page.goto("/");
    await expect(page.getByText(/Workspace is ready/i)).toBeVisible();
    await assertNoHorizontalOverflow(page);
  });
});
