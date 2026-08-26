import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import {
  assertNoHorizontalOverflow,
  assertNoSessionTokenPersistence,
  mockAnonymousSession,
  mockAuthenticatedSession,
  passwordField,
} from "./helpers";

const forbidden = ["Preview", "Foundation", "coming soon", "1,250.00", "Online", "Synced"];

test.describe("sign-in and session", () => {
  test("unauthenticated / redirects to /sign-in", async ({ page }) => {
    await mockAnonymousSession(page);
    await page.goto("/");
    await expect(page).toHaveURL(/\/sign-in$/);
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
  });

  test("invalid credentials stay generic and keep the password hidden", async ({ page }) => {
    await mockAuthenticatedSession(page);
    await page.goto("/sign-in");
    await page.getByLabel("Username or email").fill("olivia");
    await passwordField(page).fill("wrong");
    await page.getByRole("button", { name: "Sign in" }).click();
    await expect(page.getByText("Sign in failed. Check your username and password.")).toBeVisible();
    await expect(passwordField(page)).toHaveAttribute("type", "password");
  });

  test("real login, refresh, logout, and refresh stay signed out", async ({ page }) => {
    await mockAuthenticatedSession(page);
    await page.goto("/sign-in");
    await page.getByLabel("Username or email").fill("olivia");
    await passwordField(page).fill("local-only");
    await page.getByRole("button", { name: "Sign in" }).click();
    await expect(page.getByRole("heading", { name: "Pinoy Loan Manager" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Olivia Mendoza" })).toBeVisible();
    await assertNoSessionTokenPersistence(page);
    await page.evaluate(async () => {
      const registrations = await navigator.serviceWorker.getRegistrations();
      await Promise.all(registrations.map((registration) => registration.unregister()));
    });
    await page.reload();
    await expect(page.getByRole("heading", { name: "Pinoy Loan Manager" })).toBeVisible();
    await page.getByRole("button", { name: "Olivia Mendoza" }).click();
    await page.getByRole("menuitem", { name: "Sign out" }).click();
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
    await page.reload();
    await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
  });

  test("production preview hides Test User", async ({ page }) => {
    await mockAnonymousSession(page);
    await page.goto("/sign-in");
    await expect(page.getByLabel("Test User")).toHaveCount(0);
    await expect(page.getByText(/Local Validation/i)).toHaveCount(0);
  });

  test("sign-in fits 320, 375, 768, and desktop without overflow", async ({ page }) => {
    await mockAnonymousSession(page);
    for (const viewport of [
      { width: 320, height: 568 },
      { width: 375, height: 812 },
      { width: 768, height: 1024 },
      { width: 1440, height: 900 },
    ] as const) {
      await page.setViewportSize(viewport);
      await page.goto("/sign-in");
      await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
      await assertNoHorizontalOverflow(page);
      const text = (await page.locator("body").innerText()).toLowerCase();
      for (const phrase of forbidden) {
        expect(text).not.toContain(phrase.toLowerCase());
      }
    }
  });

  test("axe has no serious or critical violations on sign-in", async ({ page }) => {
    await mockAnonymousSession(page);
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto("/sign-in");
    const results = await new AxeBuilder({ page }).analyze();
    const serious = results.violations.filter(
      (violation) => violation.impact === "serious" || violation.impact === "critical",
    );
    expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
  });
});
