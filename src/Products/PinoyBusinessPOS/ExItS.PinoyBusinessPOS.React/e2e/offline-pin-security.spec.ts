import { expect, test } from "@playwright/test";
import { mockAuthorizedPosDevice, seedInstallationId } from "./mock-sell-ready";
import {
  expectSellEntryVisible,
  mockBoundCashierSession,
  signInAndBindCashier,
} from "./mock-bound-session";

test.describe("offline PIN security", () => {
  test("Organization Web cold restart while offline shows Online Required", async ({
    page,
    context,
  }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockAuthorizedPosDevice(page);

    await signInAndBindCashier(page);
    await expectSellEntryVisible(page);

    await context.setOffline(true);
    await page.route("**/platform-api/**", async (route) => {
      await route.abort("failed");
    });
    await page.route("**/pos-api/**", async (route) => {
      await route.abort("failed");
    });
    await page.reload();
    // Web online-only: do not unlock via offline PIN. Accept Online Required boot,
    // Connectivity offline notice on sign-in, or the sign-in offline banner.
    await expect(
      page
        .getByTestId("online-required-boot")
        .or(page.getByTestId("sign-in-offline-banner"))
        .or(page.getByText("You're offline")),
    ).toBeVisible({ timeout: 20000 });
    await expect(page.getByTestId("offline-pin-unlock-page")).toHaveCount(0);
  });
});
