import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { assertNoHorizontalOverflow } from "./helpers";

const viewports = [
  { name: "320", width: 320, height: 568 },
  { name: "375", width: 375, height: 812 },
  { name: "768", width: 768, height: 1024 },
  { name: "1440", width: 1440, height: 900 },
] as const;

test.describe("POS React foundation", () => {
  test("foundation loads in English by default", async ({ page }) => {
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Pinoy Business POS" })).toBeVisible();
    await expect(page.getByText("React client foundation")).toBeVisible();
    await expect(page.getByText(/Static PWA shell is online-first/i)).toBeVisible();
    await expect(page.locator("html")).toHaveAttribute("lang", "en");
    await expect(page.locator("html")).toHaveAttribute("data-theme", "system");
  });

  for (const viewport of viewports) {
    test(`${viewport.name} has no horizontal overflow`, async ({ page }) => {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await page.goto("/");
      await expect(page.getByRole("heading", { name: "Pinoy Business POS" })).toBeVisible();
      await assertNoHorizontalOverflow(page);
    });
  }

  test("theme switch is global", async ({ page }) => {
    await page.goto("/");
    await page.getByRole("radio", { name: /Dark/ }).click();
    await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
    await page.getByRole("radio", { name: /Light/ }).click();
    await expect(page.locator("html")).toHaveAttribute("data-theme", "light");
    await page.getByRole("radio", { name: /System/ }).click();
    await expect(page.locator("html")).toHaveAttribute("data-theme", "system");
  });

  test("locale switch proves English and Filipino", async ({ page }) => {
    await page.goto("/");
    await page.getByRole("radio", { name: /Filipino/ }).click();
    await expect(page.locator("html")).toHaveAttribute("lang", "fil-PH");
    await expect(page.getByText("Pundasyon ng React client")).toBeVisible();
    await page.getByRole("radio", { name: /English/ }).click();
    await expect(page.getByText("React client foundation")).toBeVisible();
  });

  test("unknown routes render 404", async ({ page }) => {
    await page.goto("/this-route-does-not-exist");
    await expect(page.getByRole("heading", { name: "Page not found" })).toBeVisible();
    await page.getByRole("link", { name: "Back to foundation" }).click();
    await expect(page.getByRole("heading", { name: "Pinoy Business POS" })).toBeVisible();
  });

  test("axe has no serious or critical violations on the foundation shell", async ({ page }) => {
    await page.goto("/");
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(
      (violation) => violation.impact === "serious" || violation.impact === "critical",
    );
    expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
  });
});
