import type { Page } from "@playwright/test";

export const E2E_ORG_ID = "11111111-1111-1111-1111-111111111111";
export const E2E_BRANCH_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
export const E2E_HOME_ORG_ID = E2E_ORG_ID;

type MockGrantOptions = {
  mappedPosRoleCode?: string | null;
  productLocalRoleCode?: string | null;
  productAccessAllowed?: boolean;
  organizationManagementAuthority?: boolean;
  membershipRole?: string | null;
};

type SessionClassOptions = {
  accountClass?: "Personal" | "Organization" | "Platform";
  username?: string;
  displayName?: string;
  email?: string;
  homeOrganizationId?: string | null;
  organizationContextLocked?: boolean;
  organizations?: Array<{ organizationId: string; displayName: string; slug: string }>;
};

function sessionBody(opts: SessionClassOptions) {
  const accountClass = opts.accountClass ?? "Organization";
  return {
    sessionId: "22222222-2222-2222-2222-222222222222",
    username: opts.username ?? "cashier",
    displayName: opts.displayName ?? "Cashier One",
    email: opts.email ?? opts.username ?? "cashier",
    accountClass,
    homeOrganizationId:
      opts.homeOrganizationId === undefined
        ? accountClass === "Organization"
          ? E2E_HOME_ORG_ID
          : null
        : opts.homeOrganizationId,
    organizationContextLocked: opts.organizationContextLocked ?? accountClass === "Organization",
  };
}

async function mockBoundOrgSession(
  page: Page,
  sessionOpts: SessionClassOptions,
  grant: MockGrantOptions = {},
) {
  const {
    mappedPosRoleCode = "Cashier",
    productLocalRoleCode = "Cashier",
    productAccessAllowed = true,
    organizationManagementAuthority = false,
    membershipRole = null,
  } = grant;

  let loggedIn = false;
  const session = sessionBody({
    accountClass: "Organization",
    username: sessionOpts.username ?? "cashier",
    displayName: sessionOpts.displayName ?? "Cashier One",
    email: sessionOpts.email ?? "cashier@ORG000001",
    organizationContextLocked: sessionOpts.organizationContextLocked ?? true,
    homeOrganizationId: sessionOpts.homeOrganizationId,
  });

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
        body: JSON.stringify(session),
      });
    }

    if (url.includes("/api/v1/platform/auth/login") && method === "POST") {
      loggedIn = true;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          ...session,
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
      const csrf = route.request().headers()["x-xsrf-token"];
      if (!csrf) {
        return route.fulfill({
          status: 400,
          contentType: "application/json",
          body: JSON.stringify({ detail: "antiforgery token required" }),
        });
      }
      loggedIn = false;
      return route.fulfill({ status: 204, body: "" });
    }

    return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });

  await page.route("**/pos-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    if (url.includes("/api/v1/pos/operational-branch") && method === "PUT") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          organizationId: E2E_ORG_ID,
          branchId: E2E_BRANCH_ID,
          name: "Main Branch",
          deviceMatchesSelectedBranch: false,
          deviceBoundBranchId: null,
          openCashierShiftPresent: false,
        }),
      });
    }
    return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });
}

export async function mockBoundCashierSession(page: Page, grant: MockGrantOptions = {}) {
  await mockBoundOrgSession(
    page,
    {
      username: "cashier",
      displayName: "Cashier One",
      email: "cashier@ORG000001",
      organizationContextLocked: true,
    },
    {
      mappedPosRoleCode: "Cashier",
      productLocalRoleCode: "Cashier",
      organizationManagementAuthority: false,
      membershipRole: "OrganizationMember",
      ...grant,
    },
  );
}

export async function mockBoundOwnerSession(page: Page, grant: MockGrantOptions = {}) {
  await mockBoundOrgSession(
    page,
    {
      username: "owner",
      displayName: "Owner One",
      email: "owner@example.com",
      organizationContextLocked: false,
    },
    {
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      organizationManagementAuthority: true,
      membershipRole: "OrganizationOwner",
      ...grant,
    },
  );
}

export async function mockBoundManagerSession(page: Page, grant: MockGrantOptions = {}) {
  await mockBoundOrgSession(
    page,
    {
      username: "manager",
      displayName: "Manager One",
      email: "manager@ORG000001",
      organizationContextLocked: true,
    },
    {
      mappedPosRoleCode: "StoreManager",
      productLocalRoleCode: "Manager",
      organizationManagementAuthority: false,
      membershipRole: "OrganizationMember",
      ...grant,
    },
  );
}

export async function mockBoundOrgAdminSession(page: Page, grant: MockGrantOptions = {}) {
  await mockBoundOrgSession(
    page,
    {
      username: "orgadmin",
      displayName: "Org Admin",
      email: "admin@ORG000001",
      organizationContextLocked: true,
    },
    {
      mappedPosRoleCode: null,
      productLocalRoleCode: null,
      organizationManagementAuthority: true,
      membershipRole: "OrganizationAdministrator",
      productAccessAllowed: true,
      ...grant,
    },
  );
}

/** Personal AccountClass session with no eligible organizations. */
export async function mockPersonalSession(page: Page) {
  let loggedIn = false;
  const session = sessionBody({
    accountClass: "Personal",
    username: "paul@gmail.com",
    displayName: "Paul Personal",
    email: "paul@gmail.com",
    homeOrganizationId: null,
    organizationContextLocked: false,
  });

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
        body: JSON.stringify(session),
      });
    }

    if (url.includes("/api/v1/platform/auth/login") && method === "POST") {
      loggedIn = true;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ ...session, sessionToken: "must-not-persist" }),
      });
    }

    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([]),
      });
    }

    if (url.includes("/api/v1/platform/auth/logout") && method === "POST") {
      const csrf = route.request().headers()["x-xsrf-token"];
      if (!csrf) {
        return route.fulfill({
          status: 400,
          contentType: "application/json",
          body: JSON.stringify({ detail: "antiforgery token required" }),
        });
      }
      loggedIn = false;
      return route.fulfill({ status: 204, body: "" });
    }

    return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });
}

/** Staff principal using local@ORG###### login shape. */
export async function mockStaffOrgSession(page: Page, grant: MockGrantOptions = {}) {
  await mockBoundCashierSession(page, grant);
}

export async function signInAndBindCashier(page: Page) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill("cashier");
  await page.getByLabel("Password").fill("secret");
  await page.getByRole("button", { name: "Sign in" }).click();
}

export async function signInAndBindOwner(page: Page) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill("owner");
  await page.getByLabel("Password").fill("secret");
  await page.getByRole("button", { name: "Sign in" }).click();
}

export async function signInAndBindManager(page: Page) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill("manager");
  await page.getByLabel("Password").fill("secret");
  await page.getByRole("button", { name: "Sign in" }).click();
}

export async function signInAndBindOrgAdmin(page: Page) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill("orgadmin");
  await page.getByLabel("Password").fill("secret");
  await page.getByRole("button", { name: "Sign in" }).click();
}

export async function signInAsPersonal(page: Page) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill("paul@gmail.com");
  await page.getByLabel("Password").fill("secret");
  await page.getByRole("button", { name: "Sign in" }).click();
}

export async function signInAsStaffLogin(page: Page) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill("paul@ORG907757");
  await page.getByLabel("Password").fill("staff-secret");
  await page.getByRole("button", { name: "Sign in" }).click();
}

export async function clientNavigate(page: Page, path: string) {
  await page.evaluate((next) => {
    window.history.pushState({}, "", next);
    window.dispatchEvent(new PopStateEvent("popstate"));
  }, path);
}
