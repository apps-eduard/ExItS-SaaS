import { useQuery } from "@tanstack/react-query";
import {
  getPrivacyComplianceOverview,
  getPrivacyComplianceRequirement,
  listAggregatedPrivacyEvidence,
  listPrivacyComplianceRequirements,
  listPrivacyComplianceSystems,
} from "@/api/privacy-compliance/privacy-client";
import type { ComplianceRequirementDto } from "@/api/privacy-compliance/privacy-types";
import { env } from "@/lib/env";

export const privacyOverviewQueryKey = ["privacy-compliance", "overview"] as const;
export const privacyRequirementsQueryKey = ["privacy-compliance", "requirements"] as const;
export const privacySystemsQueryKey = ["privacy-compliance", "systems"] as const;
export const privacyRequirementDetailQueryKey = (id: string) =>
  ["privacy-compliance", "requirement", id] as const;
export const privacyEvidenceQueryKey = (requirementIdsKey: string) =>
  ["privacy-compliance", "evidence", requirementIdsKey] as const;

export function usePrivacyOverviewQuery(enabled: boolean) {
  return useQuery({
    queryKey: privacyOverviewQueryKey,
    enabled,
    queryFn: ({ signal }) => getPrivacyComplianceOverview(env.platformApiBaseUrl, signal),
  });
}

export function usePrivacyRequirementsQuery(enabled: boolean) {
  return useQuery({
    queryKey: privacyRequirementsQueryKey,
    enabled,
    queryFn: ({ signal }) => listPrivacyComplianceRequirements(env.platformApiBaseUrl, { signal }),
  });
}

export function usePrivacySystemsQuery(enabled: boolean) {
  return useQuery({
    queryKey: privacySystemsQueryKey,
    enabled,
    queryFn: ({ signal }) => listPrivacyComplianceSystems(env.platformApiBaseUrl, signal),
  });
}

export function usePrivacyRequirementDetailQuery(requirementId: string | null) {
  return useQuery({
    queryKey: privacyRequirementDetailQueryKey(requirementId ?? ""),
    enabled: requirementId != null,
    queryFn: ({ signal }) =>
      getPrivacyComplianceRequirement(env.platformApiBaseUrl, requirementId!, signal),
  });
}

export function usePrivacyAggregatedEvidenceQuery(
  enabled: boolean,
  requirements: readonly ComplianceRequirementDto[] | undefined,
) {
  const idsKey = (requirements ?? []).map((r) => r.id).join(",");
  return useQuery({
    queryKey: privacyEvidenceQueryKey(idsKey),
    enabled: enabled && requirements != null,
    queryFn: ({ signal }) =>
      listAggregatedPrivacyEvidence(env.platformApiBaseUrl, requirements ?? [], signal),
  });
}
