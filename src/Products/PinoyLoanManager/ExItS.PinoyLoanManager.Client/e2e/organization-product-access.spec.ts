import { expect, test } from "@playwright/test";
import {
  assertNoApiOrAuthTrafficInCaches,
  assertNoSensitiveAuthMaterial,
  assertNoSessionTokenPersistence,
  E2E_ORG_ALLOWED,
  E2E_ORG_DENIED,
  inspectServiceWorkerCaches,
  mockAuthenticatedSession,
} from "./helpers";

test.describe("organization product access gate", () => {
  test("allows workspace when product access is granted", async ({ page }) => {
    await mockAuthenticatedSession(page, { startSignedIn: true });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Pinoy Loan Manager" })).toBeVisible();
    await expect(page.getByText(E2E_ORG_ALLOWED.displayName)).toBeVisible();
    await expect(page.getByText(/Workspace is ready/i)).toBeVisible();
    await assertNoSessionTokenPersistence(page);
    const caches = await inspectServiceWorkerCaches(page);
    assertNoApiOrAuthTrafficInCaches(caches.urls);
    const storage = await page.evaluate(() =>
      JSON.stringify({
        local: { ...window.localStorage },
        session: { ...window.sessionStorage },
      }),
    );
    assertNoSensitiveAuthMaterial(storage);
  });

  test("blocks workspace when product access is denied", async ({ page }) => {
    await mockAuthenticatedSession(page, {
      startSignedIn: true,
      productAccess: { allowed: false, reasonCode: "product_assignment_missing" },
    });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "No Pinoy Loan Manager access" })).toBeVisible();
  });

  test("requires organization selection for multiple org memberships", async ({ page }) => {
    await mockAuthenticatedSession(page, {
      startSignedIn: true,
      selectedOrganizationId: null,
      organizations: [E2E_ORG_ALLOWED, E2E_ORG_DENIED],
    });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Choose organization" })).toBeVisible();
    await page.getByRole("button", { name: new RegExp(E2E_ORG_ALLOWED.displayName) }).click();
    await expect(page.getByRole("heading", { name: "Pinoy Loan Manager" })).toBeVisible();
  });

  test("does not call privileged access evaluate from the browser", async ({ page }) => {
    const evaluateCalls: string[] = [];
    await mockAuthenticatedSession(page, { startSignedIn: true });
    await page.route("**/platform-api/api/v1/platform/auth/access/evaluate**", (route) => {
      evaluateCalls.push(route.request().url());
      return route.fulfill({ status: 500, body: "must-not-call" });
    });
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Pinoy Loan Manager" })).toBeVisible();
    expect(evaluateCalls).toHaveLength(0);
  });
});
