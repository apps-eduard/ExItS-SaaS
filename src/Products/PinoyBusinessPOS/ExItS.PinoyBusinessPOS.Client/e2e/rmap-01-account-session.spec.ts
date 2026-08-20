import { expect, test } from "@playwright/test";
import {
  mockBoundCashierSession,
  mockPersonalSession,
  signInAndBindCashier,
  signInAsPersonal,
} from "./mock-bound-session";

test.describe("RMAP-01 account / session parity", () => {
  // Preview builds register a SW that can bypass Playwright routes on full navigation.
  test.use({ serviceWorkers: "block" });

  test("Personal login lands on Personal home and survives reload", async ({ page }) => {
    await mockPersonalSession(page);
    await signInAsPersonal(page);

    await expect(page.getByRole("heading", { name: "Personal home" })).toBeVisible();
    await expect(page).toHaveURL(/\/personal$/);

    await page.reload();
    await expect(page.getByRole("heading", { name: "Personal home" })).toBeVisible();
    await expect(page).toHaveURL(/\/personal$/);
  });

  test("Personal session is denied Organization sell surface", async ({ page }) => {
    await mockPersonalSession(page);
    await signInAsPersonal(page);
    await expect(page.getByRole("heading", { name: "Personal home" })).toBeVisible();

    await page.goto("/sell");
    await expect(page.getByTestId("account-class-denied")).toBeVisible();
    await expect(
      page.getByRole("heading", { name: "Account type not allowed" }).first(),
    ).toBeVisible();
  });

  test("staff @ORG login hint appears and Organization session binds cashier home", async ({
    page,
  }) => {
    await mockBoundCashierSession(page);

    await page.goto("/sign-in");
    await page.getByLabel("Email or staff login").fill("paul@ORG907757");
    await expect(page.getByTestId("staff-login-hint")).toBeVisible();
    await page.getByLabel("Password").fill("staff-secret");
    await page.getByRole("button", { name: "Sign in" }).click();

    await expect(page.getByRole("heading", { name: "Cashier home" })).toBeVisible();
  });

  test("Organization session is denied Personal-only surface", async ({ page }) => {
    await mockBoundCashierSession(page);
    await signInAndBindCashier(page);
    await expect(page.getByRole("heading", { name: "Cashier home" })).toBeVisible();

    await page.goto("/personal");
    await expect(page.getByTestId("account-class-denied")).toBeVisible();
  });

  test("logout clears Personal session", async ({ page }) => {
    await mockPersonalSession(page);
    await signInAsPersonal(page);
    await expect(page.getByRole("heading", { name: "Personal home" })).toBeVisible();

    await page.getByTestId("account-menu-trigger").click();
    await page.getByRole("menuitem", { name: "Sign out" }).click();
    await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();
    await expect(page).toHaveURL(/\/sign-in$/);
  });
});
