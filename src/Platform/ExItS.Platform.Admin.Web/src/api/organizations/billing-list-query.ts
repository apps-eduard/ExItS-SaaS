import { withQuery } from "@/lib/http/query-string";

export const ORGANIZATION_BILLING_PAGE_SIZE = 20;

export const ORGANIZATION_PAYMENT_STATUSES = [
  "PendingConfirmation",
  "Confirmed",
  "Rejected",
  "Voided",
] as const;

export type OrganizationPaymentStatus = (typeof ORGANIZATION_PAYMENT_STATUSES)[number];

export type OrganizationPayment = {
  id: string;
  organizationId: string;
  productCode: string;
  subscriptionId?: string;
  amount: number;
  currencyCode: string;
  method: string;
  externalReference?: string;
  status: string;
  paidAtUtc?: string;
  confirmedAtUtc?: string;
  rejectedAtUtc?: string;
  voidedAtUtc?: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
};

export type OrganizationBillingUrlState = {
  status: OrganizationPaymentStatus | "";
  page: number;
};

export function isOrganizationPaymentStatus(value: string): value is OrganizationPaymentStatus {
  return (ORGANIZATION_PAYMENT_STATUSES as readonly string[]).includes(value);
}

function parsePage(raw: string | null): number {
  const value = Number(raw ?? "1");
  return Number.isFinite(value) && value >= 1 ? Math.floor(value) : 1;
}

export function parseOrganizationBillingSearchParams(
  params: URLSearchParams,
): OrganizationBillingUrlState {
  const statusRaw = params.get("status") ?? "";
  return {
    status: isOrganizationPaymentStatus(statusRaw) ? statusRaw : "",
    page: parsePage(params.get("page")),
  };
}

export function organizationBillingSearchParams(
  state: OrganizationBillingUrlState,
): URLSearchParams {
  const params = new URLSearchParams();
  if (state.status) {
    params.set("status", state.status);
  }
  if (state.page > 1) {
    params.set("page", String(state.page));
  }
  return params;
}

export function organizationPaymentsRequestPath(
  organizationId: string,
  query: { status?: string; page: number; pageSize?: number },
): string {
  return withQuery(`/api/v1/platform/organizations/${organizationId}/payments`, {
    status: query.status,
    page: query.page,
    pageSize: query.pageSize ?? ORGANIZATION_BILLING_PAGE_SIZE,
  });
}
