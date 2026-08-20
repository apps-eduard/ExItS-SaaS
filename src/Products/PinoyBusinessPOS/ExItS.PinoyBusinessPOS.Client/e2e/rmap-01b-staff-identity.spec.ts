import { expect, test } from "@playwright/test";
import { mockBoundCashierSession, mockPersonalSession, signInAndBindCashier, signInAsPersonal } from "./mock-bound-session";

test.describe("RMAP-01b staff identity flows", () => {
  test.use({ serviceWorkers: "block" });

  test("anonymous accept success shows staff login distinct from contact email", async ({ page }) => {
    await page.route("**/platform-api/**", async (route) => {
      const url = route.request().url();
      const method = route.request().method();

      if (url.includes("/api/v1/platform/antiforgery/token")) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ headerName: "X-XSRF-TOKEN", token: "e2e-csrf" }),
        });
      }

      if (url.includes("/api/v1/platform/auth/me") && method === "GET") {
        return route.fulfill({ status: 401, contentType: "application/json", body: "{}" });
      }

      if (url.includes("/organization-invitations/accept") && !url.includes("accept-as-personal")) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            userId: "33333333-3333-3333-3333-333333333333",
            staffLogin: "maria@ORG123456",
            contactEmail: "maria.contact@example.com",
            organizationDisplayName: "Kizy Store",
            organizationId: "11111111-1111-1111-1111-111111111111",
            membershipId: "44444444-4444-4444-4444-444444444444",
            role: "OrganizationMember",
            linkedPersonalUserId: null,
          }),
        });
      }

      return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
    });

    await page.goto("/personal/invitations/accept?token=invite-token");
    await expect(page.getByTestId("staff-accept-page")).toBeVisible();
    await page.getByLabel("New staff password").fill("Staff-Pass-1!");
    await page.getByRole("button", { name: "Accept invitation" }).click();

    await expect(page.getByTestId("staff-accept-success")).toBeVisible();
    await expect(page.getByTestId("staff-login-value")).toContainText("maria@ORG123456");
    await expect(page.getByText("maria.contact@example.com")).toBeVisible();
    await expect(page.getByText("Kizy Store")).toBeVisible();
  });

  test("anonymous accept requiring Personal guides to Personal sign-in", async ({ page }) => {
    await page.route("**/platform-api/**", async (route) => {
      const url = route.request().url();
      const method = route.request().method();

      if (url.includes("/api/v1/platform/antiforgery/token")) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ headerName: "X-XSRF-TOKEN", token: "e2e-csrf" }),
        });
      }

      if (url.includes("/api/v1/platform/auth/me") && method === "GET") {
        return route.fulfill({ status: 401, contentType: "application/json", body: "{}" });
      }

      if (url.includes("/organization-invitations/accept") && !url.includes("accept-as-personal")) {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Sign in with your Personal account to accept this invitation.",
            errorCode: "application.invitation.requires_authenticated_personal",
          }),
        });
      }

      return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
    });

    await page.goto("/personal/invitations/accept?token=needs-personal");
    await page.getByLabel("New staff password").fill("Staff-Pass-1!");
    await page.getByRole("button", { name: "Accept invitation" }).click();
    await expect(page.getByTestId("staff-accept-requires-personal")).toBeVisible();
    await expect(page.getByRole("link", { name: "Sign in as Personal" })).toBeVisible();
  });

  test("Personal accept uses accept-as-personal and shows linked success", async ({ page }) => {
    await mockPersonalSession(page);
    await page.route("**/platform-api/api/v1/platform/auth/organization-invitations/accept-as-personal", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          userId: "55555555-5555-5555-5555-555555555555",
          staffLogin: "paul@ORG907757",
          contactEmail: "paul@gmail.com",
          organizationDisplayName: "Org A",
          organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          membershipId: "66666666-6666-6666-6666-666666666666",
          role: "OrganizationMember",
          linkedPersonalUserId: "77777777-7777-7777-7777-777777777777",
        }),
      });
    });

    await signInAsPersonal(page);
    await page.goto("/personal/invitations/accept?token=personal-token");
    await expect(page.getByText("Personal accept")).toBeVisible();
    await page.getByLabel("New staff password").fill("Staff-Pass-2!");
    await page.getByRole("button", { name: "Accept invitation" }).click();
    await expect(page.getByTestId("staff-login-value")).toContainText("paul@ORG907757");
  });

  test("Organization session cannot open invitation accept", async ({ page }) => {
    await mockBoundCashierSession(page);
    await signInAndBindCashier(page);
    await page.goto("/personal/invitations/accept?token=x");
    await expect(page.getByTestId("account-class-denied")).toBeVisible();
  });

  test("org staff invite form creates invitation and shows accept path", async ({ page }) => {
    await mockBoundCashierSession(page);
    await page.route("**/platform-api/api/v1/platform/organizations/*/invitations", async (route) => {
      if (route.request().method() === "POST") {
        return route.fulfill({
          status: 201,
          contentType: "application/json",
          body: JSON.stringify({
            id: "inv-1",
            organizationId: "11111111-1111-1111-1111-111111111111",
            email: "newhire@example.com",
            role: "OrganizationMember",
            status: "Pending",
            acceptToken: "one-shot-token",
          }),
        });
      }
      return route.fulfill({ status: 404, body: "{}" });
    });

    await signInAndBindCashier(page);
    await expect(page.getByRole("heading", { name: "Cashier home" })).toBeVisible();
    // Client-side navigate — full page.goto re-runs workspace auto-bind race in preview.
    await page.evaluate(() => {
      window.history.pushState({}, "", "/org/staff/invite");
      window.dispatchEvent(new PopStateEvent("popstate"));
    });
    await expect(page.getByTestId("staff-invite-page")).toBeVisible();
    await expect(page).toHaveURL(/\/org\/staff\/invite/);
    await page.getByLabel("Contact / recovery email").fill("newhire@example.com");
    await page.getByRole("button", { name: "Create invitation" }).click();
    await expect(page.getByTestId("staff-invite-created")).toBeVisible();
    await expect(page.getByText(/one-shot-token/)).toBeVisible();
  });
});
