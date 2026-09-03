import { test, expect } from "@playwright/test";

test("POS page renders flagship sections with confirmed claims only", async ({ page }) => {
  await page.goto("/pos");

  await expect(page.getByRole("heading", { name: "Pinoy Business POS", exact: true })).toBeVisible();
  await expect(page.getByRole("navigation", { name: /breadcrumb/i })).toContainText("Products");
  await expect(page.getByRole("navigation", { name: /breadcrumb/i })).toContainText(
    "Pinoy Business POS",
  );
  await expect(
    page.getByRole("heading", { name: /sell confidently — online or offline/i }),
  ).toBeVisible();
  await expect(page.getByRole("heading", { name: /what is an area\?/i })).toBeVisible();
  await expect(page.locator("body")).not.toContainText("BIR");
  await expect(page.locator("body")).not.toContainText("₱");
  await expect(page.locator("body")).not.toContainText("Trusted by");
  await expect(page.locator("body")).not.toContainText("mobile app available");
});

test("POS page Area wording does not claim inventory ownership", async ({ page }) => {
  await page.goto("/pos");
  await expect(page.getByText(/Areas do not own inventory/i)).toBeVisible();
  await expect(
    page.getByText(/Stock, pricing, and staff stay scoped to each branch/i),
  ).toBeVisible();
});

const viewports = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
  { width: 1920, height: 1080 },
];

for (const viewport of viewports) {
  test(`POS page has no horizontal overflow at ${viewport.width}px`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await page.goto("/pos");
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );
    expect(overflow).toBe(false);
  });
}
