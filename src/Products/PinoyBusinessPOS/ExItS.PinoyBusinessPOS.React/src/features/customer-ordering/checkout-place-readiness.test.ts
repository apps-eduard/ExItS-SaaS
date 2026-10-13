import { describe, expect, it } from "vitest";
import {
  checkoutPlaceBlocker,
  checkoutPlaceDisabled,
  shopCheckoutBlocker,
  type CheckoutPlaceState,
} from "@/features/customer-ordering/checkout-place-readiness";
import type { CustomerStorefrontBranchDto } from "@/api/pos/pos-customer-orders-client";

const branchId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

function ready(overrides: Partial<CheckoutPlaceState> = {}): CheckoutPlaceState {
  return {
    busy: false,
    merchantContextLoading: false,
    cartEmpty: false,
    workspaceMissing: false,
    selectionMissing: false,
    branchId,
    canPlace: true,
    fulfillmentType: "Pickup",
    onlineOrdersPaused: false,
    fulfillmentAvailability: "ready",
    paymentMethod: "Cash",
    gcashReference: "",
    utangInsufficient: false,
    deliveryIncomplete: false,
    deliveryQuoteUnavailable: false,
    buyerIdentityMissing: false,
    linkedCustomerMissing: false,
    ...overrides,
  };
}

function branch(partial: Partial<CustomerStorefrontBranchDto>): CustomerStorefrontBranchDto {
  return {
    branchId,
    name: "Main",
    pickupEnabled: true,
    deliveryEnabled: false,
    customerOrderingOperational: true,
    pickupOperational: true,
    deliveryOperational: false,
    onlineOrdersPaused: false,
    storeStatusMessage: null,
    deliveryServiceAreas: [],
    ...partial,
  };
}

describe("checkout place readiness", () => {
  it("allows a valid cash pickup", () => {
    expect(checkoutPlaceBlocker(ready())).toBeNull();
    expect(checkoutPlaceDisabled(ready())).toBe(false);
  });

  it("disables place when the branch is missing or cannot place", () => {
    expect(checkoutPlaceDisabled(ready({ branchId: null }))).toBe(true);
    expect(checkoutPlaceBlocker(ready({ branchId: null }))).not.toBeNull();
    expect(checkoutPlaceDisabled(ready({ canPlace: false }))).toBe(true);
    expect(checkoutPlaceBlocker(ready({ canPlace: false, fulfillmentAvailability: "pickup-unavailable" }))).toBe(
      "orders.placePickupUnavailable",
    );
  });

  it("explains every invalid checkout instead of a silent return", () => {
    const cases: Array<[Partial<CheckoutPlaceState>, string]> = [
      [{ workspaceMissing: true }, "orders.checkoutMissingWorkspace"],
      [{ cartEmpty: true }, "orders.checkoutEmptyCart"],
      [{ selectionMissing: true, branchId: null }, "orders.placePickupUnavailable"],
      [{ buyerIdentityMissing: true }, "orders.missingBuyerIdentity"],
      [{ linkedCustomerMissing: true }, "orders.missingLinkedCustomer"],
      [{ paymentMethod: "ManualGCash", gcashReference: " " }, "checkout.gcashReferenceRequired"],
      [{ paymentMethod: "Utang", utangInsufficient: true }, "orders.utangInsufficient"],
      [{ fulfillmentType: "Delivery", deliveryIncomplete: true }, "orders.deliveryFieldsRequired"],
      [{ fulfillmentType: "Delivery", deliveryQuoteUnavailable: true }, "orders.deliveryUnavailable"],
      [{ canPlace: false, onlineOrdersPaused: true }, "orders.placePaused"],
      [{ canPlace: false, fulfillmentAvailability: "no-method", branchId: null }, "orders.placeNoFulfillmentMethod"],
      [{ canPlace: false, fulfillmentAvailability: "store-closed" }, "orders.placeStoreClosed"],
    ];
    for (const [overrides, message] of cases) {
      expect(checkoutPlaceBlocker(ready(overrides))).toBe(message);
    }
  });

  it("keeps the button disabled while busy or the merchant context is loading", () => {
    expect(checkoutPlaceDisabled(ready({ busy: true }))).toBe(true);
    expect(checkoutPlaceDisabled(ready({ merchantContextLoading: true }))).toBe(true);
  });
});

describe("shop checkout guard", () => {
  it("blocks zero fulfillment methods and allows pickup-only or delivery-only", () => {
    expect(
      shopCheckoutBlocker(
        [branch({ pickupEnabled: false, deliveryEnabled: false, pickupOperational: false })],
        true,
        "no-method",
      ),
    ).toBe("orders.placeNoFulfillmentMethod");
    expect(shopCheckoutBlocker([branch({ pickupEnabled: true, deliveryEnabled: false })], false, "ready")).toBeNull();
    expect(
      shopCheckoutBlocker(
        [branch({ pickupEnabled: false, deliveryEnabled: true, deliveryOperational: true })],
        true,
        "ready",
      ),
    ).toBeNull();
  });

  it("blocks paused and temporarily unavailable pickup", () => {
    expect(
      shopCheckoutBlocker(
        [branch({ pickupOperational: false, onlineOrdersPaused: true, customerOrderingOperational: false })],
        false,
        "paused",
      ),
    ).toBe("orders.placePaused");
    expect(
      shopCheckoutBlocker(
        [branch({ pickupOperational: false })],
        false,
        "pickup-unavailable",
      ),
    ).toBe("orders.placePickupUnavailable");
  });

  it("allows a valid branch among several", () => {
    expect(
      shopCheckoutBlocker(
        [
          branch({ branchId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa", pickupOperational: false }),
          branch({ pickupEnabled: true, pickupOperational: true }),
        ],
        false,
        "ready",
      ),
    ).toBeNull();
  });
});
