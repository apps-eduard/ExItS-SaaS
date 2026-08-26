import type {
  PersonalContactDto,
  PersonalDebtRelationshipSummaryDto,
  PersonalUtangEntryDto,
} from "@/api/platform/personal-utang-client";
import { decryptPayload, encryptPayload } from "@/offline/crypto";
import {
  getActiveOfflineCryptoKeyForScope,
  OfflineCryptoLockedError,
} from "@/offline/local-store-key";
import { assertOfflineScope, getMeta, putMeta, type OfflineDb } from "@/offline/db";
import {
  PERSONAL_USER_IDENTITY_META_KEY,
  type CachedPersonalContactRecord,
  type CachedPersonalEntryRecord,
  type CachedPersonalRelationshipRecord,
} from "@/offline/types";

/**
 * Encrypted Personal Utang cache (RMAP-21F).
 *
 * Write-through from a successful online read, plus optimistic rows for work queued on this
 * device. Names, phone numbers, balances and notes are AES-GCM sealed under the Personal scope
 * key, so a shared browser profile cannot be read as "who owes this person money". Every read
 * fails closed to empty/null — an unreadable cache must never look like an authoritative
 * "nobody owes you anything".
 *
 * Locally queued rows carry `origin: "Local"` so the UI can label them as waiting to sync instead
 * of presenting a device-only balance as if the server had agreed to it.
 */

const CONTACT_AAD = "personal-contact-cache";
const RELATIONSHIP_AAD = "personal-relationship-cache";
const ENTRY_AAD = "personal-entry-cache";

function aad(prefix: string, id: string): string {
  return `${prefix}|${id}`;
}

async function seal(scopeBinding: string, value: unknown, associatedData: string) {
  const key = await getActiveOfflineCryptoKeyForScope(scopeBinding);
  return encryptPayload(key, new TextEncoder().encode(JSON.stringify(value)), associatedData);
}

/** Online write-through must never crash the page when the Offline PIN DEK is locked. */
function isWriteThroughCryptoLock(error: unknown): boolean {
  return error instanceof OfflineCryptoLockedError;
}

async function unseal<T>(
  scopeBinding: string,
  envelope: { ciphertext: ArrayBuffer; iv: ArrayBuffer },
  associatedData: string,
): Promise<T | null> {
  try {
    const key = await getActiveOfflineCryptoKeyForScope(scopeBinding);
    const plaintext = await decryptPayload(key, envelope, associatedData);
    return JSON.parse(new TextDecoder().decode(plaintext)) as T;
  } catch {
    // Wrong scope key, tampered row, or corrupt envelope — drop the row rather than guess.
    return null;
  }
}

/** Every Personal write proves it is not standing in an Organization database first. */
async function requirePersonalScope(db: OfflineDb): Promise<void> {
  await assertOfflineScope(db, "Personal");
}

export type PersonalOrigin = "Server" | "Local";

export type CachedPersonalContact = PersonalContactDto & {
  origin: PersonalOrigin;
  /** Null until the queued contact has synced and the server id is known. */
  serverId: string | null;
};

export type CachedPersonalRelationship = PersonalDebtRelationshipSummaryDto & {
  origin: PersonalOrigin;
  serverId: string | null;
};

export type CachedPersonalEntry = PersonalUtangEntryDto & {
  origin: PersonalOrigin;
  serverId: string | null;
};

/**
 * The Personal identity id is needed to name the creditor or debtor on a relationship queued
 * offline, so it is captured while online. It is an identifier, not a credential, and it is not a
 * session token — the offline store never holds anything that could authenticate a request.
 */
export async function cachePersonalUserIdentityId(db: OfflineDb, userIdentityId: string) {
  await requirePersonalScope(db);
  await putMeta(db, PERSONAL_USER_IDENTITY_META_KEY, userIdentityId);
}

export async function getCachedPersonalUserIdentityId(db: OfflineDb): Promise<string | null> {
  try {
    return await getMeta(db, PERSONAL_USER_IDENTITY_META_KEY);
  } catch {
    return null;
  }
}

export async function cachePersonalContacts(
  db: OfflineDb,
  scopeBinding: string,
  contacts: ReadonlyArray<PersonalContactDto>,
): Promise<void> {
  await requirePersonalScope(db);
  if (contacts.length === 0) {
    return;
  }
  try {
    const cachedAtUtc = new Date().toISOString();
    const records = await Promise.all(
      contacts.map(async (contact): Promise<CachedPersonalContactRecord> => {
        const envelope = await seal(scopeBinding, contact, aad(CONTACT_AAD, contact.id));
        return {
          contactId: contact.id,
          serverId: contact.id,
          origin: "Server",
          updatedAtUtc: contact.createdAtUtc,
          cachedAtUtc,
          ciphertext: envelope.ciphertext,
          iv: envelope.iv,
        };
      }),
    );
    const tx = db.transaction("personalContacts", "readwrite");
    for (const record of records) {
      await tx.store.put(record);
    }
    await tx.done;
  } catch (error) {
    if (isWriteThroughCryptoLock(error)) {
      return;
    }
    throw error;
  }
}

/** Optimistic row for a contact queued on this device — not yet agreed to by the server. */
export async function cacheLocalPersonalContact(
  db: OfflineDb,
  scopeBinding: string,
  contact: PersonalContactDto,
): Promise<void> {
  await requirePersonalScope(db);
  const envelope = await seal(scopeBinding, contact, aad(CONTACT_AAD, contact.id));
  await db.put("personalContacts", {
    contactId: contact.id,
    serverId: null,
    origin: "Local",
    updatedAtUtc: contact.createdAtUtc,
    cachedAtUtc: new Date().toISOString(),
    ciphertext: envelope.ciphertext,
    iv: envelope.iv,
  });
}

export async function listCachedPersonalContacts(
  db: OfflineDb,
  scopeBinding: string,
): Promise<CachedPersonalContact[]> {
  let rows: CachedPersonalContactRecord[];
  try {
    rows = await db.getAll("personalContacts");
  } catch {
    return [];
  }
  const decrypted = await Promise.all(
    rows.map(async (row) => {
      const contact = await unseal<PersonalContactDto>(
        scopeBinding,
        { ciphertext: row.ciphertext, iv: row.iv },
        aad(CONTACT_AAD, row.contactId),
      );
      return contact ? { ...contact, origin: row.origin, serverId: row.serverId } : null;
    }),
  );
  return decrypted
    .filter((contact): contact is CachedPersonalContact => contact != null)
    .sort((a, b) => a.displayName.localeCompare(b.displayName));
}

export async function cachePersonalRelationships(
  db: OfflineDb,
  scopeBinding: string,
  perspective: "Lent" | "Borrowed",
  relationships: ReadonlyArray<PersonalDebtRelationshipSummaryDto>,
): Promise<void> {
  await requirePersonalScope(db);
  if (relationships.length === 0) {
    return;
  }
  try {
    const cachedAtUtc = new Date().toISOString();
    const records = await Promise.all(
      relationships.map(async (relationship): Promise<CachedPersonalRelationshipRecord> => {
        const envelope = await seal(
          scopeBinding,
          relationship,
          aad(RELATIONSHIP_AAD, relationship.id),
        );
        return {
          relationshipId: relationship.id,
          serverId: relationship.id,
          perspective,
          origin: "Server",
          version: relationship.version,
          updatedAtUtc: relationship.updatedAtUtc,
          cachedAtUtc,
          ciphertext: envelope.ciphertext,
          iv: envelope.iv,
        };
      }),
    );
    const tx = db.transaction("personalRelationships", "readwrite");
    for (const record of records) {
      await tx.store.put(record);
    }
    await tx.done;
  } catch (error) {
    if (isWriteThroughCryptoLock(error)) {
      return;
    }
    throw error;
  }
}

export async function cachePersonalRelationship(
  db: OfflineDb,
  scopeBinding: string,
  perspective: "Lent" | "Borrowed",
  relationship: PersonalDebtRelationshipSummaryDto,
): Promise<void> {
  await cachePersonalRelationships(db, scopeBinding, perspective, [relationship]);
}

export async function cacheLocalPersonalRelationship(
  db: OfflineDb,
  scopeBinding: string,
  perspective: "Lent" | "Borrowed",
  relationship: PersonalDebtRelationshipSummaryDto,
): Promise<void> {
  await requirePersonalScope(db);
  const envelope = await seal(scopeBinding, relationship, aad(RELATIONSHIP_AAD, relationship.id));
  await db.put("personalRelationships", {
    relationshipId: relationship.id,
    serverId: null,
    perspective,
    origin: "Local",
    version: null,
    updatedAtUtc: relationship.updatedAtUtc,
    cachedAtUtc: new Date().toISOString(),
    ciphertext: envelope.ciphertext,
    iv: envelope.iv,
  });
}

export async function listCachedPersonalRelationships(
  db: OfflineDb,
  scopeBinding: string,
  perspective: "Lent" | "Borrowed",
): Promise<CachedPersonalRelationship[]> {
  let rows: CachedPersonalRelationshipRecord[];
  try {
    rows = await db.getAll("personalRelationships");
  } catch {
    return [];
  }
  const decrypted = await Promise.all(
    rows
      .filter((row) => row.perspective === perspective)
      .map(async (row) => {
        const relationship = await unseal<PersonalDebtRelationshipSummaryDto>(
          scopeBinding,
          { ciphertext: row.ciphertext, iv: row.iv },
          aad(RELATIONSHIP_AAD, row.relationshipId),
        );
        return relationship
          ? { ...relationship, origin: row.origin, serverId: row.serverId }
          : null;
      }),
  );
  return decrypted
    .filter((row): row is CachedPersonalRelationship => row != null)
    .sort((a, b) => b.updatedAtUtc.localeCompare(a.updatedAtUtc));
}

export async function getCachedPersonalRelationship(
  db: OfflineDb,
  scopeBinding: string,
  relationshipId: string,
): Promise<CachedPersonalRelationship | null> {
  try {
    const row = await db.get("personalRelationships", relationshipId);
    if (!row) {
      return null;
    }
    const relationship = await unseal<PersonalDebtRelationshipSummaryDto>(
      scopeBinding,
      { ciphertext: row.ciphertext, iv: row.iv },
      aad(RELATIONSHIP_AAD, relationshipId),
    );
    return relationship ? { ...relationship, origin: row.origin, serverId: row.serverId } : null;
  } catch {
    return null;
  }
}

export async function cachePersonalEntries(
  db: OfflineDb,
  scopeBinding: string,
  entries: ReadonlyArray<PersonalUtangEntryDto>,
): Promise<void> {
  await requirePersonalScope(db);
  if (entries.length === 0) {
    return;
  }
  try {
    const cachedAtUtc = new Date().toISOString();
    const records = await Promise.all(
      entries.map(async (entry): Promise<CachedPersonalEntryRecord> => {
        const envelope = await seal(scopeBinding, entry, aad(ENTRY_AAD, entry.id));
        return {
          entryId: entry.id,
          relationshipId: entry.relationshipId,
          serverId: entry.id,
          origin: "Server",
          occurredAtUtc: entry.createdAtUtc,
          cachedAtUtc,
          ciphertext: envelope.ciphertext,
          iv: envelope.iv,
        };
      }),
    );
    const tx = db.transaction("personalEntries", "readwrite");
    for (const record of records) {
      await tx.store.put(record);
    }
    await tx.done;
  } catch (error) {
    if (isWriteThroughCryptoLock(error)) {
      return;
    }
    throw error;
  }
}

/**
 * Optimistic history row for an entry queued on this device.
 *
 * `balanceAfter` is deliberately left as the caller's local estimate and the row stays labelled
 * `Local`: the server recomputes the real running balance when the entry finally posts.
 */
export async function cacheLocalPersonalEntry(
  db: OfflineDb,
  scopeBinding: string,
  entry: PersonalUtangEntryDto,
): Promise<void> {
  await requirePersonalScope(db);
  const envelope = await seal(scopeBinding, entry, aad(ENTRY_AAD, entry.id));
  await db.put("personalEntries", {
    entryId: entry.id,
    relationshipId: entry.relationshipId,
    serverId: null,
    origin: "Local",
    occurredAtUtc: entry.createdAtUtc,
    cachedAtUtc: new Date().toISOString(),
    ciphertext: envelope.ciphertext,
    iv: envelope.iv,
  });
}

export async function listCachedPersonalEntries(
  db: OfflineDb,
  scopeBinding: string,
  relationshipId: string,
): Promise<CachedPersonalEntry[]> {
  let rows: CachedPersonalEntryRecord[];
  try {
    rows = await db.getAllFromIndex("personalEntries", "byRelationship", relationshipId);
  } catch {
    return [];
  }
  const decrypted = await Promise.all(
    rows.map(async (row) => {
      const entry = await unseal<PersonalUtangEntryDto>(
        scopeBinding,
        { ciphertext: row.ciphertext, iv: row.iv },
        aad(ENTRY_AAD, row.entryId),
      );
      return entry ? { ...entry, origin: row.origin, serverId: row.serverId } : null;
    }),
  );
  return decrypted
    .filter((entry): entry is CachedPersonalEntry => entry != null)
    .sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc));
}
