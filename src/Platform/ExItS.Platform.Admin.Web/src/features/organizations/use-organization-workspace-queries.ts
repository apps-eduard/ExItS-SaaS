import { useQuery } from "@tanstack/react-query";
import {
  getOrganization,
  getOrganizationCommercialSummary,
  listOrganizationBranches,
  listOrganizationEntitlementSnapshots,
  listOrganizationInvitations,
  listOrganizationMembers,
  listOrganizationPayments,
  listOrganizationSubscriptions,
} from "@/api/organizations/organization-client";
import type { OrganizationBillingUrlState } from "@/api/organizations/billing-list-query";
import { ORGANIZATION_PEOPLE_PAGE_SIZE } from "@/api/organizations/organization-types";
import { ORGANIZATION_ENTITLEMENT_PAGE_SIZE } from "@/api/organizations/entitlement-list-query";
import type { OrganizationSubscriptionUrlState } from "@/api/organizations/subscription-list-query";
import { env } from "@/lib/env";

export const organizationDetailQueryKey = (organizationId: string) =>
  ["organizations", "detail", organizationId] as const;

export const organizationCommercialSummaryQueryKey = (organizationId: string) =>
  ["organizations", "commercial-summary", organizationId] as const;

export function useOrganizationDetailQuery(organizationId: string | null) {
  return useQuery({
    queryKey: organizationDetailQueryKey(organizationId ?? ""),
    enabled: organizationId != null,
    queryFn: ({ signal }) => getOrganization(env.platformApiBaseUrl, organizationId!, signal),
  });
}

export function useOrganizationCommercialSummaryQuery(organizationId: string | null) {
  return useQuery({
    queryKey: organizationCommercialSummaryQueryKey(organizationId ?? ""),
    enabled: organizationId != null,
    queryFn: ({ signal }) =>
      getOrganizationCommercialSummary(env.platformApiBaseUrl, organizationId!, signal),
  });
}

export const organizationBranchesQueryKey = (organizationId: string) =>
  ["organizations", "branches", organizationId] as const;

export function useOrganizationBranchesQuery(organizationId: string | null) {
  return useQuery({
    queryKey: organizationBranchesQueryKey(organizationId ?? ""),
    enabled: organizationId != null,
    queryFn: ({ signal }) =>
      listOrganizationBranches(env.platformApiBaseUrl, organizationId!, signal),
  });
}

export const organizationMembersQueryKey = (organizationId: string, status: string, page: number) =>
  ["organizations", "members", organizationId, status, page] as const;

export function useOrganizationMembersQuery(
  organizationId: string | null,
  options: { status?: string; page: number },
) {
  return useQuery({
    queryKey: organizationMembersQueryKey(organizationId ?? "", options.status ?? "", options.page),
    enabled: organizationId != null,
    queryFn: ({ signal }) =>
      listOrganizationMembers(env.platformApiBaseUrl, organizationId!, {
        status: options.status,
        page: options.page,
        pageSize: ORGANIZATION_PEOPLE_PAGE_SIZE,
        signal,
      }),
  });
}

export const organizationInvitationsQueryKey = (
  organizationId: string,
  status: string,
  page: number,
) => ["organizations", "invitations", organizationId, status, page] as const;

export function useOrganizationInvitationsQuery(
  organizationId: string | null,
  options: { status?: string; page: number },
) {
  return useQuery({
    queryKey: organizationInvitationsQueryKey(
      organizationId ?? "",
      options.status ?? "",
      options.page,
    ),
    enabled: organizationId != null,
    queryFn: ({ signal }) =>
      listOrganizationInvitations(env.platformApiBaseUrl, organizationId!, {
        status: options.status,
        page: options.page,
        pageSize: ORGANIZATION_PEOPLE_PAGE_SIZE,
        signal,
      }),
  });
}

export const organizationSubscriptionsQueryKey = (
  organizationId: string,
  state: OrganizationSubscriptionUrlState,
) =>
  [
    "organizations",
    "subscriptions",
    organizationId,
    state.page,
    state.search,
    state.status,
    state.isTrial,
    state.productCode,
    state.sortBy,
    state.sortDesc,
  ] as const;

export function useOrganizationSubscriptionsQuery(
  organizationId: string | null,
  state: OrganizationSubscriptionUrlState,
) {
  return useQuery({
    queryKey: organizationSubscriptionsQueryKey(organizationId ?? "", state),
    enabled: organizationId != null,
    queryFn: ({ signal }) =>
      listOrganizationSubscriptions(env.platformApiBaseUrl, organizationId!, state, signal),
  });
}

export const organizationEntitlementSnapshotsQueryKey = (
  organizationId: string,
  product: string,
  page: number,
) => ["organizations", "entitlement-snapshots", organizationId, product, page] as const;

export function useOrganizationEntitlementSnapshotsQuery(
  organizationId: string | null,
  product: string | null,
  page: number,
) {
  return useQuery({
    queryKey: organizationEntitlementSnapshotsQueryKey(organizationId ?? "", product ?? "", page),
    enabled: organizationId != null && product != null && product.length > 0,
    queryFn: ({ signal }) =>
      listOrganizationEntitlementSnapshots(env.platformApiBaseUrl, organizationId!, product!, {
        page,
        pageSize: ORGANIZATION_ENTITLEMENT_PAGE_SIZE,
        signal,
      }),
  });
}

export const organizationPaymentsQueryKey = (
  organizationId: string,
  state: OrganizationBillingUrlState,
) => ["organizations", "payments", organizationId, state.status, state.page] as const;

export function useOrganizationPaymentsQuery(
  organizationId: string | null,
  state: OrganizationBillingUrlState,
) {
  return useQuery({
    queryKey: organizationPaymentsQueryKey(organizationId ?? "", state),
    enabled: organizationId != null,
    queryFn: ({ signal }) =>
      listOrganizationPayments(env.platformApiBaseUrl, organizationId!, state, signal),
  });
}
