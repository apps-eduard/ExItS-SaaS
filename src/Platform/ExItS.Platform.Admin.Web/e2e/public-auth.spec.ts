import { expect, test, type Page } from "@playwright/test";

async function mockUnauthenticated(page: Page) {
  await page.route("**/api/v1/platform/auth/me", async (route) => {
    await route.fulfill({
      status: 401,
      json: { status: 401, errorCode: "application.auth.session_invalid" },
    });
  });
  await page.route("**/api/v1/platform/local-validation/enabled", async (route) => {
    await route.fulfill({ json: false });
  });
}

test("login links open registration and forgot-password pages", async ({ page }) => {
  await mockUnauthenticated(page);
  await page.goto("/admin/login");
  await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
  await page.getByRole("link", { name: "Forgot password?" }).click();
  await expect(page.getByRole("heading", { name: "Forgot password" })).toBeVisible();
  await expect(page.getByText(/not implemented/i)).toHaveCount(0);
  await page.getByRole("link", { name: "Sign In" }).click();
  await page.getByRole("link", { name: "Create account" }).click();
  await expect(page.getByRole("heading", { name: "Create your ExItS account" })).toBeVisible();
});

test("production preview does not show Mailpit after registration success", async ({ page }) => {
  await mockUnauthenticated(page);
  await page.route("**/api/v1/platform/auth/register", async (route) => {
    await route.fulfill({
      json: { message: "If the email is eligible, a verification message was sent." },
    });
  });
  await page.goto("/admin/register");
  await page.getByLabel("Display name").fill("Ana Cruz");
  await page.getByLabel("Email").fill("ana@example.test");
  await page.getByRole("button", { name: "Create account" }).click();
  await expect(page.getByRole("heading", { name: "Check your email" })).toBeVisible();
  await expect(page.getByText("Open Mailpit")).toHaveCount(0);
});

test("activation missing token is rejected safely", async ({ page }) => {
  await mockUnauthenticated(page);
  await page.goto("/admin/activate-account");
  await expect(page.getByRole("alert")).toHaveText(
    "This activation link is invalid or has expired.",
  );
  await expect(page.getByRole("button", { name: "Activate account" })).toBeDisabled();
});
