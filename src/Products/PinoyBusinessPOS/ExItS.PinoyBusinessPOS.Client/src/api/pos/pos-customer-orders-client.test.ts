import { afterEach, describe, expect, it, vi } from "vitest";
import {
  getCustomerStorefront,
  listSellerCustomerOrders,
  placeCustomerOrder,
  quoteCustomerDelivery,
  sellerWorkspace,
} from "@/api/pos/pos-customer-orders-client";

vi.mock("@/api/platform/pos-access-token", () => ({
  getPosAccessToken: () => "test-access-token",
}));

vi.mock("@/workspace/browser-installation-identity", () => ({
  getDurableInstallationDeviceId: () => ({
    ok: true as const,
    installationDeviceId: "inst-1",
  }),
}));

const ORG = "11111111-1111-4111-8111-111111111111";
const BRANCH = "22222222-2222-4222-8222-222222222222";
const PRODUCT = "33333333-3333-4333-8333-333333333333";
const ORDER = "44444444-4444-4444-8444-444444444444";
const USER = "55555555-5555-4555-8555-555555555555";

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("pos-customer-orders-client", () => {
  it("loads storefront with fulfillmentBranchId query", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        organizationId: ORG,
        organizationDisplayName: "Store",
        canCustomerOrder: true,
        canCustomerDelivery: true,
        categories: [],
        products: [],
        productTotalCount: 0,
        page: 1,
        pageSize: 40,
        branches: [
          {
            branchId: BRANCH,
            name: "Main",
            pickupEnabled: true,
            deliveryEnabled: true,
            customerOrderingOperational: true,
            pickupOperational: true,
            deliveryOperational: true,
            onlineOrdersPaused: false,
          },
        ],
      }),
    });
    vi.stubGlobal("fetch", fetchMock);

    const dto = await getCustomerStorefront(sellerWorkspace(ORG), ORG, {
      fulfillmentBranchId: BRANCH,
    });
    expect(dto.canCustomerOrder).toBe(true);
    const url = String(fetchMock.mock.calls[0][0]);
    expect(url).toContain(`/customer-orders/organizations/${ORG}/storefront`);
    expect(url).toContain(`fulfillmentBranchId=${BRANCH}`);
  });

  it("quotes delivery fee from server path", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        available: true,
        unavailableReason: null,
        distanceKm: 1.5,
        extraDistanceKm: 0,
        distanceCharge: 0,
        deliveryFee: 40,
        freeDeliveryApplied: false,
        minimumOrderAmount: 100,
        maximumDeliveryDistanceKm: 8,
      }),
    });
    vi.stubGlobal("fetch", fetchMock);

    const quote = await quoteCustomerDelivery(sellerWorkspace(ORG), ORG, {
      fulfillmentBranchId: BRANCH,
      merchandiseSubtotal: 200,
      destinationLatitude: 14.5,
      destinationLongitude: 121.0,
    });
    expect(quote.deliveryFee).toBe(40);
    expect(String(fetchMock.mock.calls[0][0])).toContain("/quote-delivery");
  });

  it("places customer order with idempotency headers when clientOrderId set", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 201,
      json: async () => ({
        orderId: ORDER,
        sellerOrganizationId: ORG,
        orderNumber: "CO-1",
        status: "Submitted",
        fulfillmentStatus: "Pending",
        paymentStatus: "Unpaid",
        paymentMethod: "Cash",
        fulfillmentType: "Pickup",
        fulfillmentBranchId: BRANCH,
        branchNameSnapshot: "Main",
        customerPartyType: "Personal",
        customerDisplayName: "Paul",
        merchandiseSubtotal: 50,
        deliveryFee: 0,
        total: 50,
        stockReservationState: "Reserved",
        lines: [],
        createdAtUtc: "2026-08-21T00:00:00Z",
        updatedAtUtc: "2026-08-21T00:00:00Z",
      }),
    });
    vi.stubGlobal("fetch", fetchMock);

    await placeCustomerOrder(sellerWorkspace(ORG), ORG, {
      fulfillmentType: "Pickup",
      fulfillmentBranchId: BRANCH,
      customerPartyType: "Personal",
      customerDisplayName: "Paul",
      customerPlatformUserId: USER,
      lines: [{ productId: PRODUCT, quantity: 1 }],
      clientOrderId: ORDER,
      paymentMethod: "Cash",
    });

    const init = fetchMock.mock.calls[0][1] as RequestInit;
    const headers = new Headers(init.headers);
    expect(headers.get("Idempotency-Key")).toBeTruthy();
    expect(headers.get("X-Pos-Operation-Type")).toBe("customer_order.place");
  });

  it("lists seller orders under org path", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
    });
    vi.stubGlobal("fetch", fetchMock);
    await listSellerCustomerOrders(sellerWorkspace(ORG), { status: "Submitted" });
    expect(String(fetchMock.mock.calls[0][0])).toContain(`/organizations/${ORG}/customer-orders?`);
    expect(String(fetchMock.mock.calls[0][0])).toContain("status=Submitted");
  });
});
