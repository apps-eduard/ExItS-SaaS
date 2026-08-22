import { expect, test } from "@playwright/test";
import { mockAuthorizedPosDevice, seedInstallationId } from "./mock-sell-ready";
import { mockBoundCashierSession, signInAndBindCashier } from "./mock-bound-session";
import { buildSignedOfflineGrantDto } from "./mock-signed-offline-grant";

test.describe("login UX", () => {
  const viewports = [
    { width: 390, height: 844, label: "phone" },
    { width: 464, height: 1018, label: "large-phone" },
    { width: 768, height: 1024, label: "tablet-portrait" },
    { width: 1024, height: 768, label: "tablet-landscape" },
    { width: 1440, height: 900, label: "desktop" },
  ] as const;

  test("renders redesigned sign-in layout across required viewports", async ({ page }) => {
    await mockBoundCashierSession(page);
    await page.goto("/sign-in");
    await expect(page.getByTestId("sign-in-page")).toBeVisible();
    await expect(page.getByTestId("auth-experience-hero")).toBeVisible();
    await expect(page.getByRole("button", { name: "Continue with Google" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Continue with Facebook" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Use Offline PIN" })).toBeVisible();

    for (const viewport of viewports) {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await expect(page.getByTestId("auth-experience-sheet")).toBeVisible();
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
      expect(overflow).toBe(false);
    }
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
