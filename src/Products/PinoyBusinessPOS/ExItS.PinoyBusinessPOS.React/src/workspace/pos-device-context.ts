/**
 * Browser/PWA POS device authorization context (RMAP-10b).
 * Does not invent an authorized terminal — status comes from durable identity + Platform authorize.
 * When POS API reports device transaction enforcement paused (pure PWA Local Validation),
 * money-post UX gates treat the device check as satisfied; server remains authoritative.
 */

export type PosDeviceContextStatus =
  | "not_required"
  | "deferred"
  | "loading"
  | "authorized"
  | "revoked"
  | "wrong_org"
  | "wrong_branch"
  | "unregistered"
  | "unavailable"
  | "unknown"
  | "error";

export type PosDeviceRegistrationStatus =
  | "unknown"
  | "unavailable"
  | "loading"
  | "unregistered"
  | "registered"
  | "authorized"
  | "revoked"
  | "wrong_branch"
  | "error";

export type PosDeviceContext = {
  status: PosDeviceContextStatus;
  durableIdentityAvailable: boolean;
  registrationStatus: PosDeviceRegistrationStatus;
  installationDeviceId: string | null;
  posDeviceId: string | null;
  registeredBranchId: string | null;
  authorizedForSelectedBranch: boolean;
  detail: string;
};

/** Pre-hydrate placeholder — replaced once durable identity + authorize run. */
export const INITIAL_POS_DEVICE_CONTEXT: PosDeviceContext = {
  status: "loading",
  durableIdentityAvailable: false,
  registrationStatus: "loading",
  installationDeviceId: null,
  posDeviceId: null,
  registeredBranchId: null,
  authorizedForSelectedBranch: false,
  detail: "Resolving browser POS installation identity…",
};

/** @deprecated Use INITIAL_POS_DEVICE_CONTEXT — retained for older test imports. */
export const DEFERRED_POS_DEVICE_CONTEXT: PosDeviceContext = {
  status: "deferred",
  durableIdentityAvailable: false,
  registrationStatus: "unknown",
  installationDeviceId: null,
  posDeviceId: null,
  registeredBranchId: null,
  authorizedForSelectedBranch: false,
  detail:
    "Registered POS installation device identity is not available until RMAP-10b hydration completes.",
};

export function isPosDeviceReadyForMoney(
  device: PosDeviceContext | null | undefined,
  options?: { enforcementEnabled?: boolean | null },
): boolean {
  // Pure React PWA: server PosDeviceAuthorization.EnforcementEnabled=false.
  if (options?.enforcementEnabled === false) {
    return true;
  }
  return (
    device?.status === "authorized" &&
    device.authorizedForSelectedBranch === true &&
    Boolean(device.installationDeviceId) &&
    device.durableIdentityAvailable === true
  );
}

export function unavailablePosDeviceContext(detail: string): PosDeviceContext {
  return {
    status: "unavailable",
    durableIdentityAvailable: false,
    registrationStatus: "unavailable",
    installationDeviceId: null,
    posDeviceId: null,
    registeredBranchId: null,
    authorizedForSelectedBranch: false,
    detail,
  };
}

export function unregisteredPosDeviceContext(
  installationDeviceId: string,
  detail: string,
): PosDeviceContext {
  return {
    status: "unregistered",
    durableIdentityAvailable: true,
    registrationStatus: "unregistered",
    installationDeviceId,
    posDeviceId: null,
    registeredBranchId: null,
    authorizedForSelectedBranch: false,
    detail,
  };
}

export function authorizedPosDeviceContext(input: {
  installationDeviceId: string;
  posDeviceId: string;
  registeredBranchId: string;
}): PosDeviceContext {
  return {
    status: "authorized",
    durableIdentityAvailable: true,
    registrationStatus: "authorized",
    installationDeviceId: input.installationDeviceId,
    posDeviceId: input.posDeviceId,
    registeredBranchId: input.registeredBranchId,
    authorizedForSelectedBranch: true,
    detail: "This browser is registered and authorized for the selected branch.",
  };
}
