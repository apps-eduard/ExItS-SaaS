import { expect, test } from "@playwright/test";
import { passwordField } from "./helpers";

const identity = process.env.PLM_CLIENT_GATE_D0_LOGIN ?? "olivia.mendoza@exits.local";
const password = process.env.LOCAL_VALIDATION_SHARED_PASSWORD ?? "";

test.describe("real Local Validation sign-in", () => {
  test.use({ baseURL: "http://127.0.0.1:5176" });

  test("signs in through /auth/login and restores the cookie session", async ({ page }) => {
    test.skip(
      !password,
      "LOCAL_VALIDATION_SHARED_PASSWORD is required for real Local Validation proof.",
    );

    await page.context().clearCookies();
    await page.goto("/sign-in");
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible({ timeout: 20_000 });
    const testUser = page.getByLabel("Test User");
    if (await testUser.count()) {
      const options = await testUser.locator("option").allTextContents();
      const match = options.find((label) => /olivia/i.test(label));
      if (match) {
        await testUser.selectOption({ label: match });
        await expect(page.getByLabel("Username or email")).not.toHaveValue("");
      }
    } else {
      await page.getByLabel("Username or email").fill(identity);
    }
    await expect(passwordField(page)).toHaveValue("");
    await passwordField(page).fill(password);
    await page.getByRole("button", { name: "Sign in" }).click();
    await expect(page.getByRole("heading", { name: "Pinoy Loan Manager" })).toBeVisible();
    await page.reload();
    await expect(page.getByRole("heading", { name: "Pinoy Loan Manager" })).toBeVisible();
    await page.getByRole("button", { name: /olivia/i }).click();
    await page.getByRole("menuitem", { name: "Sign out" }).click();
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
    await page.reload();
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
  });
});
