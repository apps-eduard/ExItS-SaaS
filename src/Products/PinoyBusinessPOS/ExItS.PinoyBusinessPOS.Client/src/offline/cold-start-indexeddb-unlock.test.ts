import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it } from "vitest";
import { openOfflineDatabase, organizationScopeKey } from "@/offline/db";
import {
  enqueueEncryptedOperation,
  listSafeOutboxMetadata,
} from "@/offline/outbox";
import {
  buildBoundWorkspaceFromGrant,
  buildPosDeviceFromGrant,
  establishOfflineOperatingGrant,
  evaluateColdStartOfflineGrant,
  synthesizeSessionFromGrant,
} from "@/offline/offline-operating-grant";
import { INSTALLATION_DEVICE_ID_STORAGE_KEY } from "@/workspace/browser-installation-identity";

const USER = "248935e9-e462-425f-88f5-a9255bf12748";
const ORG = "ca023f5b-925e-4aa5-a843-d48c4c06fa14";
const BRANCH = "742fb3f3-14f9-4bee-a94e-f5acccc7cbc5";
const POS_DEVICE = "11111111-1111-1111-1111-111111111111";
const INSTALLATION = "22222222-2222-4222-8222-222222222222";

describe("cold-start IndexedDB unlock", () => {
  beforeEach(() => {
    window.localStorage.clear();
    window.localStorage.setItem(INSTALLATION_DEVICE_ID_STORAGE_KEY, INSTALLATION);
  });

  it("opens encrypted outbox after cold-start grant unlock", async () => {
    await establishOfflineOperatingGrant({
      userId: USER,
      scopeKind: "Organization",
      organizationId: ORG,
      organizationDisplayName: "Kizy Store",
      branchId: BRANCH,
      branchName: "Main Branch",
      installationDeviceId: INSTALLATION,
      posDeviceId: POS_DEVICE,
    });

    const warmScope = organizationScopeKey({
      userId: USER,
      organizationId: ORG,
      branchId: BRANCH,
      installationDeviceId: INSTALLATION,
    });
    const warmDb = await openOfflineDatabase("Organization", warmScope);
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
    });
    warmDb.close();

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
