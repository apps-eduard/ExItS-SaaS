import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";

const UI_PREFERENCES_STORAGE_KEY = "exits.pos-client.ui-preferences.v1";

async function mockUnauthenticated(page: import("@playwright/test").Page) {
  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    if (url.includes("/auth/me")) {
      return route.fulfill({ status: 401, contentType: "application/json", body: "{}" });
    }
    if (url.includes("/local-validation/enabled")) {
      return route.fulfill({ status: 200, contentType: "application/json", body: "true" });
    }
    if (url.includes("/local-validation/quick-login-identities")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([
          {
            key: "ql:demo",
            username: "cashier",
            displayName: "Cashier One",
            email: "cashier@example.com",
            listLabel: "Cashier One",
          },
        ]),
      });
    }
    return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });
}

test.describe("canonical sign-in", () => {
  test("English and Filipino share the same sign-in page structure", async ({ page }) => {
    await mockUnauthenticated(page);
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/sign-in");
    await expect(page.getByTestId("sign-in-page")).toBeVisible();
    await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();
    await assertNoHorizontalOverflow(page);

    await page.evaluate(
      ([key]) => {
        window.localStorage.setItem(key, JSON.stringify({ locale: "fil-PH", theme: "system" }));
      },
      [UI_PREFERENCES_STORAGE_KEY],
    );
    await page.reload();
    await expect(page.getByTestId("sign-in-page")).toBeVisible();
    await expect(page.getByRole("heading", { name: "Mag-sign in" })).toBeVisible();
    await expect(page.getByTestId("sign-in-page")).toBeVisible();
    await assertNoHorizontalOverflow(page);
  });

  for (const viewport of [
    { name: "320", width: 320, height: 568 },
    { name: "375", width: 375, height: 812 },
    { name: "1440", width: 1440, height: 900 },
  ] as const) {
    test(`${viewport.name} sign-in stays on the canonical page`, async ({ page }) => {
      await mockUnauthenticated(page);
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await page.goto("/sign-in");
      await expect(page.getByTestId("sign-in-page")).toBeVisible();
      await expect(page.getByTestId("sign-in-page")).toHaveAttribute(
        "data-exits-build-mode",
        /^(development|production)$/,
      );
      await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible();
      // Playwright preview is a production build; Local Validation tools are DEV-only.
      const buildMode = await page
        .getByTestId("sign-in-page")
        .getAttribute("data-exits-build-mode");
      if (buildMode === "development") {
        await expect(page.getByText(/development tools/i)).toBeVisible();
      }
      await assertNoHorizontalOverflow(page);
    });
  }
});
