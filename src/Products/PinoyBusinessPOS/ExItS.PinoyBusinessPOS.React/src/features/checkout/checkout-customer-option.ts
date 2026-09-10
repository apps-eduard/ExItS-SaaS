export type CheckoutCustomerKind = "Customer" | "Business";

/** POS person/local customer selectable at checkout. */
export type CheckoutPersonOption = {
  kind: "Customer";
  customerId: string;
  displayName: string;
  mobileNumber?: string | null;
  status: string;
  /** POS correlation only — not Platform CustomerLink Active status. */
  linkedPersonalPublicUserId?: string | null;
  /** Platform BusinessCustomer id — used to overlay Connected/Pending on the list. */
  platformBusinessCustomerId?: string | null;
  /** Personal display name from ExItS ID / QR resolve, when the cashier just looked them up. */
  resolvedPersonalDisplayName?: string | null;
};

/** Active B2B Organization counterparty — no POSCustomer row. */
export type CheckoutBusinessOption = {
  kind: "Business";
  connectionId: string;
  buyerOrganizationId: string;
  buyerPublicOrganizationId: string | null;
  displayName: string;
  status: string;
};

export type CheckoutCustomerOption = CheckoutPersonOption | CheckoutBusinessOption;

export function isCheckoutBusiness(
  option: CheckoutCustomerOption | null | undefined,
): option is CheckoutBusinessOption {
  return option?.kind === "Business";
}

export function isCheckoutPerson(
  option: CheckoutCustomerOption | null | undefined,
): option is CheckoutPersonOption {
  return option?.kind === "Customer";
}

export function checkoutOptionKey(option: CheckoutCustomerOption): string {
  return option.kind === "Business" ? `b:${option.connectionId}` : `c:${option.customerId}`;
}

/** Map API checkout-search row → selector option. Drops incomplete Business rows. */
export function mapCheckoutSearchItemToOption(item: {
  kind?: string;
  displayName: string;
  status: string;
  customerId?: string | null;
  mobileNumber?: string | null;
  connectionId?: string | null;
  buyerOrganizationId?: string | null;
  buyerPublicOrganizationId?: string | null;
  linkedPersonalPublicUserId?: string | null;
  platformBusinessCustomerId?: string | null;
  resolvedPersonalDisplayName?: string | null;
}): CheckoutCustomerOption | null {
  if (item.kind === "Business") {
    if (!item.connectionId || !item.buyerOrganizationId) {
      return null;
    }
    return {
      kind: "Business",
      connectionId: item.connectionId,
      buyerOrganizationId: item.buyerOrganizationId,
      buyerPublicOrganizationId: item.buyerPublicOrganizationId ?? null,
      displayName: item.displayName,
      status: item.status,
    };
  }

  if (!item.customerId) {
    return null;
  }

  return {
    kind: "Customer",
    customerId: item.customerId,
    displayName: item.displayName,
    mobileNumber: item.mobileNumber,
    status: item.status,
    linkedPersonalPublicUserId: item.linkedPersonalPublicUserId ?? null,
    platformBusinessCustomerId: item.platformBusinessCustomerId ?? null,
    resolvedPersonalDisplayName: item.resolvedPersonalDisplayName ?? null,
  };
}
