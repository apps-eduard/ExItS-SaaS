/**
 * PERS-SETTLE-01 — Personal Utang settle / close UX (mock-bound).
 *
 * A) Private settlement completes immediately.
 * B) Linked two-BrowserContext: User A settles → User B confirms → User A sees Settled.
 */
import { expect, test, type Page } from "@playwright/test";
import { E2E_PERSONAL_USER_ID } from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const USER_A = E2E_PERSONAL_USER_ID;
const USER_B = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const CONTACT_PRIVATE = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const CONTACT_LINKED = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const REL_PRIVATE = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const REL_LINKED = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const ENTRY_LOAN = "11111111-1111-4111-8111-111111111111";
const ENTRY_SETTLE = "22222222-2222-4222-8222-222222222222";

type EntryState = {
  id: string;
  relationshipId: string;
  entryType: string;
  amount: number;
  signedDelta: number;
  balanceAfter: number;
  notes: string | null;
  dueDateUtc: string | null;
  createdByUserIdentityId: string;
  createdAtUtc: string;
  status: string;
  resolvedByUserIdentityId: string | null;
  resolvedAtUtc: string | null;
  disputeReason: string | null;
  canConfirm: boolean;
  canDispute: boolean;
  canCancel: boolean;
  affectsBalance: boolean;
  isSharedLedger: boolean;
  intent: string;
  settlementBalanceSnapshot: number | null;
  isSettlement: boolean;
};

type RelState = {
  id: string;
  perspectiveFor: Record<string, string>;
  creditorUserIdentityId: string | null;
  creditorContactId: string | null;
  debtorUserIdentityId: string | null;
  debtorContactId: string | null;
  currencyCode: string;
  currentBalance: number;
  dueDateUtc: string | null;
  status: string;
  version: number;
  updatedAtUtc: string;
  isSharedLedger: boolean;
  isPrivate: boolean;
};

type SharedSettleState = {
  privateRel: RelState;
  linkedRel: RelState;
  privateHistory: EntryState[];
  linkedHistory: EntryState[];
  contactsByUser: Record<string, Array<Record<string, unknown>>>;
};

function json(route: { fulfill: (r: object) => Promise<void> }, body: unknown, status = 200) {
  return route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

function createPrivateState(): SharedSettleState {
  return {
    privateRel: {
      id: REL_PRIVATE,
      perspectiveFor: { [USER_A]: "Lent" },
      creditorUserIdentityId: USER_A,
      creditorContactId: null,
      debtorUserIdentityId: null,
      debtorContactId: CONTACT_PRIVATE,
      currencyCode: "PHP",
      currentBalance: 150,
      dueDateUtc: null,
      status: "Active",
      version: 2,
      updatedAtUtc: "2026-08-21T00:00:00Z",
      isSharedLedger: false,
      isPrivate: true,
    },
    linkedRel: {
      id: REL_LINKED,
      perspectiveFor: { [USER_A]: "Lent", [USER_B]: "Borrowed" },
      creditorUserIdentityId: USER_A,
      creditorContactId: null,
      debtorUserIdentityId: USER_B,
      debtorContactId: CONTACT_LINKED,
      currencyCode: "PHP",
      currentBalance: 200,
      dueDateUtc: null,
      status: "Active",
      version: 3,
      updatedAtUtc: "2026-08-21T00:00:00Z",
      isSharedLedger: true,
      isPrivate: false,
    },
    privateHistory: [
      {
        id: ENTRY_LOAN,
        relationshipId: REL_PRIVATE,
        entryType: "Loan",
        amount: 150,
        signedDelta: 150,
        balanceAfter: 150,
        notes: "Lunch",
        dueDateUtc: null,
        createdByUserIdentityId: USER_A,
        createdAtUtc: "2026-08-20T00:00:00Z",
        status: "Confirmed",
        resolvedByUserIdentityId: null,
        resolvedAtUtc: null,
        disputeReason: null,
        canConfirm: false,
        canDispute: false,
        canCancel: false,
        affectsBalance: true,
        isSharedLedger: false,
        intent: "Regular",
        settlementBalanceSnapshot: null,
        isSettlement: false,
      },
    ],
    linkedHistory: [
      {
        id: ENTRY_LOAN,
        relationshipId: REL_LINKED,
        entryType: "Loan",
        amount: 200,
        signedDelta: 200,
        balanceAfter: 200,
        notes: "Shared loan",
        dueDateUtc: null,
        createdByUserIdentityId: USER_A,
        createdAtUtc: "2026-08-20T00:00:00Z",
        status: "Confirmed",
        resolvedByUserIdentityId: USER_B,
        resolvedAtUtc: "2026-08-20T01:00:00Z",
        disputeReason: null,
        canConfirm: false,
        canDispute: false,
        canCancel: false,
        affectsBalance: true,
        isSharedLedger: true,
        intent: "Regular",
        settlementBalanceSnapshot: null,
        isSettlement: false,
      },
    ],
    contactsByUser: {
      [USER_A]: [
        {
          id: CONTACT_PRIVATE,
          displayName: "Walk-in Ana",
          phone: null,
          email: null,
          linkedUserIdentityId: null,
          publicUserId: null,
          status: "Active",
          createdAtUtc: "2026-08-20T00:00:00Z",
        },
        {
          id: CONTACT_LINKED,
          displayName: "Linked Ben",
          phone: null,
          email: null,
          linkedUserIdentityId: USER_B,
          publicUserId: "EX-BEN-001",
          status: "Active",
          createdAtUtc: "2026-08-20T00:00:00Z",
        },
      ],
      [USER_B]: [
        {
          id: CONTACT_LINKED,
          displayName: "Paul",
          phone: null,
          email: null,
          linkedUserIdentityId: USER_A,
          publicUserId: "EX-PAUL-001",
          status: "Active",
          createdAtUtc: "2026-08-20T00:00:00Z",
        },
      ],
    },
  };
}

function relationshipDto(rel: RelState, userId: string) {
  return {
    id: rel.id,
    perspective: rel.perspectiveFor[userId] ?? "Lent",
    creditorUserIdentityId: rel.creditorUserIdentityId,
    creditorContactId: rel.creditorContactId,
    debtorUserIdentityId: rel.debtorUserIdentityId,
    debtorContactId: rel.debtorContactId,
    currencyCode: rel.currencyCode,
    currentBalance: rel.currentBalance,
    dueDateUtc: rel.dueDateUtc,
    status: rel.status,
    version: rel.version,
    updatedAtUtc: rel.updatedAtUtc,
    isSharedLedger: rel.isSharedLedger,
    isPrivate: rel.isPrivate,
  };
}

function historyForActor(entries: EntryState[], userId: string): EntryState[] {
  return entries.map((entry) => {
    if (entry.status !== "Pending") return entry;
    const isAuthor = entry.createdByUserIdentityId === userId;
    return {
      ...entry,
      canConfirm: !isAuthor && entry.isSharedLedger,
      canDispute: !isAuthor && entry.isSharedLedger,
      canCancel: isAuthor && entry.isSharedLedger,
    };
  });
}

async function installUtangMocks(page: Page, state: SharedSettleState, userId: string) {
  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return json(route, { headerName: "X-XSRF-TOKEN", token: "e2e-csrf" });
    }

    if (url.includes("/api/v1/personal/me") && method === "GET") {
      return json(route, { userIdentityId: userId });
    }

    if (url.includes("/api/v1/personal/dashboard") && method === "GET") {
      return json(route, {
        userIdentityId: userId,
        accountProfileId: "33333333-3333-4333-8333-333333333333",
        accountClass: "Personal",
        utangAvailable: true,
        contactCount: 1,
        activeRelationshipCount: 1,
        totalLentBalance: userId === USER_A ? state.linkedRel.currentBalance + state.privateRel.currentBalance : 0,
        totalBorrowedBalance: userId === USER_B ? state.linkedRel.currentBalance : 0,
        pendingConfirmationCount: 0,
      });
    }

    if (url.includes("/api/v1/personal/utang/contacts") && method === "GET") {
      return json(route, state.contactsByUser[userId] ?? []);
    }

    if (url.includes("/relationships/lent") && method === "GET") {
      const rows = [];
      if (userId === USER_A) {
        rows.push(relationshipDto(state.privateRel, userId));
        rows.push(relationshipDto(state.linkedRel, userId));
      }
      return json(route, rows);
    }

    if (url.includes("/relationships/borrowed") && method === "GET") {
      const rows = [];
      if (userId === USER_B) {
        rows.push(relationshipDto(state.linkedRel, userId));
      }
      return json(route, rows);
    }

    const relMatch = url.match(/\/relationships\/([0-9a-fA-F-]{36})(?:\/(balance|history|settle|close|entries))?/);
    if (relMatch) {
      const relId = relMatch[1]!;
      const action = relMatch[2];
      const rel =
        relId === REL_PRIVATE ? state.privateRel : relId === REL_LINKED ? state.linkedRel : null;
      if (!rel) return json(route, {}, 404);

      if (!action && method === "GET") {
        return json(route, relationshipDto(rel, userId));
      }
      if (action === "balance" && method === "GET") {
        return json(route, {
          relationshipId: rel.id,
          currentBalance: rel.currentBalance,
          currencyCode: rel.currencyCode,
          version: rel.version,
          updatedAtUtc: rel.updatedAtUtc,
        });
      }
      if (action === "history" && method === "GET") {
        const history = relId === REL_PRIVATE ? state.privateHistory : state.linkedHistory;
        return json(route, historyForActor(history, userId));
      }
      if (action === "settle" && method === "POST") {
        const body = route.request().postDataJSON() as {
          expectedVersion?: number;
          settlementEntryId?: string;
        };
        const settlementId = body.settlementEntryId || ENTRY_SETTLE;
        const amount = rel.currentBalance;
        if (rel.isSharedLedger) {
          const settlement: EntryState = {
            id: settlementId,
            relationshipId: rel.id,
            entryType: "Payment",
            amount,
            signedDelta: -amount,
            balanceAfter: amount,
            notes: null,
            dueDateUtc: null,
            createdByUserIdentityId: userId,
            createdAtUtc: "2026-08-21T04:00:00Z",
            status: "Pending",
            resolvedByUserIdentityId: null,
            resolvedAtUtc: null,
            disputeReason: null,
            canConfirm: false,
            canDispute: false,
            canCancel: true,
            affectsBalance: false,
            isSharedLedger: true,
            intent: "Settlement",
            settlementBalanceSnapshot: amount,
            isSettlement: true,
          };
          state.linkedHistory = [settlement, ...state.linkedHistory];
          rel.version += 1;
          rel.updatedAtUtc = "2026-08-21T04:00:00Z";
          return json(route, {
            outcome: "AwaitingCounterpartyConfirmation",
            relationship: relationshipDto(rel, userId),
            settlementEntry: historyForActor([settlement], userId)[0],
          });
        }

        const settlement: EntryState = {
          id: settlementId,
          relationshipId: rel.id,
          entryType: "Payment",
          amount,
          signedDelta: -amount,
          balanceAfter: 0,
          notes: null,
          dueDateUtc: null,
          createdByUserIdentityId: userId,
          createdAtUtc: "2026-08-21T04:00:00Z",
          status: "Confirmed",
          resolvedByUserIdentityId: null,
          resolvedAtUtc: null,
          disputeReason: null,
          canConfirm: false,
          canDispute: false,
          canCancel: false,
          affectsBalance: true,
          isSharedLedger: false,
          intent: "Settlement",
          settlementBalanceSnapshot: amount,
          isSettlement: true,
        };
        state.privateHistory = [settlement, ...state.privateHistory];
        rel.currentBalance = 0;
        rel.status = "Closed";
        rel.version += 1;
        rel.updatedAtUtc = "2026-08-21T04:00:00Z";
        return json(route, {
          outcome: "Completed",
          relationship: relationshipDto(rel, userId),
          settlementEntry: settlement,
        });
      }
      if (action === "close" && method === "POST") {
        rel.currentBalance = 0;
        rel.status = "Closed";
        rel.version += 1;
        return json(route, {
          outcome: "Closed",
          relationship: relationshipDto(rel, userId),
        });
      }

      const entryConfirm = url.match(
        /\/relationships\/([0-9a-fA-F-]{36})\/entries\/([0-9a-fA-F-]{36})\/confirm/,
      );
      if (entryConfirm && method === "POST") {
        const entryId = entryConfirm[2]!;
        const entry = state.linkedHistory.find((e) => e.id === entryId);
        if (!entry) return json(route, {}, 404);
        entry.status = "Confirmed";
        entry.affectsBalance = true;
        entry.canConfirm = false;
        entry.canDispute = false;
        entry.canCancel = false;
        entry.resolvedByUserIdentityId = userId;
        entry.resolvedAtUtc = "2026-08-21T05:00:00Z";
        entry.balanceAfter = 0;
        if (entry.isSettlement) {
          state.linkedRel.currentBalance = 0;
          state.linkedRel.status = "Closed";
          state.linkedRel.version += 1;
          state.linkedRel.updatedAtUtc = "2026-08-21T05:00:00Z";
        }
        return json(route, historyForActor([entry], userId)[0]);
      }
    }

    if (
      url.includes("/api/v1/personal/notifications")
      || url.includes("/api/v1/me/public-identity")
      || url.includes("/api/v1/personal/linked-merchants")
      || url.includes("/api/v1/personal/customer-link-requests")
      || url.includes("/api/v1/personal/todo")
    ) {
      return json(route, Array.isArray([]) ? [] : {});
    }

    return route.fallback();
  });
}

async function mockPersonalUtangSession(page: Page, userId: string, email: string, displayName: string) {
  let loggedIn = false;
  const personalMe = {
    sessionId: "22222222-2222-2222-2222-222222222222",
    userId,
    username: email,
    displayName,
    email,
    accountClass: "Personal",
    homeOrganizationId: null,
    organizationContextLocked: false,
  };

  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return json(route, { headerName: "X-XSRF-TOKEN", token: "e2e-csrf" });
    }

    if (url.includes("/api/v1/platform/auth/me") && method === "GET") {
      if (!loggedIn) return json(route, {}, 401);
      return json(route, personalMe);
    }

    if (url.includes("/api/v1/platform/auth/login") && method === "POST") {
      loggedIn = true;
      return json(route, { ...personalMe, sessionToken: "must-not-persist" });
    }

    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      return json(route, []);
    }

    if (url.includes("/api/v1/platform/auth/token") && method === "POST") {
      return json(route, {
        accessToken: "e2e-personal-token",
        productAccessAllowed: false,
      });
    }

    if (url.includes("/api/v1/platform/auth/logout") && method === "POST") {
      loggedIn = false;
      return route.fulfill({ status: 204, body: "" });
    }

    return route.fallback();
  });
}

async function signInPersonalAs(page: Page, email: string) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill(email);
  await page.getByRole("textbox", { name: "Password" }).fill("secret");
  await page.getByTestId("sign-in-submit").click();
}

test.describe("PERS-SETTLE-01 Personal Utang settle", () => {
  test("A: private settlement completes and shows Settled with history", async ({ page }) => {
    const state = createPrivateState();
    await mockPersonalUtangSession(page, USER_A, "paul@gmail.com", "Paul Personal");
    await installUtangMocks(page, state, USER_A);
    await signInPersonalAs(page, "paul@gmail.com");
    await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 15000 });
    await page.goto(`/personal/utang/relationships/${REL_PRIVATE}`);

    await expect(page.getByTestId("personal-utang-detail")).toBeVisible({ timeout: 15000 });
    await expect(page.getByTestId("utang-detail-status")).toHaveText("Active");
    await page.getByTestId("utang-settle").click();
    await page.getByTestId("utang-settle-confirm").click();

    await expect(page.getByTestId("utang-detail-status")).toHaveText("Settled");
    await expect(page.getByTestId("utang-history")).toBeVisible();
    await expect(page.getByTestId("utang-history")).toContainText("Settlement");
    await expect(page.getByTestId("utang-entry-type")).toHaveCount(0);
  });

  test("B: linked settle awaits counterparty confirm then closes", async ({ browser }) => {
    const state = createPrivateState();
    const contextA = await browser.newContext();
    const contextB = await browser.newContext();
    const pageA = await contextA.newPage();
    const pageB = await contextB.newPage();

    await mockPersonalUtangSession(pageA, USER_A, "paul@gmail.com", "Paul Personal");
    await installUtangMocks(pageA, state, USER_A);
    await mockPersonalUtangSession(pageB, USER_B, "ben@example.com", "Linked Ben");
    await installUtangMocks(pageB, state, USER_B);

    await signInPersonalAs(pageA, "paul@gmail.com");
    await expect(pageA.getByTestId("personal-shell")).toBeVisible({ timeout: 15000 });
    await pageA.goto(`/personal/utang/relationships/${REL_LINKED}`);
    await expect(pageA.getByTestId("utang-settle")).toBeVisible({ timeout: 15000 });
    await pageA.getByTestId("utang-settle").click();
    await pageA.getByTestId("utang-settle-confirm").click();
    await expect(pageA.getByTestId("utang-settle-awaiting")).toBeVisible();

    await signInPersonalAs(pageB, "ben@example.com");
    await expect(pageB.getByTestId("personal-shell")).toBeVisible({ timeout: 15000 });
    await pageB.goto(`/personal/utang/relationships/${REL_LINKED}`);
    const confirmButton = pageB.locator('[data-testid^="utang-confirm-"]');
    await expect(confirmButton).toBeVisible({ timeout: 15000 });
    await confirmButton.click();

    await pageA.goto(`/personal/utang/relationships/${REL_LINKED}`);
    await expect(pageA.getByTestId("utang-detail-status")).toHaveText("Settled");
    await expect(pageA.getByTestId("utang-settle-awaiting")).toHaveCount(0);

    await contextA.close();
    await contextB.close();
  });
});
