import { test, expect } from "@playwright/test";

test("products page shows truthful readiness for all products", async ({ page }) => {
  await page.goto("/products");

  await expect(page.getByRole("heading", { name: "Our Products" })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Pinoy Business POS", exact: true }),
  ).toBeVisible();
  await expect(page.getByText("Available").first()).toBeVisible();
  await expect(page.getByText("Coming Soon").first()).toBeVisible();
  await expect(page.getByText("In Development").first()).toBeVisible();
  await expect(page.getByRole("link", { name: /^Explore$/i })).toHaveAttribute("href", "/pos");
  await expect(page.getByRole("link", { name: /Learn More/i }).first()).toBeVisible();
  await expect(page.locator("body")).not.toContainText("BIR");
  await expect(page.locator("body")).not.toContainText("₱");
  await expect(page.locator("body")).not.toContainText("Trusted by");
});

test("service-pro page is a coming-soon experience without SoftwareApplication claims", async ({
  page,
}) => {
  await page.goto("/service-pro");

  await expect(page.getByRole("heading", { name: "Pinoy Service Pro" })).toBeVisible();
  await expect(page.getByText("Coming Soon").first()).toBeVisible();
  await expect(page.getByText(/implementation has not started/i)).toBeVisible();
  await expect(page.getByText(/\(planned\)/i).first()).toBeVisible();
  await expect(page.getByRole("navigation", { name: /breadcrumb/i })).toContainText("Products");
  await expect(page.locator('script[type="application/ld+json"]')).toHaveCount(0);
  await expect(page.locator("body")).not.toContainText("available now");
});

const viewports = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
  { width: 1920, height: 1080 },
];

for (const viewport of viewports) {
  test(`products page has no horizontal overflow at ${viewport.width}px`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await page.goto("/products");
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );
    expect(overflow).toBe(false);
  });

  test(`service-pro page has no horizontal overflow at ${viewport.width}px`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await page.goto("/service-pro");
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );
    expect(overflow).toBe(false);
  });
}
