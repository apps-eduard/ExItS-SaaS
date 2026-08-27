/**
 * EXITS-V1-COMPLETENESS-02 — Ordering readiness + notifications + durable Personal cart (mock).
 */
import { expect, test, type Page } from "@playwright/test";
import {
  chooseOwnerManageBusiness,
  clientNavigate,
  completeOfflinePinSetupIfNeeded,
  E2E_BRANCH_ID,
  E2E_ORG_ID,
  E2E_PERSONAL_USER_ID,
  mockBoundOwnerSession,
  mockPersonalSession,
  signInAndBindOwner,
  signInAsPersonal,
} from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const ORDER_ID = "44444444-4444-4444-8444-444444444444";
const PRODUCT_ID = "55555555-5555-4555-8555-555555555555";
const CATEGORY_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const TRANSFER_ID = "66666666-6666-4666-8666-666666666666";
const NOTIF_SELLER = "77777777-7777-4777-8777-777777777777";
const NOTIF_BUYER = "88888888-8888-4888-8888-888888888888";
const NOTIF_OWN = "99999999-9999-4999-8999-999999999999";
const BC_ID = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";

function json(route: { fulfill: (r: object) => Promise<void> }, body: unknown, status = 200) {
  return route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

function storefrontBody() {
  return {
    organizationId: E2E_ORG_ID,
    organizationDisplayName: "Kizy Store",
    canCustomerOrder: true,
    canCustomerDelivery: false,
    categories: [{ categoryId: CATEGORY_ID, name: "Grocery" }],
    products: [
      {
        productId: PRODUCT_ID,
        name: "Rice 1kg",
        sku: "RICE",
        unitOfMeasure: "pc",
        categoryId: CATEGORY_ID,
        unitPrice: 100,
        isAvailable: true,
        tracksInventory: false,
        availableQuantity: null,
        availabilityStatus: "Untracked",
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
        name: "Main",
        pickupEnabled: true,
        deliveryEnabled: false,
        customerOrderingOperational: true,
        pickupOperational: true,
        deliveryOperational: false,
        onlineOrdersPaused: false,
        storeStatusMessage: null,
      },
    ],
  };
}

function orderDto(status = "Submitted") {
  return {
    orderId: ORDER_ID,
    sellerOrganizationId: E2E_ORG_ID,
    orderNumber: "CO-C02",
    status,
    fulfillmentStatus: "Pending",
    paymentStatus: "Unpaid",
    paymentMethod: "Cash",
    fulfillmentType: "Pickup",
    fulfillmentBranchId: E2E_BRANCH_ID,
    branchNameSnapshot: "Main",
    customerPartyType: "Personal",
    customerDisplayName: "Buyer",
    customerPlatformUserId: E2E_PERSONAL_USER_ID,
    platformBusinessCustomerId: BC_ID,
    customerBuyerOrganizationId: null,
    customerBuyerPublicOrganizationId: null,
    merchandiseSubtotal: 100,
    deliveryFee: 0,
    total: 100,
    stockReservationState: "None",
    rejectReason: null,
    rejectNotes: null,
    delivery: null,
    lines: [
      {
        lineId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
        productId: PRODUCT_ID,
        lineNumber: 1,
        nameSnapshot: "Rice 1kg",
        skuSnapshot: "RICE",
        unitSnapshot: "pc",
        quantity: 1,
        unitPrice: 100,
        discount: 0,
        lineTotal: 100,
      },
    ],
    createdAtUtc: "2026-08-27T10:00:00Z",
    submittedAtUtc: "2026-08-27T10:00:00Z",
    acceptedAtUtc: status === "Accepted" ? "2026-08-27T10:05:00Z" : null,
    readyAtUtc: null,
    readyBy: null,
    outForDeliveryAtUtc: null,
    outForDeliveryBy: null,
    deliveredAtUtc: null,
    deliveredBy: null,
    collectedAtUtc: null,
    collectedBy: null,
    completedAtUtc: null,
    updatedAtUtc: "2026-08-27T10:00:00Z",
  };
}

/** Soften mockPersonalSession 404s that trip the global client-error overlay. */
async function installPersonalPlatformSafetyNet(
  page: Page,
  opts?: { notifications?: unknown[] },
) {
  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/api/v1/platform/auth/token") && method === "POST") {
      return json(route, {
        accessToken: "e2e-personal-buyer-token",
        productAccessAllowed: false,
      });
    }

    if (url.includes("/api/v1/personal/linked-merchants")) {
      if (url.includes("/ordering-capability")) {
        return json(route, {
          organizationId: E2E_ORG_ID,
          canCustomerOrder: true,
          canCustomerDelivery: false,
          organizationDisplayName: "Kizy Store",
        });
      }
      return json(route, {
        items: [
          {
            organizationId: E2E_ORG_ID,
            organizationDisplayName: "Kizy Store",
            businessCustomerId: BC_ID,
            publicOrganizationId: "ORG123456",
            linkedCustomerId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
            customerDisplayName: "Buyer",
            linkStatus: "Active",
            linkedAtUtc: "2026-08-01T00:00:00Z",
            canCustomerOrder: true,
            canCustomerDelivery: false,
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 50,
      });
    }

    if (url.includes("/api/v1/personal/notifications")) {
      if (url.includes("unread-count")) {
        return json(route, { unreadCount: opts?.notifications?.length ? 1 : 0 });
      }
      if (url.includes("/read") && method === "POST") {
        return json(route, {
          id: NOTIF_BUYER,
          title: "ok",
          preview: "ok",
          relatedType: "CustomerOrderAccepted",
          relatedId: ORDER_ID,
          isRead: true,
          createdAtUtc: "2026-08-27T10:00:00Z",
        });
      }
      if (method === "GET") {
        return json(route, opts?.notifications ?? []);
      }
    }

    if (url.includes("/api/v1/personal/dashboard") && method === "GET") {
      return json(route, {
        userIdentityId: E2E_PERSONAL_USER_ID,
        accountProfileId: E2E_PERSONAL_USER_ID,
        accountClass: "Personal",
        utangAvailable: true,
        contactCount: 0,
        activeRelationshipCount: 0,
        totalLentBalance: 0,
        totalBorrowedBalance: 0,
        pendingConfirmationCount: 0,
      });
    }

    if (url.includes("/api/v1/personal/ownership-transfers") && method === "GET") {
      return json(route, [
        {
          id: TRANSFER_ID,
          organizationId: E2E_ORG_ID,
          organizationDisplayName: "Kizy Store",
          publicOrganizationId: "ORG123456",
          fromOwnerUserId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
          toUserId: E2E_PERSONAL_USER_ID,
          toDisplayName: "Buyer",
          toPublicUserId: "EX-2222-3333",
          status: "Pending",
          createdAtUtc: "2026-08-27T11:00:00Z",
          expiresAtUtc: "2099-08-27T11:00:00Z",
          acceptedAtUtc: null,
          declinedAtUtc: null,
          cancelledAtUtc: null,
          completedAtUtc: null,
          updatedAtUtc: "2026-08-27T11:00:00Z",
        },
      ]);
    }

    if (url.includes("/api/v1/me/public-identity") && method === "GET") {
      return json(route, {
        publicUserId: "EX-BUYER-01",
        qrPayload: null,
        displayName: "Buyer",
      });
    }

    if (url.includes("/api/v1/personal/") && method === "GET") {
      return json(route, []);
    }

    return route.fallback();
  });
}

async function installPosStorefrontAndOrders(page: Page) {
  await page.route("**/pos-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/storefront") && method === "GET") {
      return json(route, storefrontBody());
    }

    if (url.includes("/linked-customers/") && method === "GET") {
      return json(route, { items: [], outstandingBalance: 0, currency: "PHP" });
    }

    if (url.includes("/health")) {
      return json(route, { status: "Healthy" });
    }

    if (
      method === "POST"
      && url.includes(`/customer-orders/organizations/${E2E_ORG_ID}`)
      && !url.includes("/cancel")
      && !url.includes("/quote")
    ) {
      return json(route, orderDto("Submitted"), 201);
    }

    if (url.includes(ORDER_ID) && method === "GET") {
      return json(route, orderDto(url.includes("Accepted") ? "Accepted" : "Submitted"));
    }

    if (url.includes("/customer-orders/") && method === "GET") {
      return json(route, orderDto("Submitted"));
    }

    return route.fallback();
  });
}

async function signInPersonalReady(page: Page) {
  await signInAsPersonal(page);
  await Promise.race([
    page.getByTestId("personal-shell").waitFor({ state: "visible", timeout: 20000 }),
    page.getByTestId("offline-pin-setup-page").waitFor({ state: "visible", timeout: 20000 }),
  ]);
  await completeOfflinePinSetupIfNeeded(page);
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 20000 });
}

async function signInOwnerReady(page: Page) {
  await signInAndBindOwner(page);
  await Promise.race([
    page
      .getByTestId("workspace-destination-manage_business")
      .waitFor({ state: "visible", timeout: 20000 }),
    page.getByTestId("offline-pin-setup-page").waitFor({ state: "visible", timeout: 20000 }),
  ]);
  await completeOfflinePinSetupIfNeeded(page);
  await chooseOwnerManageBusiness(page);
  const overlayDismiss = page.getByRole("button", { name: "Dismiss" });
  if (await overlayDismiss.isVisible().catch(() => false)) {
    await overlayDismiss.click();
  }
  await expect(page.getByTestId("org-essentials-page")).toBeVisible({ timeout: 20000 });
}

test.describe("EXITS-V1-COMPLETENESS-02", () => {
  test("Story A — durable cart survives refresh then clears after order", async ({ page }) => {
    test.setTimeout(90_000);
    await mockPersonalSession(page);
    await installPersonalPlatformSafetyNet(page);
    await installPosStorefrontAndOrders(page);

    let placed = false;
    await page.route("**/pos-api/**", async (route) => {
      const url = route.request().url();
      if (route.request().method() === "POST" && url.includes(`/organizations/${E2E_ORG_ID}`) && !url.includes("/cancel")) {
        placed = true;
        return json(route, orderDto("Submitted"), 201);
      }
      return route.fallback();
    });

    await signInPersonalReady(page);
    await clientNavigate(page, `/personal/linked-merchants/${E2E_ORG_ID}/shop`);
    await expect(page.getByTestId("merchant-shop-page")).toBeVisible({ timeout: 15000 });
    await expect(page.getByTestId("client-error-overlay")).toHaveCount(0);
    await page.getByTestId("cart-increment").click();
    await expect(page.getByTestId("cart-qty")).toHaveText("1");
    await expect(page.getByTestId("shop-cart-summary")).toBeVisible();

    await page.reload();
    await expect(page.getByTestId("merchant-shop-page")).toBeVisible({ timeout: 15000 });
    await expect(page.getByTestId("cart-qty")).toHaveText("1");

    const storage = await page.evaluate((userId) => {
      return window.localStorage.getItem(`exits.personal.cart.v1:${userId}`);
    }, E2E_PERSONAL_USER_ID);
    expect(storage).toContain(PRODUCT_ID);

    await page.getByTestId("shop-review").click();
    await expect(page.getByTestId("merchant-checkout-page")).toBeVisible();
    await page.getByTestId("place-order").click();
    await expect(page.getByTestId("my-order-detail-page")).toBeVisible({ timeout: 15000 });
    expect(placed).toBe(true);

    await clientNavigate(page, `/personal/linked-merchants/${E2E_ORG_ID}/shop`);
    await expect(page.getByTestId("merchant-shop-page")).toBeVisible();
    await expect(page.getByTestId("cart-qty")).toHaveCount(0);
    const after = await page.evaluate((userId) => {
      const raw = window.localStorage.getItem(`exits.personal.cart.v1:${userId}`);
      if (!raw) {
        return { lines: [] as unknown[] };
      }
      return JSON.parse(raw) as { lines: unknown[] };
    }, E2E_PERSONAL_USER_ID);
    expect(after.lines).toEqual([]);
  });

  test("Story B — seller notification opens seller order detail", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await page.route("**/platform-api/**", async (route) => {
      const url = route.request().url();
      const method = route.request().method();
      if (url.includes(`/organizations/${E2E_ORG_ID}/notifications`)) {
        if (url.includes("unread-count")) {
          return json(route, { unreadCount: 1 });
        }
        if (url.includes("/read") && method === "POST") {
          return json(route, {
            id: NOTIF_SELLER,
            organizationId: E2E_ORG_ID,
            recipientUserIdentityId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
            title: "ok",
            preview: "ok",
            relatedType: "CustomerOrderSubmitted",
            relatedId: ORDER_ID,
            isRead: true,
            createdAtUtc: "2026-08-27T10:00:00Z",
            readAtUtc: "2026-08-27T10:01:00Z",
          });
        }
        return json(route, [
          {
            id: NOTIF_SELLER,
            organizationId: E2E_ORG_ID,
            recipientUserIdentityId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
            title: "New customer order",
            preview: "CO-C02 · Buyer · 100.00",
            relatedType: "CustomerOrderSubmitted",
            relatedId: ORDER_ID,
            isRead: false,
            createdAtUtc: "2026-08-27T10:00:00Z",
            readAtUtc: null,
          },
        ]);
      }
      // Soften remaining org GETs that would 404 under mockBoundOwnerSession.
      if (method === "GET" && url.includes(`/organizations/${E2E_ORG_ID}/`) && !url.includes("/platform/")) {
        return json(route, []);
      }
      return route.fallback();
    });
    await page.route("**/pos-api/**", async (route) => {
      if (route.request().url().includes(ORDER_ID)) {
        return json(route, orderDto("Submitted"));
      }
      return route.fallback();
    });

    await signInOwnerReady(page);
    await clientNavigate(page, "/org/notifications");
    await expect(page.getByTestId(`org-notification-row-${NOTIF_SELLER}`)).toBeVisible({
      timeout: 15000,
    });
    const overlayDismiss = page.getByRole("button", { name: "Dismiss" });
    if (await overlayDismiss.isVisible().catch(() => false)) {
      await overlayDismiss.click();
    }
    await expect(page.getByTestId("client-error-overlay")).toHaveCount(0);
    await page.getByTestId(`org-notification-open-${NOTIF_SELLER}`).click();
    await expect(page.getByTestId("seller-order-detail-page")).toBeVisible({ timeout: 15000 });
    await expect(page).toHaveURL(new RegExp(`/orders/${ORDER_ID}`));
  });

  test("Story C — buyer notification opens personal order detail", async ({ page }) => {
    await mockPersonalSession(page);
    await installPersonalPlatformSafetyNet(page, {
      notifications: [
        {
          id: NOTIF_BUYER,
          title: "Order accepted",
          preview: "CO-C02 · Accepted",
          relatedType: "CustomerOrderAccepted",
          relatedId: ORDER_ID,
          isRead: false,
          createdAtUtc: "2026-08-27T10:05:00Z",
        },
      ],
    });
    await installPosStorefrontAndOrders(page);
    await page.route("**/pos-api/**", async (route) => {
      if (route.request().url().includes(ORDER_ID)) {
        return json(route, orderDto("Accepted"));
      }
      return route.fallback();
    });

    await signInPersonalReady(page);
    await clientNavigate(page, "/personal/notifications");
    await expect(page.getByTestId(`notification-row-${NOTIF_BUYER}`)).toBeVisible({
      timeout: 15000,
    });
    await page.getByTestId(`notification-row-${NOTIF_BUYER}`).locator("button").first().click();
    await expect(page.getByTestId("my-order-detail-page")).toBeVisible({ timeout: 15000 });
    await expect(page).toHaveURL(new RegExp(`/personal/orders/${ORDER_ID}`));
  });

  test("Story D — ownership notification opens ownership transfers", async ({ page }) => {
    await mockPersonalSession(page);
    await installPersonalPlatformSafetyNet(page, {
      notifications: [
        {
          id: NOTIF_OWN,
          title: "Ownership transfer",
          preview: "Kizy Store wants to transfer ownership to you.",
          relatedType: "OrganizationOwnershipTransfer",
          relatedId: TRANSFER_ID,
          isRead: false,
          createdAtUtc: "2026-08-27T11:00:00Z",
        },
      ],
    });

    await signInPersonalReady(page);
    await clientNavigate(page, "/personal/notifications");
    await expect(page.getByTestId(`notification-row-${NOTIF_OWN}`)).toBeVisible({ timeout: 15000 });
    await page.getByTestId(`notification-row-${NOTIF_OWN}`).locator("button").first().click();
    await expect(page).toHaveURL(/\/personal\/ownership-transfers/);
    await expect(page.getByTestId("personal-ownership-transfers-page")).toBeVisible({
      timeout: 15000,
    });
  });

  test("Story E — public landing does not falsely promise ordering", async ({ page }) => {
    const PUBLIC_ORG = "ORG123456";
    await page.route(`**/api/v1/public/stores/${PUBLIC_ORG}`, async (route) =>
      json(route, {
        publicOrganizationId: PUBLIC_ORG,
        displayName: "Kizy Store",
        orderingAvailable: false,
      }),
    );

    await page.goto(`/store/${PUBLIC_ORG}`);
    await expect(page.getByTestId("public-store-landing-page")).toBeVisible({ timeout: 15000 });
    await expect(page.getByTestId("public-store-name")).toHaveText("Kizy Store");
    await expect(page.getByText(/ordering is currently unavailable/i)).toBeVisible();
    await expect(page.getByTestId("public-store-sign-in")).toBeVisible();
  });
});
