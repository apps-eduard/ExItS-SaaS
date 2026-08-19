import { useQuery } from "@tanstack/react-query";
import { listAuditRecords } from "@/api/audit/audit-client";
import { listPlatformUsers } from "@/api/identity/identity-client";
import { getPlatformHealth } from "@/api/ops/health-client";
import { listOrganizations } from "@/api/organizations/organization-client";
import { listSubscriptions } from "@/api/subscriptions/subscription-client";
import { env } from "@/lib/env";
import {
  DASHBOARD_ATTENTION_PAGE_SIZE,
  DASHBOARD_AUDIT_PAGE_SIZE,
  DASHBOARD_COUNT_PAGE_SIZE,
  assertDashboardPageSize,
} from "@/features/overview/dashboard-bounds";

export const dashboardQueryKeys = {
  organizations: (status?: string, pageSize?: number) =>
    ["dashboard", "organizations", status ?? "all", pageSize ?? DASHBOARD_COUNT_PAGE_SIZE] as const,
  subscriptions: (status?: string) => ["dashboard", "subscriptions", status ?? "all"] as const,
  unassignedUsers: ["dashboard", "users", "unassigned"] as const,
  pendingUsers: ["dashboard", "users", "pending-verification"] as const,
  audit: ["dashboard", "audit"] as const,
  health: ["dashboard", "health"] as const,
};

export function useOrganizationCountQuery(enabled: boolean, status?: string) {
  return useQuery({
    queryKey: dashboardQueryKeys.organizations(status, DASHBOARD_COUNT_PAGE_SIZE),
    enabled,
    queryFn: ({ signal }) => {
      assertDashboardPageSize(DASHBOARD_COUNT_PAGE_SIZE);
      return listOrganizations(env.platformApiBaseUrl, {
        status,
        pageSize: DASHBOARD_COUNT_PAGE_SIZE,
        signal,
      });
    },
  });
}

export function useSuspendedOrganizationsQuery(enabled: boolean) {
  return useQuery({
    queryKey: dashboardQueryKeys.organizations("Suspended", DASHBOARD_ATTENTION_PAGE_SIZE),
    enabled,
    queryFn: ({ signal }) => {
      assertDashboardPageSize(DASHBOARD_ATTENTION_PAGE_SIZE);
      return listOrganizations(env.platformApiBaseUrl, {
        status: "Suspended",
        pageSize: DASHBOARD_ATTENTION_PAGE_SIZE,
        signal,
      });
    },
  });
}

export function useOrganizationSummaryQueries(enabled: boolean) {
  const total = useOrganizationCountQuery(enabled);
  const active = useOrganizationCountQuery(enabled, "Active");
  const closed = useOrganizationCountQuery(enabled, "Closed");
  const suspended = useSuspendedOrganizationsQuery(enabled);
  return { total, active, closed, suspended };
}

export function useSubscriptionCountQuery(enabled: boolean, status?: string) {
  return useQuery({
    queryKey: dashboardQueryKeys.subscriptions(status),
    enabled,
    queryFn: ({ signal }) =>
      listSubscriptions(env.platformApiBaseUrl, {
        status,
        pageSize: DASHBOARD_COUNT_PAGE_SIZE,
        signal,
      }),
  });
}

export function useSubscriptionSummaryQueries(enabled: boolean) {
  const total = useSubscriptionCountQuery(enabled);
  const trialing = useSubscriptionCountQuery(enabled, "Trialing");
  const active = useSubscriptionCountQuery(enabled, "Active");
  const pastDue = useSubscriptionCountQuery(enabled, "PastDue");
  const gracePeriod = useSubscriptionCountQuery(enabled, "GracePeriod");
  return { total, trialing, active, pastDue, gracePeriod };
}

export function useUnassignedAccountsQuery(enabled: boolean) {
  return useQuery({
    queryKey: dashboardQueryKeys.unassignedUsers,
    enabled,
    queryFn: ({ signal }) =>
      listPlatformUsers(env.platformApiBaseUrl, {
        directory: "Unassigned",
        pageSize: DASHBOARD_ATTENTION_PAGE_SIZE,
        signal,
      }),
  });
}

export function usePendingVerificationAccountsQuery(enabled: boolean) {
  return useQuery({
    queryKey: dashboardQueryKeys.pendingUsers,
    enabled,
    queryFn: ({ signal }) =>
      listPlatformUsers(env.platformApiBaseUrl, {
        status: "PendingVerification",
        pageSize: DASHBOARD_COUNT_PAGE_SIZE,
        signal,
      }),
  });
}

export function useRecentAuditQuery(enabled: boolean) {
  return useQuery({
    queryKey: dashboardQueryKeys.audit,
    enabled,
    queryFn: ({ signal }) =>
      listAuditRecords(env.platformApiBaseUrl, {
        pageSize: DASHBOARD_AUDIT_PAGE_SIZE,
        signal,
      }),
  });
}

export function usePlatformHealthQuery(enabled: boolean) {
  return useQuery({
    queryKey: dashboardQueryKeys.health,
    enabled,
    queryFn: ({ signal }) => getPlatformHealth(env.platformApiBaseUrl, signal),
  });
}
