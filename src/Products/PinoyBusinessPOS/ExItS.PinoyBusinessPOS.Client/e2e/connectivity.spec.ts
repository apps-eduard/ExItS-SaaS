import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import {
  assertNoApiOrAuthTrafficInCaches,
  assertNoHorizontalOverflow,
  inspectServiceWorkerCaches,
} from "./helpers";

test.describe("connectivity advisory", () => {
  test("shows an advisory offline notice and recovers without mutation replay", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    const mutating: string[] = [];
    page.on("request", (request) => {
      if (["POST", "PUT", "PATCH", "DELETE"].includes(request.method())) {
        mutating.push(`${request.method()} ${request.url()}`);
      }
    });

    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Pinoy Business POS" })).toBeVisible();
    await expect(page.getByTestId("connectivity-notice")).toHaveCount(0);

    await page.context().setOffline(true);
    await expect(page.getByTestId("connectivity-notice")).toContainText("You're offline");
    await expect(page.getByTestId("connectivity-notice")).toContainText("Reconnect to continue.");
    await expect(page.getByTestId("connectivity-notice")).not.toContainText(/offline pos mode/i);
    await expect(
      page.getByText(/authenticated|workspace authorized|checkout available/i),
    ).toHaveCount(0);
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
    await page.addInitScript(() => {
      window.localStorage.setItem(
        "exits.pos-client.ui-preferences.v1",
        JSON.stringify({ theme: "dark", locale: "fil-PH" }),
      );
    });
    await page.goto("/");
    await page.context().setOffline(true);
    await expect(page.getByTestId("connectivity-notice")).toContainText("Wala kang koneksyon");
    await page.context().setOffline(false);
  });

  test("fits 320, 375, 768, and 1440 without overflow", async ({ page }) => {
    await page.goto("/");
    await page.context().setOffline(true);
    await expect(page.getByTestId("connectivity-notice")).toBeVisible();
    for (const size of [
      { width: 320, height: 568 },
      { width: 375, height: 812 },
      { width: 768, height: 1024 },
      { width: 1440, height: 900 },
    ]) {
      await page.setViewportSize(size);
      await assertNoHorizontalOverflow(page);
    }
    await page.context().setOffline(false);
  });

  test("offline reload keeps a neutral shell and does not cache API URLs", async ({ page }) => {
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Pinoy Business POS" })).toBeVisible();
    const caches = await inspectServiceWorkerCaches(page);
    assertNoApiOrAuthTrafficInCaches(caches.urls);

    await page.context().setOffline(true);
    await page.reload({ waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: "Pinoy Business POS" })).toBeVisible();
    await page.evaluate(() => window.dispatchEvent(new Event("offline")));
    await expect(page.getByText(/authenticated|selling available|checkout available/i)).toHaveCount(
      0,
    );
    await expect(page.getByTestId("connectivity-notice")).toBeVisible();
    const after = await inspectServiceWorkerCaches(page);
    assertNoApiOrAuthTrafficInCaches(after.urls);
    await page.context().setOffline(false);
  });
});
