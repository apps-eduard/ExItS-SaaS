import { platformRequest } from "@/api/platform/platform-http";

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

/** Platform-controlled online supplier payments status (GET contract). */
export type OnlineSupplierPaymentsStatus = "Disabled" | "Available" | "Suspended";

export type OrganizationOnlineSupplierPaymentsCapability = {
  organizationId: string;
  status: OnlineSupplierPaymentsStatus;
  updatedAtUtc: string | null;
  updatedByActorReference: string | null;
  reason: string | null;
};

export function parseOnlineSupplierPaymentsStatus(
  value: unknown,
): OnlineSupplierPaymentsStatus {
  if (value === "Available" || value === "Suspended" || value === "Disabled") {
    return value;
  }
  return "Disabled";
}

/**
 * Reads Platform online-supplier-payments capability.
 * Missing/failed responses fail closed as Disabled (no Pay now CTA).
 * SHARED sync: Platform GET must implement
 * GET /api/v1/platform/organizations/{id}/online-supplier-payments
 */
export async function getOrganizationOnlineSupplierPaymentsCapability(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationOnlineSupplierPaymentsCapability> {
  try {
    const raw = await platformRequest<unknown>({
      path: `/api/v1/platform/organizations/${organizationId}/online-supplier-payments`,
      signal,
    });
    if (!raw || typeof raw !== "object") {
      return {
        organizationId,
        status: "Disabled",
        updatedAtUtc: null,
        updatedByActorReference: null,
        reason: null,
      };
    }
    const r = raw as Record<string, unknown>;
    return {
      organizationId: String(pick(r, "organizationId", "OrganizationId") ?? organizationId),
      status: parseOnlineSupplierPaymentsStatus(pick(r, "status", "Status")),
      updatedAtUtc: (pick(r, "updatedAtUtc", "UpdatedAtUtc") as string | null) ?? null,
      updatedByActorReference:
        (pick(r, "updatedByActorReference", "UpdatedByActorReference") as string | null) ?? null,
      reason: (pick(r, "reason", "Reason") as string | null) ?? null,
    };
  } catch {
    return {
      organizationId,
      status: "Disabled",
      updatedAtUtc: null,
      updatedByActorReference: null,
      reason: null,
    };
  }
}
