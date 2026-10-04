import { platformRequest } from "@/api/platform/platform-http";

export const COMPLIANCE_REGISTRATION_TYPES = [
  "PosPermitToUse",
  "CasRegistration",
  "EisCertification",
  "EisPermitToTransmit",
  "Other",
] as const;

export type ComplianceRegistrationType = (typeof COMPLIANCE_REGISTRATION_TYPES)[number];

export type OrganizationComplianceProfile = {
  organizationId: string;
  registeredTaxpayerName: string | null;
  maskedTin: string | null;
  setupStatus: string;
  complianceEligibilityStatus: string;
  taxConfigurationEnabled: boolean;
  taxDocumentImplementationAvailable: boolean;
};

export type ComplianceChecklistItem = {
  code: string;
  label: string;
  done: boolean;
};

export type ComplianceActivationReadiness = {
  organizationId: string;
  overallStatus: string;
  isReadyForTaxDocumentActivation: boolean;
  blockingReasons: string[];
  checklist: ComplianceChecklistItem[];
};

export type BranchComplianceProfile = {
  organizationBranchId: string;
  birBranchCode: string | null;
  setupStatus: string;
  notes: string | null;
};

export type ComplianceRegistrationRecord = {
  id: string;
  registrationType: string;
  referenceNumber: string | null;
  status: string;
  recordedAtUtc: string;
};

export type OrganizationBranchSummary = {
  id: string;
  name: string;
  code: string;
  status: string;
};

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : {};
}

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

function readString(raw: Record<string, unknown>, camel: string, pascal: string): string | null {
  const value = pick(raw, camel, pascal);
  if (typeof value !== "string") {
    return null;
  }
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function readStringList(value: unknown): string[] {
  if (!Array.isArray(value)) {
    return [];
  }
  return value.filter((item): item is string => typeof item === "string" && item.trim().length > 0);
}

/**
 * Map the server projection. Persisted TIN is never copied, even if a payload includes one.
 */
export function mapComplianceProfile(
  raw: unknown,
  organizationId: string,
): OrganizationComplianceProfile {
  const record = asRecord(raw);
  return {
    organizationId: readString(record, "organizationId", "OrganizationId") ?? organizationId,
    registeredTaxpayerName: readString(record, "registeredTaxpayerName", "RegisteredTaxpayerName"),
    maskedTin: readString(record, "maskedTin", "MaskedTin"),
    setupStatus: readString(record, "setupStatus", "SetupStatus") ?? "NotConfigured",
    complianceEligibilityStatus:
      readString(record, "complianceEligibilityStatus", "ComplianceEligibilityStatus") ??
      "NotRequested",
    taxConfigurationEnabled: Boolean(
      pick(record, "taxConfigurationEnabled", "TaxConfigurationEnabled"),
    ),
    taxDocumentImplementationAvailable: Boolean(
      pick(record, "taxDocumentImplementationAvailable", "TaxDocumentImplementationAvailable"),
    ),
  };
}

export function mapComplianceReadiness(raw: unknown, organizationId: string): ComplianceActivationReadiness {
  const record = asRecord(raw);
  const checklistRaw = pick(record, "checklist", "Checklist");
  const checklist = Array.isArray(checklistRaw)
    ? checklistRaw.map((item) => {
        const row = asRecord(item);
        return {
          code: readString(row, "code", "Code") ?? "",
          label: readString(row, "label", "Label") ?? "",
          done: Boolean(pick(row, "done", "Done")),
        };
      })
    : [];
  return {
    organizationId: readString(record, "organizationId", "OrganizationId") ?? organizationId,
    overallStatus: readString(record, "overallStatus", "OverallStatus") ?? "NotReady",
    isReadyForTaxDocumentActivation: Boolean(
      pick(record, "isReadyForTaxDocumentActivation", "IsReadyForTaxDocumentActivation"),
    ),
    blockingReasons: readStringList(pick(record, "blockingReasons", "BlockingReasons")),
    checklist,
  };
}

export function mapBranchComplianceProfile(raw: unknown): BranchComplianceProfile {
  const record = asRecord(raw);
  return {
    organizationBranchId: readString(record, "organizationBranchId", "OrganizationBranchId") ?? "",
    birBranchCode: readString(record, "birBranchCode", "BirBranchCode"),
    setupStatus: readString(record, "setupStatus", "SetupStatus") ?? "NotConfigured",
    notes: readString(record, "notes", "Notes"),
  };
}

export function mapRegistrationRecord(raw: unknown): ComplianceRegistrationRecord {
  const record = asRecord(raw);
  return {
    id: readString(record, "id", "Id") ?? "",
    registrationType: readString(record, "registrationType", "RegistrationType") ?? "Other",
    referenceNumber: readString(record, "referenceNumber", "ReferenceNumber"),
    status: readString(record, "status", "Status") ?? "Provided",
    recordedAtUtc: readString(record, "recordedAtUtc", "RecordedAtUtc") ?? "",
  };
}

export function mapBranchSummary(raw: unknown): OrganizationBranchSummary {
  const record = asRecord(raw);
  return {
    id: readString(record, "id", "Id") ?? "",
    name: readString(record, "name", "Name") ?? "",
    code: readString(record, "code", "Code") ?? "",
    status: readString(record, "status", "Status") ?? "Active",
  };
}

function orgPath(organizationId: string, suffix: string): string {
  return `/api/v1/platform/organizations/${organizationId}${suffix}`;
}

export async function getOrganizationComplianceProfile(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationComplianceProfile> {
  const raw = await platformRequest<unknown>({
    path: orgPath(organizationId, "/compliance-profile"),
    signal,
  });
  return mapComplianceProfile(raw, organizationId);
}

export async function getComplianceActivationReadiness(
  organizationId: string,
  signal?: AbortSignal,
): Promise<ComplianceActivationReadiness> {
  const raw = await platformRequest<unknown>({
    path: orgPath(organizationId, "/compliance/readiness"),
    signal,
  });
  return mapComplianceReadiness(raw, organizationId);
}

export async function listBranchComplianceProfiles(
  organizationId: string,
  signal?: AbortSignal,
): Promise<BranchComplianceProfile[]> {
  const raw = await platformRequest<unknown>({
    path: orgPath(organizationId, "/compliance/branch-profiles"),
    signal,
  });
  return Array.isArray(raw) ? raw.map(mapBranchComplianceProfile) : [];
}

export async function listComplianceRegistrationRecords(
  organizationId: string,
  signal?: AbortSignal,
): Promise<ComplianceRegistrationRecord[]> {
  const raw = await platformRequest<unknown>({
    path: orgPath(organizationId, "/compliance/registration-records"),
    signal,
  });
  return Array.isArray(raw) ? raw.map(mapRegistrationRecord) : [];
}

export async function listOrganizationBranches(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationBranchSummary[]> {
  const raw = await platformRequest<unknown>({
    path: orgPath(organizationId, "/branches"),
    signal,
  });
  return Array.isArray(raw) ? raw.map(mapBranchSummary) : [];
}

export async function updateRegisteredTaxpayer(
  organizationId: string,
  body: { registeredTaxpayerName: string | null; tin: string | null },
): Promise<OrganizationComplianceProfile> {
  const raw = await platformRequest<unknown>({
    method: "PUT",
    path: orgPath(organizationId, "/compliance-profile/registered-taxpayer"),
    body: {
      registeredTaxpayerName: body.registeredTaxpayerName,
      tin: body.tin,
    },
  });
  return mapComplianceProfile(raw, organizationId);
}

export async function upsertBranchComplianceProfile(
  organizationId: string,
  branchId: string,
  body: { birBranchCode: string | null; notes: string | null },
): Promise<void> {
  await platformRequest<unknown>({
    method: "PUT",
    path: orgPath(organizationId, `/branches/${branchId}/compliance-profile`),
    body: {
      birBranchCode: body.birBranchCode,
      setupStatus: "SetupInProgress",
      notes: body.notes,
    },
  });
}

export async function addComplianceRegistrationRecord(
  organizationId: string,
  body: { registrationType: string; referenceNumber: string | null },
): Promise<void> {
  await platformRequest<unknown>({
    method: "POST",
    path: orgPath(organizationId, "/compliance/registration-records"),
    body: {
      registrationType: body.registrationType,
      referenceNumber: body.referenceNumber,
    },
  });
}

export async function submitComplianceReadinessForReview(
  organizationId: string,
): Promise<ComplianceActivationReadiness> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: orgPath(organizationId, "/compliance/readiness/submit"),
  });
  return mapComplianceReadiness(raw, organizationId);
}

export type OrganizationComplianceStatus = {
  organizationId: string;
  complianceEligibilityStatus: string;
  taxDocumentIssuanceEnabled: boolean;
  taxDocumentIssuanceStatus: string;
  taxConfigurationEnabled: boolean;
  taxConfigurationStatus: string;
  taxDocumentImplementationAvailable: boolean;
  currentOwnerEducationAcknowledged: boolean;
  educationVersion: string;
};

/** Owner may request review only from these eligibility states. */
export const COMPLIANCE_REVIEW_REQUEST_STATUSES = ["NotRequested", "Rejected", "Revoked"] as const;

export function canRequestComplianceReview(status: string | null | undefined): boolean {
  return (
    status == null ||
    (COMPLIANCE_REVIEW_REQUEST_STATUSES as readonly string[]).includes(status)
  );
}

export function mapComplianceStatus(raw: unknown, organizationId: string): OrganizationComplianceStatus {
  const record = asRecord(raw);
  return {
    organizationId: readString(record, "organizationId", "OrganizationId") ?? organizationId,
    complianceEligibilityStatus:
      readString(record, "complianceEligibilityStatus", "ComplianceEligibilityStatus") ??
      "NotRequested",
    taxDocumentIssuanceEnabled: Boolean(
      pick(record, "taxDocumentIssuanceEnabled", "TaxDocumentIssuanceEnabled"),
    ),
    taxDocumentIssuanceStatus:
      readString(record, "taxDocumentIssuanceStatus", "TaxDocumentIssuanceStatus") ?? "NotEnabled",
    taxConfigurationEnabled: Boolean(
      pick(record, "taxConfigurationEnabled", "TaxConfigurationEnabled"),
    ),
    taxConfigurationStatus:
      readString(record, "taxConfigurationStatus", "TaxConfigurationStatus") ?? "NotEnabled",
    taxDocumentImplementationAvailable: Boolean(
      pick(record, "taxDocumentImplementationAvailable", "TaxDocumentImplementationAvailable"),
    ),
    currentOwnerEducationAcknowledged: Boolean(
      pick(record, "currentOwnerEducationAcknowledged", "CurrentOwnerEducationAcknowledged"),
    ),
    educationVersion: readString(record, "educationVersion", "EducationVersion") ?? "",
  };
}

export async function getOrganizationComplianceStatus(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationComplianceStatus> {
  const raw = await platformRequest<unknown>({
    path: orgPath(organizationId, "/compliance-status"),
    signal,
  });
  return mapComplianceStatus(raw, organizationId);
}

/** Empty body. The server resolves the Owner; the client does not send an actor id. */
export async function requestOrganizationComplianceReview(
  organizationId: string,
): Promise<OrganizationComplianceStatus> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: orgPath(organizationId, "/compliance/request"),
  });
  return mapComplianceStatus(raw, organizationId);
}
