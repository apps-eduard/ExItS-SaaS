import { mkdirSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";
import { E2E_ORG_ALLOWED, E2E_ORG_DENIED, mockAuthenticatedSession } from "./helpers";

const screenshotDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../Docs/Reports/impl-gate-d3-organization-product-access",
);

test.describe("D3 screenshots", () => {
  test.beforeAll(() => {
    mkdirSync(screenshotDir, { recursive: true });
  });

  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
  });

  test("01 organization select", async ({ page }) => {
    await mockAuthenticatedSession(page, {
      startSignedIn: true,
      selectedOrganizationId: null,
      organizations: [E2E_ORG_ALLOWED, E2E_ORG_DENIED],
    });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Choose organization" })).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "01-organization-select-375x812.png"),
      fullPage: true,
    });
  });

  test("02 product access denied", async ({ page }) => {
    await mockAuthenticatedSession(page, {
      startSignedIn: true,
      productAccess: { allowed: false, reasonCode: "product_assignment_missing" },
    });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "No Pinoy Loan Manager access" })).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "02-product-access-denied-375x812.png"),
      fullPage: true,
    });
  });

  test("03 subscription inactive", async ({ page }) => {
    await mockAuthenticatedSession(page, {
      startSignedIn: true,
      productAccess: {
        allowed: false,
        reasonCode: "subscription_ineligible",
        subscriptionStatus: "Inactive",
      },
    });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Subscription inactive" })).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "03-subscription-inactive-375x812.png"),
      fullPage: true,
    });
  });

  test("04 account scope denied", async ({ page }) => {
    await mockAuthenticatedSession(page, { startSignedIn: true, accountClass: "Platform" });
    await page.goto("/");
    await expect(
      page.getByRole("heading", { name: "Organization account required" }),
    ).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "04-account-scope-denied-375x812.png"),
      fullPage: true,
    });
  });

  test("05 workspace ready", async ({ page }) => {
    await mockAuthenticatedSession(page, { startSignedIn: true });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Pinoy Loan Manager" })).toBeVisible();
    await page.screenshot({
      path: path.join(screenshotDir, "05-workspace-ready-375x812.png"),
      fullPage: true,
    });
  });
});
