import {
  findCustomerByLinkedPersonalPublicUserId,
  searchCheckoutCustomers,
  type CheckoutCustomerSearchItem,
} from "@/api/pos/pos-customers-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";

/**
 * Checkout / create must not send another add/link when this Personal ExItS ID
 * already has a POS customer in the organization (column or notes search).
 */
export async function findExistingCheckoutCustomerForPersonalId(
  workspace: PosWorkspaceScope,
  personalPublicUserId: string,
  signal?: AbortSignal,
): Promise<CheckoutCustomerSearchItem | null> {
  const needle = personalPublicUserId.trim();
  if (!needle) {
    return null;
  }

  const linked = await findCustomerByLinkedPersonalPublicUserId(workspace, needle, signal);
  if (linked) {
    return linked;
  }

  const page = await searchCheckoutCustomers(workspace, { search: needle, pageSize: 20 }, signal);
  if (page.items.length === 0) {
    return null;
  }
  if (page.items.length === 1) {
    return page.items[0];
  }

  const upper = needle.toUpperCase();
  return (
    page.items.find((item) => item.displayName.trim().toUpperCase() === upper) ?? page.items[0]
  );
}
