import { useQuery } from "@tanstack/react-query";
import {
  getOrganization,
  getOrganizationCommercialSummary,
  listOrganizationBranches,
} from "@/api/organizations/organization-client";
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
