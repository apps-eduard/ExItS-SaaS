import type {
  CustomerStorefrontBranchDto,
  CustomerStorefrontProductDto,
} from "@/api/pos/pos-customer-orders-client";
import { canIncrementStorefrontQuantity } from "@/features/customer-ordering/storefront-availability";

export type PersonalMerchantCartLine = {
  productId: string;
  name: string;
  sku: string | null;
  unitOfMeasure: string;
  unitPrice: number;
  quantity: number;
};

export type PersonalMerchantCartState = {
  sellerOrganizationId: string | null;
  organizationDisplayName: string | null;
  lines: PersonalMerchantCartLine[];
};

export const EMPTY_PERSONAL_MERCHANT_CART: PersonalMerchantCartState = {
  sellerOrganizationId: null,
  organizationDisplayName: null,
  lines: [],
};

function lineTotal(line: PersonalMerchantCartLine): number {
  return Math.round(line.unitPrice * line.quantity * 100) / 100;
}

export function cartMerchandiseSubtotal(state: PersonalMerchantCartState): number {
  const sum = state.lines.reduce((acc, line) => acc + lineTotal(line), 0);
  return Math.round(sum * 100) / 100;
}

export function cartItemCount(state: PersonalMerchantCartState): number {
  return state.lines.reduce((acc, line) => acc + line.quantity, 0);
}

export function getCartQuantity(state: PersonalMerchantCartState, productId: string): number {
  return state.lines.find((l) => l.productId === productId)?.quantity ?? 0;
}

export function ensureMerchantCart(
  state: PersonalMerchantCartState,
  sellerOrganizationId: string,
  organizationDisplayName: string | null,
): PersonalMerchantCartState {
  if (state.sellerOrganizationId === sellerOrganizationId) {
    if (organizationDisplayName && organizationDisplayName !== state.organizationDisplayName) {
      return { ...state, organizationDisplayName };
    }
    return state;
  }
  return {
    sellerOrganizationId,
    organizationDisplayName,
    lines: [],
  };
}

export function incrementCartLine(
  state: PersonalMerchantCartState,
  product: CustomerStorefrontProductDto,
): PersonalMerchantCartState {
  const qty = getCartQuantity(state, product.productId);
  if (!canIncrementStorefrontQuantity(product, qty)) {
    return state;
  }
  const existing = state.lines.find((l) => l.productId === product.productId);
  if (existing) {
    return {
      ...state,
      lines: state.lines.map((l) =>
        l.productId === product.productId ? { ...l, quantity: l.quantity + 1 } : l,
      ),
    };
  }
  return {
    ...state,
    lines: [
      ...state.lines,
      {
        productId: product.productId,
        name: product.name,
        sku: product.sku ?? null,
        unitOfMeasure: product.unitOfMeasure,
        unitPrice: product.unitPrice,
        quantity: 1,
      },
    ],
  };
}

export function decrementCartLine(
  state: PersonalMerchantCartState,
  productId: string,
): PersonalMerchantCartState {
  const existing = state.lines.find((l) => l.productId === productId);
  if (!existing) {
    return state;
  }
  if (existing.quantity <= 1) {
    return { ...state, lines: state.lines.filter((l) => l.productId !== productId) };
  }
  return {
    ...state,
    lines: state.lines.map((l) =>
      l.productId === productId ? { ...l, quantity: l.quantity - 1 } : l,
    ),
  };
}

export function clearCartLines(state: PersonalMerchantCartState): PersonalMerchantCartState {
  if (state.lines.length === 0) {
    return state;
  }
  return { ...state, lines: [] };
}

export function clearPersonalMerchantCart(): PersonalMerchantCartState {
  return EMPTY_PERSONAL_MERCHANT_CART;
}

export const FulfillmentPickup = "Pickup";
export const FulfillmentDelivery = "Delivery";

export const PAYMENT_METHOD_CODES = ["Cash", "ManualGCash", "Utang"] as const;

export type PersonalMerchantFulfillmentSelection = {
  fulfillmentType: string;
  branchId: string | null;
  branchName: string | null;
  showFulfillmentToggle: boolean;
  showBranchSelector: boolean;
  canPlace: boolean;
};

export function pickupAvailable(branches: CustomerStorefrontBranchDto[]): boolean {
  return branches.some((b) => b.pickupEnabled);
}

export function deliveryAvailable(
  branches: CustomerStorefrontBranchDto[],
  canCustomerDelivery: boolean,
): boolean {
  return canCustomerDelivery && branches.some((b) => b.deliveryEnabled);
}

export function eligibleBranches(
  branches: CustomerStorefrontBranchDto[],
  canCustomerDelivery: boolean,
  fulfillmentType: string,
): CustomerStorefrontBranchDto[] {
  if (fulfillmentType.toLowerCase() === FulfillmentDelivery.toLowerCase()) {
    return canCustomerDelivery ? branches.filter((b) => b.deliveryEnabled) : [];
  }
  return branches.filter((b) => b.pickupEnabled);
}

export function resolveFulfillmentSelection(
  branches: CustomerStorefrontBranchDto[],
  canCustomerDelivery: boolean,
  requestedFulfillment: string,
  currentBranchId: string | null,
): PersonalMerchantFulfillmentSelection {
  const pickupOk = pickupAvailable(branches);
  const deliveryOk = deliveryAvailable(branches, canCustomerDelivery);
  let fulfillment: string;
  if (pickupOk && deliveryOk) {
    fulfillment =
      requestedFulfillment.toLowerCase() === FulfillmentDelivery.toLowerCase()
        ? FulfillmentDelivery
        : FulfillmentPickup;
  } else if (deliveryOk) {
    fulfillment = FulfillmentDelivery;
  } else {
    fulfillment = FulfillmentPickup;
  }

  const eligible = eligibleBranches(branches, canCustomerDelivery, fulfillment);
  if (eligible.length === 0) {
    return {
      fulfillmentType: fulfillment,
      branchId: null,
      branchName: null,
      showFulfillmentToggle: pickupOk && deliveryOk,
      showBranchSelector: false,
      canPlace: false,
    };
  }

  const selected =
    (currentBranchId ? eligible.find((b) => b.branchId === currentBranchId) : undefined) ??
    eligible[0];

  return {
    fulfillmentType: fulfillment,
    branchId: selected.branchId,
    branchName: selected.name,
    showFulfillmentToggle: pickupOk && deliveryOk,
    showBranchSelector: eligible.length > 1,
    canPlace:
      fulfillment.toLowerCase() === FulfillmentDelivery.toLowerCase()
        ? selected.deliveryOperational
        : selected.pickupOperational,
  };
}
