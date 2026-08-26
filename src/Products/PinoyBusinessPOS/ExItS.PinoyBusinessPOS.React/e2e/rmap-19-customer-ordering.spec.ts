import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import {
  E2E_BRANCH_ID,
  E2E_ORG_ID,
  chooseOwnerOperations,
  clientNavigate,
  mockBoundCashierSession,
  mockBoundOwnerSession,
  signInAndBindCashier,
  signInAndBindOwner,
  signInAsPersonal,
} from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const VIEWPORTS = [
  { width: 375, height: 812 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1440, height: 900 },
] as const;

const SELLER_ORG = E2E_ORG_ID;
const PRODUCT_ID = "33333333-3333-4333-8333-333333333333";
const ORDER_ID = "44444444-4444-4444-8444-444444444444";
const USER_ID = "55555555-5555-4555-8555-555555555555";

type OrderState = {
  status: string;
  fulfillmentStatus: string;
  fulfillmentType: string;
  deliveryFee: number;
  placeAttempts: number;
  stockConflictOnce: boolean;
  transitions: string[];
};

function storefrontBody(paused = false) {
  return {
    organizationId: SELLER_ORG,
    organizationDisplayName: "E2E Sari-Sari",
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
        onlineOrdersPaused: paused,
        storeStatusMessage: paused ? "Paused" : null,
      },
    ],
  };
}

function orderDto(state: OrderState) {
  return {
    orderId: ORDER_ID,
    sellerOrganizationId: SELLER_ORG,
    orderNumber: "CO-1001",
    status: state.status,
    fulfillmentStatus: state.fulfillmentStatus,
    paymentStatus: "Unpaid",
    paymentMethod: "Cash",
    fulfillmentType: state.fulfillmentType,
    fulfillmentBranchId: E2E_BRANCH_ID,
    branchNameSnapshot: "Main Branch",
    customerPartyType: "Personal",
    customerDisplayName: "Paul Personal",
    customerPlatformUserId: USER_ID,
    merchandiseSubtotal: 55,
    deliveryFee: state.deliveryFee,
    total: 55 + state.deliveryFee,
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
    delivery:
      state.fulfillmentType === "Delivery"
        ? {
            recipientName: "Paul Personal",
            recipientPhone: "09171234567",
            addressLine1: "123 Test St",
            addressLine2: null,
            city: "Manila",
            deliveryNotes: null,
            destinationLatitude: 14.5995,
            destinationLongitude: 120.9842,
            branchLatitudeSnapshot: 14.6,
            branchLongitudeSnapshot: 121.0,
            distanceKm: 1.2,
            minimumOrderAmountSnapshot: 100,
            baseDeliveryFeeSnapshot: 40,
            includedDistanceKmSnapshot: 2,
            additionalFeePerKmSnapshot: 10,
            maximumDeliveryDistanceKmSnapshot: 8,
            freeDeliveryThresholdSnapshot: null,
            distanceCharge: 0,
            finalDeliveryFee: state.deliveryFee,
            freeDeliveryApplied: false,
          }
        : null,
    createdAtUtc: "2026-08-21T00:00:00Z",
    submittedAtUtc: "2026-08-21T00:00:00Z",
    updatedAtUtc: "2026-08-21T00:00:00Z",
  };
}

async function mockCustomerOrderingApis(page: import("@playwright/test").Page) {
  const state: OrderState = {
    status: "Submitted",
    fulfillmentStatus: "Pending",
    fulfillmentType: "Pickup",
    deliveryFee: 0,
    placeAttempts: 0,
    stockConflictOnce: true,
    transitions: [],
  };

  await page.route("**/platform-api/api/v1/personal/linked-merchants**", async (route) => {
    const url = route.request().url();
    if (url.includes("/ordering-capability")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          organizationId: SELLER_ORG,
          canCustomerOrder: true,
          canCustomerDelivery: true,
          organizationDisplayName: "E2E Sari-Sari",
        }),
      });
    }
    return route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        items: [
          {
            linkedCustomerId: "77777777-7777-4777-8777-777777777777",
            businessCustomerId: "88888888-8888-4888-8888-888888888888",
            organizationId: SELLER_ORG,
            organizationDisplayName: "E2E Sari-Sari",
            customerDisplayName: "Paul Personal",
            linkStatus: "Active",
            linkedAtUtc: "2026-08-01T00:00:00Z",
            canCustomerOrder: true,
            canCustomerDelivery: true,
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 50,
      }),
    });
  });

  await page.route("**/pos-api/api/v1/pos/customer-orders/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/storefront") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(storefrontBody()),
      });
    }

    if (url.includes("/quote-delivery") && method === "POST") {
      const body = route.request().postDataJSON() as {
        destinationLatitude: number;
        destinationLongitude: number;
        merchandiseSubtotal: number;
      };
      const fee = 40;
      state.deliveryFee = fee;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          available: true,
          unavailableReason: null,
          distanceKm: 1.2,
          extraDistanceKm: 0,
          distanceCharge: 0,
          deliveryFee: fee,
          freeDeliveryApplied: false,
          minimumOrderAmount: 100,
          maximumDeliveryDistanceKm: 8,
          requestedLatitude: body.destinationLatitude,
          requestedLongitude: body.destinationLongitude,
          merchandiseSubtotal: body.merchandiseSubtotal,
        }),
      });
    }

    if (url.match(/\/organizations\/[^/]+$/) && method === "POST") {
      state.placeAttempts += 1;
      if (state.stockConflictOnce && state.placeAttempts === 1) {
        state.stockConflictOnce = false;
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({
            title: "Conflict",
            detail: "Insufficient stock for one or more lines.",
            errorCode: "pos.inventory.insufficient_stock",
          }),
        });
      }
      const body = route.request().postDataJSON() as {
        fulfillmentType: string;
      };
      state.status = "Submitted";
      state.fulfillmentStatus = "Pending";
      state.fulfillmentType = body.fulfillmentType ?? "Pickup";
      if (state.fulfillmentType !== "Delivery") {
        state.deliveryFee = 0;
      }
      return route.fulfill({
        status: 201,
        contentType: "application/json",
        body: JSON.stringify(orderDto(state)),
      });
    }

    if (url.includes("/mine/") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(orderDto(state)),
      });
    }

    if (url.includes("/mine") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [
            {
              orderId: ORDER_ID,
              orderNumber: "CO-1001",
              status: state.status,
              fulfillmentStatus: state.fulfillmentStatus,
              fulfillmentType: state.fulfillmentType,
              fulfillmentBranchId: E2E_BRANCH_ID,
              branchNameSnapshot: "Main Branch",
              customerDisplayName: "Paul Personal",
              total: 55 + state.deliveryFee,
              createdAtUtc: "2026-08-21T00:00:00Z",
              updatedAtUtc: "2026-08-21T00:00:00Z",
              lineCount: 1,
              sellerOrganizationId: SELLER_ORG,
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 40,
        }),
      });
    }

    return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });

  await page.route("**/pos-api/api/v1/pos/organizations/*/customer-orders**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (method === "GET" && !url.includes(ORDER_ID)) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [
            {
              orderId: ORDER_ID,
              orderNumber: "CO-1001",
              status: state.status,
              fulfillmentStatus: state.fulfillmentStatus,
              fulfillmentType: state.fulfillmentType,
              fulfillmentBranchId: E2E_BRANCH_ID,
              branchNameSnapshot: "Main Branch",
              customerDisplayName: "Paul Personal",
              total: 55 + state.deliveryFee,
              createdAtUtc: "2026-08-21T00:00:00Z",
              updatedAtUtc: "2026-08-21T00:00:00Z",
              lineCount: 1,
              sellerOrganizationId: SELLER_ORG,
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 40,
        }),
      });
    }

    if (method === "GET" && url.includes(ORDER_ID)) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(orderDto(state)),
      });
    }

    if (method === "POST" && url.includes("/accept")) {
      state.status = "Accepted";
      state.fulfillmentStatus = "Pending";
      state.transitions.push("accept");
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(orderDto(state)),
      });
    }

    if (method === "POST" && url.includes("/start-preparing")) {
      state.fulfillmentStatus = "Preparing";
      state.transitions.push("start-preparing");
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(orderDto(state)),
      });
    }

    if (method === "POST" && url.includes("/mark-ready")) {
      state.fulfillmentStatus = state.fulfillmentType === "Pickup" ? "ReadyForPickup" : "Ready";
      state.transitions.push("mark-ready");
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(orderDto(state)),
      });
    }

    if (method === "POST" && url.includes("/mark-collected")) {
      state.fulfillmentStatus = "Collected";
      state.transitions.push("mark-collected");
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(orderDto(state)),
      });
    }

    if (method === "POST" && url.includes("/complete")) {
      state.status = "Completed";
      state.transitions.push("complete");
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(orderDto(state)),
      });
    }

    return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });

  return state;
}

async function mockPersonalBuyerSession(page: import("@playwright/test").Page) {
  let loggedIn = false;
  const personalMe = {
    sessionId: "22222222-2222-2222-2222-222222222222",
    userId: USER_ID,
    username: "paul@gmail.com",
    displayName: "Paul Personal",
    email: "paul@gmail.com",
    accountClass: "Personal",
    homeOrganizationId: null,
    organizationContextLocked: false,
  };

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
        body: JSON.stringify(personalMe),
      });
    }

    if (url.includes("/api/v1/platform/auth/login") && method === "POST") {
      loggedIn = true;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ ...personalMe, sessionToken: "must-not-persist" }),
      });
    }

    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([]),
      });
    }

    if (url.includes("/api/v1/platform/auth/token") && method === "POST") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          accessToken: "e2e-personal-buyer-token",
          productAccessAllowed: false,
        }),
      });
    }

    if (url.includes("/api/v1/platform/auth/logout") && method === "POST") {
      loggedIn = false;
      return route.fulfill({ status: 204, body: "" });
    }

    if (url.includes("/api/v1/personal/linked-merchants")) {
      return route.fallback();
    }

    return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
  });
}

async function signInPersonalBuyer(page: import("@playwright/test").Page) {
  await signInAsPersonal(page);
  await page.getByTestId("personal-home-page").waitFor({ state: "visible", timeout: 15000 });
}

async function signInOwnerOperations(page: import("@playwright/test").Page) {
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

test.describe("RMAP-19 customer ordering", () => {
  test("personal buyer browses, handles stock conflict, places pickup order", async ({ page }) => {
    await mockPersonalBuyerSession(page);
    const state = await mockCustomerOrderingApis(page);
    await signInPersonalBuyer(page);

    await clientNavigate(page, "/personal/linked-merchants");
    await expect(page.getByTestId("linked-merchants-page")).toBeVisible();
    await expect(page.getByTestId("linked-merchant-card")).toBeVisible();
    await page.getByTestId("open-merchant-shop").click();
    await expect(page.getByTestId("merchant-shop-page")).toBeVisible();
    await page.getByTestId("cart-increment").click();
    await expect(page.getByTestId("cart-qty")).toHaveText("1");
    await page.getByTestId("shop-review").click();
    await expect(page.getByTestId("merchant-checkout-page")).toBeVisible();

    await page.getByTestId("place-order").click();
    await expect(page.getByText("Stock changed")).toBeVisible();
    await expect(page.getByTestId("stock-conflict-refresh")).toBeVisible();

    await page.getByTestId("place-order").click();
    await expect(page.getByTestId("my-order-detail-page")).toBeVisible({ timeout: 15000 });
    expect(state.status).toBe("Submitted");
    expect(state.fulfillmentType).toBe("Pickup");
  });

  test("delivery fee comes from server quote", async ({ page }) => {
    await mockPersonalBuyerSession(page);
    await mockCustomerOrderingApis(page);
    await signInPersonalBuyer(page);
    await clientNavigate(page, `/personal/linked-merchants/${SELLER_ORG}/shop`);
    await page.getByTestId("cart-increment").click();
    await page.getByTestId("shop-review").click();
    await page.getByTestId("fulfillment-delivery").click();
    await page.getByTestId("delivery-recipient").fill("Paul Personal");
    await page.getByTestId("delivery-address").fill("123 Test St");
    await page.getByTestId("delivery-lat").fill("14.5995");
    await page.getByTestId("delivery-lng").fill("120.9842");
    await expect(page.getByTestId("delivery-fee-quote")).toContainText("40.00");
    await expect(page.getByText(/calculated by the server/i)).toBeVisible();
  });

  test("seller queue accepts and advances allowed transitions", async ({ page }) => {
    await mockBoundOwnerSession(page);
    const state = await mockCustomerOrderingApis(page);
    await signInOwnerOperations(page);
    await clientNavigate(page, "/orders");
    await expect(page.getByTestId("seller-orders-page")).toBeVisible();
    await expect(page.getByTestId("seller-order-card")).toBeVisible();
    await page.getByRole("link", { name: "Open" }).click();
    await expect(page.getByTestId("seller-order-detail-page")).toBeVisible();
    await page.getByTestId("seller-action-accept").click();
    await expect(page.getByTestId("seller-action-startpreparing")).toBeVisible();
    await page.getByTestId("seller-action-startpreparing").click();
    await page.getByTestId("seller-action-markready").click();
    await page.getByTestId("seller-action-markcollected").click();
    await page.getByTestId("seller-action-complete").click();
    expect(state.transitions).toEqual([
      "accept",
      "start-preparing",
      "mark-ready",
      "mark-collected",
      "complete",
    ]);
    expect(state.status).toBe("Completed");
  });

  test("cashier is denied seller order queue", async ({ page }) => {
    await mockBoundCashierSession(page);
    await mockCustomerOrderingApis(page);
    await signInAndBindCashier(page);
    await clientNavigate(page, "/orders");
    await expect(page.getByTestId("customer-orders-view-denied")).toBeVisible();
  });

  test("does not expose public slug landing routes", async ({ page }) => {
    await mockPersonalBuyerSession(page);
    await signInPersonalBuyer(page);
    await clientNavigate(page, "/store/demo-slug");
    await expect(page.getByRole("heading", { name: /Page not found/i })).toBeVisible();
  });

  test("seller queue is responsive across viewports", async ({ page }) => {
    await mockBoundOwnerSession(page);
    await mockCustomerOrderingApis(page);
    await signInOwnerOperations(page);
    for (const viewport of VIEWPORTS) {
      await page.setViewportSize(viewport);
      await clientNavigate(page, "/orders");
      await expect(page.getByTestId("seller-orders-page")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    }
  });
});
