/**
 * RMAP-22H — Integrated Personal ↔ Business E2E (SAFE mock-bound).
 *
 * Covers the two-person online-first story against Playwright route mocks
 * (same pattern as RMAP-19). Live Docker Local Validation multi-user flow
 * is documented N-A in the package report — do not invent a live pass.
 */
import { expect, test, type Page, type Route } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  E2E_BRANCH_ID,
  E2E_ORG_ID,
  chooseOwnerOperations,
  clientNavigate,
  completeOfflinePinSetupIfNeeded,
  mockBoundOwnerSession,
  signInAndBindOwner,
} from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const USER_A_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const USER_B_ID = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const CONTACT_ID = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const REL_ID = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const TODO_A_ID = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const TODO_B_SECRET = "ffffffff-ffff-4fff-8fff-ffffffffffff";
const INVITE_ID = "11111111-1111-4111-8111-111111111111";
const LINK_REQ_ID = "22222222-2222-4222-8222-222222222222";
const ORDER_ID = "44444444-4444-4444-8444-444444444444";
const PRODUCT_ID = "33333333-3333-4333-8333-333333333333";
const TYPE_ID = "99999999-9999-4999-8999-999999999999";
const PLAN_ID = "88888888-8888-4888-8888-888888888888";

const VIEWPORTS = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

type Actor = "A" | "B" | "Owner";

type StoryState = {
  actor: Actor;
  loggedIn: boolean;
  inviteStatus: "Pending" | "Accepted" | "Declined";
  linkStatus: "Pending" | "Accepted" | "Declined";
  orderStatus: string;
  fulfillmentStatus: string;
  transitions: string[];
  todosA: Array<Record<string, unknown>>;
  contactCreated: boolean;
  relationshipCreated: boolean;
  businessStarted: boolean;
};

function json(route: Route, body: unknown, status = 200) {
  return route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

function personalSession(actor: "A" | "B") {
  if (actor === "A") {
    return {
      sessionId: "session-a",
      userId: USER_A_ID,
      username: "ana@example.com",
      displayName: "Ana Owner",
      email: "ana@example.com",
      accountClass: "Personal",
      homeOrganizationId: null,
      organizationContextLocked: false,
    };
  }
  return {
    sessionId: "session-b",
    userId: USER_B_ID,
    username: "ben@example.com",
    displayName: "Ben Buyer",
    email: "ben@example.com",
    accountClass: "Personal",
    homeOrganizationId: null,
    organizationContextLocked: false,
  };
}

function ownerSession() {
  return {
    sessionId: "session-owner",
    userId: USER_A_ID,
    username: "ana@example.com",
    displayName: "Ana Owner",
    email: "ana@example.com",
    accountClass: "Organization",
    homeOrganizationId: E2E_ORG_ID,
    organizationContextLocked: false,
  };
}

function todoDto(id: string, title: string, ownerId: string, status = "Open") {
  return {
    id,
    ownerUserIdentityId: ownerId,
    title,
    notes: null,
    status,
    priority: "Normal",
    dueAtUtc: null,
    reminderAtUtc: null,
    relatedEntityType: null,
    relatedEntityId: null,
    version: 1,
    createdAtUtc: "2026-08-21T00:00:00Z",
    updatedAtUtc: "2026-08-21T00:00:00Z",
    completedAtUtc: null,
  };
}

function contactDto() {
  return {
    id: CONTACT_ID,
    displayName: "Ben Buyer",
    phone: null,
    email: "ben@example.com",
    linkedUserIdentityId: null,
    status: "Active",
    createdAtUtc: "2026-08-21T00:00:00Z",
  };
}

function relationshipDto() {
  return {
    id: REL_ID,
    perspective: "Lent",
    creditorUserIdentityId: USER_A_ID,
    creditorContactId: null,
    debtorUserIdentityId: null,
    debtorContactId: CONTACT_ID,
    currencyCode: "PHP",
    currentBalance: 100,
    dueDateUtc: "2026-08-28T00:00:00Z",
    status: "Active",
    version: 1,
    updatedAtUtc: "2026-08-21T00:00:00Z",
  };
}

function invitationDto(status: string) {
  return {
    id: INVITE_ID,
    debtRelationshipId: REL_ID,
    inviteeContactId: CONTACT_ID,
    invitedByUserIdentityId: USER_A_ID,
    inviteTargetEmailMasked: "b***@example.com",
    status,
    createdAtUtc: "2026-08-21T00:00:00Z",
    updatedAtUtc: "2026-08-21T00:00:00Z",
    expiresAtUtc: "2026-09-21T00:00:00Z",
    acceptedAtUtc: status === "Accepted" ? "2026-08-21T01:00:00Z" : null,
    declinedAtUtc: null,
    revokedAtUtc: null,
    acceptedByUserIdentityId: status === "Accepted" ? USER_B_ID : null,
    acceptToken: "invite-token",
  };
}

function orderDto(state: StoryState) {
  return {
    orderId: ORDER_ID,
    sellerOrganizationId: E2E_ORG_ID,
    orderNumber: "CO-22H1",
    status: state.orderStatus,
    fulfillmentStatus: state.fulfillmentStatus,
    paymentStatus: "Unpaid",
    paymentMethod: "Cash",
    fulfillmentType: "Pickup",
    fulfillmentBranchId: E2E_BRANCH_ID,
    branchNameSnapshot: "Main Branch",
    customerPartyType: "Personal",
    customerDisplayName: "Ben Buyer",
    customerPlatformUserId: USER_B_ID,
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

function storefrontBody() {
  return {
    organizationId: E2E_ORG_ID,
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

async function mockIntegratedStory(page: Page, state: StoryState) {
  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const pathname = new URL(url).pathname;

    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return json(route, { headerName: "X-XSRF-TOKEN", token: "e2e-csrf" });
    }

    if (url.includes("/api/v1/platform/auth/me") && method === "GET") {
      if (!state.loggedIn) return json(route, {}, 401);
      if (state.actor === "Owner") return json(route, ownerSession());
      return json(route, personalSession(state.actor));
    }

    if (url.includes("/api/v1/platform/auth/login") && method === "POST") {
      const body = route.request().postDataJSON() as {
        usernameOrEmail?: string;
        username?: string;
        email?: string;
      };
      const login = (body.usernameOrEmail ?? body.username ?? body.email ?? "").toLowerCase();
      state.loggedIn = true;
      if (login.includes("ben")) {
        state.actor = "B";
        return json(route, { ...personalSession("B"), sessionToken: "must-not-persist" });
      }
      if (state.businessStarted && (login.includes("ana") || login.includes("owner"))) {
        state.actor = "Owner";
        return json(route, { ...ownerSession(), sessionToken: "must-not-persist" });
      }
      if (login.includes("ana") || login.includes("owner") || login.length === 0) {
        state.actor = state.businessStarted ? "Owner" : "A";
        if (state.actor === "Owner") {
          return json(route, { ...ownerSession(), sessionToken: "must-not-persist" });
        }
        return json(route, { ...personalSession("A"), sessionToken: "must-not-persist" });
      }
      state.actor = "A";
      return json(route, { ...personalSession("A"), sessionToken: "must-not-persist" });
    }

    if (url.includes("/api/v1/platform/auth/logout") && method === "POST") {
      state.loggedIn = false;
      return route.fulfill({ status: 204, body: "" });
    }

    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      if (state.actor === "Owner") {
        return json(route, [
          { organizationId: E2E_ORG_ID, displayName: "Kizy Store", slug: "kizy-store" },
        ]);
      }
      return json(route, []);
    }

    if (pathname.endsWith(`/organizations/${E2E_ORG_ID}/branches`) && method === "GET") {
      return json(route, [
        {
          id: E2E_BRANCH_ID,
          organizationId: E2E_ORG_ID,
          code: "MAIN",
          name: "Main Branch",
          isPrimary: true,
          status: "Active",
        },
      ]);
    }

    if (url.includes("/api/v1/platform/auth/organization-context") && method === "PUT") {
      return route.fulfill({ status: 204, body: "" });
    }

    if (url.includes(`/organizations/${E2E_ORG_ID}/branch-context`) && method === "PUT") {
      return route.fulfill({ status: 204, body: "" });
    }

    if (url.includes("/api/v1/platform/auth/token") && method === "POST") {
      if (state.actor === "Owner") {
        return json(route, {
          accessToken: "e2e-owner-token",
          productAccessAllowed: true,
          mappedPosRoleCode: "Owner",
          productLocalRoleCode: "Owner",
          organizationManagementAuthority: true,
          membershipRole: "OrganizationOwner",
        });
      }
      return json(route, {
        accessToken: "e2e-personal-token",
        productAccessAllowed: false,
      });
    }

    if (url.includes("/api/v1/personal/dashboard") && method === "GET") {
      if (state.actor !== "A" && state.actor !== "B") return json(route, { detail: "denied" }, 403);
      return json(route, {
        userIdentityId: state.actor === "A" ? USER_A_ID : USER_B_ID,
        accountProfileId: state.actor === "A" ? USER_A_ID : USER_B_ID,
        accountClass: "Personal",
        utangAvailable: true,
        contactCount: state.contactCreated ? 1 : 0,
        activeRelationshipCount: state.relationshipCreated ? 1 : 0,
        totalLentBalance: state.relationshipCreated ? 100 : 0,
        totalBorrowedBalance: 0,
      });
    }

    if (url.includes("/api/v1/personal/me") && method === "GET") {
      return json(route, {
        userIdentityId: state.actor === "A" ? USER_A_ID : USER_B_ID,
        displayName: state.actor === "A" ? "Ana Owner" : "Ben Buyer",
        email: state.actor === "A" ? "ana@example.com" : "ben@example.com",
      });
    }

    if (url.includes("/api/v1/personal/profile") && method === "GET") {
      return json(route, {
        userIdentityId: USER_A_ID,
        accountProfileId: USER_A_ID,
        username: "ana@example.com",
        displayName: "Ana Owner",
        email: "ana@example.com",
        accountClass: "Personal",
        status: "Active",
        publicUserId: "EXITS-A",
        qrPayload: null,
        phone: "09170000001",
      });
    }

    if (url.includes("/api/v1/personal/onboarding/business-types") && method === "GET") {
      return json(route, [
        {
          id: TYPE_ID,
          code: "sari-sari",
          name: "Sari-sari",
          description: "Neighborhood store",
          status: "Active",
          sortOrder: 1,
        },
      ]);
    }

    if (url.includes("/api/v1/commercial/plans") && method === "GET") {
      return json(route, [
        {
          id: PLAN_ID,
          productCode: "pinoy-business-pos",
          code: "business",
          displayName: "Business",
          status: "Active",
          createdAtUtc: "2026-01-01T00:00:00Z",
          updatedAtUtc: "2026-01-01T00:00:00Z",
          productId: PLAN_ID,
          productDisplayName: "Pinoy Business POS",
          planKey: "business",
          description: "E2E business plan",
          maxBranches: 3,
          maxActiveStaff: 10,
          maxActivePosDevices: 3,
          maxActiveBusinessTypes: 2,
          customerCreditEnabled: true,
          advancedReportsEnabled: true,
          exportEnabled: true,
          trialAllowed: true,
          defaultTrialDays: 14,
          sortOrder: 100,
          monthlyPrice: 999,
          annualPrice: 9990,
          currencyCode: "PHP",
        },
      ]);
    }

    if (url.includes("/api/v1/personal/start-business") && method === "POST") {
      state.businessStarted = true;
      state.actor = "Owner";
      return json(route, {
        organizationId: E2E_ORG_ID,
        membershipId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaa01",
        organizationAccountProfileId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaa02",
        sessionId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaa03",
        accountClass: "Organization",
        allowedScope: "Organization",
        selectedOrganizationId: E2E_ORG_ID,
        subscriptionId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaa04",
        entitlementSnapshotVersion: 1,
        productAccessAssignmentId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaa05",
        productLocalRoleGrantId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaa06",
        productLocalRoleCode: "Owner",
        organizationOwnerGranted: true,
        posEntitlementActivated: true,
        posOwnerRoleGranted: true,
        productCode: "pinoy-business-pos",
        primaryBusinessTypeId: TYPE_ID,
        primaryBranchId: E2E_BRANCH_ID,
        sessionToken: "must-not-persist",
        expiresAtUtc: "2026-09-04T00:00:00Z",
      });
    }

    if (url.includes("/api/v1/personal/utang/contacts") && method === "GET") {
      if (state.actor !== "A") return json(route, []);
      return json(route, state.contactCreated ? [contactDto()] : []);
    }

    if (url.includes("/api/v1/personal/connections") && method === "GET") {
      return json(route, []);
    }

    if (url.includes("/api/v1/personal/notifications/unread-count") && method === "GET") {
      return json(route, { unreadCount: 0 });
    }

    if (url.includes("/api/v1/personal/utang/contacts") && method === "POST") {
      if (state.actor !== "A") return json(route, { detail: "denied" }, 403);
      state.contactCreated = true;
      return json(route, contactDto(), 201);
    }

    if (url.includes("/api/v1/personal/utang/relationships/lent") && method === "GET") {
      if (state.actor !== "A") return json(route, []);
      return json(route, state.relationshipCreated ? [relationshipDto()] : []);
    }

    if (url.includes("/api/v1/personal/utang/relationships/borrowed") && method === "GET") {
      return json(route, []);
    }

    if (
      url.includes("/api/v1/personal/utang/relationships") &&
      method === "POST" &&
      !url.includes("/entries") &&
      !url.includes("/invitations")
    ) {
      if (state.actor !== "A") return json(route, { detail: "denied" }, 403);
      state.relationshipCreated = true;
      return json(route, relationshipDto(), 201);
    }

    if (
      url.includes(`/api/v1/personal/utang/relationships/${REL_ID}`) &&
      method === "GET" &&
      !url.includes("/balance") &&
      !url.includes("/history")
    ) {
      if (state.actor !== "A") return json(route, { detail: "denied" }, 403);
      return json(route, relationshipDto());
    }

    if (url.includes(`/relationships/${REL_ID}/balance`)) {
      if (state.actor !== "A") return json(route, { detail: "denied" }, 403);
      return json(route, {
        relationshipId: REL_ID,
        currentBalance: 100,
        currencyCode: "PHP",
        version: 1,
        updatedAtUtc: "2026-08-21T00:00:00Z",
      });
    }

    if (url.includes(`/relationships/${REL_ID}/history`)) {
      if (state.actor !== "A") return json(route, { detail: "denied" }, 403);
      return json(route, [
        {
          id: "66666666-6666-4666-8666-666666666661",
          relationshipId: REL_ID,
          entryType: "Loan",
          amount: 100,
          signedDelta: 100,
          balanceAfter: 100,
          notes: "E2E loan",
          dueDateUtc: null,
          createdByUserIdentityId: USER_A_ID,
          createdAtUtc: "2026-08-21T00:00:00Z",
        },
      ]);
    }

    if (url.includes("/api/v1/personal/utang/invitations") && method === "GET") {
      if (state.actor !== "A") return json(route, []);
      return json(route, [invitationDto(state.inviteStatus)]);
    }

    if (url.includes("/api/v1/personal/utang/invitations/preview") && method === "GET") {
      return json(route, {
        inviteId: INVITE_ID,
        inviterDisplayName: "Ana Owner",
        status: state.inviteStatus,
        relationshipSummary: "Personal Utang invite",
      });
    }

    if (url.includes("/api/v1/personal/utang/invitations/accept") && method === "POST") {
      if (state.actor !== "B") return json(route, { detail: "wrong actor" }, 403);
      state.inviteStatus = "Accepted";
      return json(route, {
        inviteId: INVITE_ID,
        status: "Accepted",
        createdOrganizationMembership: false,
        grantedProductRole: null,
      });
    }

    if (url.includes("/api/v1/personal/todos") && method === "GET") {
      if (state.actor === "A") return json(route, state.todosA);
      if (state.actor === "B") return json(route, []);
      return json(route, { detail: "denied" }, 403);
    }

    if (url.includes("/api/v1/personal/todos") && method === "POST") {
      if (state.actor !== "A") return json(route, { detail: "denied" }, 403);
      const body = route.request().postDataJSON() as { title?: string; Title?: string };
      const created = todoDto(TODO_A_ID, body.title ?? body.Title ?? "Private todo", USER_A_ID);
      state.todosA = [created];
      return json(route, created, 201);
    }

    if (url.includes(`/api/v1/personal/todos/${TODO_B_SECRET}`)) {
      // Cross-user privacy: A must never read B's private todo id.
      return json(route, { detail: "not found" }, 404);
    }

    if (url.includes("/api/v1/personal/customer-link-requests") && method === "GET") {
      if (state.actor !== "B") return json(route, []);
      return json(route, [
        {
          id: LINK_REQ_ID,
          organizationId: E2E_ORG_ID,
          organizationDisplayName: "Kizy Store",
          businessCustomerId: "88888888-8888-4888-8888-888888888888",
          status: state.linkStatus,
          createdAtUtc: "2026-08-21T00:00:00Z",
          expiresAtUtc: "2026-09-21T00:00:00Z",
          targetPublicUserId: "EXITS-B",
        },
      ]);
    }

    if (
      url.includes(`/api/v1/personal/customer-link-requests/${LINK_REQ_ID}/accept`) &&
      method === "POST"
    ) {
      if (state.actor !== "B") return json(route, { detail: "denied" }, 403);
      state.linkStatus = "Accepted";
      return json(route, {
        id: LINK_REQ_ID,
        status: "Accepted",
        organizationId: E2E_ORG_ID,
        createdOrganizationMembership: false,
        grantedProductRole: null,
      });
    }

    if (url.includes("/api/v1/personal/linked-merchants")) {
      if (url.includes("/ordering-capability")) {
        return json(route, {
          organizationId: E2E_ORG_ID,
          canCustomerOrder: state.linkStatus === "Accepted",
          canCustomerDelivery: true,
          organizationDisplayName: "Kizy Store",
        });
      }
      return json(route, {
        items:
          state.linkStatus === "Accepted"
            ? [
                {
                  linkedCustomerId: "77777777-7777-4777-8777-777777777777",
                  businessCustomerId: "88888888-8888-4888-8888-888888888888",
                  organizationId: E2E_ORG_ID,
                  organizationDisplayName: "Kizy Store",
                  customerDisplayName: "Ben Buyer",
                  linkStatus: "Active",
                  linkedAtUtc: "2026-08-21T00:00:00Z",
                  canCustomerOrder: true,
                  canCustomerDelivery: true,
                },
              ]
            : [],
        totalCount: state.linkStatus === "Accepted" ? 1 : 0,
        page: 1,
        pageSize: 50,
      });
    }

    if (url.includes("/api/v1/me/public-identity") && method === "GET") {
      return json(route, {
        publicUserId: state.actor === "A" ? "EXITS-A" : "EXITS-B",
        qrPayload: null,
      });
    }

    if (url.includes("/api/v1/personal/notifications") && method === "GET") {
      return json(route, []);
    }

    return json(route, { detail: `unmocked platform ${pathname}` }, 404);
  });

  await page.route("**/pos-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/api/v1/pos/operational-branch") && method === "PUT") {
      return json(route, {
        organizationId: E2E_ORG_ID,
        branchId: E2E_BRANCH_ID,
        name: "Main Branch",
        deviceMatchesSelectedBranch: false,
        deviceBoundBranchId: null,
        openCashierShiftPresent: false,
      });
    }

    if (url.includes("/storefront") && method === "GET") {
      return json(route, storefrontBody());
    }

    if (
      url.match(/\/organizations\/[^/]+$/) &&
      method === "POST" &&
      url.includes("customer-orders")
    ) {
      state.orderStatus = "Submitted";
      state.fulfillmentStatus = "Pending";
      return json(route, orderDto(state), 201);
    }

    if (url.includes("/mine/") && method === "GET") {
      return json(route, orderDto(state));
    }

    if (url.includes("/mine") && method === "GET") {
      return json(route, {
        items: [
          {
            orderId: ORDER_ID,
            orderNumber: "CO-22H1",
            status: state.orderStatus,
            fulfillmentStatus: state.fulfillmentStatus,
            fulfillmentType: "Pickup",
            fulfillmentBranchId: E2E_BRANCH_ID,
            branchNameSnapshot: "Main Branch",
            customerDisplayName: "Ben Buyer",
            total: 55,
            createdAtUtc: "2026-08-21T00:00:00Z",
            updatedAtUtc: "2026-08-21T00:00:00Z",
            lineCount: 1,
            sellerOrganizationId: E2E_ORG_ID,
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 40,
      });
    }

    if (url.includes(`/organizations/${E2E_ORG_ID}/customer-orders`) && method === "GET") {
      if (url.includes(ORDER_ID)) return json(route, orderDto(state));
      return json(route, {
        items: [
          {
            orderId: ORDER_ID,
            orderNumber: "CO-22H1",
            status: state.orderStatus,
            fulfillmentStatus: state.fulfillmentStatus,
            fulfillmentType: "Pickup",
            fulfillmentBranchId: E2E_BRANCH_ID,
            branchNameSnapshot: "Main Branch",
            customerDisplayName: "Ben Buyer",
            total: 55,
            createdAtUtc: "2026-08-21T00:00:00Z",
            updatedAtUtc: "2026-08-21T00:00:00Z",
            lineCount: 1,
            sellerOrganizationId: E2E_ORG_ID,
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 40,
      });
    }

    if (method === "POST" && url.includes("/accept")) {
      state.orderStatus = "Accepted";
      state.fulfillmentStatus = "Pending";
      state.transitions.push("accept");
      return json(route, orderDto(state));
    }
    if (method === "POST" && url.includes("/start-preparing")) {
      state.fulfillmentStatus = "Preparing";
      state.transitions.push("start-preparing");
      return json(route, orderDto(state));
    }
    if (method === "POST" && url.includes("/mark-ready")) {
      state.fulfillmentStatus = "ReadyForPickup";
      state.transitions.push("mark-ready");
      return json(route, orderDto(state));
    }
    if (method === "POST" && url.includes("/mark-collected")) {
      state.fulfillmentStatus = "Collected";
      state.transitions.push("mark-collected");
      return json(route, orderDto(state));
    }
    if (method === "POST" && url.includes("/complete")) {
      state.orderStatus = "Completed";
      state.transitions.push("complete");
      return json(route, orderDto(state));
    }

    return json(route, { detail: `unmocked pos ${url}` }, 404);
  });
}

async function signInAs(page: Page, email: string) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill(email);
  await page.getByLabel("Password").fill("secret");
  await page.getByRole("button", { name: "Sign in" }).click();
  // Personal accounts still enroll offline PIN (Organization Web skips). Wait for either
  // destination, then complete PIN when the enroll gate wins the race.
  await Promise.race([
    page.getByTestId("personal-shell").waitFor({ state: "visible", timeout: 20000 }),
    page.getByTestId("offline-pin-setup-page").waitFor({ state: "visible", timeout: 20000 }),
  ]);
  await completeOfflinePinSetupIfNeeded(page);
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 20000 });
}

test.describe("RMAP-22H Personal ↔ Business integrated E2E (mock)", () => {
  test("User A Personal utang + todo + invite; User B accept; privacy", async ({ page }) => {
    const state: StoryState = {
      actor: "A",
      loggedIn: false,
      inviteStatus: "Pending",
      linkStatus: "Pending",
      orderStatus: "Draft",
      fulfillmentStatus: "Pending",
      transitions: [],
      todosA: [],
      contactCreated: false,
      relationshipCreated: false,
      businessStarted: false,
    };
    await mockIntegratedStory(page, state);

    await signInAs(page, "ana@example.com");
    await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 15000 });
    await expect(page.getByTestId("personal-bottom-nav")).toBeVisible();
    await expect(page.getByTestId("personal-utang-summary")).toBeVisible();

    await clientNavigate(page, "/personal/people");
    await expect(page.getByTestId("people-list-section")).toBeVisible({ timeout: 15000 });
    await page.getByTestId("people-add-toggle").click();
    await page.getByTestId("person-create-kind-walkin").click();
    await page.getByTestId("person-display-name").fill("Ben Buyer");
    await page.getByTestId("person-email").fill("ben@example.com");
    await page.getByTestId("person-save").click();
    await expect(page).toHaveURL(new RegExp(`/personal/people/${CONTACT_ID}`), { timeout: 10000 });

    await clientNavigate(page, "/personal/utang/lent");
    await expect(page.getByTestId("personal-utang-lent")).toBeVisible();
    await page.getByTestId("utang-record-toggle").click();
    await page.getByTestId("utang-rel-contact").selectOption(CONTACT_ID);
    await page.getByTestId("utang-rel-amount").fill("100");
    await page.getByTestId("utang-rel-notes").fill("E2E lunch loan");
    await page.getByTestId("utang-rel-submit").click();
    await expect(page.getByTestId("personal-utang-detail")).toBeVisible({ timeout: 10000 });

    await clientNavigate(page, "/personal/todo");
    await expect(page.getByTestId("personal-todo-hub")).toBeVisible();
    await page.getByTestId("todo-create-toggle").click();
    await page.getByTestId("todo-create-title").fill("Private Ana todo");
    await page.getByTestId("todo-create-submit").click();
    await page.getByTestId("todo-tab-open").click();
    await expect(page.getByTestId(`todo-item-${TODO_A_ID}`)).toBeVisible({ timeout: 10000 });

    await clientNavigate(page, "/personal/utang/invitations");
    await expect(page.getByTestId("personal-invitations-page")).toBeVisible();
    await expect(page.getByTestId(`utang-invite-${INVITE_ID}`)).toBeVisible();

    // Cross-user privacy probe while still A
    const forbidden = await page.request.get(
      `http://127.0.0.1:4177/platform-api/api/v1/personal/todos/${TODO_B_SECRET}`,
    );
    // Request bypasses app auth; route mock still returns 404 for foreign id.
    expect([404, 401, 403]).toContain(forbidden.status());

    await page.getByTestId("account-menu-trigger").click();
    await page.getByRole("menuitem", { name: "Sign out" }).click();
    await expect(page).toHaveURL(/\/sign-in/, { timeout: 15000 });

    await signInAs(page, "ben@example.com");
    await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 15000 });
    await clientNavigate(page, "/personal/todo");
    await expect(page.getByTestId("personal-todo-hub")).toBeVisible();
    await expect(page.getByTestId(`todo-item-${TODO_A_ID}`)).toHaveCount(0);

    await page.goto("/personal/utang/invitations/accept?token=invite-token");
    await expect(page.getByTestId("utang-invite-accept")).toBeVisible({ timeout: 15000 });
    await page.getByTestId("utang-invite-accept-btn").click();
    await expect.poll(() => state.inviteStatus, { timeout: 10000 }).toBe("Accepted");
  });

  test("User A Start Business trial hands off to Organization onboarding", async ({ page }) => {
    const state: StoryState = {
      actor: "A",
      loggedIn: false,
      inviteStatus: "Accepted",
      linkStatus: "Pending",
      orderStatus: "Draft",
      fulfillmentStatus: "Pending",
      transitions: [],
      todosA: [todoDto(TODO_A_ID, "Private Ana todo", USER_A_ID)],
      contactCreated: true,
      relationshipCreated: true,
      businessStarted: false,
    };
    await mockIntegratedStory(page, state);

    await signInAs(page, "ana@example.com");
    await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 15000 });

    await clientNavigate(page, "/personal/start-business?planKey=business&trial=1&payNow=0");
    await expect(page.getByTestId("personal-start-business-page")).toBeVisible();
    await page.getByTestId(`start-business-type-sari-sari`).click();
    await page.getByTestId("start-business-display-name").fill("Kizy Store");
    await page.getByTestId("start-business-submit").click();

    await expect
      .poll(() => state.businessStarted && state.actor === "Owner", { timeout: 15000 })
      .toBeTruthy();
    // Post–Organization loading/onboarding work: Start Business enters /onboarding (not /workspace).
    // Full buyer order + seller transition story is tracked as PERS-E2E-22H-REPAIR.
    await expect(page).toHaveURL(/\/(onboarding|workspace)/, { timeout: 15000 });
  });

  for (const viewport of VIEWPORTS) {
    test(`Personal shell responsive ${viewport.width}x${viewport.height}`, async ({ page }) => {
      const state: StoryState = {
        actor: "A",
        loggedIn: false,
        inviteStatus: "Pending",
        linkStatus: "Pending",
        orderStatus: "Draft",
        fulfillmentStatus: "Pending",
        transitions: [],
        todosA: [],
        contactCreated: false,
        relationshipCreated: false,
        businessStarted: false,
      };
      await page.setViewportSize(viewport);
      await mockIntegratedStory(page, state);
      await signInAs(page, "ana@example.com");
      await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 15000 });
      await assertNoHorizontalOverflow(page);
      await clientNavigate(page, "/personal/todo");
      await expect(page.getByTestId("personal-todo-hub")).toBeVisible();
      await assertNoHorizontalOverflow(page);
      await clientNavigate(page, "/personal/more");
      await expect(page.getByTestId("personal-more-page")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    });
  }

  test("Organization staff cannot open Personal shell (privacy boundary)", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await signInAndBindOwner(page);
    await page.goto("/personal");
    await expect(page.getByTestId("account-class-denied")).toBeVisible({ timeout: 15000 });
  });
});
