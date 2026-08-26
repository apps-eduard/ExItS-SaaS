import { useQuery } from "@tanstack/react-query";
import { listOrganizations } from "@/api/organizations/organization-client";
import { listOrganizationSubscriptions } from "@/api/organizations/organization-client";
import {
  DEFAULT_SUBSCRIPTION_SORT,
  ORGANIZATION_SUBSCRIPTION_PAGE_SIZE,
  type OrganizationSubscriptionUrlState,
} from "@/api/organizations/subscription-list-query";
import { env } from "@/lib/env";

const ORG_PICKER_PAGE_SIZE = 20;

export const testPaymentsOrganizationsQueryKey = (search: string) =>
  ["local-validation", "test-payments", "organizations", search] as const;

export const testPaymentsSubscriptionsQueryKey = (organizationId: string) =>
  ["local-validation", "test-payments", "subscriptions", organizationId] as const;

const emptySubscriptionState: OrganizationSubscriptionUrlState = {
  page: 1,
  search: "",
  status: "",
  isTrial: "",
  productCode: "",
  sortBy: DEFAULT_SUBSCRIPTION_SORT,
  sortDesc: true,
};

export function useTestPaymentsOrganizationsQuery(search: string, enabled: boolean) {
  const trimmed = search.trim();
  return useQuery({
    queryKey: testPaymentsOrganizationsQueryKey(trimmed),
    enabled,
    queryFn: ({ signal }) =>
      listOrganizations(env.platformApiBaseUrl, {
        page: 1,
        pageSize: ORG_PICKER_PAGE_SIZE,
        search: trimmed || undefined,
        sortBy: "DisplayName",
        sortDesc: false,
        signal,
      }),
  });
}

export function useTestPaymentsSubscriptionsQuery(
  organizationId: string | null,
  enabled: boolean,
) {
  return useQuery({
    queryKey: testPaymentsSubscriptionsQueryKey(organizationId ?? ""),
    enabled: enabled && organizationId != null && organizationId.length > 0,
    queryFn: ({ signal }) =>
      listOrganizationSubscriptions(
        env.platformApiBaseUrl,
        organizationId!,
        {
          ...emptySubscriptionState,
          page: 1,
        },
        signal,
      ).then((page) => ({
        ...page,
        // Prefer a larger window for the picker when the first page is full.
        pageSize: Math.max(page.pageSize, ORGANIZATION_SUBSCRIPTION_PAGE_SIZE),
      })),
  });
}
