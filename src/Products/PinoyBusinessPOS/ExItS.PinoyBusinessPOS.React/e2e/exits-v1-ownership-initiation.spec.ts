/**
 * EXITS-V1-CLOSURE-01 — Org ownership initiation + Personal recipient accept/cancel (mock).
 */
import { expect, test, type Page } from "@playwright/test";
import {
  chooseOwnerManageBusiness,
  clientNavigate,
  E2E_ORG_ID,
  mockBoundOwnerSession,
  signInAndBindOwner,
} from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const USER_A = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const USER_B = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const ORG_A = E2E_ORG_ID;
const ORG_B = "22222222-2222-4222-8222-222222222222";
const TRANSFER_ID = "33333333-3333-4333-8333-333333333333";

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

type SharedState = {
  transfer: TransferState | null;
  orgsByUser: Record<string, Array<Record<string, unknown>>>;
};

function json(route: { fulfill: (r: object) => Promise<void> }, body: unknown, status = 200) {
  return route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

function createState(): SharedState {
  return {
    transfer: null,
    orgsByUser: {
      [USER_A]: [
        {
          organizationId: ORG_A,
          displayName: "Org A Market",
          publicOrganizationId: "ORGAAAAAA",
          role: "Owner",
        },
        {
          organizationId: ORG_B,
          displayName: "Org B Bakery",
          publicOrganizationId: "ORGBBBBBB",
          role: "Owner",
        },
      ],
      [USER_B]: [],
    },
  };
}

async function installTransferRoutes(page: Page, state: SharedState, actorUserId: string) {
  await page.route("**/ownership-transfer/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    if (url.includes("/resolve-target") && method === "POST") {
      return json(route, { publicUserId: "EX-2222-3333", displayName: "User B" });
    }
    if (url.includes("/request") && method === "POST") {
      state.transfer = {
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
      };
      return json(route, state.transfer, 201);
    }
    if (url.includes("/pending") && method === "GET") {
      return json(route, state.transfer);
    }
    return route.fallback();
  });

  await page.route("**/ownership-transfers/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    if (url.includes("/cancel") && method === "POST") {
      if (!state.transfer) {
        return json(route, { detail: "not found" }, 404);
      }
      const cancelled = {
        ...state.transfer,
        status: "Cancelled",
        cancelledAtUtc: "2026-08-27T00:00:00Z",
      };
      state.transfer = null;
      return json(route, cancelled);
    }
    if (url.includes("/my-pending") && method === "GET") {
      const pending =
        state.transfer &&
        state.transfer.status === "Pending" &&
        state.transfer.toUserId === actorUserId
          ? [state.transfer]
          : [];
      return json(route, pending);
    }
    if (url.includes("/accept") && method === "POST") {
      if (!state.transfer || state.transfer.toUserId !== actorUserId) {
        return json(route, { detail: "forbidden" }, 403);
      }
      state.transfer = {
        ...state.transfer,
        status: "Accepted",
        acceptedAtUtc: "2026-08-27T01:00:00Z",
        completedAtUtc: "2026-08-27T01:00:00Z",
      };
      state.orgsByUser[USER_B] = [
        {
          organizationId: ORG_A,
          displayName: "Org A Market",
          publicOrganizationId: "ORGAAAAAA",
          role: "Owner",
        },
      ];
      state.orgsByUser[USER_A] = (state.orgsByUser[USER_A] ?? []).filter(
        (o) => o.organizationId !== ORG_A,
      );
      return json(route, state.transfer);
    }
    return route.fallback();
  });
}

async function bindOwnerToManageBusiness(page: Page) {
  await page.route("**/pos-api/**/management/overview**", async (route) =>
    json(route, {
      businessDate: "2026-08-27",
      todaySalesTotal: 0,
      todaySaleCount: 0,
      todayCashSalesTotal: 0,
      todayUtangSalesTotal: 0,
      todayPaymentsReceived: 0,
      openUtangOutstanding: 0,
      lowStockProductCount: 0,
      expiredLotCount: 0,
      nearExpiryLotCount: 0,
      pendingTransferCount: 0,
      openShiftCount: 0,
      activeRegisterCount: 0,
    }),
  );
  await signInAndBindOwner(page);
  await page
    .getByTestId("workspace-destination-manage_business")
    .waitFor({ state: "visible", timeout: 15000 });
  await chooseOwnerManageBusiness(page);
  const overlayDismiss = page.getByRole("button", { name: "Dismiss" });
  if (await overlayDismiss.isVisible().catch(() => false)) {
    await overlayDismiss.click();
  }
  await expect(page.getByTestId("org-essentials-page")).toBeVisible({ timeout: 15000 });
}

async function installPersonalRecipientMocks(page: Page, state: SharedState) {
  let loggedIn = false;
  const session = {
    sessionId: "55555555-5555-4555-8555-555555555555",
    userId: USER_B,
    accountClass: "Personal",
    username: "bob@example.com",
    displayName: "User B",
    email: "bob@example.com",
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
    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      return json(
        route,
        (state.orgsByUser[USER_B] ?? []).map((o) => ({
          organizationId: o.organizationId,
          displayName: o.displayName,
          slug: "org-a-market",
        })),
      );
    }
    if (url.includes("/ownership-transfers/my-pending") && method === "GET") {
      const pending =
        state.transfer && state.transfer.status === "Pending" && state.transfer.toUserId === USER_B
          ? [state.transfer]
          : [];
      return json(route, pending);
    }
    if (url.includes("/ownership-transfers/") && url.includes("/accept") && method === "POST") {
      state.transfer = {
        ...(state.transfer as TransferState),
        status: "Accepted",
        acceptedAtUtc: "2026-08-27T01:00:00Z",
        completedAtUtc: "2026-08-27T01:00:00Z",
      };
      state.orgsByUser[USER_B] = [
        {
          organizationId: ORG_A,
          displayName: "Org A Market",
          publicOrganizationId: "ORGAAAAAA",
          role: "Owner",
        },
      ];
      state.orgsByUser[USER_A] = (state.orgsByUser[USER_A] ?? []).filter(
        (o) => o.organizationId !== ORG_A,
      );
      return json(route, state.transfer);
    }
    if (url.includes("/api/v1/personal/dashboard") && method === "GET") {
      return json(route, {
        userIdentityId: USER_B,
        accountProfileId: USER_B,
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
    if (method === "GET") {
      return json(route, []);
    }
    return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });
}

test.describe("EXITS-V1 ownership initiation", () => {
  test("owner requests transfer; recipient accepts; multi-org retained", async ({ browser }) => {
    const state = createState();
    const ownerCtx = await browser.newContext();
    const recipientCtx = await browser.newContext();
    const owner = await ownerCtx.newPage();
    const recipient = await recipientCtx.newPage();
    try {
      await mockBoundOwnerSession(owner);
      await installTransferRoutes(owner, state, USER_A);
      await bindOwnerToManageBusiness(owner);
      await clientNavigate(owner, "/org/ownership-transfer");
      await expect(owner.getByTestId("org-ownership-transfer-page")).toBeVisible({ timeout: 15000 });
      await owner.getByTestId("ownership-target-input").fill("EX-2222-3333");
      await owner.getByTestId("ownership-resolve").click();
      await expect(owner.getByTestId("ownership-resolved-target")).toBeVisible();
      await owner.getByTestId("ownership-request").click();
      await expect(owner.getByTestId("ownership-request-confirm")).toBeVisible();
      await owner.getByTestId("ownership-request-submit").click();
      await expect(owner.getByTestId("ownership-pending-card")).toBeVisible({ timeout: 15000 });
      expect(state.transfer?.status).toBe("Pending");

      await installPersonalRecipientMocks(recipient, state);
      await recipient.goto("/sign-in");
      await recipient.getByLabel("Email or staff login").fill("bob@example.com");
      await recipient.getByRole("textbox", { name: "Password" }).fill("secret");
      await recipient.getByTestId("sign-in-submit").click();
      await expect(recipient.getByTestId("personal-shell")).toBeVisible({ timeout: 15000 });
      const overlayDismiss = recipient.getByRole("button", { name: "Dismiss" });
      if (await overlayDismiss.isVisible().catch(() => false)) {
        await overlayDismiss.click();
      }
      await clientNavigate(recipient, "/personal/ownership-transfers");
      await expect(recipient.getByTestId("ownership-transfer-card")).toBeVisible({ timeout: 15000 });
      if (await overlayDismiss.isVisible().catch(() => false)) {
        await overlayDismiss.click();
      }
      await recipient.getByTestId("ownership-transfer-accept").click();
      await recipient.getByTestId("ownership-transfer-accept-submit").click();
      await expect(recipient.getByTestId("ownership-transfer-success")).toBeVisible({
        timeout: 15000,
      });
      expect(state.transfer?.status).toBe("Accepted");
      expect(state.orgsByUser[USER_A]?.map((o) => o.organizationId)).toEqual([ORG_B]);
      expect(state.orgsByUser[USER_B]?.map((o) => o.organizationId)).toEqual([ORG_A]);
    } finally {
      await ownerCtx.close().catch(() => undefined);
      await recipientCtx.close().catch(() => undefined);
    }
  });

  test("owner cancels pending transfer", async ({ page }) => {
    const state = createState();
    state.transfer = {
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
    };
    await mockBoundOwnerSession(page);
    await installTransferRoutes(page, state, USER_A);
    await bindOwnerToManageBusiness(page);
    await clientNavigate(page, "/org/ownership-transfer");
    await expect(page.getByTestId("ownership-pending-card")).toBeVisible({ timeout: 15000 });
    await page.getByTestId("ownership-cancel").click();
    await expect(page.getByTestId("ownership-cancel-confirm")).toBeVisible();
    await page.getByTestId("ownership-cancel-submit").click();
    await expect(page.getByTestId("ownership-target-input")).toBeVisible({ timeout: 15000 });
    expect(state.transfer).toBeNull();
  });
});
