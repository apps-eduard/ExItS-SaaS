import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

test("scaffold page is visible", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "ExItS Platform Admin Web" })).toBeVisible();
  await expect(page.getByText("Design foundation preview")).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "en");
  await expect(page.locator("html")).toHaveAttribute("data-theme", "system");
  await expect(page.locator("html")).toHaveAttribute("data-density", "balanced");
});

test("scaffold page has no serious accessibility violations", async ({ page }) => {
  await page.goto("/");
  const results = await new AxeBuilder({ page }).analyze();
  const serious = results.violations.filter(
    (violation) => violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);
});
