import type { PosCustomerListItem } from "@/api/pos/pos-customers-client";
import type { BusinessCustomer } from "@/api/pos/pos-connected-suppliers-client";

export type BusinessListBadgeKind = "local" | "connected" | "exitsOrganization";

export type BusinessListRow =
  | {
      key: string;
      source: "connection";
      displayName: string;
      publicOrganizationId: string | null;
      buyerOrganizationId: string;
      href: string;
      badges: BusinessListBadgeKind[];
      alsoSupplier: boolean;
      relationshipStatus: string;
      connection: BusinessCustomer;
    }
  | {
      key: string;
      source: "pos";
      displayName: string;
      publicOrganizationId: string | null;
      buyerOrganizationId: string | null;
      href: string;
      badges: BusinessListBadgeKind[];
      alsoSupplier: boolean;
      status: string;
      customer: PosCustomerListItem;
    };

export function isPersonPosCustomer(customer: PosCustomerListItem): boolean {
  if (customer.linkedBuyerOrganizationId?.trim()) {
    return false;
  }
  const kind = (customer.partyKind ?? "Person").trim().toLowerCase();
  return kind !== "business";
}

export function isBusinessPosCustomer(customer: PosCustomerListItem): boolean {
  return !isPersonPosCustomer(customer);
}

export function buildBusinessListRows(input: {
  connections: BusinessCustomer[];
  posBusinessCustomers: PosCustomerListItem[];
  activeSupplierOrganizationIds: ReadonlySet<string>;
}): BusinessListRow[] {
  const connectedBuyerIds = new Set(
    input.connections.map((c) => c.buyerOrganizationId.toLowerCase()),
  );

  const connectionRows: BusinessListRow[] = input.connections.map((connection) => {
    const buyerId = connection.buyerOrganizationId.toLowerCase();
    return {
      key: `connection:${connection.connectionId}`,
      source: "connection",
      displayName: connection.organizationDisplayName,
      publicOrganizationId: connection.organizationPublicId ?? null,
      buyerOrganizationId: connection.buyerOrganizationId,
      href: `/customers/business/${connection.connectionId}`,
      badges: ["connected", "exitsOrganization"],
      alsoSupplier: input.activeSupplierOrganizationIds.has(buyerId),
      relationshipStatus: connection.relationshipStatus,
      connection,
    };
  });

  const posRows: BusinessListRow[] = input.posBusinessCustomers
    .filter((customer) => {
      const buyerId = customer.linkedBuyerOrganizationId?.toLowerCase();
      // Prefer the connected-relationship row when both exist for the same org.
      if (buyerId && connectedBuyerIds.has(buyerId)) {
        return false;
      }
      return true;
    })
    .map((customer) => {
      const buyerId = customer.linkedBuyerOrganizationId?.toLowerCase() ?? null;
      const badges: BusinessListBadgeKind[] = [];
      if (buyerId) {
        badges.push("exitsOrganization");
      } else {
        badges.push("local");
      }
      return {
        key: `pos:${customer.customerId}`,
        source: "pos" as const,
        displayName: customer.displayName,
        publicOrganizationId: customer.linkedBuyerPublicOrganizationId ?? null,
        buyerOrganizationId: customer.linkedBuyerOrganizationId ?? null,
        href: `/customers/${customer.customerId}`,
        badges,
        alsoSupplier: buyerId ? input.activeSupplierOrganizationIds.has(buyerId) : false,
        status: customer.status,
        customer,
      };
    });

  return [...connectionRows, ...posRows].sort((a, b) =>
    a.displayName.localeCompare(b.displayName, undefined, { sensitivity: "base" }),
  );
}
