import { posRequest, type PosWorkspaceScope } from "@/api/pos/pos-http";

export type PosDeviceAuthorizationPolicy = {
  enforcementEnabled: boolean;
};

/**
 * Reads server PosDeviceAuthorization.EnforcementEnabled for UX gates.
 * Authorization remains authoritative on money-affecting POS APIs.
 */
export async function getPosDeviceAuthorizationPolicy(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<PosDeviceAuthorizationPolicy> {
  const raw = await posRequest<Record<string, unknown>>({
    path: "/api/v1/pos/runtime/device-authorization",
    workspace,
    signal,
  });
  const value = raw.enforcementEnabled ?? raw.EnforcementEnabled;
  return {
    // Fail closed when the payload is unexpected.
    enforcementEnabled: typeof value === "boolean" ? value : true,
  };
}
