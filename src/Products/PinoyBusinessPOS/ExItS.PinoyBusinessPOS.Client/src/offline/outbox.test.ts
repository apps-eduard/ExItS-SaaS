import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it } from "vitest";
import { decryptPayload, deriveScopeKeyFromBinding } from "@/offline/crypto";
import { openOfflineDatabase, organizationScopeKey, personalScopeKey } from "@/offline/db";
import {
  claimNextPending,
  enqueueEncryptedOperation,
  getOutboxCounts,
  listOutbox,
  listSafeOutboxMetadata,
  recoverAbandonedSyncing,
  setOperationState,
  type EnqueueSensitiveOperationInput,
} from "@/offline/outbox";
import { isFullySynced, waitingSyncCount } from "@/offline/types";

async function enqueueWithLegacyScopeKey(input: EnqueueSensitiveOperationInput) {
  return enqueueEncryptedOperation({
    ...input,
    cryptoKey: await deriveScopeKeyFromBinding(input.scopeBinding),
  });
}

describe("RMAP-21B offline LocalStore / outbox", () => {
  beforeEach(() => {
    // fresh IDB per test via unique scope keys
  });

  it("creates schema and isolates Personal vs Organization databases", async () => {
    const personalKey = personalScopeKey("user-a", "profile-a");
    const orgKey = organizationScopeKey({
      userId: "user-a",
      organizationId: "org-1",
      branchId: "branch-1",
      installationDeviceId: "install-1",
    });
    const personalDb = await openOfflineDatabase("Personal", personalKey);
    const orgDb = await openOfflineDatabase("Organization", orgKey);

    await enqueueWithLegacyScopeKey({
      db: personalDb,
      scopeKind: "Personal",
      scopeBinding: personalKey,
      userId: "user-a",
      accountProfileId: "profile-a",
      productDomain: "personal.utang",
      operationType: "personal.contact.upsert",
      operationId: "op-personal-1",
      idempotencyKey: "idem-personal-1",
      plaintextJson: JSON.stringify({ displayName: "Ana" }),
    });

    await enqueueWithLegacyScopeKey({
      db: orgDb,
      scopeKind: "Organization",
      scopeBinding: orgKey,
      userId: "user-a",
      organizationId: "org-1",
      branchId: "branch-1",
      installationDeviceId: "install-1",
      productDomain: "pos.sale",
      operationType: "sale.checkout.cash",
      operationId: "op-org-1",
      idempotencyKey: "idem-org-1",
      plaintextJson: JSON.stringify({ saleId: "sale-1" }),
    });

    expect((await listOutbox(personalDb)).map((o) => o.operationId)).toEqual(["op-personal-1"]);
    expect((await listOutbox(orgDb)).map((o) => o.operationId)).toEqual(["op-org-1"]);
    personalDb.close();
    orgDb.close();
  });

  it("encrypts payload so outbox row is not plaintext JSON", async () => {
    const scopeKey = personalScopeKey("user-b", "profile-b");
    const db = await openOfflineDatabase("Personal", scopeKey);
    const secret = "debt-note-should-not-appear-as-plaintext";
    await enqueueWithLegacyScopeKey({
      db,
      scopeKind: "Personal",
      scopeBinding: scopeKey,
      userId: "user-b",
      accountProfileId: "profile-b",
      productDomain: "personal.utang",
      operationType: "personal.entry.record",
      operationId: "op-enc-1",
      idempotencyKey: "idem-enc-1",
      plaintextJson: JSON.stringify({ note: secret, amount: 100 }),
    });
    const [row] = await listOutbox(db);
    const asText = new TextDecoder().decode(new Uint8Array(row.ciphertext));
    expect(asText).not.toContain(secret);
    expect(JSON.stringify(await listSafeOutboxMetadata(db))).not.toContain(secret);

    const key = await deriveScopeKeyFromBinding(scopeKey);
    const plain = await decryptPayload(
      key,
      { ciphertext: row.ciphertext, iv: row.iv },
      `Personal|personal.entry.record|op-enc-1`,
    );
    expect(new TextDecoder().decode(plain)).toContain(secret);
    db.close();
  });

  it("preserves the same idempotency key across retries", async () => {
    const scopeKey = personalScopeKey("user-c", "profile-c");
    const db = await openOfflineDatabase("Personal", scopeKey);
    await enqueueWithLegacyScopeKey({
      db,
      scopeKind: "Personal",
      scopeBinding: scopeKey,
      userId: "user-c",
      accountProfileId: "profile-c",
      productDomain: "personal.utang",
      operationType: "personal.contact.upsert",
      operationId: "op-idem-1",
      idempotencyKey: "fixed-idem-key",
      plaintextJson: JSON.stringify({ name: "Bob" }),
    });
    const claimed = await claimNextPending(db);
    expect(claimed?.idempotencyKey).toBe("fixed-idem-key");
    await setOperationState(db, "op-idem-1", { queueState: "RetryableFailure" });
    const claimedAgain = await claimNextPending(db);
    expect(claimedAgain?.idempotencyKey).toBe("fixed-idem-key");
    expect(claimedAgain?.operationId).toBe("op-idem-1");
    db.close();
  });

  it("orders dependents after predecessor succeeds", async () => {
    const scopeKey = personalScopeKey("user-d", "profile-d");
    const db = await openOfflineDatabase("Personal", scopeKey);
    await enqueueWithLegacyScopeKey({
      db,
      scopeKind: "Personal",
      scopeBinding: scopeKey,
      userId: "user-d",
      accountProfileId: "profile-d",
      productDomain: "personal.utang",
      operationType: "personal.relationship.create",
      operationId: "op-rel",
      idempotencyKey: "idem-rel",
      plaintextJson: JSON.stringify({ contactLocalId: "local-c1" }),
      dependsOnOperationId: "op-contact",
    });
    await enqueueWithLegacyScopeKey({
      db,
      scopeKind: "Personal",
      scopeBinding: scopeKey,
      userId: "user-d",
      accountProfileId: "profile-d",
      productDomain: "personal.utang",
      operationType: "personal.contact.upsert",
      operationId: "op-contact",
      idempotencyKey: "idem-contact",
      plaintextJson: JSON.stringify({ name: "Cara" }),
      entityLocalId: "local-c1",
    });

    const first = await claimNextPending(db);
    expect(first?.operationId).toBe("op-contact");
    await setOperationState(db, "op-contact", {
      queueState: "Succeeded",
      serverReference: "server-c1",
      entityServerId: "server-c1",
    });
    const second = await claimNextPending(db);
    expect(second?.operationId).toBe("op-rel");
    db.close();
  });

  it("recovers abandoned Syncing after restart", async () => {
    const scopeKey = personalScopeKey("user-e", "profile-e");
    const db = await openOfflineDatabase("Personal", scopeKey);
    await enqueueWithLegacyScopeKey({
      db,
      scopeKind: "Personal",
      scopeBinding: scopeKey,
      userId: "user-e",
      accountProfileId: "profile-e",
      productDomain: "personal.todo",
      operationType: "personal.todo.create",
      operationId: "op-syncing",
      idempotencyKey: "idem-syncing",
      plaintextJson: JSON.stringify({ title: "Buy rice" }),
    });
    await setOperationState(db, "op-syncing", { queueState: "Syncing" });
    expect(await recoverAbandonedSyncing(db)).toBe(1);
    const [row] = await listOutbox(db);
    expect(row.queueState).toBe("Pending");
    db.close();
  });

  it("derives waiting / fully-synced counts for Connection & Sync", async () => {
    const scopeKey = personalScopeKey("user-f", "profile-f");
    const db = await openOfflineDatabase("Personal", scopeKey);
    await enqueueWithLegacyScopeKey({
      db,
      scopeKind: "Personal",
      scopeBinding: scopeKey,
      userId: "user-f",
      accountProfileId: "profile-f",
      productDomain: "personal.todo",
      operationType: "personal.todo.create",
      operationId: "op-wait",
      idempotencyKey: "idem-wait",
      plaintextJson: JSON.stringify({ title: "Call" }),
    });
    const counts = await getOutboxCounts(db);
    expect(waitingSyncCount(counts)).toBe(1);
    expect(isFullySynced(counts)).toBe(false);
    await setOperationState(db, "op-wait", { queueState: "Succeeded" });
    expect(isFullySynced(await getOutboxCounts(db))).toBe(true);
    db.close();
  });
});
