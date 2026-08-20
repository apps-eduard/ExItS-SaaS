import { posRequest, PosApiError } from "@/api/pos/pos-http";

export type OperationalBranchContext = {
  organizationId: string;
  branchId: string;
  name: string;
  deviceMatchesSelectedBranch: boolean;
  deviceBoundBranchId?: string | null;
  openCashierShiftPresent: boolean;
};

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
  | { ok: false; status: number; errorCode?: string; detail?: string }
> {
  try {
    const context = await posRequest<OperationalBranchContext>({
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
    return { ok: true, context };
  } catch (error) {
    if (error instanceof PosApiError) {
      return {
        ok: false,
        status: error.status,
        errorCode: error.errorCode,
        detail: error.problem.detail,
      };
    }
    throw error;
  }
}
