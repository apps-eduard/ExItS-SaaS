import { isOfflinePinAndDekConfigured } from "@/offline/local-store-key";
import { evaluateColdStartOfflineGrant } from "@/offline/offline-operating-grant";

export type OfflinePinLoginOffer = {
  canOfferPinUnlock: boolean;
  grantExpired: boolean;
  noEnrollment: boolean;
};

/**
 * Whether the login screen may offer offline PIN unlock for this installation.
 * Branch/org come from the server-signed grant — no picker.
 */
export async function evaluateOfflinePinLoginOffer(): Promise<OfflinePinLoginOffer> {
  const cold = await evaluateColdStartOfflineGrant();
  if (!cold.ok) {
    return {
      canOfferPinUnlock: false,
      grantExpired: cold.reason === "grant_expired",
      noEnrollment: cold.reason === "no_grant" || cold.reason === "unsupported_schema",
    };
  }

  const enrolled = isOfflinePinAndDekConfigured(cold.grant.userId);
  return {
    canOfferPinUnlock: enrolled,
    grantExpired: false,
    noEnrollment: !enrolled,
  };
}
