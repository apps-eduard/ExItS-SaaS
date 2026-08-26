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
      // Prefer an Organization identity when present; Olivia Platform is valid for cookie/CSRF proof
      // but cannot enter the PLM workspace (account-scope gate).
      const match =
        options.find((label) => /PLM D3-PRE Allowed|Organization Administration/i.test(label)) ??
        options.find((label) => /olivia/i.test(label));
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

    // Authenticated cookie session is proven by leaving /sign-in. Workspace entry depends on
    // Organization scope + Platform /auth/product-access/effective (present on PLM branches;
    // may be absent on PlatformWeb CSRF HEAD). Account-scope and access-error gates still prove
    // the live cookie session and subsequent CSRF logout.
    await expect(page.getByRole("heading", { name: "Sign In" })).toHaveCount(0, {
      timeout: 20_000,
    });
    const authenticatedHeading = page
      .getByRole("heading", { name: "Pinoy Loan Manager" })
      .or(page.getByRole("heading", { name: "Organization account required" }))
      .or(
        page.getByRole("heading", { name: /Unable to check|Product access|Something went wrong/i }),
      );
    await expect(authenticatedHeading.first()).toBeVisible({ timeout: 20_000 });

    await page.reload();
    await expect(page.getByRole("heading", { name: "Sign In" })).toHaveCount(0);
    await expect(authenticatedHeading.first()).toBeVisible({ timeout: 20_000 });

    const directSignOut = page.getByRole("button", { name: "Sign out" });
    if ((await directSignOut.count()) > 0) {
      await directSignOut.click();
    } else {
      await page
        .getByRole("button", { name: /Olivia|PLM|Kizy|Paul|Maria/i })
        .first()
        .click();
      await page.getByRole("menuitem", { name: "Sign out" }).click();
    }
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
    await page.reload();
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
  });
});
