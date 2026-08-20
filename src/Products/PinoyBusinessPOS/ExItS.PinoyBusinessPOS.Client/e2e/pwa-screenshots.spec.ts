import { mkdirSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";
import {
  assertNoHorizontalOverflow,
  inspectServiceWorkerCaches,
  waitForServiceWorker,
} from "./helpers";

const screenshotDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../../../../docs/Mobile-React/Reports/impl-pos-react-02-pwa-static-shell",
);

test.describe("PWA static shell evidence", () => {
  test.beforeAll(() => {
    mkdirSync(screenshotDir, { recursive: true });
  });

  test("01 online shell 375 and 03 update available", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Pinoy Business POS" })).toBeVisible();
    await waitForServiceWorker(page);
    await inspectServiceWorkerCaches(page);
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "01-online-shell-375x812.png"),
      fullPage: true,
    });

    await page.evaluate(() => window.dispatchEvent(new Event("exits-pos:pwa-need-refresh")));
    await expect(page.getByRole("status")).toContainText("Update available");
    await page.screenshot({
      path: path.join(screenshotDir, "03-update-available-375x812.png"),
      fullPage: true,
    });
  });

  test("02 offline shell 375", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Pinoy Business POS" })).toBeVisible();
    await waitForServiceWorker(page);
    await page.context().setOffline(true);
    await expect(page.getByTestId("connectivity-notice")).toBeVisible();
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "02-offline-shell-375x812.png"),
      fullPage: true,
    });
  });

  test("04-05 online and offline desktop 1440", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Pinoy Business POS" })).toBeVisible();
    await waitForServiceWorker(page);
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "04-online-shell-1440x900.png"),
      fullPage: true,
    });

    await page.context().setOffline(true);
    await expect(page.getByTestId("connectivity-notice")).toBeVisible();
    await assertNoHorizontalOverflow(page);
    await page.screenshot({
      path: path.join(screenshotDir, "05-offline-shell-1440x900.png"),
      fullPage: true,
    });
  });
});
