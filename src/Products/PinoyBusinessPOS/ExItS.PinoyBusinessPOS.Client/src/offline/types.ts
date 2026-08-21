export const OFFLINE_SCHEMA_VERSION = 1 as const;

export type OfflineScopeKind = "Personal" | "Organization";

export type OfflineQueueState =
  | "Pending"
  | "Syncing"
  | "Succeeded"
  | "RetryableFailure"
  | "PermanentFailure"
  | "Conflict"
  | "BlockedByAccess";

export type OfflineFailureClass = "None" | "Transient" | "Permanent" | "Conflict" | "AccessBlocked";

export type OfflineOperationRecord = {
  operationId: string;
  scopeKind: OfflineScopeKind;
  userId: string;
  accountProfileId: string | null;
  organizationId: string | null;
  branchId: string | null;
  installationDeviceId: string | null;
  posDeviceId: string | null;
  productDomain: string;
  operationType: string;
  payloadVersion: number;
  payloadHash: string;
  idempotencyKey: string;
  createdAt: string;
  nextAttemptAt: string;
  attemptCount: number;
  queueState: OfflineQueueState;
  lastAttemptAt: string | null;
  failureCode: string | null;
  failureSummary: string | null;
  serverReference: string | null;
  concurrencyToken: string | null;
  dependsOnOperationId: string | null;
  entityLocalId: string | null;
  entityServerId: string | null;
  /** AES-GCM envelope — never log plaintext. */
  ciphertext: ArrayBuffer;
  iv: ArrayBuffer;
};

export type OfflineQueueCounts = {
  pending: number;
  syncing: number;
  succeeded: number;
  retryableFailure: number;
  permanentFailure: number;
  conflict: number;
  blockedByAccess: number;
};

export function deriveOfflineQueueCounts(
  operations: ReadonlyArray<Pick<OfflineOperationRecord, "queueState">>,
): OfflineQueueCounts {
  const counts: OfflineQueueCounts = {
    pending: 0,
    syncing: 0,
    succeeded: 0,
    retryableFailure: 0,
    permanentFailure: 0,
    conflict: 0,
    blockedByAccess: 0,
  };
  for (const op of operations) {
    switch (op.queueState) {
      case "Pending":
        counts.pending += 1;
        break;
      case "Syncing":
        counts.syncing += 1;
        break;
      case "Succeeded":
        counts.succeeded += 1;
        break;
      case "RetryableFailure":
        counts.retryableFailure += 1;
        break;
      case "PermanentFailure":
        counts.permanentFailure += 1;
        break;
      case "Conflict":
        counts.conflict += 1;
        break;
      case "BlockedByAccess":
        counts.blockedByAccess += 1;
        break;
    }
  }
  return counts;
}

/** Waiting = Pending + RetryableFailure (user-facing pending sync). */
export function waitingSyncCount(counts: OfflineQueueCounts): number {
  return counts.pending + counts.retryableFailure;
}

export function needsAttentionCount(counts: OfflineQueueCounts): number {
  return counts.permanentFailure + counts.conflict;
}

export function isFullySynced(counts: OfflineQueueCounts): boolean {
  return (
    counts.pending === 0 &&
    counts.syncing === 0 &&
    counts.retryableFailure === 0 &&
    counts.permanentFailure === 0 &&
    counts.conflict === 0 &&
    counts.blockedByAccess === 0
  );
}

/** Safe metadata only — never include ciphertext or decrypted payload. */
export type SafeOfflineOperationMetadata = {
  operationId: string;
  operationType: string;
  queueState: OfflineQueueState;
  attemptCount: number;
  createdAt: string;
  failureCode: string | null;
  failureSummary: string | null;
  scopeKind: OfflineScopeKind;
  serverReference: string | null;
};
