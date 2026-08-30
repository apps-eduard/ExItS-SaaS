import { describe, expect, it } from "vitest";
import {
  canIncrementStorefrontQuantity,
  STOREFRONT_AVAILABILITY,
} from "@/features/customer-ordering/storefront-availability";
import {
  cartMerchandiseSubtotal,
  decrementCartLine,
  ensureMerchantCart,
  EMPTY_PERSONAL_MERCHANT_CART,
  FulfillmentDelivery,
  FulfillmentPickup,
  getCartQuantity,
  incrementCartLine,
  resolveFulfillmentSelection,
} from "@/features/customer-ordering/personal-merchant-cart";
import {
  availableSellerActions,
  filterSellerOrdersClientSide,
} from "@/features/customer-ordering/seller-order-actions";
import type { CustomerOrderDto } from "@/api/pos/pos-customer-orders-client";
import {
  isCustomerOrderingUnavailable,
  isInsufficientStockError,
} from "@/api/pos/pos-customer-orders-client";
import { PosApiError } from "@/api/pos/pos-http";

const product = {
  productId: "11111111-1111-4111-8111-111111111111",
  name: "Rice",
  sku: "R1",
  unitOfMeasure: "kg",
  categoryId: null,
  unitPrice: 50,
  isAvailable: true,
  tracksInventory: true,
  availableQuantity: 2,
  availabilityStatus: STOREFRONT_AVAILABILITY.InStock,
  hasImage: false,
  imageVersion: null,
  imageSource: "None",
};

describe("storefront availability", () => {
  it("blocks increment beyond available quantity", () => {
    expect(canIncrementStorefrontQuantity(product, 1)).toBe(true);
    expect(canIncrementStorefrontQuantity(product, 2)).toBe(false);
  });

  it("allows untracked products", () => {
    expect(
      canIncrementStorefrontQuantity(
        { ...product, tracksInventory: false, availableQuantity: null },
        99,
      ),
    ).toBe(true);
  });
});

describe("personal merchant cart", () => {
  it("clears lines when switching merchants", () => {
    let state = ensureMerchantCart(
      EMPTY_PERSONAL_MERCHANT_CART,
      "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      "A",
    );
    state = incrementCartLine(state, product);
    expect(getCartQuantity(state, product.productId)).toBe(1);
    state = ensureMerchantCart(state, "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb", "B");
    expect(state.lines).toHaveLength(0);
  });

  it("computes merchandise subtotal and decrement", () => {
    let state = ensureMerchantCart(
      EMPTY_PERSONAL_MERCHANT_CART,
      "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      "A",
    );
    state = incrementCartLine(state, product);
    state = incrementCartLine(state, product);
    expect(cartMerchandiseSubtotal(state)).toBe(100);
    state = decrementCartLine(state, product.productId);
    expect(getCartQuantity(state, product.productId)).toBe(1);
  });

  it("resolves pickup vs delivery without inventing readiness", () => {
    const branches = [
      {
        branchId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
        name: "Main",
        pickupEnabled: true,
        deliveryEnabled: true,
        customerOrderingOperational: true,
        pickupOperational: true,
        deliveryOperational: false,
        onlineOrdersPaused: false,
        deliveryServiceAreas: null,
        storeStatusMessage: null,
      },
    ];
    const pickup = resolveFulfillmentSelection(branches, true, FulfillmentPickup, null);
    expect(pickup.canPlace).toBe(true);
    const delivery = resolveFulfillmentSelection(branches, true, FulfillmentDelivery, null);
    expect(delivery.canPlace).toBe(false);
  });
});

describe("seller order actions", () => {
  function order(partial: Partial<CustomerOrderDto>): CustomerOrderDto {
    return {
      orderId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      sellerOrganizationId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
      orderNumber: "1",
      status: "Submitted",
      fulfillmentStatus: "Pending",
      paymentStatus: "Unpaid",
      paymentMethod: "Cash",
      fulfillmentType: "Pickup",
      fulfillmentBranchId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
      branchNameSnapshot: "Main",
      customerPartyType: "Personal",
      customerDisplayName: "Paul",
      merchandiseSubtotal: 10,
      deliveryFee: 0,
      total: 10,
      stockReservationState: "Reserved",
      lines: [],
      createdAtUtc: "2026-08-21T00:00:00Z",
      updatedAtUtc: "2026-08-21T00:00:00Z",
      ...partial,
    };
  }

  it("offers accept/reject for submitted orders only", () => {
    expect(availableSellerActions(order({ status: "Submitted" }))).toEqual(["Accept", "Reject"]);
    expect(availableSellerActions(order({ status: "Completed" }))).toEqual([]);
  });

  it("offers delivery transitions only for delivery fulfillment", () => {
    expect(
      availableSellerActions(
        order({
          status: "Accepted",
          fulfillmentType: "Delivery",
          fulfillmentStatus: "Ready",
        }),
      ),
    ).toContain("OutForDelivery");
    expect(
      availableSellerActions(
        order({
          status: "Accepted",
          fulfillmentType: "Pickup",
          fulfillmentStatus: "ReadyForPickup",
        }),
      ),
    ).toContain("MarkCollected");
  });

  it("filters queue client-side for preparing/ready/issues", () => {
    const items = [
      { status: "Submitted", fulfillmentStatus: "Pending" },
      { status: "Accepted", fulfillmentStatus: "Preparing" },
      { status: "Accepted", fulfillmentStatus: "Ready" },
      { status: "Rejected", fulfillmentStatus: "Pending" },
    ];
    expect(filterSellerOrdersClientSide(items, "New")).toHaveLength(1);
    expect(filterSellerOrdersClientSide(items, "Preparing")).toHaveLength(1);
    expect(filterSellerOrdersClientSide(items, "Ready")).toHaveLength(1);
    expect(filterSellerOrdersClientSide(items, "Issues")).toHaveLength(1);
  });
});

describe("stock conflict detection", () => {
  it("detects insufficient stock PosApiError", () => {
    expect(
      isInsufficientStockError(
        new PosApiError(409, { errorCode: "pos.inventory.insufficient_stock", detail: "low" }),
      ),
    ).toBe(true);
    expect(isInsufficientStockError(new PosApiError(400, { errorCode: "other" }))).toBe(false);
  });
});

describe("ordering unavailable detection", () => {
  it("detects customer ordering unavailable PosApiError", () => {
    expect(
      isCustomerOrderingUnavailable(
        new PosApiError(403, {
          errorCode: "pos.customer_order.ordering.unavailable",
          detail: "This merchant is not accepting customer orders.",
        }),
      ),
    ).toBe(true);
    expect(isCustomerOrderingUnavailable(new PosApiError(400, { errorCode: "other" }))).toBe(
      false,
    );
  });
});
