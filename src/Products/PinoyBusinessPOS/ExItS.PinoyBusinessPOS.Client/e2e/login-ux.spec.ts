import { expect, test } from "@playwright/test";
import { mockAuthorizedPosDevice, seedInstallationId } from "./mock-sell-ready";
import { mockBoundCashierSession, signInAndBindCashier } from "./mock-bound-session";
import { buildSignedOfflineGrantDto } from "./mock-signed-offline-grant";

test.describe("login UX", () => {
  test("renders redesigned sign-in layout at phone, tablet, and desktop widths", async ({ page }) => {
    await mockBoundCashierSession(page);
    await page.goto("/sign-in");
    await expect(page.getByTestId("sign-in-page")).toBeVisible();
    await expect(page.getByTestId("auth-experience-hero")).toBeVisible();
    await expect(page.getByTestId("auth-tab-sign-in")).toBeVisible();
    await expect(page.getByTestId("auth-tab-sign-up")).toBeVisible();
    await expect(page.getByTestId("auth-social-row")).toBeVisible();

    await page.setViewportSize({ width: 390, height: 844 });
    await expect(page.getByTestId("auth-experience-sheet")).toBeVisible();

    await page.setViewportSize({ width: 768, height: 1024 });
    await expect(page.getByTestId("auth-experience-sheet")).toBeVisible();

    await page.setViewportSize({ width: 1440, height: 900 });
    await expect(page.getByTestId("auth-experience-sheet")).toBeVisible();
  });

  test("logout while offline routes to offline PIN unlock", async ({ page, context }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockAuthorizedPosDevice(page);

    await page.route("**/pos-api/api/v1/pos/offline-operating-grants", async (route) => {
      const grant = await buildSignedOfflineGrantDto();
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ grant }),
      });
    });

    await signInAndBindCashier(page);

    await page.getByTestId("account-menu-trigger").click();
    await page.route("**/platform-api/api/v1/platform/auth/logout", async (route) => {
      await route.abort("failed");
    });
    await page.getByRole("menuitem", { name: "Sign out" }).click();

    await expect(page.getByTestId("offline-pin-unlock-page")).toBeVisible({ timeout: 15000 });

    await context.setOffline(true);
    await page.reload();
    await expect(page.getByTestId("offline-pin-unlock-page")).toBeVisible();
  });
});
