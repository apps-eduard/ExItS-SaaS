export type CheckoutCustomerKind = "Customer" | "Business";

export type CheckoutCreditStatus =
  | "NotConfigured"
  | "PendingApproval"
  | "Approved"
  | "Disabled";

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
  /** Linked buyer Organization id when this POS row is an ORG-linked business customer. */
  linkedBuyerOrganizationId?: string | null;
  linkedBuyerPublicOrganizationId?: string | null;
  /** Person (default) or Business party — Business rows appear under the Businesses filter. */
  partyKind?: "Person" | "Business" | null;
  /** Batched checkout credit projection (person rows). Eligibility overlay — not a hide filter. */
  creditStatus?: CheckoutCreditStatus | null;
  creditLimit?: number | null;
  outstandingAmount?: number | null;
  availableCredit?: number | null;
  defaultTermDays?: number | null;
};

/** Active or Pending B2B Organization counterparty — no POSCustomer row. */
export type CheckoutBusinessOption = {
  kind: "Business";
  connectionId: string;
  buyerOrganizationId: string;
  buyerPublicOrganizationId: string | null;
  displayName: string;
  status: string;
  initiatedByParty?: string | null;
  /** Batched B2B credit projection (BusinessCustomerCreditPolicy). */
  creditStatus?: CheckoutCreditStatus | null;
  creditLimit?: number | null;
  outstandingAmount?: number | null;
  availableCredit?: number | null;
  defaultTermDays?: number | null;
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

/** Active B2B connection or POS Business party (ORG-linked / partyKind Business). */
export function isCheckoutBusinessDirectoryRow(
  option: CheckoutCustomerOption | null | undefined,
): boolean {
  if (!option) {
    return false;
  }
  if (option.kind === "Business") {
    return true;
  }
  if (option.kind !== "Customer") {
    return false;
  }
  if (option.linkedBuyerOrganizationId?.trim()) {
    return true;
  }
  return (option.partyKind ?? "Person").trim().toLowerCase() === "business";
}

export function checkoutOptionKey(option: CheckoutCustomerOption): string {
  return option.kind === "Business" ? `b:${option.connectionId}` : `c:${option.customerId}`;
}

function normalizeCheckoutCreditStatus(
  raw: string | null | undefined,
): CheckoutCreditStatus | null {
  switch ((raw ?? "").trim()) {
    case "Approved":
    case "PendingApproval":
    case "Disabled":
    case "NotConfigured":
      return raw as CheckoutCreditStatus;
    default:
      return null;
  }
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
  partyKind?: string | null;
  initiatedByParty?: string | null;
  creditStatus?: string | null;
  creditLimit?: number | null;
  outstandingAmount?: number | null;
  availableCredit?: number | null;
  defaultTermDays?: number | null;
}): CheckoutCustomerOption | null {
  if (item.kind === "Business") {
    if (!item.connectionId || !item.buyerOrganizationId) {
      return null;
    }
    const creditStatus = normalizeCheckoutCreditStatus(item.creditStatus);
    return {
      kind: "Business",
      connectionId: item.connectionId,
      buyerOrganizationId: item.buyerOrganizationId,
      buyerPublicOrganizationId: item.buyerPublicOrganizationId ?? null,
      displayName: item.displayName,
      status: item.status,
      initiatedByParty: item.initiatedByParty ?? null,
      creditStatus,
      creditLimit: item.creditLimit ?? null,
      outstandingAmount: item.outstandingAmount ?? null,
      availableCredit: item.availableCredit ?? null,
      defaultTermDays: item.defaultTermDays ?? null,
    };
  }

  if (!item.customerId) {
    return null;
  }

  const partyRaw = item.partyKind?.trim();
  const partyKind =
    partyRaw && partyRaw.toLowerCase() === "business"
      ? ("Business" as const)
      : partyRaw && partyRaw.toLowerCase() === "person"
        ? ("Person" as const)
        : item.buyerOrganizationId
          ? ("Business" as const)
          : null;

  const creditStatus = normalizeCheckoutCreditStatus(item.creditStatus);

  return {
    kind: "Customer",
    customerId: item.customerId,
    displayName: item.displayName,
    mobileNumber: item.mobileNumber,
    status: item.status,
    linkedPersonalPublicUserId: item.linkedPersonalPublicUserId ?? null,
    platformBusinessCustomerId: item.platformBusinessCustomerId ?? null,
    resolvedPersonalDisplayName: item.resolvedPersonalDisplayName ?? null,
    linkedBuyerOrganizationId: item.buyerOrganizationId ?? null,
    linkedBuyerPublicOrganizationId: item.buyerPublicOrganizationId ?? null,
    partyKind,
    creditStatus,
    creditLimit: item.creditLimit ?? null,
    outstandingAmount: item.outstandingAmount ?? null,
    availableCredit: item.availableCredit ?? null,
    defaultTermDays: item.defaultTermDays ?? null,
  };
}
