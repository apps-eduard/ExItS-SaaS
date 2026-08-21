import { expect, test } from "@playwright/test";
import {
  E2E_BRANCH_ID,
  E2E_ORG_ID,
  mockBoundCashierSession,
  signInAndBindCashier,
  expectSellEntryVisible,
} from "./mock-bound-session";

test.describe("RMAP-03 branch / device operational context", () => {
  test.use({ serviceWorkers: "block" });

  test("zero accessible Active branches lands on no-location", async ({ page }) => {
    let loggedIn = false;
    await page.route("**/platform-api/**", async (route) => {
      const url = route.request().url();
      const method = route.request().method();
      if (url.includes("/antiforgery/token")) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ headerName: "X-XSRF-TOKEN", token: "e2e-csrf" }),
        });
      }
      if (url.includes("/auth/me") && method === "GET") {
        if (!loggedIn) {
          return route.fulfill({ status: 401, body: "{}" });
        }
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            sessionId: "22222222-2222-2222-2222-222222222222",
            username: "cashier",
            displayName: "Cashier One",
            email: "cashier@ORG000001",
            accountClass: "Organization",
            homeOrganizationId: E2E_ORG_ID,
            organizationContextLocked: true,
          }),
        });
      }
      if (url.includes("/auth/login") && method === "POST") {
        loggedIn = true;
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            sessionId: "22222222-2222-2222-2222-222222222222",
            username: "cashier",
            displayName: "Cashier One",
            accountClass: "Organization",
            homeOrganizationId: E2E_ORG_ID,
            organizationContextLocked: true,
            sessionToken: "x",
          }),
        });
      }
      if (url.includes("/auth/organizations") && method === "GET") {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify([
            { organizationId: E2E_ORG_ID, displayName: "Kizy Store", slug: "kizy" },
          ]),
        });
      }
      if (url.includes(`/organizations/${E2E_ORG_ID}/branches`) && method === "GET") {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify([
            {
              id: E2E_BRANCH_ID,
              organizationId: E2E_ORG_ID,
              code: "MAIN",
              name: "Closed Branch",
              isPrimary: true,
              status: "Suspended",
            },
          ]),
        });
      }
      return route.fulfill({ status: 404, body: "{}" });
    });

    await page.goto("/sign-in");
    await page.getByLabel("Email or staff login").fill("cashier");
    await page.getByLabel("Password").fill("secret");
    await page.getByRole("button", { name: "Sign in" }).click();
    await expect(page.getByTestId("no-accessible-branch")).toBeVisible();
  });

  test("single Active branch auto-binds Start Selling for cashier", async ({ page }) => {
    await mockBoundCashierSession(page);
    await signInAndBindCashier(page);
    await expectSellEntryVisible(page);
    await expect(page.getByTestId("workspace-context")).toContainText("Main Branch");
  });

  test("multiple Active branches open chooser", async ({ page }) => {
    let loggedIn = false;
    const branchB = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    await page.route("**/platform-api/**", async (route) => {
      const url = route.request().url();
      const method = route.request().method();
      if (url.includes("/antiforgery/token")) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ headerName: "X-XSRF-TOKEN", token: "e2e-csrf" }),
        });
      }
      if (url.includes("/auth/me") && method === "GET") {
        if (!loggedIn) {
          return route.fulfill({ status: 401, body: "{}" });
        }
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            sessionId: "22222222-2222-2222-2222-222222222222",
            username: "cashier",
            displayName: "Cashier One",
            accountClass: "Organization",
            homeOrganizationId: E2E_ORG_ID,
            organizationContextLocked: true,
          }),
        });
      }
      if (url.includes("/auth/login") && method === "POST") {
        loggedIn = true;
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            sessionId: "22222222-2222-2222-2222-222222222222",
            username: "cashier",
            displayName: "Cashier One",
            accountClass: "Organization",
            homeOrganizationId: E2E_ORG_ID,
            organizationContextLocked: true,
            sessionToken: "x",
          }),
        });
      }
      if (url.includes("/auth/organizations") && method === "GET") {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify([
            { organizationId: E2E_ORG_ID, displayName: "Kizy Store", slug: "kizy" },
          ]),
        });
      }
      if (url.includes(`/organizations/${E2E_ORG_ID}/branches`) && method === "GET") {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify([
            {
              id: E2E_BRANCH_ID,
              organizationId: E2E_ORG_ID,
              code: "MAIN",
              name: "Main Branch",
              isPrimary: true,
              status: "Active",
            },
            {
              id: branchB,
              organizationId: E2E_ORG_ID,
              code: "NORTH",
              name: "North Branch",
              isPrimary: false,
              status: "Active",
            },
          ]),
        });
      }
      return route.fulfill({ status: 404, body: "{}" });
    });

    await page.goto("/sign-in");
    await page.getByLabel("Email or staff login").fill("cashier");
    await page.getByLabel("Password").fill("secret");
    await page.getByRole("button", { name: "Sign in" }).click();
    await expect(page.getByRole("heading", { name: "Choose workspace" })).toBeVisible();
    await page.getByRole("button", { name: /Kizy Store/i }).click();
    await expect(page.getByText("Main Branch")).toBeVisible();
    await expect(page.getByText("North Branch")).toBeVisible();
  });

  test("logout clears bound workspace context", async ({ page }) => {
    await mockBoundCashierSession(page);
    await signInAndBindCashier(page);
    await expectSellEntryVisible(page);
    await page.getByRole("button", { name: /Account menu/i }).click();
    await page.getByRole("menuitem", { name: /Sign out/i }).click();
    await expect(page).toHaveURL(/\/sign-in/);
    await page.goto("/");
    await expect(page).toHaveURL(/\/sign-in/);
  });

  for (const viewport of [
    { width: 375, height: 812 },
    { width: 768, height: 1024 },
    { width: 1024, height: 768 },
    { width: 1440, height: 900 },
  ] as const) {
    test(`no-location usable at ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      let loggedIn = false;
      await page.route("**/platform-api/**", async (route) => {
        const url = route.request().url();
        const method = route.request().method();
        if (url.includes("/antiforgery/token")) {
          return route.fulfill({
            status: 200,
            contentType: "application/json",
            body: JSON.stringify({ headerName: "X-XSRF-TOKEN", token: "e2e-csrf" }),
          });
        }
        if (url.includes("/auth/me") && method === "GET") {
          if (!loggedIn) {
            return route.fulfill({ status: 401, body: "{}" });
          }
          return route.fulfill({
            status: 200,
            contentType: "application/json",
            body: JSON.stringify({
              sessionId: "22222222-2222-2222-2222-222222222222",
              username: "cashier",
              displayName: "Cashier One",
              accountClass: "Organization",
              homeOrganizationId: E2E_ORG_ID,
              organizationContextLocked: true,
            }),
          });
        }
        if (url.includes("/auth/login") && method === "POST") {
          loggedIn = true;
          return route.fulfill({
            status: 200,
            contentType: "application/json",
            body: JSON.stringify({
              sessionId: "22222222-2222-2222-2222-222222222222",
              username: "cashier",
              displayName: "Cashier One",
              accountClass: "Organization",
              homeOrganizationId: E2E_ORG_ID,
              organizationContextLocked: true,
              sessionToken: "x",
            }),
          });
        }
        if (url.includes("/auth/organizations")) {
          return route.fulfill({
            status: 200,
            contentType: "application/json",
            body: JSON.stringify([
              { organizationId: E2E_ORG_ID, displayName: "Kizy Store", slug: "kizy" },
            ]),
          });
        }
        if (url.includes("/branches")) {
          return route.fulfill({ status: 200, contentType: "application/json", body: "[]" });
        }
        return route.fulfill({ status: 404, body: "{}" });
      });

      await page.goto("/sign-in");
      await page.getByLabel("Email or staff login").fill("cashier");
      await page.getByLabel("Password").fill("secret");
      await page.getByRole("button", { name: "Sign in" }).click();
      const panel = page.getByTestId("no-accessible-branch");
      await expect(panel).toBeVisible();
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
      );
      expect(overflow).toBe(false);
    });
  }
});
