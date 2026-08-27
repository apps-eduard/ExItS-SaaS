import { listLinkedMerchants } from "@/api/platform/linked-merchants-client";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import { getLinkedCustomerStatement } from "@/api/pos/pos-linked-customers-client";
import { selectCanonicalLinkedMerchantPerStore } from "@/features/customer-ordering/select-canonical-linked-merchant";
import { personalStoreDisplayName } from "@/features/customer-ordering/format-personal-store-label";

/** How many store rows to show on Personal Home. */
export const STORES_TO_PAY_PREVIEW_LIMIT = 2;

/** Cap concurrent statement fetches on Home (avoids N+1 blow-up). */
export const STORES_TO_PAY_FETCH_CAP = 10;

export type StoreToPayRow = {
  organizationId: string;
  businessCustomerId: string;
  displayName: string;
  outstandingBalance: number;
  currency: string;
  href: string;
};

export type StoresToPayPreview = {
  storeCount: number;
  /** Linked stores with outstanding balance > 0 among successfully loaded statements. */
  activeCount: number;
  preview: StoreToPayRow[];
};

/**
 * Personal Home projection: linked Organization/Business Utang owed by the Personal user.
 * Isolated from P2P Personal Tracker balances.
 */
export async function loadStoresToPayPreview(
  signal?: AbortSignal,
): Promise<StoresToPayPreview> {
  const page = await listLinkedMerchants(1, 50, signal);
  const stores = selectCanonicalLinkedMerchantPerStore(page.items);
  const storeCount = stores.length;

  if (stores.length === 0) {
    return { storeCount: 0, activeCount: 0, preview: [] };
  }

  const token = await ensurePersonalBuyerPosToken();
  if (!token.ok) {
    return { storeCount, activeCount: 0, preview: [] };
  }

  const merchants = stores.slice(0, STORES_TO_PAY_FETCH_CAP);
  const settled = await Promise.allSettled(
    merchants.map(async (merchant) => {
      const statement = await getLinkedCustomerStatement(
        merchant.organizationId,
        merchant.businessCustomerId,
        { signal },
      );
      const displayName =
        personalStoreDisplayName(merchant.organizationDisplayName) ||
        personalStoreDisplayName(statement.merchantDisplayName) ||
        "Store";
      return {
        organizationId: merchant.organizationId,
        businessCustomerId: merchant.businessCustomerId,
        displayName,
        outstandingBalance: statement.outstandingBalance,
        currency: statement.currency || "PHP",
        href: `/personal/linked-merchants/${merchant.organizationId}/${merchant.businessCustomerId}`,
      } satisfies StoreToPayRow;
    }),
  );

  const withBalance = settled
    .filter((result): result is PromiseFulfilledResult<StoreToPayRow> => result.status === "fulfilled")
    .map((result) => result.value)
    .filter((row) => row.outstandingBalance > 0)
    .sort((a, b) => b.outstandingBalance - a.outstandingBalance);

  return {
    storeCount,
    activeCount: withBalance.length,
    preview: withBalance.slice(0, STORES_TO_PAY_PREVIEW_LIMIT),
  };
}
