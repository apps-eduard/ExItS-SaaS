/**
 * Canonical Organization / Personal test session factories.
 *
 * Rules:
 * - Organization tests must use createOrganization* helpers (accountClass=Organization).
 * - Personal tests must use createPersonal* helpers (accountClass=Personal).
 * - Never default ambiguously between classes.
 *
 * Platform HTTP reads bodies via response.text() (see platform-http.ts). Always use
 * jsonResponse() so both .text() and .json() return the same payload — empty text()
 * mocks silently drop accountClass and trip RequireAccountClass.
 */
import { vi } from "vitest";
import type { BrowserSessionSnapshot } from "@/api/platform/browser-session";
import { setPosSessionGrant } from "@/api/platform/pos-session-grant";
import { INSTALLATION_DEVICE_ID_STORAGE_KEY } from "@/workspace/browser-installation-identity";

/** Platform HTTP parses via response.text(); keep text + json consistent. */
export function jsonResponse(status: number, body: unknown, delayMs = 0): Promise<Response> {
  const response = {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
    text: async () => (body === null || body === undefined ? "" : JSON.stringify(body)),
  } as Response;

  if (delayMs === 0) {
    return Promise.resolve(response);
  }

  return new Promise((resolve) => {
    window.setTimeout(() => resolve(response), delayMs);
  });
}

/** Distinct deterministic IDs — do not reuse across unrelated identities. */
export const TEST_ORG_A_ID = "11111111-1111-1111-1111-111111111111";
export const TEST_ORG_B_ID = "22222222-2222-2222-2222-222222222222";
export const TEST_BRANCH_A_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
export const TEST_BRANCH_B_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
export const TEST_ACTOR_ID = "dddddddd-dddd-dddd-dddd-dddddddddddd";
export const TEST_SESSION_ID = "11111111-1111-1111-1111-111111111111";
export const TEST_INSTALL_ID = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";
export const TEST_DEVICE_ID = "ffffffff-ffff-4fff-8fff-ffffffffffff";
export const TEST_SHIFT_ID = "cccccccc-cccc-cccc-cccc-cccccccccccc";
export const TEST_REGISTER_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
export const TEST_PERSONAL_USER_ID = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
export const TEST_PERSONAL_SESSION_ID = "99999999-9999-4999-8999-999999999999";

export type OrganizationTestContextOptions = {
  organizationId?: string;
  organizationName?: string;
  branchId?: string | null;
  branchName?: string;
  actorId?: string;
  userId?: string;
  displayName?: string;
  username?: string;
  email?: string;
  role?: string;
  productAccessAllowed?: boolean;
  organizationContextLocked?: boolean;
  openShift?: boolean;
  deviceAuthorized?: boolean;
};

export type PersonalTestContextOptions = {
  userId?: string;
  displayName?: string;
  username?: string;
  email?: string;
};

export type BoundWorkspaceTestShape = {
  organizationId: string;
  organizationDisplayName: string;
  branchId: string | null;
  branchName: string | null;
  experience: "manage_business" | "operations" | "start_selling";
};

export function createOrganizationSessionSnapshot(
  options: OrganizationTestContextOptions = {},
): BrowserSessionSnapshot {
  const organizationId = options.organizationId ?? TEST_ORG_A_ID;
  return {
    sessionId: TEST_SESSION_ID,
    userId: options.userId ?? options.actorId ?? TEST_ACTOR_ID,
    username: options.username ?? "org-cashier",
    displayName: options.displayName ?? "Org Cashier",
    email: options.email ?? "cashier@ORG000001",
    selectedOrganizationId: organizationId,
    accountClass: "Organization",
    homeOrganizationId: organizationId,
    organizationContextLocked: options.organizationContextLocked ?? true,
  };
}

export function createPersonalSessionSnapshot(
  options: PersonalTestContextOptions = {},
): BrowserSessionSnapshot {
  return {
    sessionId: TEST_PERSONAL_SESSION_ID,
    userId: options.userId ?? TEST_PERSONAL_USER_ID,
    username: options.username ?? "personal.user",
    displayName: options.displayName ?? "Personal User",
    email: options.email ?? "personal@example.com",
    selectedOrganizationId: null,
    accountClass: "Personal",
    homeOrganizationId: null,
    organizationContextLocked: false,
  };
}

export function createOrganizationBoundWorkspace(
  options: OrganizationTestContextOptions = {},
): BoundWorkspaceTestShape {
  const branchId = options.branchId === undefined ? TEST_BRANCH_A_ID : options.branchId;
  return {
    organizationId: options.organizationId ?? TEST_ORG_A_ID,
    organizationDisplayName: options.organizationName ?? "Kizy Store",
    branchId,
    branchName: branchId ? (options.branchName ?? "Main Branch") : null,
    experience: branchId ? "start_selling" : "manage_business",
  };
}

export function seedOrganizationSellReadyLocalState(
  options: OrganizationTestContextOptions = {},
): void {
  const role = options.role ?? "Cashier";
  window.localStorage.setItem(INSTALLATION_DEVICE_ID_STORAGE_KEY, TEST_INSTALL_ID);
  setPosSessionGrant({
    accessToken: "in-memory-only",
    productAccessAllowed: options.productAccessAllowed ?? true,
    mappedPosRoleCode: role,
    productLocalRoleCode: role,
  });
}

type FetchHandler = (
  url: string,
  method: string,
  init?: RequestInit,
) => Promise<Response | null> | Response | null;

/**
 * Minimal Organization Platform session: auth/me + antiforgery + orgs/branches/token.
 * Does not include Sell readiness (device/shift/catalog).
 */
export function createOrganizationPlatformFetch(
  options: OrganizationTestContextOptions = {},
  extra?: FetchHandler,
) {
  const organizationId = options.organizationId ?? TEST_ORG_A_ID;
  const branchId = options.branchId === undefined ? TEST_BRANCH_A_ID : options.branchId;
  const session = createOrganizationSessionSnapshot(options);
  const orgName = options.organizationName ?? "Kizy Store";
  const branchName = options.branchName ?? "Main Branch";
  const role = options.role ?? "Cashier";
  const productAccessAllowed = options.productAccessAllowed ?? true;

  return vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (extra) {
      const handled = await extra(url, method, init);
      if (handled) {
        return handled;
      }
    }

    if (url.includes("/api/v1/platform/auth/me")) {
      return jsonResponse(200, session);
    }
    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
    }
    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      return jsonResponse(200, [
        {
          organizationId,
          displayName: orgName,
          slug: "kizy-store",
        },
      ]);
    }
    if (url.includes(`/organizations/${organizationId}/branches`) && method === "GET") {
      if (!branchId) {
        return jsonResponse(200, []);
      }
      return jsonResponse(200, [
        {
          id: branchId,
          organizationId,
          code: "MAIN",
          name: branchName,
          isPrimary: true,
          status: "Active",
        },
      ]);
    }
    if (url.includes("/api/v1/platform/auth/organization-context") && method === "PUT") {
      return jsonResponse(204, null);
    }
    if (url.includes(`/organizations/${organizationId}/branch-context`) && method === "PUT") {
      return jsonResponse(204, null);
    }
    if (url.includes("/api/v1/platform/auth/token") && method === "POST") {
      return jsonResponse(200, {
        accessToken: "in-memory-only",
        productAccessAllowed,
        mappedPosRoleCode: role,
        productLocalRoleCode: role,
        membershipRole: "OrganizationMember",
      });
    }
    if (url.includes("/api/v1/platform/auth/logout") && method === "POST") {
      return jsonResponse(204, null);
    }

    return jsonResponse(404, { detail: "not mocked" });
  });
}

/**
 * Organization session + branch bind + device authorize + open shift + optional POS handlers.
 * Use for Sell Floor / account-shell routes that need CreateSale readiness.
 */
export function createOrganizationSellReadyFetch(
  options: OrganizationTestContextOptions & {
    catalogCategories?: unknown;
    catalogProducts?: (url: string) => unknown;
    onPosRequest?: FetchHandler;
  } = {},
) {
  const organizationId = options.organizationId ?? TEST_ORG_A_ID;
  const branchId = options.branchId === undefined ? TEST_BRANCH_A_ID : options.branchId;
  const actorId = options.actorId ?? TEST_ACTOR_ID;
  const openShift = options.openShift ?? true;
  const deviceAuthorized = options.deviceAuthorized ?? true;
  const branchName = options.branchName ?? "Main Branch";

  return createOrganizationPlatformFetch(options, async (url, method, init) => {
    if (options.onPosRequest) {
      const handled = await options.onPosRequest(url, method, init);
      if (handled) {
        return handled;
      }
    }

    if (url.includes("/pos-api/")) {
      if (url.includes("/operational-branch") && method === "PUT") {
        return jsonResponse(200, {
          organizationId,
          branchId,
          name: branchName,
          deviceMatchesSelectedBranch: deviceAuthorized,
          deviceBoundBranchId: deviceAuthorized ? branchId : null,
          openCashierShiftPresent: openShift,
        });
      }

      if (url.includes("/cashier-shifts/current") && method === "GET") {
        if (!openShift || !branchId) {
          return jsonResponse(404, { detail: "no open shift" });
        }
        return jsonResponse(200, {
          shiftId: TEST_SHIFT_ID,
          organizationId,
          shiftNumber: "S-1",
          status: "Open",
          actorId,
          registerId: TEST_REGISTER_ID,
          registerCode: "REG-1",
          registerName: "Front",
          businessDate: "2026-08-21",
          openingCashAmount: 100,
          openingCashCounted: true,
          effectiveCashCountMode: "Required",
          openedAtUtc: "2026-08-21T01:00:00Z",
          openedBy: actorId,
          createdAtUtc: "2026-08-21T01:00:00Z",
          updatedAtUtc: "2026-08-21T01:00:00Z",
        });
      }

      if (url.includes("/catalog/categories") && options.catalogCategories !== undefined) {
        return jsonResponse(200, options.catalogCategories);
      }

      if (url.includes("/catalog/products") && options.catalogProducts) {
        return jsonResponse(200, options.catalogProducts(url));
      }

      return jsonResponse(404, { detail: "not mocked" });
    }

    if (url.includes("/pos-devices/authorize") && method === "POST") {
      if (!deviceAuthorized || !branchId) {
        return jsonResponse(403, { detail: "device not authorized" });
      }
      return jsonResponse(200, {
        posDeviceId: TEST_DEVICE_ID,
        branchId,
        installationDeviceId: TEST_INSTALL_ID,
      });
    }

    return null;
  });
}

/** Personal Platform session fetch — never includes Organization accountClass. */
export function createPersonalPlatformFetch(
  options: PersonalTestContextOptions = {},
  extra?: FetchHandler,
) {
  const session = createPersonalSessionSnapshot(options);

  return vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (extra) {
      const handled = await extra(url, method, init);
      if (handled) {
        return handled;
      }
    }

    if (url.includes("/api/v1/platform/auth/me")) {
      return jsonResponse(200, session);
    }
    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
    }
    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      return jsonResponse(200, []);
    }
    if (url.includes("/api/v1/platform/auth/logout") && method === "POST") {
      return jsonResponse(204, null);
    }

    return jsonResponse(404, { detail: "not mocked" });
  });
}
