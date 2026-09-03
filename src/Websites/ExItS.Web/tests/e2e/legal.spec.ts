import { test, expect } from "@playwright/test";

test("privacy page shows draft placeholder, not template legal policy text", async ({ page }) => {
  await page.goto("/privacy");

  await expect(page.getByRole("heading", { name: "Privacy Policy" })).toBeVisible();
  await expect(page.getByText(/draft — pending legal review/i).first()).toBeVisible();
  await expect(page.getByText(/last updated:/i)).toBeVisible();
  await expect(page.getByText(/pending legal review/i).first()).toBeVisible();
  await expect(
    page.getByText(/privacy policy is being finalized/i),
  ).toBeVisible();
  await expect(page.getByRole("link", { name: /contact us/i }).first()).toHaveAttribute(
    "href",
    "/contact",
  );
  await expect(page.locator("body")).not.toContainText("We collect the following categories");
  await expect(page.locator("body")).not.toContainText("BIR-accredited");
  await expect(page.locator("body")).not.toContainText("NPC Registration");
  await expect(page.getByRole("navigation", { name: "Breadcrumb" })).toContainText(
    "Privacy Policy",
  );
});

test("terms page shows draft placeholder, not template terms text", async ({ page }) => {
  await page.goto("/terms");

  await expect(page.getByRole("heading", { name: "Terms of Service" })).toBeVisible();
  await expect(page.getByText(/draft — pending legal review/i).first()).toBeVisible();
  await expect(
    page.getByText(/terms of service are being finalized/i),
  ).toBeVisible();
  await expect(page.getByRole("link", { name: /contact us/i }).first()).toHaveAttribute(
    "href",
    "/contact",
  );
  await expect(page.locator("body")).not.toContainText("By accessing or using our Service");
  await expect(page.locator("body")).not.toContainText("Governing Law and Jurisdiction");
  await expect(page.getByRole("navigation", { name: "Breadcrumb" })).toContainText(
    "Terms of Service",
  );
});

test("footer legal links resolve to privacy and terms", async ({ page }) => {
  await page.goto("/");
  await page.getByRole("contentinfo").getByRole("link", { name: "Privacy" }).click();
  await expect(page).toHaveURL(/\/privacy$/);
  await expect(page.getByRole("heading", { name: "Privacy Policy" })).toBeVisible();

  await page.getByRole("contentinfo").getByRole("link", { name: "Terms" }).click();
  await expect(page).toHaveURL(/\/terms$/);
  await expect(page.getByRole("heading", { name: "Terms of Service" })).toBeVisible();
});

const viewports = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
  { width: 1920, height: 1080 },
];

for (const viewport of viewports) {
  test(`privacy page has no horizontal overflow at ${viewport.width}px`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await page.goto("/privacy");
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );
    expect(overflow).toBe(false);
  });

  test(`terms page has no horizontal overflow at ${viewport.width}px`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await page.goto("/terms");
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );
    expect(overflow).toBe(false);
  });
}
