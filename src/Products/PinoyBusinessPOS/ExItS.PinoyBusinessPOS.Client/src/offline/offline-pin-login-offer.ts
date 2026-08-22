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
 */
export async function evaluateOfflinePinLoginOffer(): Promise<OfflinePinLoginOffer> {
  const profiles = await listEligibleOfflinePinProfiles();
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
