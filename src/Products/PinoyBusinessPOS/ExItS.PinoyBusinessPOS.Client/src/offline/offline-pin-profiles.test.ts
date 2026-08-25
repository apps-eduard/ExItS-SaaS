import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it } from "vitest";
import { evaluateOfflinePinLoginOffer } from "@/offline/offline-pin-login-offer";
import {
  hasExpiredOfflineGrantOnInstallation,
  listEligibleOfflinePinProfiles,
} from "@/offline/offline-pin-profiles";
import { enrollOfflinePinAndDek } from "@/offline/local-store-key";
import { persistServerSignedGrant, type StoredOfflineOperatingGrant } from "@/offline/offline-operating-grant";
import {
  canonicalizeOfflineOperatingGrant,
  scopeKindToNumeric,
  signOfflineOperatingGrantForTests,
} from "@/offline/server-signed-offline-grant";
import { INSTALLATION_DEVICE_ID_STORAGE_KEY } from "@/workspace/browser-installation-identity";

const DEV_PRIVATE_KEY_PEM = `-----BEGIN PRIVATE KEY-----
MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgJuN+Pa6hk6BZUISu
lodghNrUkSR+VQsjrIW49hJ21dihRANCAASSV3pYY5NEuiiPYCs/ZRXZL6dNW0DJ
8VhI3X4k2jMfgEoBV/n9zUzAIZMsJ6XfzAHR+cz3/VxgoQYquH3GV0Lt
-----END PRIVATE KEY-----`;

const USER = "248935e9-e462-425f-88f5-a9255bf12748";
const INSTALLATION = "22222222-2222-4222-8222-222222222222";
const PIN = "123456";

async function buildGrant(userId: string, displayName: string): Promise<StoredOfflineOperatingGrant> {
  const grantId = "33333333-3333-4333-8333-333333333333";
  const grant: StoredOfflineOperatingGrant = {
    grantId,
    schemaVersion: 4,
    userId,
    scopeKind: "Organization",
    organizationId: "ca023f5b-925e-4aa5-a843-d48c4c06fa14",
    organizationDisplayName: "Kizy Store",
    branchId: "742fb3f3-14f9-4bee-a94e-f5acccc7cbc5",
    branchName: "Main Branch",
    installationDeviceId: INSTALLATION,
    posDeviceId: "11111111-1111-1111-1111-111111111111",
    roleCode: "Cashier",
    displayName,
    username: "cashier",
    issuedAtUtc: "2026-01-01T12:00:00.000Z",
    lastOnlineValidatedAtUtc: "2026-01-01T12:00:00.000Z",
    expiresAtUtc: "2030-01-01T12:00:00.000Z",
    signature: "",
  };
  const canonical = canonicalizeOfflineOperatingGrant({
    grantId: grant.grantId,
    schemaVersion: grant.schemaVersion,
    userId: grant.userId,
    scopeKind: scopeKindToNumeric(grant.scopeKind),
    organizationId: grant.organizationId,
    organizationDisplayName: grant.organizationDisplayName,
    branchId: grant.branchId,
    branchName: grant.branchName,
    installationDeviceId: grant.installationDeviceId,
    posDeviceId: grant.posDeviceId,
    roleCode: grant.roleCode,
    displayName: grant.displayName,
    username: grant.username,
    issuedAtUtc: grant.issuedAtUtc,
    lastOnlineValidatedAtUtc: grant.lastOnlineValidatedAtUtc,
    expiresAtUtc: grant.expiresAtUtc,
  });
  grant.signature = await signOfflineOperatingGrantForTests(canonical, DEV_PRIVATE_KEY_PEM);
  return grant;
}

describe("offline PIN profiles", () => {
  beforeEach(async () => {
    window.localStorage.clear();
    window.localStorage.setItem(INSTALLATION_DEVICE_ID_STORAGE_KEY, INSTALLATION);
    persistServerSignedGrant(await buildGrant(USER, "Maria Santos"));
    await enrollOfflinePinAndDek(USER, PIN);
  });

  it("lists eligible offline PIN profiles for this installation", async () => {
    const profiles = await listEligibleOfflinePinProfiles();
    expect(profiles).toHaveLength(1);
    expect(profiles[0]?.displayName).toBe("Maria Santos");
    expect(profiles[0]?.branchName).toBe("Main Branch");
  });

  it("offers PIN unlock when a profile is prepared", async () => {
    const offer = await evaluateOfflinePinLoginOffer();
    expect(offer.canOfferPinUnlock).toBe(true);
    expect(offer.grantExpired).toBe(false);
  });

  it("detects expired grants without offering PIN unlock", async () => {
    window.localStorage.clear();
    window.localStorage.setItem(INSTALLATION_DEVICE_ID_STORAGE_KEY, INSTALLATION);
    const expired = await buildGrant(USER, "Maria Santos");
    expired.expiresAtUtc = "2020-01-01T12:00:00.000Z";
    persistServerSignedGrant(expired);
    await enrollOfflinePinAndDek(USER, PIN);
    expect(hasExpiredOfflineGrantOnInstallation()).toBe(true);
    const offer = await evaluateOfflinePinLoginOffer();
    expect(offer.canOfferPinUnlock).toBe(false);
    expect(offer.grantExpired).toBe(true);
  });
});
