import { test, expect } from "@playwright/test";

test("homepage renders foundation placeholder", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: /ExItS Public Website/i })).toBeVisible();
});

