import type { MessageKey } from "@/i18n/messages";
import type { CustomerStorefrontBranchDto } from "@/api/pos/pos-customer-orders-client";
import {
  deliveryAvailable,
  FulfillmentDelivery,
  FulfillmentPickup,
  pickupAvailable,
  resolveFulfillmentSelection,
} from "@/features/customer-ordering/personal-merchant-cart";

export type CheckoutPlaceState = {
  busy: boolean;
  merchantContextLoading: boolean;
  cartEmpty: boolean;
  workspaceMissing: boolean;
  selectionMissing: boolean;
  branchId: string | null;
  canPlace: boolean;
  fulfillmentType: string;
  onlineOrdersPaused: boolean;
  fulfillmentAvailability: string | null;
  paymentMethod: string;
  utangInsufficient: boolean;
  deliveryIncomplete: boolean;
  deliveryQuoteUnavailable: boolean;
  buyerIdentityMissing: boolean;
  linkedCustomerMissing: boolean;
  /** When true, date and time are required before place (closed store or schedule checkout). */
  scheduleRequired?: boolean;
  scheduleComplete?: boolean;
};

export type ShopCheckoutUiMode = "open" | "schedule" | "paused" | "closed" | "blocked";

export function shopCheckoutUiMode(
  branches: CustomerStorefrontBranchDto[],
  canCustomerDelivery: boolean,
  fulfillmentAvailability?: string | null,
  selectedBranch?: CustomerStorefrontBranchDto | null,
): ShopCheckoutUiMode {
  const availability = fulfillmentAvailability ?? null;
  if (selectedBranch?.onlineOrdersPaused || availability === "paused") {
    return "paused";
  }

  const immediate = branches.some(
    (branch) =>
      !branch.onlineOrdersPaused
      && branch.customerOrderingOperational
      && ((branch.pickupEnabled && branch.pickupOperational)
        || (canCustomerDelivery && branch.deliveryEnabled && branch.deliveryOperational)),
  );
  if (immediate) {
    return "open";
  }
  // Closed-by-hours is informational only — customer may still schedule & checkout.
  if (availability === "schedule-available" || availability === "store-closed") {
    return "schedule";
  }
  return "blocked";
}

export function checkoutPlaceBlocker(state: CheckoutPlaceState): MessageKey | null {
  if (state.busy || state.merchantContextLoading) {
    return null;
  }
  if (state.workspaceMissing) {
    return "orders.checkoutMissingWorkspace";
  }
  if (state.cartEmpty) {
    return "orders.checkoutEmptyCart";
  }
  if (state.buyerIdentityMissing) {
    return "orders.missingBuyerIdentity";
  }
  if (state.linkedCustomerMissing) {
    return "orders.missingLinkedCustomer";
  }
  if (state.selectionMissing || !state.branchId) {
    return fulfillmentBlockerMessage(state.fulfillmentAvailability, state.fulfillmentType);
  }
  if (state.onlineOrdersPaused) {
    return "orders.placePaused";
  }
  if (state.scheduleRequired && !state.scheduleComplete) {
    return "orders.scheduleDateTimeRequired";
  }
  if (!state.canPlace) {
    if (state.scheduleRequired && state.scheduleComplete) {
      // Scheduled placement while closed — server validates; do not hard-block on operational flags.
    } else {
      return fulfillmentBlockerMessage(
        state.fulfillmentAvailability,
        state.fulfillmentType,
      );
    }
  }
  // Manual GCash: buyer does not enter a reference at checkout; seller verifies receipt later.
  if (state.paymentMethod === "Utang" && state.utangInsufficient) {
    return "orders.utangInsufficient";
  }
  if (state.fulfillmentType === FulfillmentDelivery && state.deliveryIncomplete) {
    return "orders.deliveryFieldsRequired";
  }
  if (state.fulfillmentType === FulfillmentDelivery && state.deliveryQuoteUnavailable) {
    return "orders.deliveryUnavailable";
  }
  return null;
}

export function checkoutPlaceDisabled(state: CheckoutPlaceState): boolean {
  return (
    state.busy
    || state.merchantContextLoading
    || checkoutPlaceBlocker({ ...state, busy: false, merchantContextLoading: false }) !== null
  );
}

export function fulfillmentBlockerMessage(
  availability: string | null | undefined,
  fulfillmentType: string,
): MessageKey {
  switch (availability) {
    case "paused":
      return "orders.placePaused";
    case "no-method":
      return "orders.placeNoFulfillmentMethod";
    case "store-closed":
      return "orders.placeStoreClosed";
    case "schedule-available":
      return "orders.scheduleDateTimeRequired";
    case "delivery-unavailable":
      return "orders.placeDeliveryUnavailable";
    case "pickup-unavailable":
      return "orders.placePickupUnavailable";
    default:
      return fulfillmentType === FulfillmentDelivery
        ? "orders.placeDeliveryUnavailable"
        : "orders.placePickupUnavailable";
  }
}

export function shopCheckoutBlocker(
  branches: CustomerStorefrontBranchDto[],
  canCustomerDelivery: boolean,
  fulfillmentAvailability?: string | null,
): MessageKey | null {
  const availability = fulfillmentAvailability ?? null;
  if (availability === "schedule-available" || availability === "store-closed") {
    return null;
  }

  const placeable = branches.some(
    (branch) =>
      !branch.onlineOrdersPaused
      && branch.customerOrderingOperational
      && ((branch.pickupEnabled && branch.pickupOperational)
        || (canCustomerDelivery && branch.deliveryEnabled && branch.deliveryOperational)),
  );
  if (placeable) {
    return null;
  }

  if (availability === "paused") {
    return "orders.placePaused";
  }

  const selection = resolveFulfillmentSelection(
    branches,
    canCustomerDelivery,
    pickupAvailable(branches) || !deliveryAvailable(branches, canCustomerDelivery)
      ? FulfillmentPickup
      : FulfillmentDelivery,
    null,
  );
  if (!pickupAvailable(branches) && !deliveryAvailable(branches, canCustomerDelivery)) {
    return fulfillmentBlockerMessage(availability ?? "no-method", selection.fulfillmentType);
  }
  return fulfillmentBlockerMessage(availability, selection.fulfillmentType);
}
