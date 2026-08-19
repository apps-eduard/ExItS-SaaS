import { withQuery } from "@/lib/http/query-string";

export const DASHBOARD_PAGE = 1;
export const DASHBOARD_COUNT_PAGE_SIZE = 1;
export const DASHBOARD_ATTENTION_PAGE_SIZE = 5;
export const DASHBOARD_AUDIT_PAGE_SIZE = 8;

export const ORGANIZATION_STATUSES = ["Active", "Suspended", "Closed"] as const;
export const SUBSCRIPTION_SUMMARY_STATUSES = [
  "Trialing",
  "Active",
  "PastDue",
  "GracePeriod",
] as const;

export function organizationsListPath(options: { status?: string; pageSize: number }): string {
  return withQuery("/api/v1/platform/organizations", {
    page: DASHBOARD_PAGE,
    pageSize: options.pageSize,
    status: options.status,
  });
}

export function subscriptionsListPath(options: { status?: string; pageSize: number }): string {
  return withQuery("/api/v1/platform/subscriptions", {
    page: DASHBOARD_PAGE,
    pageSize: options.pageSize,
    status: options.status,
  });
}

export function usersListPath(options: {
  status?: string;
  directory?: string;
  pageSize: number;
}): string {
  return withQuery("/api/v1/platform/users", {
    page: DASHBOARD_PAGE,
    pageSize: options.pageSize,
    status: options.status,
    directory: options.directory,
  });
}

export function auditListPath(options: { pageSize: number }): string {
  return withQuery("/api/v1/platform/audit", {
    page: DASHBOARD_PAGE,
    pageSize: options.pageSize,
  });
}

export function assertDashboardPageSize(pageSize: number): void {
  if (pageSize < 1 || pageSize > DASHBOARD_AUDIT_PAGE_SIZE) {
    throw new Error("Dashboard requests must stay within the bounded page-size window.");
  }
}
