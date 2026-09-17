import { commercialMutationRequest } from "@/api/commercial/commercial-http";
import { platformRequest } from "@/api/platform-http";

export const ONLINE_SUPPLIER_PAYMENTS_STATUSES = [
  "Disabled",
  "Available",
  "Suspended",
] as const;

export type OnlineSupplierPaymentsStatus =
  (typeof ONLINE_SUPPLIER_PAYMENTS_STATUSES)[number];

export type OrganizationOnlineSupplierPaymentsCapability = {
  organizationId: string;
  status: OnlineSupplierPaymentsStatus;
  updatedAtUtc?: string;
  updatedByActorReference?: string;
  reason?: string;
};

export const BIR_COMPLIANCE_STATUSES = [
  "NotRequested",
  "Requested",
  "DocumentsRequired",
  "UnderReview",
  "Approved",
  "Rejected",
  "Suspended",
  "Revoked",
] as const;

export type BirComplianceStatus = (typeof BIR_COMPLIANCE_STATUSES)[number];

export type OrganizationComplianceStatus = {
  organizationId: string;
  complianceEligibilityStatus: BirComplianceStatus;
  taxDocumentIssuanceEnabled: boolean;
  taxDocumentIssuanceStatus: string;
  taxConfigurationEnabled: boolean;
  taxConfigurationStatus: string;
  taxDocumentImplementationAvailable: boolean;
  currentOwnerEducationAcknowledged: boolean;
  educationVersion: string;
  updatedAtUtc?: string;
  updatedByActorReference?: string;
};

function asRecord(payload: unknown): Record<string, unknown> | null {
  if (typeof payload !== "object" || payload === null) {
    return null;
  }
  return payload as Record<string, unknown>;
}

function readString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.length > 0) {
      return value;
    }
  }
  return undefined;
}

function readBoolean(record: Record<string, unknown>, ...keys: string[]): boolean {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "boolean") {
      return value;
    }
  }
  return false;
}

function isOnlineStatus(value: string | undefined): value is OnlineSupplierPaymentsStatus {
  return (
    value != null &&
    (ONLINE_SUPPLIER_PAYMENTS_STATUSES as readonly string[]).includes(value)
  );
}

function isBirStatus(value: string | undefined): value is BirComplianceStatus {
  return value != null && (BIR_COMPLIANCE_STATUSES as readonly string[]).includes(value);
}

export function mapOnlineSupplierPaymentsCapability(
  payload: unknown,
): OrganizationOnlineSupplierPaymentsCapability {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid online supplier payments capability.");
  }
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const status = readString(record, "status", "Status");
  if (!organizationId || !isOnlineStatus(status)) {
    throw new Error("Invalid online supplier payments capability.");
  }
  return {
    organizationId,
    status,
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
    updatedByActorReference: readString(
      record,
      "updatedByActorReference",
      "UpdatedByActorReference",
    ),
    reason: readString(record, "reason", "Reason"),
  };
}

export function mapOrganizationComplianceStatus(payload: unknown): OrganizationComplianceStatus {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid organization compliance status.");
  }
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const complianceEligibilityStatus = readString(
    record,
    "complianceEligibilityStatus",
    "ComplianceEligibilityStatus",
  );
  if (!organizationId || !isBirStatus(complianceEligibilityStatus)) {
    throw new Error("Invalid organization compliance status.");
  }
  return {
    organizationId,
    complianceEligibilityStatus,
    taxDocumentIssuanceEnabled: readBoolean(
      record,
      "taxDocumentIssuanceEnabled",
      "TaxDocumentIssuanceEnabled",
    ),
    taxDocumentIssuanceStatus:
      readString(record, "taxDocumentIssuanceStatus", "TaxDocumentIssuanceStatus") ?? "NotEnabled",
    taxConfigurationEnabled: readBoolean(
      record,
      "taxConfigurationEnabled",
      "TaxConfigurationEnabled",
    ),
    taxConfigurationStatus:
      readString(record, "taxConfigurationStatus", "TaxConfigurationStatus") ?? "NotEnabled",
    taxDocumentImplementationAvailable: readBoolean(
      record,
      "taxDocumentImplementationAvailable",
      "TaxDocumentImplementationAvailable",
    ),
    currentOwnerEducationAcknowledged: readBoolean(
      record,
      "currentOwnerEducationAcknowledged",
      "CurrentOwnerEducationAcknowledged",
    ),
    educationVersion: readString(record, "educationVersion", "EducationVersion") ?? "",
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
    updatedByActorReference: readString(
      record,
      "updatedByActorReference",
      "UpdatedByActorReference",
    ),
  };
}

export function getOrganizationOnlineSupplierPaymentsCapability(
  baseUrl: string,
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationOnlineSupplierPaymentsCapability> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/organizations/${organizationId}/online-supplier-payments`,
    signal,
  }).then(mapOnlineSupplierPaymentsCapability);
}

export function transitionOrganizationOnlineSupplierPayments(
  baseUrl: string,
  organizationId: string,
  body: { status: OnlineSupplierPaymentsStatus; reason?: string | null },
  signal?: AbortSignal,
): Promise<OrganizationOnlineSupplierPaymentsCapability> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/organizations/${organizationId}/online-supplier-payments/transition`,
    body: { status: body.status, reason: body.reason ?? null },
    signal,
  }).then(mapOnlineSupplierPaymentsCapability);
}

export function getOrganizationComplianceStatus(
  baseUrl: string,
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationComplianceStatus> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/organizations/${organizationId}/compliance-status`,
    signal,
  }).then(mapOrganizationComplianceStatus);
}

export function transitionOrganizationCompliance(
  baseUrl: string,
  organizationId: string,
  targetStatus: BirComplianceStatus,
  signal?: AbortSignal,
): Promise<OrganizationComplianceStatus> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/organizations/${organizationId}/compliance/transition`,
    body: { targetStatus },
    signal,
  }).then(mapOrganizationComplianceStatus);
}

/** Domain-allowed online supplier payments transitions. */
export function isOnlineSupplierPaymentsTransitionAllowed(
  from: OnlineSupplierPaymentsStatus,
  to: OnlineSupplierPaymentsStatus,
): boolean {
  if (from === to) {
    return false;
  }
  switch (`${from}->${to}`) {
    case "Disabled->Available":
    case "Available->Disabled":
    case "Available->Suspended":
    case "Suspended->Available":
    case "Suspended->Disabled":
      return true;
    default:
      return false;
  }
}

/** Domain-allowed BIR compliance eligibility transitions (mirrors Platform domain). */
export function isBirComplianceTransitionAllowed(
  from: BirComplianceStatus,
  to: BirComplianceStatus,
): boolean {
  if (from === to) {
    return false;
  }
  const allowed: Record<BirComplianceStatus, BirComplianceStatus[]> = {
    NotRequested: ["Requested", "UnderReview"],
    Requested: ["DocumentsRequired", "UnderReview", "Rejected"],
    DocumentsRequired: ["UnderReview", "Rejected"],
    UnderReview: ["DocumentsRequired", "Approved", "Rejected"],
    Approved: ["Suspended", "Revoked", "UnderReview"],
    Rejected: ["Requested", "UnderReview"],
    Suspended: ["Approved", "UnderReview", "Revoked"],
    Revoked: ["Requested", "UnderReview"],
  };
  return allowed[from]?.includes(to) === true;
}

export function birComplianceDisplayLabel(status: BirComplianceStatus): string {
  switch (status) {
    case "NotRequested":
      return "Not approved";
    case "Requested":
    case "DocumentsRequired":
    case "UnderReview":
      return "Pending review";
    case "Approved":
      return "Approved";
    case "Rejected":
      return "Rejected";
    case "Suspended":
    case "Revoked":
      return "Suspended/Revoked";
    default:
      return status;
  }
}

export function onlineSupplierPaymentsDisplayLabel(
  status: OnlineSupplierPaymentsStatus,
): "Enabled" | "Disabled" | "Suspended" {
  switch (status) {
    case "Available":
      return "Enabled";
    case "Suspended":
      return "Suspended";
    default:
      return "Disabled";
  }
}
