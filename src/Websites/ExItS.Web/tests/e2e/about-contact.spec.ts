import { test, expect } from "@playwright/test";

test("about page renders mission without invented metrics or team profiles", async ({ page }) => {
  await page.goto("/about");

  await expect(page.getByRole("heading", { name: "About ExItS" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Mission" })).toBeVisible();
  await expect(page.getByText("Available").first()).toBeVisible();
  await expect(page.locator("body")).not.toContainText("Our Team");
  await expect(page.locator("body")).not.toContainText("founded in");
  await expect(page.locator("body")).not.toContainText("employees");
  await expect(page.locator("body")).not.toContainText("Trusted by");
  await expect(page.locator("body")).not.toContainText("₱");
});

test("contact page validates and keeps submission honestly unconnected", async ({ page }) => {
  await page.goto("/contact");

  await expect(page.getByRole("heading", { name: "Contact ExItS" })).toBeVisible();
  await page.getByRole("button", { name: /send message/i }).click();
  await expect(page.getByText(/name is required/i)).toBeVisible();

  await page.getByLabel("Name", { exact: true }).fill("Ada Owner");
  await page.getByLabel("Email", { exact: true }).fill("ada@business.ph");
  await page.getByLabel("Message", { exact: true }).fill("Looking for Pinoy Business POS.");
  await page.getByRole("button", { name: /send message/i }).click();
  await expect(page.getByRole("status")).toContainText(/not connected yet/i);
  await expect(page.locator("body")).not.toContainText("Thank you");
});

const viewports = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
  { width: 1920, height: 1080 },
];

for (const viewport of viewports) {
  test(`about page has no horizontal overflow at ${viewport.width}px`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await page.goto("/about");
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );
    expect(overflow).toBe(false);
  });

  test(`contact page has no horizontal overflow at ${viewport.width}px`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await page.goto("/contact");
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );
    expect(overflow).toBe(false);
  });
}
