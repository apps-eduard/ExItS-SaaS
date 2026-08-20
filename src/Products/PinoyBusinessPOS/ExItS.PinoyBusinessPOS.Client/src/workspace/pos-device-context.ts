/**
 * Browser/PWA POS device authorization for money operations is not yet contracted.
 * RMAP-03 must not invent a registered installation device.
 */
export type PosDeviceContextStatus =
  "not_required" | "deferred" | "authorized" | "revoked" | "wrong_org" | "wrong_branch" | "unknown";

export type PosDeviceContext = {
  status: PosDeviceContextStatus;
  installationDeviceId: string | null;
  detail: string;
};

export const DEFERRED_POS_DEVICE_CONTEXT: PosDeviceContext = {
  status: "deferred",
  installationDeviceId: null,
  detail:
    "Registered POS installation device identity is not available in the React/PWA client. Branch operational context is bound without inventing a device. Money endpoints remain server-gated until a device contract exists.",
};

export function isPosDeviceReadyForMoney(device: PosDeviceContext | null | undefined): boolean {
  return device?.status === "authorized" && Boolean(device.installationDeviceId);
}
