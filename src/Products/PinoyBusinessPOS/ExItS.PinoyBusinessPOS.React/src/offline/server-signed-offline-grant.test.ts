import { describe, expect, it } from "vitest";
import {
  canonicalizeOfflineOperatingGrant,
  scopeKindToNumeric,
  signOfflineOperatingGrantForTests,
  verifyOfflineOperatingGrantSignature,
} from "@/offline/server-signed-offline-grant";

const DEV_PRIVATE_KEY_PEM = `-----BEGIN PRIVATE KEY-----
MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgJuN+Pa6hk6BZUISu
lodghNrUkSR+VQsjrIW49hJ21dihRANCAASSV3pYY5NEuiiPYCs/ZRXZL6dNW0DJ
8VhI3X4k2jMfgEoBV/n9zUzAIZMsJ6XfzAHR+cz3/VxgoQYquH3GV0Lt
-----END PRIVATE KEY-----`;

const USER = "248935e9-e462-425f-88f5-a9255bf12748";
const ORG = "ca023f5b-925e-4aa5-a843-d48c4c06fa14";
const BRANCH = "742fb3f3-14f9-4bee-a94e-f5acccc7cbc5";
const POS_DEVICE = "11111111-1111-1111-1111-111111111111";
const INSTALLATION = "22222222-2222-4222-8222-222222222222";
const GRANT_ID = "33333333-3333-4333-8333-333333333333";

function baseFields() {
  return {
    grantId: GRANT_ID,
    schemaVersion: 4,
    userId: USER,
    scopeKind: scopeKindToNumeric("Organization"),
    organizationId: ORG,
    organizationDisplayName: "Kizy Store",
    branchId: BRANCH,
    branchName: "Main Branch",
    installationDeviceId: INSTALLATION,
    posDeviceId: POS_DEVICE,
    roleCode: "Cashier",
    displayName: "Kizy Uy",
    username: "kizy",
    issuedAtUtc: "2026-01-01T12:00:00.000Z",
    lastOnlineValidatedAtUtc: "2026-01-01T12:00:00.000Z",
    expiresAtUtc: "2026-02-01T12:00:00.000Z",
  };
}

describe("server-signed offline grant", () => {
  it("verifies a valid server signature", async () => {
    const canonical = canonicalizeOfflineOperatingGrant(baseFields());
    const signature = await signOfflineOperatingGrantForTests(canonical, DEV_PRIVATE_KEY_PEM);
    expect(await verifyOfflineOperatingGrantSignature(canonical, signature)).toBe(true);
  });

  it("rejects tampered role", async () => {
    const canonical = canonicalizeOfflineOperatingGrant(baseFields());
    const signature = await signOfflineOperatingGrantForTests(canonical, DEV_PRIVATE_KEY_PEM);
    const tampered = canonicalizeOfflineOperatingGrant({ ...baseFields(), roleCode: "Owner" });
    expect(await verifyOfflineOperatingGrantSignature(tampered, signature)).toBe(false);
  });

  it("rejects tampered organization", async () => {
    const canonical = canonicalizeOfflineOperatingGrant(baseFields());
    const signature = await signOfflineOperatingGrantForTests(canonical, DEV_PRIVATE_KEY_PEM);
    const tampered = canonicalizeOfflineOperatingGrant({
      ...baseFields(),
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    });
    expect(await verifyOfflineOperatingGrantSignature(tampered, signature)).toBe(false);
  });

  it("rejects tampered branch", async () => {
    const canonical = canonicalizeOfflineOperatingGrant(baseFields());
    const signature = await signOfflineOperatingGrantForTests(canonical, DEV_PRIVATE_KEY_PEM);
    const tampered = canonicalizeOfflineOperatingGrant({
      ...baseFields(),
      branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    });
    expect(await verifyOfflineOperatingGrantSignature(tampered, signature)).toBe(false);
  });

  it("rejects tampered device", async () => {
    const canonical = canonicalizeOfflineOperatingGrant(baseFields());
    const signature = await signOfflineOperatingGrantForTests(canonical, DEV_PRIVATE_KEY_PEM);
    const tampered = canonicalizeOfflineOperatingGrant({
      ...baseFields(),
      installationDeviceId: "44444444-4444-4444-8444-444444444444",
    });
    expect(await verifyOfflineOperatingGrantSignature(tampered, signature)).toBe(false);
  });

  it("rejects tampered expiry", async () => {
    const canonical = canonicalizeOfflineOperatingGrant(baseFields());
    const signature = await signOfflineOperatingGrantForTests(canonical, DEV_PRIVATE_KEY_PEM);
    const tampered = canonicalizeOfflineOperatingGrant({
      ...baseFields(),
      expiresAtUtc: "2027-01-01T12:00:00.000Z",
    });
    expect(await verifyOfflineOperatingGrantSignature(tampered, signature)).toBe(false);
  });
});
