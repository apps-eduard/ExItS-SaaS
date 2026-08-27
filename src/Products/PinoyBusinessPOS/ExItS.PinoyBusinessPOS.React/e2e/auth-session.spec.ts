import { expect, test } from "@playwright/test";
import { mockBoundCashierSession, signInAndBindCashier } from "./mock-bound-session";

test.describe("auth session", () => {
  test("mocked login happy path keeps sessionToken and Bearer out of storage", async ({ page }) => {
    await mockBoundCashierSession(page);
    await signInAndBindCashier(page);

    await expect(page.getByRole("heading", { name: "New Sale" })).toBeVisible();
    await expect(page.getByRole("banner").getByTestId("workspace-context")).toContainText(
      "Kizy Store",
    );

    const storageScan = await page.evaluate(() => {
      const values: string[] = [];
      for (const storage of [window.localStorage, window.sessionStorage]) {
        for (let index = 0; index < storage.length; index += 1) {
          const key = storage.key(index);
          if (key) {
            values.push(`${key}=${storage.getItem(key) ?? ""}`);
          }
        }
      }
      return values.join("\n");
    });

    expect(storageScan.toLowerCase()).not.toMatch(/sessiontoken/);
    expect(storageScan).not.toMatch(/Bearer /i);
    expect(storageScan).not.toMatch(/in-memory-only-access-token/);
  });

  test("sign out posts logout with CSRF, clears shell, and blocks protected routes", async ({
    page,
  }) => {
    const logoutRequests: { method: string; csrf: string | undefined }[] = [];
    await mockBoundCashierSession(page);
    await page.route("**/platform-api/api/v1/platform/auth/logout", async (route) => {
      logoutRequests.push({
        method: route.request().method(),
        csrf: route.request().headers()["x-xsrf-token"],
      });
      await route.fallback();
    });

    await signInAndBindCashier(page);
    await expect(page.getByRole("heading", { name: "New Sale" })).toBeVisible();

    await page.getByTestId("account-menu-trigger").click();
    await page.getByRole("menuitem", { name: "Sign out" }).click();
    await expect(page.getByTestId("sign-in-page")).toBeVisible({ timeout: 15000 });
    await expect(page).toHaveURL(/\/sign-in$/);

    expect(logoutRequests.length).toBeGreaterThan(0);
    expect(logoutRequests[0]?.method).toBe("POST");
    expect(logoutRequests[0]?.csrf).toBeTruthy();

    await page.goto("/sell");
    await expect(page.getByTestId("sign-in-page")).toBeVisible();
    await expect(page).toHaveURL(/\/sign-in/);

    await page.reload();
    await expect(page.getByTestId("sign-in-page")).toBeVisible();
    await expect(page.getByRole("banner")).toHaveCount(0);
  });

  test("failed remote logout still locks locally and reaches sign-in", async ({ page }) => {
    await mockBoundCashierSession(page);
    await page.route("**/platform-api/api/v1/platform/auth/logout", async (route) => {
      await route.fulfill({
        status: 500,
        contentType: "application/json",
        body: JSON.stringify({ detail: "logout unavailable" }),
      });
    });

    await signInAndBindCashier(page);
    await expect(page.getByRole("heading", { name: "New Sale" })).toBeVisible();

    await page.getByTestId("account-menu-trigger").click();
    await page.getByRole("menuitem", { name: "Sign out" }).click();
    // Current contract: local session clears even when Platform logout fails; pending remote logout is marked.
    await expect(page.getByTestId("sign-in-page")).toBeVisible({ timeout: 15000 });
    await expect(page).toHaveURL(/\/sign-in/);
    const pending = await page.evaluate(() =>
      window.localStorage.getItem("exits.pos-client.pending-remote-logout.v1"),
    );
    expect(pending).toBeTruthy();
  });
});
