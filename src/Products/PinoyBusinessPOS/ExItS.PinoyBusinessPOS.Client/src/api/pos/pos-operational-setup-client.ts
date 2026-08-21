import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const SETUP_PATH = "/api/v1/pos/operational-setup";

export type PosOperationalSetupDto = {
  organizationId: string;
  isComplete: boolean;
  currencyCode: string;
  cashCountMode: string;
};

/** Cash count policy for open/close shift — ViewOperationalSetup. */
export function getOperationalSetup(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<PosOperationalSetupDto> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: SETUP_PATH,
  });
}

export function resolveOpeningCashRequired(cashCountMode: string | null | undefined): boolean {
  if (!cashCountMode || cashCountMode.trim().length === 0) {
    return true;
  }
  return cashCountMode.localeCompare("Required", undefined, { sensitivity: "accent" }) === 0;
}

export function resolveOpeningCashVisible(cashCountMode: string | null | undefined): boolean {
  if (!cashCountMode || cashCountMode.trim().length === 0) {
    return true;
  }
  return cashCountMode.localeCompare("Off", undefined, { sensitivity: "accent" }) !== 0;
}
