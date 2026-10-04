import { platformRequest } from "@/api/platform/platform-http";

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function pick(record: Record<string, unknown>, camel: string, pascal: string): unknown {
  return record[camel] ?? record[pascal];
}

function readString(record: Record<string, unknown>, camel: string, pascal: string): string | null {
  const value = pick(record, camel, pascal);
  return typeof value === "string" && value.trim().length > 0 ? value : null;
}

export const SALES_DOCUMENT_EDUCATION_VERSION = "transaction-summary-v1";

export type SalesDocumentEducationStatus = {
  organizationId: string;
  currentVersion: string;
  currentOwnerAcknowledged: boolean;
  acknowledgedAtUtc: string | null;
  requiresOwnerAction: boolean;
  transactionSummaryAvailable: boolean;
  taxDocumentIssuanceEnabled: boolean;
  documentMode: string;
};

export function mapSalesDocumentEducationStatus(
  raw: unknown,
  organizationId: string,
): SalesDocumentEducationStatus {
  const record = asRecord(raw);
  const acknowledged = Boolean(pick(record, "currentOwnerAcknowledged", "CurrentOwnerAcknowledged"));
  return {
    organizationId: readString(record, "organizationId", "OrganizationId") ?? organizationId,
    currentVersion:
      readString(record, "currentVersion", "CurrentVersion") ?? SALES_DOCUMENT_EDUCATION_VERSION,
    currentOwnerAcknowledged: acknowledged,
    acknowledgedAtUtc: readString(record, "acknowledgedAtUtc", "AcknowledgedAtUtc"),
    requiresOwnerAction: Boolean(pick(record, "requiresOwnerAction", "RequiresOwnerAction") ?? !acknowledged),
    transactionSummaryAvailable: Boolean(
      pick(record, "transactionSummaryAvailable", "TransactionSummaryAvailable") ?? true,
    ),
    taxDocumentIssuanceEnabled: Boolean(
      pick(record, "taxDocumentIssuanceEnabled", "TaxDocumentIssuanceEnabled"),
    ),
    documentMode: readString(record, "documentMode", "DocumentMode") ?? "TransactionSummary",
  };
}

export async function getSalesDocumentEducationStatus(
  organizationId: string,
  signal?: AbortSignal,
): Promise<SalesDocumentEducationStatus> {
  const raw = await platformRequest<unknown>({
    path: `/api/v1/platform/organizations/${organizationId}/sales-document-education`,
    signal,
  });
  return mapSalesDocumentEducationStatus(raw, organizationId);
}

/**
 * Acknowledgment is idempotent and owner-scoped on the server.
 * No actor or user id is sent; the body stays empty.
 */
export async function acknowledgeSalesDocumentEducation(
  organizationId: string,
): Promise<SalesDocumentEducationStatus> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `/api/v1/platform/organizations/${organizationId}/sales-document-education/acknowledge`,
  });
  return mapSalesDocumentEducationStatus(raw, organizationId);
}
