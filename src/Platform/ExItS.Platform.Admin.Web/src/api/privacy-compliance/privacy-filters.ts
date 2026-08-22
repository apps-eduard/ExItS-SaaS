import type { ComplianceRequirementDto, PrivacyCategorySegment } from "@/api/privacy-compliance/privacy-types";

/** Mirrors Blazor PrivacyComplianceFilters (client presentation filters only). */
export const DOCUMENT_CATEGORIES = new Set(["CustomerFacing", "Internal", "RegulatoryReadiness"]);

export function matchesDocuments(
  requirement: ComplianceRequirementDto,
  categoryFilter: string | null | undefined,
): boolean {
  if (categoryFilter && categoryFilter.trim().length > 0) {
    return requirement.category.localeCompare(categoryFilter, undefined, { sensitivity: "accent" }) === 0;
  }
  return DOCUMENT_CATEGORIES.has(requirement.category);
}

export function matchesPias(requirement: ComplianceRequirementDto): boolean {
  return (
    requirement.category.localeCompare("PrivacyImpactAssessment", undefined, {
      sensitivity: "accent",
    }) === 0 || requirement.code.toUpperCase().includes("PIA")
  );
}

export function matchesDataInventory(requirement: ComplianceRequirementDto): boolean {
  return (
    requirement.category.localeCompare("DataInventory", undefined, { sensitivity: "accent" }) === 0
  );
}

export function matchesRetention(requirement: ComplianceRequirementDto): boolean {
  return requirement.category.localeCompare("Retention", undefined, { sensitivity: "accent" }) === 0;
}

export function matchesIncidents(requirement: ComplianceRequirementDto): boolean {
  return (
    requirement.category.localeCompare("IncidentBreach", undefined, { sensitivity: "accent" }) === 0
  );
}

export function matchesVendors(requirement: ComplianceRequirementDto): boolean {
  return (
    requirement.category.localeCompare("VendorProcessor", undefined, { sensitivity: "accent" }) === 0
  );
}

export function matchesDpoNpc(requirement: ComplianceRequirementDto): boolean {
  return (
    requirement.category.localeCompare("DpoNpc", undefined, { sensitivity: "accent" }) === 0 ||
    requirement.category.localeCompare("RegulatoryReadiness", undefined, {
      sensitivity: "accent",
    }) === 0
  );
}

export function isImportantGap(requirement: ComplianceRequirementDto): boolean {
  const levelOk =
    requirement.requirementLevel.localeCompare("Required", undefined, { sensitivity: "accent" }) ===
    0;
  const statusGap =
    requirement.status.localeCompare("NotStarted", undefined, { sensitivity: "accent" }) === 0 ||
    requirement.status.localeCompare("NeedsUpdate", undefined, { sensitivity: "accent" }) === 0;
  return levelOk && statusGap;
}

export function matchesCategorySegment(
  requirement: ComplianceRequirementDto,
  segment: PrivacyCategorySegment,
): boolean {
  switch (segment) {
    case "pias":
      return matchesPias(requirement);
    case "data-inventory":
      return matchesDataInventory(requirement);
    case "retention":
      return matchesRetention(requirement);
    case "incidents":
      return matchesIncidents(requirement);
    case "vendors":
      return matchesVendors(requirement);
    case "dpo-npc":
      return matchesDpoNpc(requirement);
  }
}

export function sortByCode<T extends { code: string }>(items: readonly T[]): T[] {
  return [...items].sort((a, b) => a.code.localeCompare(b.code, undefined, { sensitivity: "base" }));
}
