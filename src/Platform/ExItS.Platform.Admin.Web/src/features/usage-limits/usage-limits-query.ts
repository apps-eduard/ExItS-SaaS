import { isGuid } from "@/api/support/support-identity-client";

export type UsageLimitsUrlState = {
  organizationId: string;
  productCode: string;
  page: number;
};

function parsePage(raw: string | null): number {
  const value = Number(raw ?? "1");
  return Number.isFinite(value) && value >= 1 ? Math.floor(value) : 1;
}

export function parseUsageLimitsSearchParams(params: URLSearchParams): UsageLimitsUrlState {
  const organizationRaw = params.get("organizationId")?.trim() ?? "";
  return {
    organizationId: isGuid(organizationRaw) ? organizationRaw : "",
    productCode: params.get("productCode")?.trim() ?? "",
    page: parsePage(params.get("page")),
  };
}

export function usageLimitsSearchParams(state: UsageLimitsUrlState): URLSearchParams {
  const params = new URLSearchParams();
  if (state.organizationId) {
    params.set("organizationId", state.organizationId);
  }
  if (state.productCode) {
    params.set("productCode", state.productCode);
  }
  if (state.page > 1) {
    params.set("page", String(state.page));
  }
  return params;
}
