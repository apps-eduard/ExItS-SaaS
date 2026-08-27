/**
 * PERS-OWNERSHIP-01 — Personal ownership-transfer recipient UX (mock-bound).
 *
 * A) Accept: Personal User B sees pending Org A, accepts, success + Go to business.
 * B) Decline: pending disappears; transfer Declined; no org membership.
 * C) Privacy: User C cannot see B's transfer.
 * D) Account class: Org staff denied at /personal/ownership-transfers.
 * E) Multi-org story: after accept, User B has Org A; User A's remaining Org B noted in shared state.
 */
import { expect, test, type Page } from "@playwright/test";
import {
  clientNavigate,
  mockBoundOwnerSession,
  signInAndBindOwner,
} from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const USER_A = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const USER_B = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const USER_C = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const ORG_A = "11111111-1111-4111-8111-111111111111";
const ORG_B = "22222222-2222-4222-8222-222222222222";
const TRANSFER_ID = "33333333-3333-4333-8333-333333333333";
const BRANCH_A = "44444444-4444-4444-8444-444444444444";

type TransferState = {
  id: string;
  organizationId: string;
  organizationDisplayName: string;
  publicOrganizationId: string;
  fromOwnerUserId: string;
  toUserId: string;
  toDisplayName: string;
  toPublicUserId: string;
  status: string;
  createdAtUtc: string;
  expiresAtUtc: string;
  acceptedAtUtc: string | null;
  declinedAtUtc: string | null;
  cancelledAtUtc: string | null;
  completedAtUtc: string | null;
  updatedAtUtc: string;
};

type SharedOwnershipState = {
  transfer: TransferState | null;
  /** Org memberships by Personal user id after accept/decline. */
  orgsByUser: Record<string, Array<Record<string, unknown>>>;
  /** User A's remaining org after transferring Org A (multi-org story). */
  userARemainingOrgs: Array<Record<string, unknown>>;
};

function json(route: { fulfill: (r: object) => Promise<void> }, body: unknown, status = 200) {
  return route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

function createSharedState(): SharedOwnershipState {
  return {
    transfer: {
      id: TRANSFER_ID,
      organizationId: ORG_A,
      organizationDisplayName: "Org A Market",
      publicOrganizationId: "ORGAAAAAA",
      fromOwnerUserId: USER_A,
      toUserId: USER_B,
      toDisplayName: "User B",
      toPublicUserId: "EX-2222-3333",
      status: "Pending",
      createdAtUtc: "2026-08-20T00:00:00Z",
      expiresAtUtc: "2099-08-27T00:00:00Z",
      acceptedAtUtc: null,
      declinedAtUtc: null,
      cancelledAtUtc: null,
      completedAtUtc: null,
      updatedAtUtc: "2026-08-20T00:00:00Z",
    },
    orgsByUser: {
      [USER_B]: [],
      [USER_C]: [],
    },
    // E) After Org A transfer completes, User A still owns Org B in the story.
    userARemainingOrgs: [
      {
        organizationId: ORG_B,
        displayName: "Org B Bakery",
        publicOrganizationId: "ORGBBBBBB",
        role: "Owner",
      },
    ],
  };
}

function orgWorkspaceDto(org: Record<string, unknown>) {
  return {
    organizationId: org.organizationId,
    displayName: org.displayName,
    slug: org.slug ?? "org-a-market",
  };
}

async function installOwnershipMocks(
  page: Page,
  state: SharedOwnershipState,
  userId: string,
  opts: { email: string; displayName: string } = {
    email: "paul@gmail.com",
    displayName: "Paul Personal",
  },
) {
  let loggedIn = false;
  const session = {
    sessionId: "55555555-5555-4555-8555-555555555555",
    userId,
    accountClass: "Personal",
    username: opts.email,
    displayName: opts.displayName,
    email: opts.email,
    homeOrganizationId: null,
    organizationContextLocked: false,
    roles: [] as string[],
  };

  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return json(route, { headerName: "X-XSRF-TOKEN", token: "e2e-csrf" });
    }

    if (url.includes("/api/v1/platform/auth/me") && method === "GET") {
      if (!loggedIn) {
        return route.fulfill({ status: 401, contentType: "application/json", body: "{}" });
      }
      return json(route, session);
    }

    if (url.includes("/api/v1/platform/auth/login") && method === "POST") {
      loggedIn = true;
      return json(route, { ...session, sessionToken: "must-not-persist" });
    }

    if (url.includes("/api/v1/platform/auth/logout") && method === "POST") {
      loggedIn = false;
      return route.fulfill({ status: 204, body: "" });
    }

    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      const orgs = (state.orgsByUser[userId] ?? []).map(orgWorkspaceDto);
      return json(route, orgs);
    }

    if (url.includes("/api/v1/platform/organizations/") && url.includes("/branches") && method === "GET") {
      if (url.includes(ORG_A)) {
        return json(route, [
          {
            id: BRANCH_A,
            organizationId: ORG_A,
            code: "MAIN",
            name: "Main",
            isPrimary: true,
            status: "Active",
          },
        ]);
      }
      return json(route, []);
    }

    if (url.includes("/api/v1/platform/ownership-transfers/my-pending") && method === "GET") {
      if (!state.transfer || state.transfer.toUserId !== userId || state.transfer.status !== "Pending") {
        return json(route, []);
      }
      return json(route, [state.transfer]);
    }

    const acceptMatch = url.match(
      /\/api\/v1\/platform\/ownership-transfers\/([0-9a-fA-F-]{36})\/accept/,
    );
    if (acceptMatch && method === "POST") {
      const id = acceptMatch[1]!;
      if (!state.transfer || state.transfer.id !== id || state.transfer.toUserId !== userId) {
        return json(route, { detail: "forbidden", errorCode: "application.ownership_transfer.not_found" }, 404);
      }
      if (state.transfer.status !== "Pending") {
        return json(route, { detail: "conflict", errorCode: "application.ownership_transfer.conflict" }, 409);
      }
      state.transfer = {
        ...state.transfer,
        status: "Accepted",
        acceptedAtUtc: "2026-08-21T12:00:00Z",
        updatedAtUtc: "2026-08-21T12:00:00Z",
      };
      state.orgsByUser[userId] = [
        {
          organizationId: ORG_A,
          displayName: "Org A Market",
          publicOrganizationId: "ORGAAAAAA",
          role: "Owner",
        },
      ];
      return json(route, state.transfer);
    }

    const declineMatch = url.match(
      /\/api\/v1\/platform\/ownership-transfers\/([0-9a-fA-F-]{36})\/decline/,
    );
    if (declineMatch && method === "POST") {
      const id = declineMatch[1]!;
      if (!state.transfer || state.transfer.id !== id || state.transfer.toUserId !== userId) {
        return json(route, { detail: "forbidden" }, 403);
      }
      state.transfer = {
        ...state.transfer,
        status: "Declined",
        declinedAtUtc: "2026-08-21T12:00:00Z",
        updatedAtUtc: "2026-08-21T12:00:00Z",
      };
      return json(route, state.transfer);
    }

    if (url.includes("/api/v1/personal/dashboard") && method === "GET") {
      return json(route, {
        userIdentityId: userId,
        accountProfileId: userId,
        accountClass: "Personal",
        utangAvailable: true,
        contactCount: 0,
        activeRelationshipCount: 0,
        totalLentBalance: 0,
        totalBorrowedBalance: 0,
        pendingConfirmationCount: 0,
      });
    }

    if (url.includes("/api/v1/personal/notifications")) {
      return json(route, []);
    }

    if (url.includes("/api/v1/personal/") && method === "GET") {
      return json(route, []);
    }

    // Unmatched platform GETs should not hang the SPA.
    if (method === "GET") {
      return json(route, []);
    }

    return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });
}

async function signInPersonalAs(page: Page, email: string) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill(email);
  await page.getByRole("textbox", { name: "Password" }).fill("secret");
  await page.getByTestId("sign-in-submit").click();
}

test.describe("PERS-OWNERSHIP-01", () => {
  test("A) Accept ownership transfer and offer Go to business", async ({ page }) => {
    const state = createSharedState();
    await installOwnershipMocks(page, state, USER_B, {
      email: "bob@example.com",
      displayName: "User B",
    });
    await signInPersonalAs(page, "bob@example.com");
    await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 15000 });

    await clientNavigate(page, "/personal/ownership-transfers");
    await expect(page.getByTestId("personal-ownership-transfers-page")).toBeVisible();
    await expect(page.getByTestId("ownership-transfer-card")).toBeVisible({ timeout: 15000 });
    await expect(page.getByText("Org A Market")).toBeVisible();

    await page.getByTestId("ownership-transfer-accept").click();
    await expect(page.getByTestId("ownership-transfer-accept-confirm")).toBeVisible();

    const acceptResponsePromise = page.waitForResponse(
      (res) =>
        res.url().includes("/ownership-transfers/") &&
        res.url().includes("/accept") &&
        res.request().method() === "POST",
      { timeout: 15000 },
    );
    await page.getByTestId("ownership-transfer-accept-submit").click();
    const acceptResponse = await acceptResponsePromise;
    const acceptBody = await acceptResponse.text();
    expect(acceptResponse.status(), `accept body: ${acceptBody}`).toBe(200);

    await expect(page.getByTestId("ownership-transfer-success")).toBeVisible({ timeout: 15000 });
    expect(state.transfer?.status).toBe("Accepted");
    expect(state.orgsByUser[USER_B]?.[0]?.organizationId).toBe(ORG_A);
    // E) User A's remaining Org B still present in shared story state.
    expect(state.userARemainingOrgs[0]?.organizationId).toBe(ORG_B);

    await expect(page.getByTestId("ownership-go-to-business")).toBeVisible();
    await expect(page.getByTestId("ownership-stay-personal")).toBeVisible();
  });

  test("B) Decline ownership transfer removes pending and keeps no org", async ({ page }) => {
    const state = createSharedState();
    await installOwnershipMocks(page, state, USER_B, {
      email: "bob@example.com",
      displayName: "User B",
    });
    await signInPersonalAs(page, "bob@example.com");
    await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 15000 });
    await clientNavigate(page, "/personal/ownership-transfers");
    await expect(page.getByTestId("ownership-transfer-card")).toBeVisible({ timeout: 15000 });

    await page.getByTestId("ownership-transfer-decline").click();
    await expect(page.getByTestId("ownership-transfer-decline-confirm")).toBeVisible();
    await page.getByTestId("ownership-transfer-decline-submit").click();

    await expect(page.getByTestId("ownership-transfer-empty")).toBeVisible({ timeout: 15000 });
    expect(state.transfer?.status).toBe("Declined");
    expect(state.orgsByUser[USER_B]).toEqual([]);
  });

  test("C) Privacy — User C cannot see User B's transfer", async ({ browser }) => {
    const state = createSharedState();
    const contextB = await browser.newContext();
    const contextC = await browser.newContext();
    const pageB = await contextB.newPage();
    const pageC = await contextC.newPage();

    await installOwnershipMocks(pageB, state, USER_B, {
      email: "bob@example.com",
      displayName: "User B",
    });
    await installOwnershipMocks(pageC, state, USER_C, {
      email: "cara@example.com",
      displayName: "User C",
    });

    await signInPersonalAs(pageB, "bob@example.com");
    await expect(pageB.getByTestId("personal-shell")).toBeVisible({ timeout: 15000 });
    await clientNavigate(pageB, "/personal/ownership-transfers");
    await expect(pageB.getByTestId("ownership-transfer-card")).toBeVisible({ timeout: 15000 });

    await signInPersonalAs(pageC, "cara@example.com");
    await expect(pageC.getByTestId("personal-shell")).toBeVisible({ timeout: 15000 });
    await clientNavigate(pageC, "/personal/ownership-transfers");
    await expect(pageC.getByTestId("ownership-transfer-empty")).toBeVisible({ timeout: 15000 });
    await expect(pageC.getByText("Org A Market")).toHaveCount(0);

    await contextB.close();
    await contextC.close();
  });

  test("D) Org staff cannot open Personal ownership transfers", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await signInAndBindOwner(page);
    await page.goto("/personal/ownership-transfers");
    await expect(page.getByTestId("account-class-denied")).toBeVisible({ timeout: 15000 });
  });
});
