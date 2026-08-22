import { expect, test } from "@playwright/test";
import {
  LIVE_ANTIFORGERY_COOKIE,
  LIVE_OWNER_EMAIL,
  loadLocalValidationSharedPassword,
  skipUnlessLivePlatformApi,
} from "./live-runtime-helpers";

test.describe("workspace live runtime (@8091)", () => {
  test.use({ serviceWorkers: "block" });

  // Playwright requires fixture destructuring; no browser fixture needed for API gate.
  // eslint-disable-next-line no-empty-pattern -- Playwright fixture slot
  test.beforeEach(async ({}, testInfo) => {
    await skipUnlessLivePlatformApi(testInfo);
  });

  test("antiforgery cookie + owner workspace destinations against live Platform API", async ({
    page,
    context,
  }) => {
    const password = loadLocalValidationSharedPassword();
    expect(password).toBeTruthy();

    await page.goto("/sign-in");
    await expect(page.getByTestId("sign-in-page")).toBeVisible({ timeout: 15000 });
    await page.waitForTimeout(500);

    const cookiesAfterSignInPage = await context.cookies();
    const antiforgeryOnSignIn = cookiesAfterSignInPage.some((c) =>
      c.name.includes("Antiforgery"),
    );
    expect(antiforgeryOnSignIn).toBe(true);

    await page.getByLabel("Email or staff login").fill(LIVE_OWNER_EMAIL);
    await page.getByLabel("Password").fill(password!);
    await page.getByTestId("sign-in-submit").click();

    await expect(page.getByTestId("offline-pin-setup-page")).toBeVisible({ timeout: 30000 });
    await page.getByTestId("offline-pin-enroll-input").fill("123456");
    await page.getByTestId("offline-pin-enroll-confirm").fill("123456");
    await page.getByTestId("offline-pin-enroll-submit").click();

    await expect(page).toHaveURL(/\/workspace$/, { timeout: 90000 });
    await expect(page.getByTestId("workspace-grant-probe-error")).toHaveCount(0);
    await expect(page.getByTestId("workspace-grant-loading")).toHaveCount(0, { timeout: 30000 });
    await expect(page.getByTestId("workspace-destination-manage_business")).toBeVisible({
      timeout: 30000,
    });
    await expect(page.getByTestId("workspace-destination-operations")).toHaveCount(2);
    await expect(page.getByTestId("workspace-destination-start_selling")).toHaveCount(2);

    const cookiesAfterLogin = await context.cookies();
    expect(
      cookiesAfterLogin.some(
        (c) => c.name === LIVE_ANTIFORGERY_COOKIE || c.name.includes("Antiforgery"),
      ),
    ).toBe(true);

    await page.reload();
    await expect(page).toHaveURL(/\/workspace$/, { timeout: 30000 });
    await expect(page.getByTestId("workspace-grant-probe-error")).toHaveCount(0);
    await expect(page.getByTestId("workspace-destination-manage_business")).toBeVisible({
      timeout: 30000,
    });
    await expect(page.getByTestId("workspace-destination-operations")).toHaveCount(2);
  });
});
