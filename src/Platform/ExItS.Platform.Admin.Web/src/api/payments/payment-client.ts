import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import { mapOrganizationPayment } from "@/api/organizations/organization-client";
import type { OrganizationPayment } from "@/api/organizations/billing-list-query";
import { withQuery } from "@/lib/http/query-string";

export const PAYMENT_PORTFOLIO_PAGE_SIZE = 20;
export const PAYMENT_PORTFOLIO_STATUSES = [
  "Pending",
  "Confirmed",
  "Rejected",
  "Voided",
] as const;

export type PaymentPortfolioUrlState = {
  page: number;
  pageSize: number;
  status: string;
  productCode: string;
  method: string;
};

export function parsePaymentPortfolioSearchParams(params: URLSearchParams): PaymentPortfolioUrlState {
  const pageRaw = Number(params.get("page") ?? "1");
  return {
    page: Number.isFinite(pageRaw) && pageRaw >= 1 ? Math.floor(pageRaw) : 1,
    pageSize: PAYMENT_PORTFOLIO_PAGE_SIZE,
    status: params.get("status")?.trim() ?? "",
    productCode: params.get("productCode")?.trim() ?? "",
    method: params.get("method")?.trim() ?? "",
  };
}

export function paymentPortfolioSearchParams(state: PaymentPortfolioUrlState): URLSearchParams {
  const params = new URLSearchParams();
  if (state.status) params.set("status", state.status);
  if (state.productCode) params.set("productCode", state.productCode);
  if (state.method) params.set("method", state.method);
  if (state.page > 1) params.set("page", String(state.page));
  return params;
}

export function listPaymentPortfolio(
  baseUrl: string,
  state: PaymentPortfolioUrlState,
  signal?: AbortSignal,
): Promise<PagedResult<OrganizationPayment>> {
  return platformRequest<unknown>(baseUrl, {
    path: withQuery("/api/v1/platform/payments", {
      page: state.page,
      pageSize: state.pageSize,
      status: state.status || undefined,
      productCode: state.productCode || undefined,
      method: state.method || undefined,
    }),
    signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapOrganizationPayment),
    };
  });
}

const GUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function parsePaymentId(value: string | undefined): string | null {
  if (!value || !GUID_PATTERN.test(value)) {
    return null;
  }
  return value;
}

export function paymentDetailHref(paymentId: string): string {
  return `/admin/payments/${paymentId}`;
}

export function paymentsListHref(): string {
  return "/admin/payments";
}

export function hasActivePaymentPortfolioFilters(state: PaymentPortfolioUrlState): boolean {
  return Boolean(state.status || state.productCode || state.method);
}

export function getPayment(
  baseUrl: string,
  paymentId: string,
  signal?: AbortSignal,
): Promise<OrganizationPayment> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/payments/${paymentId}`,
    signal,
  }).then(mapOrganizationPayment);
}
