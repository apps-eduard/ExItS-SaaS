import { platformRequest } from "@/api/platform/platform-http";

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

export type OrganizationSalesDocumentCapability = {
  organizationId: string;
  complianceEligibilityStatus: string;
  taxDocumentIssuanceEnabled: boolean;
  taxConfigurationEnabled: boolean;
  taxDocumentImplementationAvailable: boolean;
};

/** Platform eligibility Approved unlocks BIR Compliance tab — not TaxDocument issuance. */
export function isBirComplianceModuleUnlocked(
  capability: OrganizationSalesDocumentCapability | null | undefined,
): boolean {
  return capability?.complianceEligibilityStatus === "Approved";
}

export async function getOrganizationSalesDocumentCapability(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationSalesDocumentCapability | null> {
  try {
    const raw = await platformRequest<unknown>({
      path: `/api/v1/platform/organizations/${organizationId}/sales-document-capability`,
      signal,
    });
    if (!raw || typeof raw !== "object") {
      return null;
    }
    const r = raw as Record<string, unknown>;
    return {
      organizationId: String(pick(r, "organizationId", "OrganizationId") ?? organizationId),
      complianceEligibilityStatus: String(
        pick(r, "complianceEligibilityStatus", "ComplianceEligibilityStatus") ?? "NotRequested",
      ),
      taxDocumentIssuanceEnabled: Boolean(
        pick(r, "taxDocumentIssuanceEnabled", "TaxDocumentIssuanceEnabled") ?? false,
      ),
      taxConfigurationEnabled: Boolean(
        pick(r, "taxConfigurationEnabled", "TaxConfigurationEnabled") ?? false,
      ),
      taxDocumentImplementationAvailable: Boolean(
        pick(r, "taxDocumentImplementationAvailable", "TaxDocumentImplementationAvailable") ??
          false,
      ),
    };
  } catch {
    return null;
  }
}

export type OrganizationComplianceProfileSummary = {
  organizationId: string;
  registeredTaxpayerName: string | null;
  maskedTin: string | null;
  setupStatus: string | null;
};

export async function getOrganizationComplianceProfileSummary(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationComplianceProfileSummary | null> {
  try {
    const raw = await platformRequest<unknown>({
      path: `/api/v1/platform/organizations/${organizationId}/compliance-profile`,
      signal,
    });
    if (!raw || typeof raw !== "object") {
      return null;
    }
    const r = raw as Record<string, unknown>;
    return {
      organizationId: String(pick(r, "organizationId", "OrganizationId") ?? organizationId),
      registeredTaxpayerName:
        (pick(r, "registeredTaxpayerName", "RegisteredTaxpayerName") as string | null) ?? null,
      maskedTin: (pick(r, "maskedTin", "MaskedTin") as string | null) ?? null,
      setupStatus: (pick(r, "setupStatus", "SetupStatus") as string | null) ?? null,
    };
  } catch {
    return null;
  }
}

export type OrganizationComplianceReadinessSummary = {
  organizationId: string;
  isReady: boolean | null;
  blockingCount: number;
  warningCount: number;
};

export async function getOrganizationComplianceReadinessSummary(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationComplianceReadinessSummary | null> {
  try {
    const raw = await platformRequest<unknown>({
      path: `/api/v1/platform/organizations/${organizationId}/compliance/readiness`,
      signal,
    });
    if (!raw || typeof raw !== "object") {
      return null;
    }
    const r = raw as Record<string, unknown>;
    const blockers = (pick(r, "blockingReasons", "BlockingReasons") ?? []) as unknown[];
    const warnings = (pick(r, "warnings", "Warnings") ?? []) as unknown[];
    const isReadyRaw =
      pick(r, "isReadyForTaxDocumentActivation", "IsReadyForTaxDocumentActivation") ??
      pick(r, "isReady", "IsReady");
    return {
      organizationId: String(pick(r, "organizationId", "OrganizationId") ?? organizationId),
      isReady: typeof isReadyRaw === "boolean" ? isReadyRaw : null,
      blockingCount: Array.isArray(blockers) ? blockers.length : 0,
      warningCount: Array.isArray(warnings) ? warnings.length : 0,
    };
  } catch {
    return null;
  }
}
