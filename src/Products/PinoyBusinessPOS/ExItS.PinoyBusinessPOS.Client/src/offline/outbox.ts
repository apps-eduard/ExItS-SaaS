import type { OfflineDb } from "@/offline/db";
import { deriveScopeKeyFromBinding, encryptPayload, sha256Hex } from "@/offline/crypto";
import {
  deriveOfflineQueueCounts,
  type OfflineOperationRecord,
  type OfflineQueueState,
  type OfflineScopeKind,
  type SafeOfflineOperationMetadata,
} from "@/offline/types";

export type EnqueueSensitiveOperationInput = {
  db: OfflineDb;
  scopeKind: OfflineScopeKind;
  scopeBinding: string;
  userId: string;
  accountProfileId?: string | null;
  organizationId?: string | null;
  branchId?: string | null;
  installationDeviceId?: string | null;
  posDeviceId?: string | null;
  productDomain: string;
  operationType: string;
  operationId: string;
  idempotencyKey: string;
  payloadVersion?: number;
  plaintextJson: string;
  concurrencyToken?: string | null;
  dependsOnOperationId?: string | null;
  entityLocalId?: string | null;
  /** Optional second store write inside the same transaction (e.g. local domain row). */
  withLocalDomainWrite?: (tx: {
    putDomain: (storeName: never, value: never) => Promise<void>;
  }) => Promise<void>;
};

function associatedData(
  op: Pick<OfflineOperationRecord, "operationId" | "scopeKind" | "operationType">,
): string {
  return `${op.scopeKind}|${op.operationType}|${op.operationId}`;
}

/**
 * Encrypt + enqueue in one IndexedDB transaction.
 * Domain companion writes can be added in later packages via schema stores.
 */
export async function enqueueEncryptedOperation(
  input: EnqueueSensitiveOperationInput,
): Promise<OfflineOperationRecord> {
  const plaintext = new TextEncoder().encode(input.plaintextJson);
  const payloadHash = await sha256Hex(plaintext);
  const key = await deriveScopeKeyFromBinding(input.scopeBinding);
  const envelope = await encryptPayload(
    key,
    plaintext,
    associatedData({
      operationId: input.operationId,
      scopeKind: input.scopeKind,
      operationType: input.operationType,
    }),
  );

  const now = new Date().toISOString();
  const record: OfflineOperationRecord = {
    operationId: input.operationId,
    scopeKind: input.scopeKind,
    userId: input.userId,
    accountProfileId: input.accountProfileId ?? null,
    organizationId: input.organizationId ?? null,
    branchId: input.branchId ?? null,
    installationDeviceId: input.installationDeviceId ?? null,
    posDeviceId: input.posDeviceId ?? null,
    productDomain: input.productDomain,
    operationType: input.operationType,
    payloadVersion: input.payloadVersion ?? 1,
    payloadHash,
    idempotencyKey: input.idempotencyKey,
    createdAt: now,
    nextAttemptAt: now,
    attemptCount: 0,
    queueState: "Pending",
    lastAttemptAt: null,
    failureCode: null,
    failureSummary: null,
    serverReference: null,
    concurrencyToken: input.concurrencyToken ?? null,
    dependsOnOperationId: input.dependsOnOperationId ?? null,
    entityLocalId: input.entityLocalId ?? null,
    entityServerId: null,
    ciphertext: envelope.ciphertext,
    iv: envelope.iv,
  };

  const tx = input.db.transaction("outbox", "readwrite");
  await tx.store.put(record);
  await tx.done;
  return record;
}

export async function listOutbox(db: OfflineDb): Promise<OfflineOperationRecord[]> {
  return db.getAll("outbox");
}

export async function listSafeOutboxMetadata(
  db: OfflineDb,
): Promise<SafeOfflineOperationMetadata[]> {
  const rows = await listOutbox(db);
  return rows.map((row) => ({
    operationId: row.operationId,
    operationType: row.operationType,
    queueState: row.queueState,
    attemptCount: row.attemptCount,
    createdAt: row.createdAt,
    failureCode: row.failureCode,
    failureSummary: row.failureSummary,
    scopeKind: row.scopeKind,
    serverReference: row.serverReference,
  }));
}

export async function getOutboxCounts(db: OfflineDb) {
  return deriveOfflineQueueCounts(await listOutbox(db));
}

export async function setOperationState(
  db: OfflineDb,
  operationId: string,
  patch: Partial<
    Pick<
      OfflineOperationRecord,
      | "queueState"
      | "attemptCount"
      | "lastAttemptAt"
      | "nextAttemptAt"
      | "failureCode"
      | "failureSummary"
      | "serverReference"
      | "entityServerId"
    >
  >,
): Promise<OfflineOperationRecord | null> {
  const existing = await db.get("outbox", operationId);
  if (!existing) {
    return null;
  }
  const next = { ...existing, ...patch };
  await db.put("outbox", next);
  return next;
}

/** Recover abandoned Syncing rows after crash (return to Pending). */
export async function recoverAbandonedSyncing(db: OfflineDb): Promise<number> {
  const all = await listOutbox(db);
  let recovered = 0;
  for (const row of all) {
    if (row.queueState === "Syncing") {
      await setOperationState(db, row.operationId, {
        queueState: "Pending",
        failureSummary: "Recovered abandoned Syncing after restart",
      });
      recovered += 1;
    }
  }
  return recovered;
}

export async function claimNextPending(db: OfflineDb): Promise<OfflineOperationRecord | null> {
  const all = await listOutbox(db);
  const succeeded = new Set(
    all.filter((op) => op.queueState === "Succeeded").map((op) => op.operationId),
  );
  const candidates = all
    .filter((op) => op.queueState === "Pending" || op.queueState === "RetryableFailure")
    .sort((a, b) => a.createdAt.localeCompare(b.createdAt));

  for (const op of candidates) {
    if (op.dependsOnOperationId && !succeeded.has(op.dependsOnOperationId)) {
      continue;
    }
    return setOperationState(db, op.operationId, {
      queueState: "Syncing",
      lastAttemptAt: new Date().toISOString(),
      attemptCount: op.attemptCount + 1,
    });
  }
  return null;
}

export function assertPrincipalOwnsOperation(
  op: OfflineOperationRecord,
  principalUserId: string,
): boolean {
  return op.userId === principalUserId;
}

export const TERMINAL_OR_ACTIVE_STATES: OfflineQueueState[] = [
  "Pending",
  "Syncing",
  "RetryableFailure",
  "PermanentFailure",
  "Conflict",
  "BlockedByAccess",
  "Succeeded",
];
