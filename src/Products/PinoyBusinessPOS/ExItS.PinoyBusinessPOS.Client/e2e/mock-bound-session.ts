import type { Page } from "@playwright/test";

export const E2E_ORG_ID = "11111111-1111-1111-1111-111111111111";
export const E2E_BRANCH_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

type MockGrantOptions = {
  mappedPosRoleCode?: string | null;
  productLocalRoleCode?: string | null;
  productAccessAllowed?: boolean;
  organizationManagementAuthority?: boolean;
  membershipRole?: string | null;
};

export async function mockBoundCashierSession(page: Page, grant: MockGrantOptions = {}) {
  const {
    mappedPosRoleCode = "Cashier",
    productLocalRoleCode = "Cashier",
    productAccessAllowed = true,
    organizationManagementAuthority = false,
    membershipRole = null,
  } = grant;

  let loggedIn = false;

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
      if (!loggedIn) {
        return route.fulfill({ status: 401, contentType: "application/json", body: "{}" });
      }

      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          sessionId: "22222222-2222-2222-2222-222222222222",
          username: "cashier",
          displayName: "Cashier One",
        }),
      });
    }

    if (url.includes("/api/v1/platform/auth/login") && method === "POST") {
      loggedIn = true;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          sessionId: "22222222-2222-2222-2222-222222222222",
          username: "cashier",
          displayName: "Cashier One",
          sessionToken: "must-not-persist",
        }),
      });
    }

    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([
          {
            organizationId: E2E_ORG_ID,
            displayName: "Kizy Store",
            slug: "kizy-store",
          },
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
        ]),
      });
    }

    if (url.includes("/api/v1/platform/auth/organization-context") && method === "PUT") {
      return route.fulfill({ status: 204, body: "" });
    }

    if (url.includes(`/organizations/${E2E_ORG_ID}/branch-context`) && method === "PUT") {
      return route.fulfill({ status: 204, body: "" });
    }

    if (url.includes("/api/v1/platform/auth/token") && method === "POST") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          accessToken: "in-memory-only-access-token",
          productAccessAllowed,
          mappedPosRoleCode,
          productLocalRoleCode,
          organizationManagementAuthority,
          membershipRole,
        }),
      });
    }

    if (url.includes("/api/v1/platform/auth/logout") && method === "POST") {
      return route.fulfill({ status: 204, body: "" });
    }

    return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });
}

export async function signInAndBindCashier(page: Page) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or username").fill("cashier");
  await page.getByLabel("Password").fill("secret");
  await page.getByRole("button", { name: "Sign in" }).click();
}
