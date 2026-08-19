import { expect, test } from "@playwright/test";

const enabled = process.env.PWEB_CONTAINER_SMOKE === "1";

test.describe("local-validation React container smoke", () => {
  test.skip(
    !enabled,
    "Set PWEB_CONTAINER_SMOKE=1 after the React Admin container is listening on 8095.",
  );
  test.use({ baseURL: process.env.PWEB_CONTAINER_BASE_URL ?? "http://127.0.0.1:8095" });

  test("serves /admin and SPA fallback for a known /admin route", async ({ page }) => {
    await page.goto("/admin");
    await expect(page.locator("#root")).toBeVisible();
    await expect(page).toHaveURL(/\/admin/);

    const response = await page.goto("/admin/organizations");
    expect(response?.ok()).toBeTruthy();
    await expect(page.locator("#root")).toBeVisible();
  });
});
