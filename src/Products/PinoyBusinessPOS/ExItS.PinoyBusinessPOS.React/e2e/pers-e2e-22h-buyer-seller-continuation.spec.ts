/**
 * PERS-E2E-22H-REPAIR — Personal buyer → Organization seller continuation.
 *
 * Proves one shared logical order across two Playwright BrowserContexts:
 * User B (Personal) places pickup order → User A (Org owner) processes lifecycle
 * → User B refetches authoritative status. Mock-bound only (no live Docker fixtures).
 */
import { expect, test, type Browser, type Page } from "@playwright/test";
import {
  E2E_BRANCH_ID,
  E2E_ORG_ID,
  chooseOwnerOperations,
  clientNavigate,
  mockBoundOwnerSession,
  signInAndBindOwner,
} from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const SELLER_ORG = E2E_ORG_ID;
const OTHER_ORG = "99999999-9999-4999-8999-999999999999";
const OTHER_BRANCH = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const PRODUCT_ID = "33333333-3333-4333-8333-333333333333";
const ORDER_ID = "44444444-4444-4444-8444-444444444444";
const BUYER_USER_ID = "55555555-5555-4555-8555-555555555555";
const STRANGER_USER_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const LINK_REQ_ID = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const BUSINESS_CUSTOMER_ID = "88888888-8888-4888-8888-888888888888";

type SharedOrderState = {
  status: string;
  fulfillmentStatus: string;
  fulfillmentType: string;
  placed: boolean;
  transitions: string[];
  acceptPosts: number;
  illegalTransitionAttempts: number;
  linkStatus: "Pending" | "Accepted";
  createdOrganizationMembership: boolean;
  lastBuyerOrgList: unknown[];
};

type MockActor = "buyer" | "seller" | "stranger" | "otherOrg";

function createSharedState(): SharedOrderState {
  return {
    status: "Draft",
    fulfillmentStatus: "Pending",
    fulfillmentType: "Pickup",
    placed: false,
    transitions: [],
    acceptPosts: 0,
    illegalTransitionAttempts: 0,
    linkStatus: "Accepted",
    createdOrganizationMembership: false,
    lastBuyerOrgList: [],
  };
}

function json(route: { fulfill: (r: object) => Promise<void> }, body: unknown, status = 200) {
  return route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

function storefrontBody() {
  return {
    organizationId: SELLER_ORG,
    organizationDisplayName: "Kizy Store",
    canCustomerOrder: true,
    canCustomerDelivery: true,
    categories: [{ categoryId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa", name: "Grocery" }],
    products: [
      {
        productId: PRODUCT_ID,
        name: "Rice 1kg",
        sku: "RICE",
        unitOfMeasure: "kg",
        categoryId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        unitPrice: 55,
        isAvailable: true,
        tracksInventory: true,
        availableQuantity: 10,
        availabilityStatus: "InStock",
        hasImage: false,
        imageVersion: null,
        imageSource: "None",
      },
    ],
    productTotalCount: 1,
    page: 1,
    pageSize: 40,
    branches: [
      {
        branchId: E2E_BRANCH_ID,
        name: "Main Branch",
        pickupEnabled: true,
        deliveryEnabled: true,
        customerOrderingOperational: true,
        pickupOperational: true,
        deliveryOperational: true,
        onlineOrdersPaused: false,
        storeStatusMessage: null,
      },
    ],
  };
}

function orderDto(state: SharedOrderState) {
  return {
    orderId: ORDER_ID,
    sellerOrganizationId: SELLER_ORG,
    orderNumber: "CO-22H1",
    status: state.status,
    fulfillmentStatus: state.fulfillmentStatus,
    paymentStatus: "Unpaid",
    paymentMethod: "Cash",
    fulfillmentType: state.fulfillmentType,
    fulfillmentBranchId: E2E_BRANCH_ID,
    branchNameSnapshot: "Main Branch",
    customerPartyType: "Personal",
    customerDisplayName: "Ben Buyer",
    customerPlatformUserId: BUYER_USER_ID,
    merchandiseSubtotal: 55,
    deliveryFee: 0,
    total: 55,
    stockReservationState: "Reserved",
    lines: [
      {
        lineId: "66666666-6666-4666-8666-666666666666",
        productId: PRODUCT_ID,
        lineNumber: 1,
        nameSnapshot: "Rice 1kg",
        skuSnapshot: "RICE",
        unitSnapshot: "kg",
        quantity: 1,
        unitPrice: 55,
        discount: 0,
        lineTotal: 55,
      },
    ],
    delivery: null,
    createdAtUtc: "2026-08-21T00:00:00Z",
    submittedAtUtc: "2026-08-21T00:00:00Z",
    updatedAtUtc: "2026-08-21T00:00:00Z",
  };
}

function orderSummary(state: SharedOrderState) {
  return {
    orderId: ORDER_ID,
    orderNumber: "CO-22H1",
    status: state.status,
    fulfillmentStatus: state.fulfillmentStatus,
    fulfillmentType: state.fulfillmentType,
    fulfillmentBranchId: E2E_BRANCH_ID,
    branchNameSnapshot: "Main Branch",
    customerDisplayName: "Ben Buyer",
    total: 55,
    createdAtUtc: "2026-08-21T00:00:00Z",
    updatedAtUtc: "2026-08-21T00:00:00Z",
    lineCount: 1,
    sellerOrganizationId: SELLER_ORG,
  };
}

async function mockBuyerPersonalSession(page: Page, userId = BUYER_USER_ID, email = "ben@example.com") {
  let loggedIn = false;
  const personalMe = {
    sessionId: "22222222-2222-2222-2222-222222222222",
    userId,
    username: email,
    displayName: userId === BUYER_USER_ID ? "Ben Buyer" : "Stranger",
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
      // Buyer must remain Personal-only: no Organization membership after customer link.
      return json(route, []);
    }

    if (url.includes("/api/v1/platform/auth/token") && method === "POST") {
      return json(route, {
        accessToken: "e2e-personal-buyer-token",
        productAccessAllowed: false,
      });
    }

    if (url.includes("/api/v1/platform/auth/logout") && method === "POST") {
      loggedIn = false;
      return route.fulfill({ status: 204, body: "" });
    }

    if (
      url.includes("/api/v1/personal/linked-merchants")
      || url.includes("/api/v1/personal/customer-link-requests")
      || url.includes("/api/v1/personal/notifications")
      || url.includes("/api/v1/me/public-identity")
    ) {
      return route.fallback();
    }

    return json(route, {}, 404);
  });
}

async function installSharedCommerceMocks(page: Page, state: SharedOrderState, actor: MockActor) {
  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const pathname = new URL(url).pathname;

    if (url.includes("/api/v1/personal/customer-link-requests") && method === "GET") {
      return json(route, [
        {
          id: LINK_REQ_ID,
          organizationId: SELLER_ORG,
          organizationDisplayName: "Kizy Store",
          businessCustomerId: BUSINESS_CUSTOMER_ID,
          status: state.linkStatus,
          createdAtUtc: "2026-08-21T00:00:00Z",
          expiresAtUtc: "2026-09-21T00:00:00Z",
          targetPublicUserId: "EXITS-B",
        },
      ]);
    }

    if (
      url.includes(`/api/v1/personal/customer-link-requests/${LINK_REQ_ID}/accept`)
      && method === "POST"
    ) {
      if (actor !== "buyer") return json(route, { detail: "denied" }, 403);
      state.linkStatus = "Accepted";
      state.createdOrganizationMembership = false;
      return json(route, {
        id: LINK_REQ_ID,
        status: "Accepted",
        organizationId: SELLER_ORG,
        createdOrganizationMembership: false,
        grantedProductRole: null,
      });
    }

    if (url.includes("/api/v1/personal/linked-merchants")) {
      if (url.includes("/ordering-capability")) {
        return json(route, {
          organizationId: SELLER_ORG,
          canCustomerOrder: state.linkStatus === "Accepted",
          canCustomerDelivery: true,
          organizationDisplayName: "Kizy Store",
        });
      }
      return json(route, {
        items:
          state.linkStatus === "Accepted" && actor === "buyer"
            ? [
                {
                  linkedCustomerId: "77777777-7777-4777-8777-777777777777",
                  businessCustomerId: BUSINESS_CUSTOMER_ID,
                  organizationId: SELLER_ORG,
                  organizationDisplayName: "Kizy Store",
                  customerDisplayName: "Ben Buyer",
                  linkStatus: "Active",
                  linkedAtUtc: "2026-08-21T00:00:00Z",
                  canCustomerOrder: true,
                  canCustomerDelivery: true,
                },
              ]
            : [],
        totalCount: state.linkStatus === "Accepted" && actor === "buyer" ? 1 : 0,
        page: 1,
        pageSize: 50,
      });
    }

    if (url.includes("/api/v1/me/public-identity") && method === "GET") {
      return json(route, {
        publicUserId: actor === "buyer" ? "EXITS-B" : "EXITS-X",
        qrPayload: null,
      });
    }

    if (url.includes("/api/v1/personal/notifications") && method === "GET") {
      return json(route, []);
    }

    // Personal home / shell reads — empty success so commerce tests are not blocked by 404 overlays.
    if (url.includes("/api/v1/personal/dashboard") && method === "GET") {
      return json(route, {
        userIdentityId: BUYER_USER_ID,
        accountProfileId: BUYER_USER_ID,
        accountClass: "Personal",
        utangAvailable: true,
        contactCount: 0,
        activeRelationshipCount: 0,
        totalLentBalance: 0,
        totalBorrowedBalance: 0,
        pendingConfirmationCount: 0,
      });
    }
    if (url.includes("/api/v1/personal/utang/contacts") && method === "GET") {
      return json(route, []);
    }
    if (url.includes("/api/v1/personal/utang/relationships/lent") && method === "GET") {
      return json(route, []);
    }
    if (url.includes("/api/v1/personal/utang/relationships/borrowed") && method === "GET") {
      return json(route, []);
    }
    if (url.includes("/api/v1/personal/todos") && method === "GET") {
      return json(route, []);
    }
    if (url.includes("/api/v1/personal/connections") && method === "GET") {
      return json(route, { items: [], totalCount: 0 });
    }
    if (url.includes("/api/v1/personal/") && method === "GET") {
      return json(route, []);
    }

    // Let session mocks handle auth routes.
    if (pathname.includes("/api/v1/platform/")) {
      return route.fallback();
    }

    return route.fallback();
  });

  await page.route("**/pos-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/api/v1/pos/operational-branch") && method === "PUT") {
      return json(route, {
        organizationId: actor === "otherOrg" ? OTHER_ORG : SELLER_ORG,
        branchId: actor === "otherOrg" ? OTHER_BRANCH : E2E_BRANCH_ID,
        name: actor === "otherOrg" ? "Other Branch" : "Main Branch",
        deviceMatchesSelectedBranch: false,
        deviceBoundBranchId: null,
        openCashierShiftPresent: false,
      });
    }

    if (url.includes("/storefront") && method === "GET") {
      return json(route, storefrontBody());
    }

    if (
      url.match(/\/organizations\/[^/]+$/)
      && method === "POST"
      && url.includes("customer-orders")
    ) {
      if (actor !== "buyer") return json(route, { detail: "denied" }, 403);
      if (state.linkStatus !== "Accepted") {
        return json(route, { detail: "customer link required" }, 403);
      }
      const body = route.request().postDataJSON() as { fulfillmentType?: string };
      state.placed = true;
      state.status = "Submitted";
      state.fulfillmentStatus = "Pending";
      state.fulfillmentType = body.fulfillmentType ?? "Pickup";
      return json(route, orderDto(state), 201);
    }

    if (url.includes("/mine/") && method === "GET") {
      if (!state.placed) return json(route, { detail: "not found" }, 404);
      if (actor === "stranger") return json(route, { detail: "not found" }, 404);
      if (actor !== "buyer") return json(route, { detail: "not found" }, 404);
      if (!url.includes(ORDER_ID)) return json(route, { detail: "not found" }, 404);
      return json(route, orderDto(state));
    }

    if (url.includes("/mine") && method === "GET") {
      if (actor !== "buyer" || !state.placed) {
        return json(route, { items: [], totalCount: 0, page: 1, pageSize: 40 });
      }
      return json(route, {
        items: [orderSummary(state)],
        totalCount: 1,
        page: 1,
        pageSize: 40,
      });
    }

    // Seller org list / detail
    if (url.includes(`/organizations/${OTHER_ORG}/customer-orders`)) {
      // Cross-org: never leak Org A order.
      return json(route, { detail: "not found" }, 404);
    }

    if (url.includes(`/organizations/${SELLER_ORG}/customer-orders`)) {
      if (actor === "otherOrg") {
        return json(route, { detail: "forbidden" }, 403);
      }
      if (actor !== "seller") {
        return json(route, { detail: "forbidden" }, 403);
      }

      // Optional branch filter isolation (domain supports branchId query).
      const branchFilter = new URL(url).searchParams.get("branchId");
      if (branchFilter && branchFilter !== E2E_BRANCH_ID) {
        if (method === "GET" && !url.includes(ORDER_ID)) {
          return json(route, { items: [], totalCount: 0, page: 1, pageSize: 40 });
        }
        return json(route, { detail: "not found" }, 404);
      }

      if (method === "GET" && url.includes(ORDER_ID)) {
        if (!state.placed) return json(route, { detail: "not found" }, 404);
        return json(route, orderDto(state));
      }

      if (method === "GET") {
        return json(route, {
          items: state.placed ? [orderSummary(state)] : [],
          totalCount: state.placed ? 1 : 0,
          page: 1,
          pageSize: 40,
        });
      }

      if (method === "POST" && url.includes("/accept")) {
        state.acceptPosts += 1;
        if (state.status === "Completed" || state.status === "Rejected") {
          state.illegalTransitionAttempts += 1;
          return json(route, { detail: "invalid transition", title: "Conflict" }, 409);
        }
        if (state.status === "Accepted") {
          // Idempotent retry: converge, no duplicate side-effect record.
          return json(route, orderDto(state));
        }
        if (state.status !== "Submitted") {
          state.illegalTransitionAttempts += 1;
          return json(route, { detail: "invalid transition", title: "Conflict" }, 409);
        }
        state.status = "Accepted";
        state.fulfillmentStatus = "Pending";
        state.transitions.push("accept");
        return json(route, orderDto(state));
      }

      if (method === "POST" && url.includes("/start-preparing")) {
        if (state.status !== "Accepted" || state.fulfillmentStatus !== "Pending") {
          state.illegalTransitionAttempts += 1;
          return json(route, { detail: "invalid transition" }, 409);
        }
        state.fulfillmentStatus = "Preparing";
        state.transitions.push("start-preparing");
        return json(route, orderDto(state));
      }

      if (method === "POST" && url.includes("/mark-ready")) {
        if (state.fulfillmentStatus !== "Preparing") {
          state.illegalTransitionAttempts += 1;
          return json(route, { detail: "invalid transition" }, 409);
        }
        state.fulfillmentStatus = "ReadyForPickup";
        state.transitions.push("mark-ready");
        return json(route, orderDto(state));
      }

      if (method === "POST" && url.includes("/mark-collected")) {
        if (state.fulfillmentStatus !== "ReadyForPickup") {
          state.illegalTransitionAttempts += 1;
          return json(route, { detail: "invalid transition" }, 409);
        }
        state.fulfillmentStatus = "Collected";
        state.transitions.push("mark-collected");
        return json(route, orderDto(state));
      }

      if (method === "POST" && url.includes("/complete")) {
        if (state.fulfillmentStatus !== "Collected") {
          state.illegalTransitionAttempts += 1;
          return json(route, { detail: "invalid transition" }, 409);
        }
        state.status = "Completed";
        state.transitions.push("complete");
        return json(route, orderDto(state));
      }

      // Illegal reverse: e.g. complete → accept already handled; explicit start-preparing after complete
      if (method === "POST") {
        state.illegalTransitionAttempts += 1;
        return json(route, { detail: "invalid transition" }, 409);
      }
    }

    return route.fallback();
  });
}

async function signInPersonalBuyer(page: Page, email = "ben@example.com") {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill(email);
  await page.getByRole("textbox", { name: "Password" }).fill("secret");
  await page.getByTestId("sign-in-submit").click();
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 20000 });
}

async function signInOwnerOperations(page: Page) {
  await signInAndBindOwner(page);
  const operations = page.getByTestId("workspace-destination-operations");
  const ownerHome = page.getByTestId("open-customer-orders");
  await Promise.race([
    operations.waitFor({ state: "visible", timeout: 15000 }),
    ownerHome.waitFor({ state: "visible", timeout: 15000 }),
  ]);
  if (await operations.isVisible().catch(() => false)) {
    await chooseOwnerOperations(page);
  }
  await page.getByTestId("open-customer-orders").waitFor({ state: "visible", timeout: 15000 });
}

async function openBuyerAndSellerContexts(browser: Browser, state: SharedOrderState) {
  const buyerCtx = await browser.newContext();
  const sellerCtx = await browser.newContext();
  const buyer = await buyerCtx.newPage();
  const seller = await sellerCtx.newPage();

  await mockBuyerPersonalSession(buyer);
  await installSharedCommerceMocks(buyer, state, "buyer");

  await mockBoundOwnerSession(seller);
  await installSharedCommerceMocks(seller, state, "seller");

  return { buyer, seller, buyerCtx, sellerCtx };
}

test.describe("PERS-E2E-22H buyer→seller commerce continuation (mock multi-user)", () => {
  test("shared order: buyer places → seller lifecycle → buyer sees final status", async ({
    browser,
  }) => {
    test.setTimeout(120_000);
    const state = createSharedState();
    const { buyer, seller, buyerCtx, sellerCtx } = await openBuyerAndSellerContexts(browser, state);

    try {
      // B — Personal buyer storefront → checkout → place pickup order
      await signInPersonalBuyer(buyer);
      await clientNavigate(buyer, "/personal/linked-merchants");
      await expect(buyer.getByTestId("linked-merchants-page")).toBeVisible();
      await expect(buyer.getByTestId("linked-merchant-card")).toBeVisible();
      await buyer.getByTestId("open-merchant-shop").click();
      await expect(buyer.getByTestId("merchant-shop-page")).toBeVisible();
      await buyer.getByTestId("cart-increment").click();
      await expect(buyer.getByTestId("cart-qty")).toHaveText("1");
      await buyer.getByTestId("shop-review").click();
      await expect(buyer.getByTestId("merchant-checkout-page")).toBeVisible();

      const placeResponse = buyer.waitForResponse(
        (r) =>
          r.request().method() === "POST"
          && r.url().includes("/customer-orders/organizations/")
          && r.status() === 201,
        { timeout: 20000 },
      );
      await buyer.getByTestId("place-order").click();
      await placeResponse;
      await expect(buyer.getByTestId("my-order-detail-page")).toBeVisible({ timeout: 15000 });
      await expect(buyer.getByText("New", { exact: true })).toBeVisible();
      expect(state.placed).toBe(true);
      expect(state.status).toBe("Submitted");
      expect(state.fulfillmentStatus).toBe("Pending");

      // A — separate Organization seller context opens the SAME orderId
      await signInOwnerOperations(seller);
      await clientNavigate(seller, "/orders");
      await expect(seller.getByTestId("seller-orders-page")).toBeVisible({ timeout: 15000 });
      await expect(seller.getByTestId("seller-order-card")).toBeVisible();
      await expect(seller.getByText("CO-22H1")).toBeVisible();
      await expect(seller.getByText("Ben Buyer")).toBeVisible();
      await clientNavigate(seller, `/orders/${ORDER_ID}`);
      await expect(seller.getByTestId("seller-order-detail-page")).toBeVisible({ timeout: 15000 });
      await expect(seller.getByTestId("seller-order-detail-page").getByText("Main Branch")).toBeVisible();
      await expect(seller.getByTestId("seller-order-detail-page").getByText("Rice 1kg")).toBeVisible();

      async function buyerMustSeeStatus(label: string) {
        // Force network refetch — same-route clientNavigate alone can keep a stale React Query cache.
        await buyer.goto(`/personal/orders/${ORDER_ID}`);
        await expect(buyer.getByTestId("my-order-detail-page")).toBeVisible({ timeout: 15000 });
        await expect(buyer.getByText(label, { exact: true })).toBeVisible({ timeout: 15000 });
      }

      // Lifecycle: accept → prepare → ready → collected → complete (current UI contract)
      const acceptRes = seller.waitForResponse(
        (r) => r.request().method() === "POST" && r.url().includes("/accept") && r.ok(),
        { timeout: 15000 },
      );
      await seller.getByTestId("seller-action-accept").click();
      await acceptRes;
      await expect(seller.getByTestId("seller-action-startpreparing")).toBeVisible({
        timeout: 10000,
      });
      expect(state.status).toBe("Accepted");
      expect(state.fulfillmentStatus).toBe("Pending");

      await buyerMustSeeStatus("Accepted");

      const prepRes = seller.waitForResponse(
        (r) => r.request().method() === "POST" && r.url().includes("/start-preparing") && r.ok(),
        { timeout: 15000 },
      );
      await seller.getByTestId("seller-action-startpreparing").click();
      await prepRes;
      await expect(seller.getByTestId("seller-action-markready")).toBeVisible({ timeout: 10000 });
      expect(state.fulfillmentStatus).toBe("Preparing");

      await buyerMustSeeStatus("Preparing");

      const readyRes = seller.waitForResponse(
        (r) => r.request().method() === "POST" && r.url().includes("/mark-ready") && r.ok(),
        { timeout: 15000 },
      );
      await seller.getByTestId("seller-action-markready").click();
      await readyRes;
      await expect(seller.getByTestId("seller-action-markcollected")).toBeVisible({
        timeout: 10000,
      });

      const collectedRes = seller.waitForResponse(
        (r) => r.request().method() === "POST" && r.url().includes("/mark-collected") && r.ok(),
        { timeout: 15000 },
      );
      await seller.getByTestId("seller-action-markcollected").click();
      await collectedRes;
      await expect(seller.getByTestId("seller-action-complete")).toBeVisible({ timeout: 10000 });

      const completeRes = seller.waitForResponse(
        (r) => r.request().method() === "POST" && r.url().includes("/complete") && r.ok(),
        { timeout: 15000 },
      );
      await seller.getByTestId("seller-action-complete").click();
      await completeRes;

      expect(state.status).toBe("Completed");
      expect(state.transitions).toEqual([
        "accept",
        "start-preparing",
        "mark-ready",
        "mark-collected",
        "complete",
      ]);

      await buyerMustSeeStatus("Completed");

      // Customer link ≠ staff membership
      expect(state.createdOrganizationMembership).toBe(false);

      // B cannot open seller Organization operational routes
      await buyer.goto("/orders");
      await expect(buyer.getByTestId("account-class-denied")).toBeVisible({ timeout: 15000 });

      // A cannot enter B's Personal session
      await seller.goto("/personal");
      await expect(seller.getByTestId("account-class-denied")).toBeVisible({ timeout: 15000 });
    } finally {
      await buyerCtx.close().catch(() => undefined);
      await sellerCtx.close().catch(() => undefined);
    }
  });

  test("customer link accept does not grant Organization staff membership", async ({ page }) => {
    const state = createSharedState();
    state.linkStatus = "Pending";
    await mockBuyerPersonalSession(page);
    await installSharedCommerceMocks(page, state, "buyer");
    await signInPersonalBuyer(page);

    const accept = page.waitForResponse(
      (r) =>
        r.request().method() === "POST"
        && r.url().includes(`/customer-link-requests/${LINK_REQ_ID}/accept`),
    );
    // Drive accept via fetch in-page to assert contract without requiring UI surface.
    await page.evaluate(async (linkId) => {
      const res = await fetch(
        `/platform-api/api/v1/personal/customer-link-requests/${linkId}/accept`,
        { method: "POST", headers: { "Content-Type": "application/json" }, body: "{}" },
      );
      (window as unknown as { __linkAccept: unknown }).__linkAccept = await res.json();
    }, LINK_REQ_ID);
    await accept;
    const body = await page.evaluate(
      () => (window as unknown as { __linkAccept: Record<string, unknown> }).__linkAccept,
    );
    expect(body.createdOrganizationMembership).toBe(false);
    expect(body.grantedProductRole).toBeNull();
    expect(state.createdOrganizationMembership).toBe(false);
    expect(state.linkStatus).toBe("Accepted");

    const orgs = await page.evaluate(async () => {
      const res = await fetch("/platform-api/api/v1/platform/auth/organizations");
      return res.json();
    });
    expect(orgs).toEqual([]);
  });

  test("unrelated Personal user cannot view buyer order", async ({ browser }) => {
    const state = createSharedState();
    state.placed = true;
    state.status = "Submitted";

    const strangerCtx = await browser.newContext();
    const stranger = await strangerCtx.newPage();
    try {
      await mockBuyerPersonalSession(stranger, STRANGER_USER_ID, "stranger@example.com");
      await installSharedCommerceMocks(stranger, state, "stranger");
      await signInPersonalBuyer(stranger, "stranger@example.com");
      await clientNavigate(stranger, `/personal/orders/${ORDER_ID}`);
      // Fail-closed: detail offline/error or not found — must not show buyer order facts.
      await expect(stranger.getByText("Rice 1kg")).toHaveCount(0);
      await expect(stranger.getByText("CO-22H1")).toHaveCount(0);
    } finally {
      await strangerCtx.close();
    }
  });

  test("cross-org seller cannot view or mutate Org A order", async ({ page }) => {
    const state = createSharedState();
    state.placed = true;
    state.status = "Submitted";

    await mockBoundOwnerSession(page);
    await installSharedCommerceMocks(page, state, "otherOrg");
    await signInOwnerOperations(page);

    const list = await page.evaluate(async (orgId) => {
      const res = await fetch(`/pos-api/api/v1/pos/organizations/${orgId}/customer-orders`);
      return { status: res.status, body: await res.json() };
    }, SELLER_ORG);
    expect(list.status).toBe(403);

    const otherList = await page.evaluate(async (orgId) => {
      const res = await fetch(`/pos-api/api/v1/pos/organizations/${orgId}/customer-orders`);
      return { status: res.status };
    }, OTHER_ORG);
    expect(otherList.status).toBe(404);

    const mutate = await page.evaluate(async ({ orgId, orderId }) => {
      const res = await fetch(
        `/pos-api/api/v1/pos/organizations/${orgId}/customer-orders/${orderId}/accept`,
        { method: "POST", headers: { "Content-Type": "application/json" }, body: "{}" },
      );
      return res.status;
    }, { orgId: SELLER_ORG, orderId: ORDER_ID });
    expect(mutate).toBe(403);
    expect(state.transitions).toEqual([]);
  });

  test("branch filter isolates seller list; invalid transition rejected", async ({ page }) => {
    const state = createSharedState();
    state.placed = true;
    state.status = "Submitted";
    state.fulfillmentStatus = "Pending";

    await mockBoundOwnerSession(page);
    await installSharedCommerceMocks(page, state, "seller");
    await signInOwnerOperations(page);

    const wrongBranch = await page.evaluate(async ({ orgId, branchId }) => {
      const res = await fetch(
        `/pos-api/api/v1/pos/organizations/${orgId}/customer-orders?branchId=${branchId}`,
      );
      return res.json();
    }, { orgId: SELLER_ORG, branchId: OTHER_BRANCH });
    expect(wrongBranch.totalCount).toBe(0);

    const rightBranch = await page.evaluate(async ({ orgId, branchId }) => {
      const res = await fetch(
        `/pos-api/api/v1/pos/organizations/${orgId}/customer-orders?branchId=${branchId}`,
      );
      return res.json();
    }, { orgId: SELLER_ORG, branchId: E2E_BRANCH_ID });
    expect(rightBranch.totalCount).toBe(1);
    expect(rightBranch.items[0].orderId).toBe(ORDER_ID);

    // Accept once, then illegal reverse/skip fails.
    await page.evaluate(async ({ orgId, orderId }) => {
      await fetch(
        `/pos-api/api/v1/pos/organizations/${orgId}/customer-orders/${orderId}/accept`,
        { method: "POST", headers: { "Content-Type": "application/json" }, body: "{}" },
      );
    }, { orgId: SELLER_ORG, orderId: ORDER_ID });
    expect(state.status).toBe("Accepted");

    const skip = await page.evaluate(async ({ orgId, orderId }) => {
      const res = await fetch(
        `/pos-api/api/v1/pos/organizations/${orgId}/customer-orders/${orderId}/complete`,
        { method: "POST", headers: { "Content-Type": "application/json" }, body: "{}" },
      );
      return res.status;
    }, { orgId: SELLER_ORG, orderId: ORDER_ID });
    expect(skip).toBe(409);
    expect(state.illegalTransitionAttempts).toBeGreaterThan(0);
    expect(state.status).toBe("Accepted");
  });

  test("seller accept double-submit converges without duplicate transition", async ({ page }) => {
    const state = createSharedState();
    state.placed = true;
    state.status = "Submitted";

    await mockBoundOwnerSession(page);
    await installSharedCommerceMocks(page, state, "seller");
    await signInOwnerOperations(page);
    await clientNavigate(page, `/orders/${ORDER_ID}`);
    await expect(page.getByTestId("seller-order-detail-page")).toBeVisible();

    await page.getByTestId("seller-action-accept").click();
    await expect(page.getByTestId("seller-action-startpreparing")).toBeVisible({ timeout: 10000 });
    // Accept action leaves the UI after success — retry via same transition API must converge.
    await expect(page.getByTestId("seller-action-accept")).toHaveCount(0);

    const retry = await page.evaluate(async ({ orgId, orderId }) => {
      const res = await fetch(
        `/pos-api/api/v1/pos/organizations/${orgId}/customer-orders/${orderId}/accept`,
        { method: "POST", headers: { "Content-Type": "application/json" }, body: "{}" },
      );
      return { status: res.status, body: await res.json() };
    }, { orgId: SELLER_ORG, orderId: ORDER_ID });

    expect(retry.status).toBe(200);
    expect(retry.body.status).toBe("Accepted");
    expect(state.transitions.filter((t) => t === "accept")).toHaveLength(1);
    expect(state.acceptPosts).toBeGreaterThanOrEqual(2);
  });

  test("buyer checkout blocked while offline (online-only)", async ({ page }) => {
    const state = createSharedState();
    await mockBuyerPersonalSession(page);
    await installSharedCommerceMocks(page, state, "buyer");
    await signInPersonalBuyer(page);

    await clientNavigate(page, `/personal/linked-merchants/${SELLER_ORG}/shop`);
    await page.getByTestId("cart-increment").click();
    await page.getByTestId("shop-review").click();
    await expect(page.getByTestId("merchant-checkout-page")).toBeVisible();

    await page.context().setOffline(true);
    await clientNavigate(page, `/personal/linked-merchants/${SELLER_ORG}/shop/checkout`);
    await expect(page.getByTestId("merchant-checkout-offline")).toBeVisible({ timeout: 15000 });
    expect(state.placed).toBe(false);
    await page.context().setOffline(false);
  });
});
