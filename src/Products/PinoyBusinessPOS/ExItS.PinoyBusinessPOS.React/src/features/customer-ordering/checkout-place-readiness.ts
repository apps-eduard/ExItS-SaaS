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
  gcashReference: string;
  utangInsufficient: boolean;
  deliveryIncomplete: boolean;
  deliveryQuoteUnavailable: boolean;
  buyerIdentityMissing: boolean;
  linkedCustomerMissing: boolean;
};

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
  if (!state.canPlace) {
    return fulfillmentBlockerMessage(
      state.onlineOrdersPaused ? "paused" : state.fulfillmentAvailability,
      state.fulfillmentType,
    );
  }
  if (state.paymentMethod === "ManualGCash" && state.gcashReference.trim().length === 0) {
    return "checkout.gcashReferenceRequired";
  }
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
  const selection = resolveFulfillmentSelection(
    branches,
    canCustomerDelivery,
    pickupAvailable(branches) || !deliveryAvailable(branches, canCustomerDelivery)
      ? FulfillmentPickup
      : FulfillmentDelivery,
    null,
  );
  if (!pickupAvailable(branches) && !deliveryAvailable(branches, canCustomerDelivery)) {
    return fulfillmentBlockerMessage(fulfillmentAvailability ?? "no-method", selection.fulfillmentType);
  }
  return fulfillmentBlockerMessage(fulfillmentAvailability, selection.fulfillmentType);
}
