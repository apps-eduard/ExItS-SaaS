import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";

export type BillingOperationsSummary = {
  pendingPaymentCount: number;
  rejectedPaymentCount: number;
  voidedPaymentCount: number;
  confirmedPaymentCount: number;
  pastDueSubscriptionCount: number;
  gracePeriodSubscriptionCount: number;
};

export type BillingIssueType =
  | "pending-payment"
  | "rejected-payment"
  | "voided-payment"
  | "past-due-subscription"
  | "grace-period-subscription";

export type BillingIssue = {
  issueType: BillingIssueType;
  severity: "warning" | "danger";
  summary: string;
  detail?: string | null;
  organizationId?: string | null;
  organizationDisplayName?: string | null;
  productCode?: string | null;
  productDisplayName?: string | null;
  subscriptionId?: string | null;
  paymentId?: string | null;
  occurredAtUtc?: string | null;
};

export const BILLING_ISSUES_PAGE_SIZE = 20;

export const BILLING_SUMMARY_PATH = "/api/v1/platform/admin/billing/summary";
export const BILLING_ISSUES_PATH = "/api/v1/platform/admin/billing/issues";

export function getBillingOperationsSummary(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<BillingOperationsSummary> {
  return platformRequest<BillingOperationsSummary>(baseUrl, {
    path: BILLING_SUMMARY_PATH,
    signal,
  });
}

export type ListBillingIssuesOptions = {
  issueType?: BillingIssueType | "";
  page?: number;
  pageSize?: number;
  signal?: AbortSignal;
};

export function listBillingIssues(
  baseUrl: string,
  options: ListBillingIssuesOptions = {},
): Promise<PagedResult<BillingIssue>> {
  const params = new URLSearchParams();
  if (options.issueType) {
    params.set("issueType", options.issueType);
  }
  if (options.page && options.page > 1) {
    params.set("page", String(options.page));
  }
  if (options.pageSize) {
    params.set("pageSize", String(options.pageSize));
  }
  const query = params.toString();
  return platformRequest<unknown>(baseUrl, {
    path: query ? `${BILLING_ISSUES_PATH}?${query}` : BILLING_ISSUES_PATH,
    signal: options.signal,
  }).then((payload) => parsePagedResult<BillingIssue>(payload));
}

export function billingIssueHref(issue: BillingIssue): string {
  if (issue.paymentId) {
    return `/admin/payments/${issue.paymentId}`;
  }
  if (issue.subscriptionId) {
    return `/admin/subscriptions/${issue.subscriptionId}`;
  }
  if (issue.organizationId) {
    return `/admin/organizations/${issue.organizationId}/billing`;
  }
  return "/admin/payments/issues";
}
