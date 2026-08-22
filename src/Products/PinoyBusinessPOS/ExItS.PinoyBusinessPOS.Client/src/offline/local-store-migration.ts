import type { OfflineDb } from "@/offline/db";
import { getMeta, putMeta } from "@/offline/db";
import { decryptPayload, deriveScopeKeyFromBinding, encryptPayload } from "@/offline/crypto";
import { getActiveOfflineCryptoKey } from "@/offline/local-store-key";
import type { OfflineOperationRecord } from "@/offline/types";

export const FIX02_MIGRATION_META_KEY = "fix02MigrationComplete";

type EncryptedRow = {
  ciphertext: ArrayBuffer;
  iv: ArrayBuffer;
};

async function reencryptRow(
  legacyKey: CryptoKey,
  dek: CryptoKey,
  row: EncryptedRow,
  associatedData: string,
): Promise<EncryptedRow | null> {
  try {
    const plaintext = await decryptPayload(legacyKey, row, associatedData);
    return encryptPayload(dek, plaintext, associatedData);
  } catch {
    return null;
  }
}

function outboxAssociatedData(
  op: Pick<OfflineOperationRecord, "operationId" | "scopeKind" | "operationType">,
): string {
  return `${op.scopeKind}|${op.operationType}|${op.operationId}`;
}

/**
 * When online with a trusted session, re-encrypt legacy scope-derived records with the random DEK.
 * Preserves queued sales and other outbox rows.
 */
export async function migrateLegacyLocalStoreToFix02(
  db: OfflineDb,
  scopeBinding: string,
  userId: string,
): Promise<boolean> {
  const existing = await getMeta(db, FIX02_MIGRATION_META_KEY);
  if (existing === "1") {
    return true;
  }

  const dek = await getActiveOfflineCryptoKey(userId);
  const legacyKey = await deriveScopeKeyFromBinding(scopeBinding);

  const outbox = await db.getAll("outbox");
  for (const op of outbox) {
    const next = await reencryptRow(
      legacyKey,
      dek,
      { ciphertext: op.ciphertext, iv: op.iv },
      outboxAssociatedData(op),
    );
    if (next) {
      await db.put("outbox", { ...op, ciphertext: next.ciphertext, iv: next.iv });
    }
  }

  const customerAad = (customerId: string) => `customer-cache|${customerId}`;
  const creditAad = (customerId: string) => `customer-credit-cache|${customerId}`;

  for (const row of await db.getAll("customers")) {
    const next = await reencryptRow(
      legacyKey,
      dek,
      { ciphertext: row.ciphertext, iv: row.iv },
      customerAad(row.customerId),
    );
    if (next) {
      await db.put("customers", { ...row, ciphertext: next.ciphertext, iv: next.iv });
    }
  }

  for (const row of await db.getAll("customerCredit")) {
    const next = await reencryptRow(
      legacyKey,
      dek,
      { ciphertext: row.ciphertext, iv: row.iv },
      creditAad(row.customerId),
    );
    if (next) {
      await db.put("customerCredit", { ...row, ciphertext: next.ciphertext, iv: next.iv });
    }
  }

  for (const row of await db.getAll("personalTodos")) {
    const next = await reencryptRow(
      legacyKey,
      dek,
      { ciphertext: row.ciphertext, iv: row.iv },
      `personal-todo-cache|${row.todoId}`,
    );
    if (next) {
      await db.put("personalTodos", { ...row, ciphertext: next.ciphertext, iv: next.iv });
    }
  }

  for (const row of await db.getAll("personalContacts")) {
    const aad = `personal-contact-cache|${row.contactId}`;
    const next = await reencryptRow(
      legacyKey,
      dek,
      { ciphertext: row.ciphertext, iv: row.iv },
      aad,
    );
    if (next) {
      await db.put("personalContacts", { ...row, ciphertext: next.ciphertext, iv: next.iv });
    }
  }

  for (const row of await db.getAll("personalRelationships")) {
    const aad = `personal-relationship-cache|${row.relationshipId}`;
    const next = await reencryptRow(
      legacyKey,
      dek,
      { ciphertext: row.ciphertext, iv: row.iv },
      aad,
    );
    if (next) {
      await db.put("personalRelationships", { ...row, ciphertext: next.ciphertext, iv: next.iv });
    }
  }

  for (const row of await db.getAll("personalEntries")) {
    const aad = `personal-entry-cache|${row.entryId}`;
    const next = await reencryptRow(
      legacyKey,
      dek,
      { ciphertext: row.ciphertext, iv: row.iv },
      aad,
    );
    if (next) {
      await db.put("personalEntries", { ...row, ciphertext: next.ciphertext, iv: next.iv });
    }
  }

  await putMeta(db, FIX02_MIGRATION_META_KEY, "1");
  return true;
}

export async function isFix02MigrationComplete(db: OfflineDb): Promise<boolean> {
  return (await getMeta(db, FIX02_MIGRATION_META_KEY)) === "1";
}

/**
 * Runs FIX01→FIX02 re-encryption when online with a trusted session and PIN-unlocked DEK.
 */
export async function maybeMigrateLegacyLocalStoreWhenReady(
  db: OfflineDb,
  scopeBinding: string,
  userId: string,
  options: { online: boolean; trustedSession: boolean },
): Promise<boolean> {
  if (!options.online || !options.trustedSession) {
    return false;
  }
  if (await isFix02MigrationComplete(db)) {
    return true;
  }
  try {
    await getActiveOfflineCryptoKey(userId);
  } catch {
    return false;
  }
  return migrateLegacyLocalStoreToFix02(db, scopeBinding, userId);
}
