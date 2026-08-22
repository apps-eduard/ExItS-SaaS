import { platformRequest } from "@/api/platform-http";
import type {
  ComplianceEvidenceDto,
  ComplianceRequirementDto,
  PrivacyComplianceOverviewDto,
  PrivacyEvidenceRow,
  ProcessingSystemDto,
} from "@/api/privacy-compliance/privacy-types";

const BASE = "/api/v1/platform/privacy-compliance";

export function getPrivacyComplianceOverview(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<PrivacyComplianceOverviewDto> {
  return platformRequest<PrivacyComplianceOverviewDto>(baseUrl, {
    path: `${BASE}/overview`,
    signal,
  });
}

export function listPrivacyComplianceRequirements(
  baseUrl: string,
  options?: { category?: string | null; signal?: AbortSignal },
): Promise<ComplianceRequirementDto[]> {
  const params = new URLSearchParams();
  if (options?.category && options.category.trim().length > 0) {
    params.set("category", options.category.trim());
  }
  const query = params.toString();
  return platformRequest<ComplianceRequirementDto[]>(baseUrl, {
    path: query ? `${BASE}/requirements?${query}` : `${BASE}/requirements`,
    signal: options?.signal,
  });
}

export function getPrivacyComplianceRequirement(
  baseUrl: string,
  requirementId: string,
  signal?: AbortSignal,
): Promise<ComplianceRequirementDto> {
  return platformRequest<ComplianceRequirementDto>(baseUrl, {
    path: `${BASE}/requirements/${encodeURIComponent(requirementId)}`,
    signal,
  });
}

export function listPrivacyComplianceEvidence(
  baseUrl: string,
  requirementId: string,
  signal?: AbortSignal,
): Promise<ComplianceEvidenceDto[]> {
  return platformRequest<ComplianceEvidenceDto[]>(baseUrl, {
    path: `${BASE}/requirements/${encodeURIComponent(requirementId)}/evidence`,
    signal,
  });
}

export function listPrivacyComplianceSystems(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<ProcessingSystemDto[]> {
  return platformRequest<ProcessingSystemDto[]>(baseUrl, {
    path: `${BASE}/systems`,
    signal,
  });
}

/**
 * Aggregates evidence across requirements (same approach as Blazor Evidence page).
 * Any failed evidence fetch rejects — never invents an empty success.
 */
export async function listAggregatedPrivacyEvidence(
  baseUrl: string,
  requirements: readonly ComplianceRequirementDto[],
  signal?: AbortSignal,
): Promise<PrivacyEvidenceRow[]> {
  const batches = await Promise.all(
    requirements.map(async (requirement) => {
      const items = await listPrivacyComplianceEvidence(baseUrl, requirement.id, signal);
      return items.map((evidence) => ({
        id: evidence.id,
        evidence,
        requirementCode: requirement.code,
        requirementTitle: requirement.title,
      }));
    }),
  );

  const rows = batches.flat();
  rows.sort((a, b) =>
    a.requirementCode.localeCompare(b.requirementCode, undefined, { sensitivity: "base" }),
  );
  return rows;
}

export function privacyRequirementExportPdfPath(requirementId: string, companyName?: string): string {
  const params = new URLSearchParams();
  if (companyName && companyName.trim().length > 0) {
    params.set("companyName", companyName.trim());
  }
  const query = params.toString();
  const path = `${BASE}/requirements/${encodeURIComponent(requirementId)}/export.pdf`;
  return query ? `${path}?${query}` : path;
}
