import type { OfflineDb } from "@/offline/db";
import { getMeta } from "@/offline/db";
import { decryptPayload, deriveScopeKeyFromBinding, encryptPayload } from "@/offline/crypto";
import { getActiveOfflineCryptoKey } from "@/offline/local-store-key";
import type { OfflineOperationRecord } from "@/offline/types";

export const FIX02_MIGRATION_META_KEY = "fix02MigrationComplete";

export type Fix02MigrationFailureReason =
  | "partial_decrypt_failure"
  | "dek_unavailable"
  | "commit_failure";

export type Fix02MigrationResult =
  | { ok: true }
  | { ok: false; reason: Fix02MigrationFailureReason; failedRows?: number };

/** @internal Vitest-only hook to simulate IndexedDB commit abort mid-transaction. */
export type Fix02MigrationTestOptions = {
  testAbortCommitOnStore?: StagedWrite["storeName"];
};

const FIX02_MIGRATION_TRANSACTION_STORES = [
  "outbox",
  "customers",
  "customerCredit",
  "personalTodos",
  "personalContacts",
  "personalRelationships",
  "personalEntries",
  "meta",
] as const;

async function commitStagedMigrationWrites(
  db: OfflineDb,
  staged: StagedWrite[],
  options?: { abortOnStore?: StagedWrite["storeName"] },
): Promise<void> {
  const tx = db.transaction([...FIX02_MIGRATION_TRANSACTION_STORES], "readwrite");

  for (const item of staged) {
    if (options?.abortOnStore === item.storeName) {
      tx.abort();
      try {
        await tx.done;
      } catch {
        // Expected when the migration commit is aborted.
      }
      throw new Error("FIX02 migration commit aborted");
    }
    await tx.objectStore(item.storeName).put(item.row as never);
  }

  await tx.objectStore("meta").put({
    key: FIX02_MIGRATION_META_KEY,
    value: "1",
  });

  await tx.done;
}

type EncryptedRow = {
  ciphertext: ArrayBuffer;
  iv: ArrayBuffer;
};

type StagedWrite = {
  storeName:
    | "outbox"
    | "customers"
    | "customerCredit"
    | "personalTodos"
    | "personalContacts"
    | "personalRelationships"
    | "personalEntries";
  row: Record<string, unknown> & { ciphertext: ArrayBuffer; iv: ArrayBuffer };
};

async function reencryptRowOrFail(
  legacyKey: CryptoKey,
  dek: CryptoKey,
  row: EncryptedRow,
  associatedData: string,
): Promise<EncryptedRow> {
  const plaintext = await decryptPayload(legacyKey, row, associatedData);
  return encryptPayload(dek, plaintext, associatedData);
}

function outboxAssociatedData(
  op: Pick<OfflineOperationRecord, "operationId" | "scopeKind" | "operationType">,
): string {
  return `${op.scopeKind}|${op.operationType}|${op.operationId}`;
}

/**
 * All-or-nothing FIX01→FIX02 re-encryption. Never marks migration complete unless every
 * encrypted row decrypts and re-encrypts successfully.
 */
export async function migrateLegacyLocalStoreToFix02(
  db: OfflineDb,
  scopeBinding: string,
  userId: string,
  options?: Fix02MigrationTestOptions,
): Promise<Fix02MigrationResult> {
  const existing = await getMeta(db, FIX02_MIGRATION_META_KEY);
  if (existing === "1") {
    return { ok: true };
  }

  let dek: CryptoKey;
  try {
    dek = await getActiveOfflineCryptoKey(userId);
  } catch {
    return { ok: false, reason: "dek_unavailable" };
  }

  const legacyKey = await deriveScopeKeyFromBinding(scopeBinding);
  const staged: StagedWrite[] = [];
  let failedRows = 0;

  const stageRow = async (
    storeName: StagedWrite["storeName"],
    row: Record<string, unknown> & { ciphertext: ArrayBuffer; iv: ArrayBuffer },
    associatedData: string,
  ): Promise<boolean> => {
    try {
      const next = await reencryptRowOrFail(
        legacyKey,
        dek,
        { ciphertext: row.ciphertext, iv: row.iv },
        associatedData,
      );
      staged.push({
        storeName,
        row: { ...row, ciphertext: next.ciphertext, iv: next.iv },
      });
      return true;
    } catch {
      failedRows += 1;
      return false;
    }
  };

  for (const op of await db.getAll("outbox")) {
    if (!(await stageRow("outbox", op, outboxAssociatedData(op)))) {
      return { ok: false, reason: "partial_decrypt_failure", failedRows };
    }
  }

  for (const row of await db.getAll("customers")) {
    if (!(await stageRow("customers", row, `customer-cache|${row.customerId}`))) {
      return { ok: false, reason: "partial_decrypt_failure", failedRows };
    }
  }

  for (const row of await db.getAll("customerCredit")) {
    if (!(await stageRow("customerCredit", row, `customer-credit-cache|${row.customerId}`))) {
      return { ok: false, reason: "partial_decrypt_failure", failedRows };
    }
  }

  for (const row of await db.getAll("personalTodos")) {
    if (!(await stageRow("personalTodos", row, `personal-todo-cache|${row.todoId}`))) {
      return { ok: false, reason: "partial_decrypt_failure", failedRows };
    }
  }

  for (const row of await db.getAll("personalContacts")) {
    if (
      !(await stageRow("personalContacts", row, `personal-contact-cache|${row.contactId}`))
    ) {
      return { ok: false, reason: "partial_decrypt_failure", failedRows };
    }
  }

  for (const row of await db.getAll("personalRelationships")) {
    if (
      !(await stageRow(
        "personalRelationships",
        row,
        `personal-relationship-cache|${row.relationshipId}`,
      ))
    ) {
      return { ok: false, reason: "partial_decrypt_failure", failedRows };
    }
  }

  for (const row of await db.getAll("personalEntries")) {
    if (!(await stageRow("personalEntries", row, `personal-entry-cache|${row.entryId}`))) {
      return { ok: false, reason: "partial_decrypt_failure", failedRows };
    }
  }

  try {
    await commitStagedMigrationWrites(
      db,
      staged,
      options?.testAbortCommitOnStore
        ? { abortOnStore: options.testAbortCommitOnStore }
        : undefined,
    );
  } catch {
    return { ok: false, reason: "commit_failure" };
  }

  return { ok: true };
}

export async function isFix02MigrationComplete(db: OfflineDb): Promise<boolean> {
  return (await getMeta(db, FIX02_MIGRATION_META_KEY)) === "1";
}

export async function maybeMigrateLegacyLocalStoreWhenReady(
  db: OfflineDb,
  scopeBinding: string,
  userId: string,
  options: { online: boolean; trustedSession: boolean },
): Promise<Fix02MigrationResult | { ok: false; reason: "not_ready" }> {
  if (!options.online || !options.trustedSession) {
    return { ok: false, reason: "not_ready" };
  }
  if (await isFix02MigrationComplete(db)) {
    return { ok: true };
  }
  return migrateLegacyLocalStoreToFix02(db, scopeBinding, userId);
}
