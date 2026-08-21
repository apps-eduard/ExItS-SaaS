import type { PosCatalogProductDto, PosProductCategoryDto } from "@/api/pos/pos-catalog-types";

/** v5 adds the private-by-default Personal To-do store (RMAP-21G). */
export const OFFLINE_SCHEMA_VERSION = 5 as const;

/** Written to `meta` on open so a Personal write can refuse an Organization database. */
export const OFFLINE_SCOPE_KIND_META_KEY = "scopeKind" as const;

/** Personal identity id, cached while online so an offline relationship can name its owner. */
export const PERSONAL_USER_IDENTITY_META_KEY = "personalUserIdentityId" as const;

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

/**
 * Read-only Sell reference data. Catalog rows are not money, tender, or PHI, so they are
 * cached as plaintext; anything that posts money still goes through the encrypted outbox.
 */
export type CachedCatalogProductRecord = {
  productId: string;
  cachedAtUtc: string;
  product: PosCatalogProductDto;
};

export type CachedCatalogCategoryRecord = {
  categoryId: string;
  cachedAtUtc: string;
  category: PosProductCategoryDto;
};

/**
 * Business customer projection (RMAP-21E).
 * Display name, mobile, address, notes and ExItS link ids are personal data, so the body is
 * AES-GCM encrypted like an outbox payload. Only routing and lifecycle columns stay readable,
 * mirroring the MAUI LocalEncryptedCustomerCreditStore column split.
 */
export type CachedCustomerRecord = {
  customerId: string;
  organizationId: string;
  status: string;
  updatedAtUtc: string;
  cachedAtUtc: string;
  /** Encrypted customer projection — never log plaintext. */
  ciphertext: ArrayBuffer;
  iv: ArrayBuffer;
};

/** Outstanding balance is money, so the credit summary is never cached as plaintext. */
export type CachedCustomerCreditRecord = {
  customerId: string;
  cachedAtUtc: string;
  ciphertext: ArrayBuffer;
  iv: ArrayBuffer;
};

/**
 * Personal Utang projections (RMAP-21F).
 * Every readable column here is either a local routing id or a lifecycle flag. Contact names,
 * phone numbers, amounts and notes live inside the AES-GCM body, so a stolen device profile
 * cannot be read as "who owes this person money".
 */
export type CachedPersonalContactRecord = {
  /** Local id. Equals the server id once the contact has synced. */
  contactId: string;
  serverId: string | null;
  origin: "Server" | "Local";
  updatedAtUtc: string;
  cachedAtUtc: string;
  ciphertext: ArrayBuffer;
  iv: ArrayBuffer;
};

export type CachedPersonalRelationshipRecord = {
  relationshipId: string;
  serverId: string | null;
  /** "Lent" = the signed-in person is the creditor, "Borrowed" = the debtor. */
  perspective: "Lent" | "Borrowed";
  origin: "Server" | "Local";
  /** Server row version, used only to detect that the cached balance is stale. */
  version: number | null;
  updatedAtUtc: string;
  cachedAtUtc: string;
  ciphertext: ArrayBuffer;
  iv: ArrayBuffer;
};

export type CachedPersonalEntryRecord = {
  entryId: string;
  relationshipId: string;
  serverId: string | null;
  origin: "Server" | "Local";
  occurredAtUtc: string;
  cachedAtUtc: string;
  ciphertext: ArrayBuffer;
  iv: ArrayBuffer;
};

/**
 * Personal To-do (RMAP-21G).
 *
 * A To-do is private by default: the title, notes, due and reminder times, priority and every
 * related-entity pointer live inside the AES-GCM body. Only the local id, the lifecycle status, the
 * row version and the sync bookkeeping are readable, which is what the agenda tabs and the outbox
 * need in order to work without decrypting anything.
 */
export type CachedPersonalTodoRecord = {
  todoId: string;
  serverId: string | null;
  origin: "Server" | "Local";
  status: string;
  /** Server row version, or null for a To-do that has only ever existed on this device. */
  version: number | null;
  /** True while a local edit or transition is still queued. */
  pendingLocalChange: boolean;
  updatedAtUtc: string;
  cachedAtUtc: string;
  ciphertext: ArrayBuffer;
  iv: ArrayBuffer;
};

export const SELL_READINESS_SNAPSHOT_KEY = "sell-readiness" as const;

/** Last-good device/shift readiness, so a warm session can keep selling Cash offline. */
export type SellReadinessSnapshotRecord = {
  key: string;
  deviceReady: boolean;
  moneyPostReady: boolean;
  shiftId: string | null;
  openShiftNumber: string | null;
  capturedAt: string;
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
