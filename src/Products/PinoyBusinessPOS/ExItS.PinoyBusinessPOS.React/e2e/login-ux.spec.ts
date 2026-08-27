import { expect, test } from "@playwright/test";
import { mockAuthorizedPosDevice, seedInstallationId } from "./mock-sell-ready";
import { mockBoundCashierSession, signInAndBindCashier } from "./mock-bound-session";

async function mockUnauthenticatedSignIn(page: import("@playwright/test").Page) {
  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    if (url.includes("/auth/me")) {
      return route.fulfill({ status: 401, contentType: "application/json", body: "{}" });
    }
    if (url.includes("/antiforgery/token")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ headerName: "X-XSRF-TOKEN", token: "csrf-token" }),
      });
    }
    return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });
}

test.describe("login UX", () => {
  const viewports = [
    { width: 390, height: 844, label: "phone" },
    { width: 464, height: 1018, label: "large-phone" },
    { width: 768, height: 1024, label: "tablet-portrait" },
    { width: 1024, height: 768, label: "tablet-landscape" },
    { width: 1440, height: 900, label: "desktop" },
  ] as const;

  test("holds social login UI and keeps offline PIN alternative", async ({ page }) => {
    const externalProbeUrls: string[] = [];
    await page.route("**/platform-api/api/v1/platform/auth/external/**", async (route) => {
      externalProbeUrls.push(route.request().url());
      await route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
    });
    await mockUnauthenticatedSignIn(page);
    await page.goto("/sign-in");
    await expect(page.getByTestId("sign-in-page")).toBeVisible();
    await expect(page.getByTestId("auth-experience-hero")).toBeVisible();
    await expect(page.getByRole("button", { name: "Continue with Google" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Continue with Facebook" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Use Offline PIN" })).toBeVisible();
    expect(externalProbeUrls).toHaveLength(0);

    for (const viewport of viewports) {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await expect(page.getByTestId("auth-experience-sheet")).toBeVisible();
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
      expect(overflow).toBe(false);
    }
  });

  test("password sign-in remains available", async ({ page }) => {
    await mockBoundCashierSession(page);
    await page.goto("/sign-in");
    await expect(page.getByTestId("sign-in-submit")).toBeVisible();
    await expect(page.getByLabel("Email or staff login")).toBeVisible();
    await expect(page.getByLabel("Password")).toBeVisible();
    await expect(page.getByRole("checkbox", { name: "Remember Me" })).toBeVisible();
    await expect(page.getByTestId("auth-forgot-password-link")).toBeVisible();
  });

  test("cold start offline on a protected route shows Online Required", async ({ page, context }) => {
    await seedInstallationId(page);
    await mockBoundCashierSession(page);
    await mockAuthorizedPosDevice(page);
    await signInAndBindCashier(page);

    await context.setOffline(true);
    await page.route("**/platform-api/**", async (route) => {
      await route.abort("failed");
    });
    await page.route("**/pos-api/**", async (route) => {
      await route.abort("failed");
    });
    await page.reload();
    await expect(
      page
        .getByTestId("online-required-boot")
        .or(page.getByTestId("sign-in-offline-banner"))
        .or(page.getByText("You're offline")),
    ).toBeVisible({ timeout: 20000 });
    await expect(page.getByTestId("offline-pin-unlock-page")).toHaveCount(0);
  });
});
