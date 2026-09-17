import { posRequest, PosApiError } from "@/api/pos/pos-http";
import {
  normalizeBranchType,
  type OrganizationBranchType,
} from "@/features/branches/branch-type";

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => {
    window.setTimeout(resolve, ms);
  });
}

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : {};
}

export type OperationalBranchContext = {
  organizationId: string;
  branchId: string;
  name: string;
  deviceMatchesSelectedBranch: boolean;
  deviceBoundBranchId?: string | null;
  openCashierShiftPresent: boolean;
  /** Retail (default) or Warehouse. */
  branchType: OrganizationBranchType;
};

export function normalizeOperationalBranchContext(raw: unknown): OperationalBranchContext {
  const r = asRecord(raw);
  return {
    organizationId: String(r.organizationId ?? r.OrganizationId ?? ""),
    branchId: String(r.branchId ?? r.BranchId ?? ""),
    name: String(r.name ?? r.Name ?? ""),
    deviceMatchesSelectedBranch: Boolean(
      r.deviceMatchesSelectedBranch ?? r.DeviceMatchesSelectedBranch ?? false,
    ),
    deviceBoundBranchId:
      r.deviceBoundBranchId != null || r.DeviceBoundBranchId != null
        ? String(r.deviceBoundBranchId ?? r.DeviceBoundBranchId)
        : null,
    openCashierShiftPresent: Boolean(
      r.openCashierShiftPresent ?? r.OpenCashierShiftPresent ?? false,
    ),
    branchType: normalizeBranchType(r.branchType ?? r.BranchType),
  };
}

/**
 * POS operational branch switch (MAUI parity).
 * Requires ViewCatalog on the session grant. Does not rebind or invent a POS device.
 */
export async function selectOperationalBranch(input: {
  organizationId: string;
  branchId: string;
  fromBranchId?: string | null;
}): Promise<
  | { ok: true; context: OperationalBranchContext }
  | { ok: false; status: number; errorCode?: string; detail?: string; traceId?: string }
> {
  const maxAttempts = 3;

  for (let attempt = 0; attempt < maxAttempts; attempt += 1) {
    try {
      const payload = await posRequest<unknown>({
        method: "PUT",
        path: "/api/v1/pos/operational-branch",
        workspace: {
          organizationId: input.organizationId,
          branchId: input.branchId,
        },
        body: {
          branchId: input.branchId,
          fromBranchId: input.fromBranchId ?? null,
          deviceBoundBranchId: null,
        },
      });
      return { ok: true, context: normalizeOperationalBranchContext(payload) };
    } catch (error) {
      if (error instanceof PosApiError) {
        const isRateLimited =
          error.status === 429 || error.errorCode === "pos.rate_limit.exceeded";
        if (isRateLimited && attempt + 1 < maxAttempts) {
          await sleep(750 * (attempt + 1));
          continue;
        }

        return {
          ok: false,
          status: error.status,
          errorCode: error.errorCode,
          detail: error.problem.detail,
          traceId: error.problem.traceId,
        };
      }
      throw error;
    }
  }

  return {
    ok: false,
    status: 429,
    errorCode: "pos.rate_limit.exceeded",
    detail: "Request rate limit exceeded. Retry later.",
  };
}
