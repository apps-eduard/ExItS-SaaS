import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";
import { PosApiError, posRequest } from "@/api/pos/pos-http";
import { PlatformApiError, platformRequest } from "@/api/platform/platform-http";
import { decryptPayload, deriveScopeKeyFromBinding } from "@/offline/crypto";
import type { OfflineDb } from "@/offline/db";
import { claimNextPending, recoverAbandonedSyncing, setOperationState } from "@/offline/outbox";
import {
  collectLocalRefs,
  parseQueuedRequest,
  resolveLocalRefs,
  type QueuedRequestEnvelope,
} from "@/offline/queued-request";
import { mayAutoRetry, type AttemptFailureKind } from "@/offline/server-dedupe-policy";
import type { OfflineOperationRecord, OfflineQueueState } from "@/offline/types";

const SALES_PATH = "/api/v1/pos/sales";

function associatedData(
  op: Pick<OfflineOperationRecord, "operationId" | "scopeKind" | "operationType">,
): string {
  return `${op.scopeKind}|${op.operationType}|${op.operationId}`;
}

async function decryptOperationPlaintext(
  op: OfflineOperationRecord,
  scopeBinding: string,
): Promise<string> {
  const key = await deriveScopeKeyFromBinding(scopeBinding);
  const bytes = await decryptPayload(
    key,
    { ciphertext: op.ciphertext, iv: op.iv },
    associatedData(op),
  );
  return new TextDecoder().decode(bytes);
}

async function lookupEntityServerId(db: OfflineDb, localId: string): Promise<string | null> {
  const row = await db.get("entityMap", localId);
  return row?.serverId ?? null;
}

async function rememberEntityMapping(
  db: OfflineDb,
  localId: string,
  serverId: string,
  entityType: string,
): Promise<void> {
  await db.put("entityMap", {
    mapKey: localId,
    localId,
    serverId,
    entityType,
  });
}

function extractServerEntityId(responseBody: unknown, fallback: string): string {
  if (typeof responseBody === "object" && responseBody !== null) {
    const record = responseBody as Record<string, unknown>;
    for (const key of ["id", "saleId", "customerId", "todoId", "contactId", "relationshipId"]) {
      const value = record[key];
      if (typeof value === "string" && value.trim()) {
        return value;
      }
    }
  }
  return fallback;
}

function classifyHttpStatus(status: number): {
  queueState: OfflineQueueState;
  failureCode: string;
  failureSummary: string;
} {
  if (status === 401 || status === 403) {
    return {
      queueState: "BlockedByAccess",
      failureCode: `http.${status}`,
      failureSummary: "Access required to finish syncing",
    };
  }
  if (status === 409) {
    return {
      queueState: "Conflict",
      failureCode: `http.${status}`,
      failureSummary: "Server reported a conflict",
    };
  }
  if (status >= 500) {
    return {
      queueState: "RetryableFailure",
      failureCode: `http.${status}`,
      failureSummary: "Temporary server error",
    };
  }
  return {
    queueState: "PermanentFailure",
    failureCode: `http.${status}`,
    failureSummary: "Server rejected this change",
  };
}

function nextStateForTransportFailure(
  operationType: string,
  failure: AttemptFailureKind,
): OfflineQueueState {
  if (mayAutoRetry(operationType, failure)) {
    return "RetryableFailure";
  }
  // Ambiguous transport + no server dedupe → stop and ask the person.
  return "PermanentFailure";
}

async function toQueuedEnvelope(
  op: OfflineOperationRecord,
  plaintext: string,
): Promise<QueuedRequestEnvelope | null> {
  const parsed = parseQueuedRequest(plaintext);
  if (parsed) {
    return parsed;
  }

  // RMAP-21D Cash sale stored the checkout body only (payloadVersion 1).
  if (op.operationType === OFFLINE_OPERATION_TYPES.SaleCheckout) {
    try {
      const body = JSON.parse(plaintext) as unknown;
      return {
        api: "pos",
        method: "POST",
        path: SALES_PATH,
        body,
      };
    } catch {
      return null;
    }
  }

  return null;
}

async function dispatchEnvelope(
  op: OfflineOperationRecord,
  envelope: QueuedRequestEnvelope,
): Promise<unknown> {
  const payloadJson = envelope.body === undefined ? "{}" : JSON.stringify(envelope.body);
  const headers = await buildPosMutationIdempotencyHeaders(
    op.entityLocalId ?? op.operationId,
    payloadJson,
    op.operationType,
  );

  if (envelope.api === "pos") {
    if (!op.organizationId) {
      throw new Error("Organization-scoped POS replay requires organizationId.");
    }
    return posRequest<unknown>({
      method: envelope.method,
      path: envelope.path,
      body: envelope.body,
      workspace: {
        organizationId: op.organizationId,
        branchId: op.branchId,
      },
      headers,
    });
  }

  // Platform Personal mutations use cookie + antiforgery; idempotency headers are harmless extras.
  return platformRequest<unknown>({
    method: envelope.method,
    path: envelope.path,
    body: envelope.body,
  });
}

export type ProcessOneResult =
  | { status: "idle" }
  | { status: "succeeded"; operationId: string }
  | { status: "failed"; operationId: string; queueState: OfflineQueueState };

/**
 * Claim one pending operation, decrypt, resolve local refs, replay, and update queue state.
 * Never logs or returns decrypted plaintext.
 */
export async function processNextOutboxOperation(
  db: OfflineDb,
  scopeBinding: string,
): Promise<ProcessOneResult> {
  const claimed = await claimNextPending(db);
  if (!claimed) {
    return { status: "idle" };
  }

  let plaintext: string;
  try {
    plaintext = await decryptOperationPlaintext(claimed, scopeBinding);
  } catch {
    await setOperationState(db, claimed.operationId, {
      queueState: "PermanentFailure",
      failureCode: "offline.decrypt_failed",
      failureSummary: "Could not read this queued change",
    });
    return {
      status: "failed",
      operationId: claimed.operationId,
      queueState: "PermanentFailure",
    };
  }

  const envelope = await toQueuedEnvelope(claimed, plaintext);
  if (!envelope) {
    await setOperationState(db, claimed.operationId, {
      queueState: "PermanentFailure",
      failureCode: "offline.payload_untrusted",
      failureSummary: "Queued change could not be trusted for replay",
    });
    return {
      status: "failed",
      operationId: claimed.operationId,
      queueState: "PermanentFailure",
    };
  }

  const refsNeeded = collectLocalRefs(envelope);
  const lookup = new Map<string, string | null>();
  for (const localId of refsNeeded) {
    lookup.set(localId, await lookupEntityServerId(db, localId));
  }
  const resolvedFinal = resolveLocalRefs(envelope, (localId) => lookup.get(localId) ?? null);
  if (!resolvedFinal.resolved) {
    // Dependency not mapped yet — return to Pending so a later pass can retry after predecessor Succeeded.
    await setOperationState(db, claimed.operationId, {
      queueState: "Pending",
      failureCode: "offline.local_ref_unresolved",
      failureSummary: "Waiting for a related change to finish syncing",
    });
    return { status: "failed", operationId: claimed.operationId, queueState: "Pending" };
  }

  try {
    const responseBody = await dispatchEnvelope(claimed, resolvedFinal.envelope);
    const serverId = extractServerEntityId(
      responseBody,
      claimed.entityLocalId ?? claimed.operationId,
    );
    if (claimed.entityLocalId) {
      await rememberEntityMapping(db, claimed.entityLocalId, serverId, claimed.operationType);
    }
    await setOperationState(db, claimed.operationId, {
      queueState: "Succeeded",
      failureCode: null,
      failureSummary: null,
      serverReference: serverId,
      entityServerId: serverId,
    });
    return { status: "succeeded", operationId: claimed.operationId };
  } catch (error) {
    if (error instanceof PosApiError || error instanceof PlatformApiError) {
      const classified = classifyHttpStatus(error.status);
      await setOperationState(db, claimed.operationId, {
        queueState: classified.queueState,
        failureCode: classified.failureCode,
        failureSummary: classified.failureSummary,
      });
      return {
        status: "failed",
        operationId: claimed.operationId,
        queueState: classified.queueState,
      };
    }

    const message = error instanceof Error ? error.message : "Unknown sync failure";
    const kind: AttemptFailureKind =
      typeof navigator !== "undefined" && navigator.onLine === false
        ? "not-dispatched"
        : /Failed to fetch|NetworkError|network/i.test(message)
          ? "ambiguous-transport"
          : "not-dispatched";

    const queueState = nextStateForTransportFailure(claimed.operationType, kind);
    await setOperationState(db, claimed.operationId, {
      queueState,
      failureCode:
        kind === "ambiguous-transport" ? "offline.ambiguous_transport" : "offline.not_dispatched",
      failureSummary:
        queueState === "PermanentFailure"
          ? "Could not confirm this change. Review before retrying."
          : "Will retry when connection is available",
    });
    return { status: "failed", operationId: claimed.operationId, queueState };
  }
}

export type DrainResult = {
  processed: number;
  succeeded: number;
  failed: number;
};

/** Process up to `limit` operations (or until idle). Recovers abandoned Syncing first. */
export async function drainOutbox(
  db: OfflineDb,
  scopeBinding: string,
  limit = 25,
): Promise<DrainResult> {
  await recoverAbandonedSyncing(db);
  let processed = 0;
  let succeeded = 0;
  let failed = 0;

  while (processed < limit) {
    const result = await processNextOutboxOperation(db, scopeBinding);
    if (result.status === "idle") {
      break;
    }
    processed += 1;
    if (result.status === "succeeded") {
      succeeded += 1;
    } else {
      failed += 1;
      // Avoid tight loop if dependency keeps returning Pending without progress.
      if (result.queueState === "Pending") {
        break;
      }
    }
  }

  return { processed, succeeded, failed };
}
