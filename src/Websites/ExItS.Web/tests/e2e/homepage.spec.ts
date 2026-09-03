import { test, expect } from "@playwright/test";

test("homepage renders WEB-03 sections without fake metrics", async ({ page }) => {
  await page.goto("/");

  await expect(
    page.getByRole("heading", { name: /one platform/i }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Pinoy Business POS", exact: true }),
  ).toBeVisible();
  await expect(page.getByText("Coming Soon").first()).toBeVisible();
  await expect(page.getByText("In Development").first()).toBeVisible();
  await expect(page.getByRole("heading", { name: /frequently asked questions/i })).toBeVisible();
  await expect(page.locator("body")).not.toContainText("Trusted by");
  await expect(page.locator("body")).not.toContainText("₱");
});

test("homepage drawer still opens from the header", async ({ page }) => {
  await page.goto("/");
  await page.getByRole("button", { name: /open menu/i }).click();
  await expect(page.getByRole("navigation", { name: /site navigation/i })).toBeVisible();
  await page.keyboard.press("Escape");
});

const viewports = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
  { width: 1920, height: 1080 },
];

for (const viewport of viewports) {
  test(`homepage has no horizontal overflow at ${viewport.width}px`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await page.goto("/");
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );
    expect(overflow).toBe(false);
  });
}
