import { expect, type Page } from "@playwright/test";

export const E2E_ORG_ID = "11111111-1111-1111-1111-111111111111";
export const E2E_BRANCH_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
export const E2E_HOME_ORG_ID = E2E_ORG_ID;
/** The real login wire carries the platform user id; offline LocalStore scoping needs it. */
export const E2E_USER_ID = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
export const E2E_PERSONAL_USER_ID = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";

type MockGrantOptions = {
  mappedPosRoleCode?: string | null;
  productLocalRoleCode?: string | null;
  productAccessAllowed?: boolean;
  organizationManagementAuthority?: boolean;
  membershipRole?: string | null;
  extraBranches?: Array<{
    id: string;
    organizationId: string;
    code: string;
    name: string;
    isPrimary: boolean;
    status: string;
  }>;
};

type SessionClassOptions = {
  accountClass?: "Personal" | "Organization" | "Platform";
  userId?: string;
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
    // Production /auth/login and /auth/me both carry userId (PlatformLoginResultDto /
    // PlatformAuthSessionInfoDto). The offline LocalStore is scoped by it, so a fixture that
    // omitted it would silently exercise a no-offline-store path that no real session hits.
    userId: opts.userId ?? (accountClass === "Personal" ? E2E_PERSONAL_USER_ID : E2E_USER_ID),
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
    extraBranches = [],
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

    {
      const pathname = new URL(url).pathname.replace(/\/$/, "");
      if (pathname.endsWith(`/organizations/${E2E_ORG_ID}/branches`) && method === "GET") {
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
            ...extraBranches,
          ]),
        });
      }
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

    if (url.includes(`/organizations/${E2E_ORG_ID}/members`) && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0 }),
      });
    }

    // Default: browser installation is not registered (honest fail-closed for money).
    if (url.includes(`/organizations/${E2E_ORG_ID}/pos-devices/authorize`) && method === "POST") {
      return route.fulfill({
        status: 403,
        contentType: "application/json",
        body: JSON.stringify({
          detail: "This POS installation is not registered.",
          errorCode: "application.pos_device.not_authorized",
        }),
      });
    }

    if (
      url.includes(`/organizations/${E2E_ORG_ID}/pos-devices`) &&
      !url.includes("authorize") &&
      !url.includes("registration-tokens") &&
      method === "GET"
    ) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: "[]",
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

export async function waitForSellEntry(page: Page) {
  const sellFloor = page.getByTestId("sell-floor");
  const readinessDevice = page.getByTestId("sell-readiness-device");
  const readinessShift = page.getByTestId("sell-readiness-shift");
  await Promise.race([
    sellFloor.waitFor({ state: "visible", timeout: 15000 }),
    readinessDevice.waitFor({ state: "visible", timeout: 15000 }),
    readinessShift.waitFor({ state: "visible", timeout: 15000 }),
  ]);
}

export async function expectSellEntryVisible(page: Page) {
  await expect(
    page
      .getByTestId("sell-floor")
      .or(page.getByTestId("sell-readiness-device"))
      .or(page.getByTestId("sell-readiness-shift")),
  ).toBeVisible({ timeout: 15000 });
}

export async function completeOfflinePinSetupIfNeeded(page: Page, pin = "123456"): Promise<void> {
  const onSetup =
    page.url().includes("/offline-pin-setup") ||
    (await page.getByTestId("offline-pin-setup-page").isVisible().catch(() => false));
  if (!onSetup) {
    return;
  }
  await page.getByTestId("offline-pin-enroll-input").fill(pin);
  await page.getByTestId("offline-pin-enroll-confirm").fill(pin);
  await page.getByTestId("offline-pin-enroll-submit").click();
  await page.waitForURL((url) => !url.pathname.includes("/offline-pin-setup"), { timeout: 15000 });
}

export async function signInAndBindCashier(page: Page) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill("cashier");
  await page.getByRole("textbox", { name: "Password" }).fill("secret");
  await page.getByTestId("sign-in-submit").click();
  const sellFloor = page.getByTestId("sell-floor");
  const readinessDevice = page.getByTestId("sell-readiness-device");
  const readinessShift = page.getByTestId("sell-readiness-shift");
  const startSelling = page.getByTestId("workspace-destination-start_selling");
  const chooser = page.getByRole("heading", { name: "Choose workspace" });
  await Promise.race([
    sellFloor.waitFor({ state: "visible", timeout: 15000 }),
    readinessDevice.waitFor({ state: "visible", timeout: 15000 }),
    readinessShift.waitFor({ state: "visible", timeout: 15000 }),
    startSelling.waitFor({ state: "visible", timeout: 15000 }),
    chooser.waitFor({ state: "visible", timeout: 15000 }),
    page.getByTestId("offline-pin-setup-page").waitFor({ state: "visible", timeout: 15000 }),
  ]);
  await completeOfflinePinSetupIfNeeded(page);
  if (await startSelling.isVisible().catch(() => false)) {
    await startSelling.click();
  }
  await waitForSellEntry(page);
}

export async function signInAndBindOwner(page: Page) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill("owner");
  await page.getByRole("textbox", { name: "Password" }).fill("secret");
  await page.getByTestId("sign-in-submit").click();
}

export async function signInAndBindManager(page: Page) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill("manager");
  await page.getByRole("textbox", { name: "Password" }).fill("secret");
  await page.getByTestId("sign-in-submit").click();
  const ops = page.getByTestId("workspace-destination-operations");
  const openInventory = page.getByTestId("open-inventory");
  const managerHome = page.getByTestId("manager-home");
  const chooser = page.getByRole("heading", { name: "Choose workspace" });
  await Promise.race([
    ops.waitFor({ state: "visible", timeout: 15000 }),
    openInventory.waitFor({ state: "visible", timeout: 15000 }),
    managerHome.waitFor({ state: "visible", timeout: 15000 }),
    chooser.waitFor({ state: "visible", timeout: 15000 }),
    page.getByTestId("offline-pin-setup-page").waitFor({ state: "visible", timeout: 15000 }),
  ]);
  await completeOfflinePinSetupIfNeeded(page);
  await ops.waitFor({ state: "visible", timeout: 15000 });
  await ops.click();
  await Promise.race([
    openInventory.waitFor({ state: "visible", timeout: 15000 }),
    managerHome.waitFor({ state: "visible", timeout: 15000 }),
  ]);
}

export async function signInAndBindOrgAdmin(page: Page) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill("orgadmin");
  await page.getByRole("textbox", { name: "Password" }).fill("secret");
  await page.getByTestId("sign-in-submit").click();
  const orgPage = page.getByTestId("org-essentials-page");
  const manage = page.getByTestId("workspace-destination-manage_business");
  await Promise.race([
    orgPage.waitFor({ state: "visible", timeout: 15000 }),
    manage.waitFor({ state: "visible", timeout: 15000 }),
  ]);
  if (await manage.isVisible().catch(() => false)) {
    await manage.click();
  }
  await orgPage.waitFor({ state: "visible", timeout: 15000 });
}

/** After Owner lands on the experience chooser, bind Operations (branch-scoped). */
export async function chooseOwnerOperations(page: Page) {
  await page.getByTestId("workspace-destination-operations").click();
}

/** After Owner lands on the experience chooser, bind Manage Business (org-level). */
export async function chooseOwnerManageBusiness(page: Page) {
  await page.getByTestId("workspace-destination-manage_business").click();
}

export async function signInAsPersonal(page: Page) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill("paul@gmail.com");
  await page.getByRole("textbox", { name: "Password" }).fill("secret");
  await page.getByTestId("sign-in-submit").click();
}

export async function signInAsStaffLogin(page: Page) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill("paul@ORG907757");
  await page.getByRole("textbox", { name: "Password" }).fill("staff-secret");
  await page.getByTestId("sign-in-submit").click();
}

export async function clientNavigate(page: Page, path: string) {
  await page.evaluate((next) => {
    window.history.pushState({}, "", next);
    window.dispatchEvent(new PopStateEvent("popstate"));
  }, path);
}
