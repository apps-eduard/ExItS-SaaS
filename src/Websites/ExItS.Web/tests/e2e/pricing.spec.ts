import { test, expect } from "@playwright/test";

test("pricing page renders without fake peso prices or invented plan packages", async ({
  page,
}) => {
  await page.goto("/pricing");

  await expect(
    page.getByRole("heading", {
      name: /simple pricing for every stage of your business/i,
    }),
  ).toBeVisible();
  await expect(page.getByText("Recommended")).toBeVisible();
  await expect(page.getByText("Pricing TBD").first()).toBeVisible();
  await expect(page.getByRole("heading", { name: /confirmed capabilities/i })).toBeVisible();
  await expect(page.getByRole("heading", { name: /pricing faq/i })).toBeVisible();
  await expect(page.locator("body")).not.toContainText("₱");
  await expect(page.locator("body")).not.toContainText("Starter");
  await expect(page.locator("body")).not.toContainText("Growth");
  await expect(page.locator("body")).not.toContainText("/ month");
  await expect(page.locator("body")).not.toContainText("Trusted by");
});

test("pricing recommended card appears first on mobile", async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto("/pricing");

  const cards = page.locator("article");
  await expect(cards.first()).toContainText("Recommended");
  await expect(cards.first()).toContainText("Growing business");
});

const viewports = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
  { width: 1920, height: 1080 },
];

for (const viewport of viewports) {
  test(`pricing page has no horizontal overflow at ${viewport.width}px`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await page.goto("/pricing");
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );
    expect(overflow).toBe(false);
  });
}
