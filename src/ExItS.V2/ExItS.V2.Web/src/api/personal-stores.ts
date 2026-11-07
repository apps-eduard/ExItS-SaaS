import { AUTH_TOKEN_PATH, POS_PRODUCT_CODE } from "./auth";
import { platformRequest, PlatformApiError } from "./platform-http";
import { getPosAccessToken, setPosAccessToken } from "./pos-access-token";
import { posRequest } from "./pos-http";
import { asRecord, num, str } from "./widget";

export type StoreToPayRow = {
  organizationId: string;
  businessCustomerId: string;
  displayName: string;
  outstandingBalance: number;
  currency: string;
};

export type StoresToPayPreview = {
  storeCount: number;
  activeCount: number;
  preview: StoreToPayRow[];
};

const PREVIEW_LIMIT = 2;
const FETCH_CAP = 10;

async function ensurePersonalBuyerPosToken(): Promise<
  { ok: true } | { ok: false; detail: string }
> {
  if (getPosAccessToken()) return { ok: true };
  try {
    const grant = await platformRequest<Record<string, unknown>>({
      method: "POST",
      path: AUTH_TOKEN_PATH,
      body: { grantType: "session" },
    });
    const token = str(grant, "accessToken", "AccessToken");
    if (!token) return { ok: false, detail: "Personal access token was empty." };
    setPosAccessToken(token);
    return { ok: true };
  } catch (error) {
    try {
      const grant = await platformRequest<Record<string, unknown>>({
        method: "POST",
        path: AUTH_TOKEN_PATH,
        body: { grantType: "session", productCode: POS_PRODUCT_CODE },
      });
      const token = str(grant, "accessToken", "AccessToken");
      if (!token) return { ok: false, detail: "Personal access token was empty." };
      setPosAccessToken(token);
      return { ok: true };
    } catch (inner) {
      const detail =
        inner instanceof PlatformApiError
          ? (inner.problem.detail ?? inner.message)
          : error instanceof PlatformApiError
            ? (error.problem.detail ?? error.message)
            : "Could not issue personal POS access token.";
      return { ok: false, detail };
    }
  }
}

type LinkedMerchant = {
  organizationId: string;
  businessCustomerId: string;
  organizationDisplayName: string;
};

async function listLinkedMerchants(
  signal?: AbortSignal,
): Promise<LinkedMerchant[]> {
  const raw = asRecord(
    await platformRequest<unknown>({
      path: "/api/v1/personal/linked-merchants?page=1&pageSize=50",
      signal,
    }),
  );
  const items = Array.isArray(raw.items)
    ? raw.items
    : Array.isArray(raw.Items)
      ? raw.Items
      : [];
  const byOrg = new Map<string, LinkedMerchant>();
  for (const item of items) {
    const r = asRecord(item);
    const organizationId = str(r, "organizationId", "OrganizationId");
    const businessCustomerId = str(r, "businessCustomerId", "BusinessCustomerId");
    if (!organizationId || !businessCustomerId) continue;
    if (byOrg.has(organizationId)) continue;
    byOrg.set(organizationId, {
      organizationId,
      businessCustomerId,
      organizationDisplayName: str(
        r,
        "organizationDisplayName",
        "OrganizationDisplayName",
      ),
    });
  }
  return [...byOrg.values()];
}

/**
 * Personal Home Stores to Pay — linked merchant org utang, isolated from P2P tracker.
 */
export async function loadStoresToPayPreview(
  signal?: AbortSignal,
): Promise<StoresToPayPreview> {
  const stores = await listLinkedMerchants(signal);
  if (stores.length === 0) {
    return { storeCount: 0, activeCount: 0, preview: [] };
  }

  const token = await ensurePersonalBuyerPosToken();
  if (!token.ok) {
    return { storeCount: stores.length, activeCount: 0, preview: [] };
  }

  const merchants = stores.slice(0, FETCH_CAP);
  const settled = await Promise.allSettled(
    merchants.map(async (merchant) => {
      const params = new URLSearchParams({
        organizationId: merchant.organizationId,
        currency: "PHP",
      });
      const raw = asRecord(
        await posRequest<unknown>({
          path: `/api/v1/pos/personal/linked-customers/${encodeURIComponent(merchant.businessCustomerId)}/statement?${params}`,
          workspace: { organizationId: merchant.organizationId },
          signal,
        }),
      );
      const outstandingBalance = num(raw, "outstandingBalance", "OutstandingBalance");
      const displayName =
        merchant.organizationDisplayName.trim() ||
        str(raw, "merchantDisplayName", "MerchantDisplayName") ||
        "Store";
      return {
        organizationId: merchant.organizationId,
        businessCustomerId: merchant.businessCustomerId,
        displayName,
        outstandingBalance,
        currency: str(raw, "currency", "Currency") || "PHP",
      } satisfies StoreToPayRow;
    }),
  );

  const withBalance = settled
    .filter(
      (result): result is PromiseFulfilledResult<StoreToPayRow> =>
        result.status === "fulfilled",
    )
    .map((result) => result.value)
    .filter((row) => row.outstandingBalance > 0)
    .sort((a, b) => b.outstandingBalance - a.outstandingBalance);

  return {
    storeCount: stores.length,
    activeCount: withBalance.length,
    preview: withBalance.slice(0, PREVIEW_LIMIT),
  };
}
