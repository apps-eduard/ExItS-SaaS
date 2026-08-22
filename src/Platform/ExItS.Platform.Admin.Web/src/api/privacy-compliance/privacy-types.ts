/** DTOs from GET /api/v1/platform/privacy-compliance/* (JSON string enums). */

export type ComplianceRequirementDto = {
  id: string;
  code: string;
  title: string;
  category: string;
  description: string;
  requirementLevel: string;
  status: string;
  ownerRole: string;
  version: string;
  effectiveDate: string | null;
  lastReviewedDate: string | null;
  nextReviewDate: string | null;
  notes: string | null;
  sourceReference: string | null;
  requiresDpoLegalVerification: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  evidenceCount: number;
};

export type ComplianceEvidenceDto = {
  id: string;
  requirementId: string;
  kind: string;
  label: string;
  referencePath: string;
  notes: string | null;
  createdAtUtc: string;
};

export type ProcessingSystemDto = {
  id: string;
  code: string;
  systemName: string;
  purpose: string;
  dataSubjects: string;
  personalDataCategories: string;
  sensitiveDataCategories: string | null;
  storageLocation: string;
  recipientsProcessors: string | null;
  retentionSummary: string | null;
  securityControls: string | null;
  owner: string;
  piaStatus: string;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type PrivacyReadinessCategorySummaryDto = {
  group: string;
  detailRoute: string;
  requirementCount: number;
  readyCount: number;
  actionNeededCount: number;
  evidenceCoveredCount: number;
  lastReviewedDate: string | null;
  status: string;
  hasActionNeeded: boolean;
};

export type PrivacyImpactFollowUpDto = {
  code: string;
  title: string;
  status: string;
  requiresDpoLegalVerification: boolean;
  evidenceCount: number;
  lastReviewedDate: string | null;
};

export type PrivacyComplianceOverviewDto = {
  totalRequirements: number;
  totalSystems: number;
  totalEvidence: number;
  requirementsByStatus: Record<string, number>;
  requirementsByCategory: Record<string, number>;
  lastUpdatedUtc: string | null;
  overallReadiness: string;
  readyCount: number;
  actionNeededCount: number;
  externalLegalReviewCount: number;
  requirementsWithEvidenceCount: number;
  technicalSafeguardsSummary: string;
  governanceDocumentationSummary: string;
  legalReviewSummary: string;
  npcVerificationSummary: string;
  categorySummaries: PrivacyReadinessCategorySummaryDto[] | null;
  privacyImpactFollowUps: PrivacyImpactFollowUpDto[] | null;
};

export type PrivacyEvidenceRow = {
  id: string;
  evidence: ComplianceEvidenceDto;
  requirementCode: string;
  requirementTitle: string;
};

export type PrivacyCategorySegment =
  | "pias"
  | "data-inventory"
  | "retention"
  | "incidents"
  | "vendors"
  | "dpo-npc";
