import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { assertNoHorizontalOverflow, mockAnonymousSession } from "./helpers";

const forbidden = ["Preview", "Foundation", "coming soon", "1,250.00", "Online", "Synced"];

async function assertProductCopy(page: import("@playwright/test").Page) {
  const text = (await page.locator("body").innerText()).toLowerCase();
  for (const phrase of forbidden) {
    expect(text, `must not contain “${phrase}”`).not.toContain(phrase.toLowerCase());
  }
}

test.describe("PLM client shell", () => {
  test("phone 320 and 375 do not overflow", async ({ page }) => {
    await mockAnonymousSession(page);
    for (const viewport of [
      { width: 320, height: 568 },
      { width: 375, height: 812 },
    ] as const) {
      await page.setViewportSize(viewport);
      await page.goto("/");
      await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
      await expect(page.getByText("Pinoy Loan Manager").first()).toBeVisible();
      await assertProductCopy(page);
      await assertNoHorizontalOverflow(page);
    }
  });

  test("tablet and desktop remain usable", async ({ page }) => {
    await mockAnonymousSession(page);
    for (const viewport of [
      { width: 768, height: 1024 },
      { width: 1280, height: 800 },
    ] as const) {
      await page.setViewportSize(viewport);
      await page.goto("/sign-in");
      await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
      await expect(page.getByRole("radio", { name: "English" })).toBeVisible();
      await assertNoHorizontalOverflow(page);
    }
  });

  test("Filipino Light and Dark remain usable", async ({ page }) => {
    await mockAnonymousSession(page);
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/sign-in");
    await page.getByRole("radio", { name: "Filipino" }).click();
    await expect(page.getByRole("heading", { name: "Mag-sign in" })).toBeVisible();
    await page.getByRole("radio", { name: "Dark" }).click();
    await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
    await page.getByRole("radio", { name: "Light" }).click();
    await expect(page.locator("html")).toHaveAttribute("data-theme", "light");
  });

  test("axe has no serious or critical violations", async ({ page }) => {
    await mockAnonymousSession(page);
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/sign-in");
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(
      (violation) => violation.impact === "serious" || violation.impact === "critical",
    );
    expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
  });
});
