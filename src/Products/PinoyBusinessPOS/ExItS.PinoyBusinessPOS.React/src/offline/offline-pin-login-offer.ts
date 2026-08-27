import {
  hasExpiredOfflineGrantOnInstallation,
  listEligibleOfflinePinProfiles,
} from "@/offline/offline-pin-profiles";

export type OfflinePinLoginOffer = {
  canOfferPinUnlock: boolean;
  grantExpired: boolean;
  noEnrollment: boolean;
};

/**
 * Whether the login screen may offer offline PIN unlock for this installation.
 * Branch/org come from the server-signed grant — no picker on the sign-in screen.
 * Web/PWA online-only: returns no offer unless `allowOfflineEngine` (native/tests).
 */
export async function evaluateOfflinePinLoginOffer(options?: {
  allowOfflineEngine?: boolean;
}): Promise<OfflinePinLoginOffer> {
  const profiles = await listEligibleOfflinePinProfiles(Date.now(), options);
  if (profiles.length > 0) {
    return {
      canOfferPinUnlock: true,
      grantExpired: false,
      noEnrollment: false,
    };
  }

  if (hasExpiredOfflineGrantOnInstallation()) {
    return {
      canOfferPinUnlock: false,
      grantExpired: true,
      noEnrollment: false,
    };
  }

  return {
    canOfferPinUnlock: false,
    grantExpired: false,
    noEnrollment: true,
  };
}
