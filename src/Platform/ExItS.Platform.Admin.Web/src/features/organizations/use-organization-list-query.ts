import { useQuery } from "@tanstack/react-query";
import { listOrganizations } from "@/api/organizations/organization-client";
import {
  ORGANIZATION_LIST_PAGE_SIZE,
  type OrganizationListQuery,
} from "@/api/organizations/organization-types";
import { env } from "@/lib/env";

export const organizationListQueryKey = (query: OrganizationListQuery) =>
  [
    "organizations",
    "list",
    query.page ?? 1,
    query.pageSize ?? ORGANIZATION_LIST_PAGE_SIZE,
    query.status ?? "",
    query.search ?? "",
    query.sortBy ?? "DisplayName",
    query.sortDesc === true,
    query.productCode ?? "",
  ] as const;

export function useOrganizationListQuery(query: OrganizationListQuery, enabled: boolean) {
  return useQuery({
    queryKey: organizationListQueryKey(query),
    enabled,
    queryFn: ({ signal }) =>
      listOrganizations(env.platformApiBaseUrl, {
        ...query,
        pageSize: query.pageSize ?? ORGANIZATION_LIST_PAGE_SIZE,
        signal,
      }),
  });
}
