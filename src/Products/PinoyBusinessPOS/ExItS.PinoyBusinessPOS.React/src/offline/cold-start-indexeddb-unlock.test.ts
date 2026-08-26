import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it } from "vitest";
import { openOfflineDatabase, organizationScopeKey } from "@/offline/db";
import { enrollOfflinePinAndDek } from "@/offline/local-store-key";
import { getUnlockedDek, clearUnlockedDek } from "@/offline/offline-unlock-session";
import {
  enqueueEncryptedOperation,
  listSafeOutboxMetadata,
} from "@/offline/outbox";
import {
  buildBoundWorkspaceFromGrant,
  buildPosDeviceFromGrant,
  evaluateColdStartOfflineGrant,
  persistServerSignedGrant,
  synthesizeSessionFromGrant,
  type StoredOfflineOperatingGrant,
} from "@/offline/offline-operating-grant";
import {
  canonicalizeOfflineOperatingGrant,
  scopeKindToNumeric,
  signOfflineOperatingGrantForTests,
} from "@/offline/server-signed-offline-grant";
import { clearAllOfflinePinVerifiers } from "@/offline/offline-pin";
import { clearAllWrappedDekRecords } from "@/offline/local-store-key";
import { INSTALLATION_DEVICE_ID_STORAGE_KEY } from "@/workspace/browser-installation-identity";

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
const PIN = "123456";

async function buildSignedGrant(): Promise<StoredOfflineOperatingGrant> {
  const grantId = "33333333-3333-4333-8333-333333333333";
  const grant: StoredOfflineOperatingGrant = {
    grantId,
    schemaVersion: 4,
    userId: USER,
    scopeKind: "Organization",
    organizationId: ORG,
    organizationDisplayName: "Kizy Store",
    branchId: BRANCH,
    branchName: "Main Branch",
    installationDeviceId: INSTALLATION,
    posDeviceId: POS_DEVICE,
    roleCode: "Owner",
    displayName: "Kizy Uy",
    username: "kizy",
    issuedAtUtc: "2026-08-01T12:00:00.000Z",
    lastOnlineValidatedAtUtc: "2026-08-01T12:00:00.000Z",
    expiresAtUtc: "2030-08-01T12:00:00.000Z",
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

describe("cold-start IndexedDB unlock", () => {
  beforeEach(async () => {
    window.localStorage.clear();
    window.localStorage.setItem(INSTALLATION_DEVICE_ID_STORAGE_KEY, INSTALLATION);
    clearUnlockedDek();
    clearAllOfflinePinVerifiers();
    clearAllWrappedDekRecords();
    await enrollOfflinePinAndDek(USER, PIN);
  });

  it("opens encrypted outbox after cold-start grant + PIN unlock", async () => {
    persistServerSignedGrant(await buildSignedGrant());

    const warmScope = organizationScopeKey({
      userId: USER,
      organizationId: ORG,
      branchId: BRANCH,
      installationDeviceId: INSTALLATION,
    });
    const warmDb = await openOfflineDatabase("Organization", warmScope);
    const dek = getUnlockedDek(USER)!;
    await enqueueEncryptedOperation({
      db: warmDb,
      scopeKind: "Organization",
      scopeBinding: warmScope,
      userId: USER,
      organizationId: ORG,
      branchId: BRANCH,
      installationDeviceId: INSTALLATION,
      posDeviceId: POS_DEVICE,
      productDomain: "pos.sale",
      operationType: "sale.checkout.cash",
      operationId: "op-1",
      idempotencyKey: "sale-1",
      plaintextJson: JSON.stringify({ saleId: "sale-1", total: 25 }),
      cryptoKey: dek,
    });
    warmDb.close();

    clearUnlockedDek();

    const cold = await evaluateColdStartOfflineGrant();
    expect(cold.ok).toBe(true);
    if (!cold.ok) {
      return;
    }

    const session = synthesizeSessionFromGrant(cold.grant);
    expect(session.userId).toBe(USER);

    const bound = buildBoundWorkspaceFromGrant(cold.grant);
    const device = buildPosDeviceFromGrant(cold.grant);
    expect(bound?.branchId).toBe(BRANCH);
    expect(device?.status).toBe("authorized");

    await enrollOfflinePinAndDek(USER, PIN);
    const coldScope = organizationScopeKey({
      userId: USER,
      organizationId: ORG,
      branchId: BRANCH,
      installationDeviceId: INSTALLATION,
    });
    const coldDb = await openOfflineDatabase("Organization", coldScope);
    const pending = await listSafeOutboxMetadata(coldDb);
    expect(pending.some((row) => row.operationId === "op-1")).toBe(true);
    coldDb.close();
  });
});
