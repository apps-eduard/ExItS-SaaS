import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { assertNoHorizontalOverflow, mockAnonymousSession } from "./helpers";

test.describe("PWA update lifecycle", () => {
  test("update notice is user-triggered, accessible, and does not persist tokens", async ({
    page,
  }) => {
    await mockAnonymousSession(page);
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
    await expect(page.getByTestId("pwa-update-host")).toHaveAttribute("data-ready", "true");
    await expect(page.getByRole("status")).toHaveCount(0);

    await page.evaluate(() => {
      window.dispatchEvent(new Event("plm:pwa-need-refresh"));
    });
    await expect(page.getByRole("status")).toContainText("Update available");
    await expect(page.getByRole("button", { name: "Refresh" })).toBeVisible();
    await assertNoHorizontalOverflow(page);

    const persisted = await page.evaluate(() =>
      JSON.stringify({
        local: { ...window.localStorage },
        session: { ...window.sessionStorage },
      }),
    );
    expect(persisted).not.toMatch(/sessionToken/i);

    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(
      (violation) => violation.impact === "serious" || violation.impact === "critical",
    );
    expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
  });

  test("update notice uses Filipino copy", async ({ page }) => {
    await mockAnonymousSession(page);
    await page.addInitScript(() => {
      window.localStorage.setItem(
        "exits.plm-client.ui-preferences.v1",
        JSON.stringify({ theme: "light", locale: "fil-PH" }),
      );
    });
    await page.goto("/sign-in");
    await expect(page.getByTestId("pwa-update-host")).toHaveAttribute("data-ready", "true");
    await page.evaluate(() => {
      window.dispatchEvent(new Event("plm:pwa-need-refresh"));
    });
    await expect(page.getByRole("status")).toContainText("May update");
    await expect(page.getByRole("button", { name: "I-refresh" })).toBeVisible();
  });
});
