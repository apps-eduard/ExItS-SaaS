import { openDB, type DBSchema, type IDBPDatabase } from "idb";
import {
  OFFLINE_SCHEMA_VERSION,
  OFFLINE_SCOPE_KIND_META_KEY,
  type CachedCatalogCategoryRecord,
  type CachedCatalogProductRecord,
  type CachedCustomerCreditRecord,
  type CachedCustomerRecord,
  type CachedPersonalContactRecord,
  type CachedPersonalEntryRecord,
  type CachedPersonalRelationshipRecord,
  type OfflineOperationRecord,
  type OfflineScopeKind,
  type SellReadinessSnapshotRecord,
} from "@/offline/types";

export type OfflineMetaRecord = {
  key: string;
  value: string;
};

interface OfflineDbSchema extends DBSchema {
  meta: {
    key: string;
    value: OfflineMetaRecord;
  };
  outbox: {
    key: string;
    value: OfflineOperationRecord;
    indexes: {
      byState: string;
      byCreatedAt: string;
      byDependsOn: string;
    };
  };
  entityMap: {
    key: string;
    value: {
      mapKey: string;
      localId: string;
      serverId: string | null;
      entityType: string;
    };
  };
  catalogProducts: {
    key: string;
    value: CachedCatalogProductRecord;
  };
  catalogCategories: {
    key: string;
    value: CachedCatalogCategoryRecord;
  };
  sellReadiness: {
    key: string;
    value: SellReadinessSnapshotRecord;
  };
  customers: {
    key: string;
    value: CachedCustomerRecord;
    indexes: {
      byStatus: string;
    };
  };
  customerCredit: {
    key: string;
    value: CachedCustomerCreditRecord;
  };
  personalContacts: {
    key: string;
    value: CachedPersonalContactRecord;
  };
  personalRelationships: {
    key: string;
    value: CachedPersonalRelationshipRecord;
    indexes: {
      byPerspective: string;
    };
  };
  personalEntries: {
    key: string;
    value: CachedPersonalEntryRecord;
    indexes: {
      byRelationship: string;
    };
  };
}

export type OfflineDb = IDBPDatabase<OfflineDbSchema>;

function databaseName(scope: OfflineScopeKind, scopeKey: string): string {
  // Separate DBs per Personal user vs Organization context — never share.
  // The schema version stays out of the name so a schema bump upgrades the existing database
  // instead of orphaning already queued money operations under an unreachable name.
  return `exits-offline-${scope}-${scopeKey}`;
}

/**
 * The browser session snapshot does not carry the selected AccountProfile id, and a Personal
 * identity has at most one active Personal profile, so the account-class name is the honest
 * discriminator here. It still separates a Personal store from every Organization store, and one
 * Personal user from another.
 */
export const PERSONAL_PROFILE_SCOPE_DISCRIMINATOR = "Personal" as const;

export function personalScopeKey(
  userId: string,
  accountProfileId: string = PERSONAL_PROFILE_SCOPE_DISCRIMINATOR,
): string {
  return `${userId}:${accountProfileId}`;
}

export function organizationScopeKey(input: {
  userId: string;
  organizationId: string;
  branchId: string;
  installationDeviceId: string;
}): string {
  return `${input.userId}:${input.organizationId}:${input.branchId}:${input.installationDeviceId}`;
}

export async function openOfflineDatabase(
  scope: OfflineScopeKind,
  scopeKey: string,
): Promise<OfflineDb> {
  const name = databaseName(scope, scopeKey);
  const db = await openDB<OfflineDbSchema>(name, OFFLINE_SCHEMA_VERSION, {
    upgrade(db) {
      if (!db.objectStoreNames.contains("meta")) {
        db.createObjectStore("meta", { keyPath: "key" });
      }
      if (!db.objectStoreNames.contains("outbox")) {
        const outbox = db.createObjectStore("outbox", { keyPath: "operationId" });
        outbox.createIndex("byState", "queueState");
        outbox.createIndex("byCreatedAt", "createdAt");
        outbox.createIndex("byDependsOn", "dependsOnOperationId");
      }
      if (!db.objectStoreNames.contains("entityMap")) {
        db.createObjectStore("entityMap", { keyPath: "mapKey" });
      }
      if (!db.objectStoreNames.contains("catalogProducts")) {
        db.createObjectStore("catalogProducts", { keyPath: "productId" });
      }
      if (!db.objectStoreNames.contains("catalogCategories")) {
        db.createObjectStore("catalogCategories", { keyPath: "categoryId" });
      }
      if (!db.objectStoreNames.contains("sellReadiness")) {
        db.createObjectStore("sellReadiness", { keyPath: "key" });
      }
      if (!db.objectStoreNames.contains("customers")) {
        const customers = db.createObjectStore("customers", { keyPath: "customerId" });
        customers.createIndex("byStatus", "status");
      }
      if (!db.objectStoreNames.contains("customerCredit")) {
        db.createObjectStore("customerCredit", { keyPath: "customerId" });
      }
      if (!db.objectStoreNames.contains("personalContacts")) {
        db.createObjectStore("personalContacts", { keyPath: "contactId" });
      }
      if (!db.objectStoreNames.contains("personalRelationships")) {
        const relationships = db.createObjectStore("personalRelationships", {
          keyPath: "relationshipId",
        });
        relationships.createIndex("byPerspective", "perspective");
      }
      if (!db.objectStoreNames.contains("personalEntries")) {
        const entries = db.createObjectStore("personalEntries", { keyPath: "entryId" });
        entries.createIndex("byRelationship", "relationshipId");
      }
    },
  });

  // Recorded so a Personal-domain write can prove it is not standing in an Organization store.
  const stamped = await db.get("meta", OFFLINE_SCOPE_KIND_META_KEY);
  if (stamped?.value !== scope) {
    await db.put("meta", { key: OFFLINE_SCOPE_KIND_META_KEY, value: scope });
  }
  return db;
}

/**
 * Personal Utang and To-do rows must never land in an Organization database, and Business
 * customer rows must never land in a Personal one. The scope is stamped in `meta` at open time,
 * so any writer can fail closed instead of trusting its caller.
 */
export async function assertOfflineScope(db: OfflineDb, expected: OfflineScopeKind): Promise<void> {
  const stamped = await getMeta(db, OFFLINE_SCOPE_KIND_META_KEY);
  if (stamped !== expected) {
    throw new Error(
      `Offline scope mismatch: this database is "${stamped ?? "unstamped"}", not "${expected}".`,
    );
  }
}

const sharedConnections = new Map<string, Promise<OfflineDb>>();

/**
 * One connection per scope for the lifetime of the tab.
 * Screens must not close a database the Connection & Sync shell is still reading.
 */
export function openSharedOfflineDatabase(
  scope: OfflineScopeKind,
  scopeKey: string,
): Promise<OfflineDb> {
  const name = databaseName(scope, scopeKey);
  const existing = sharedConnections.get(name);
  if (existing) {
    return existing;
  }
  const opening = openOfflineDatabase(scope, scopeKey).catch((error: unknown) => {
    sharedConnections.delete(name);
    throw error;
  });
  sharedConnections.set(name, opening);
  return opening;
}

export async function putMeta(db: OfflineDb, key: string, value: string): Promise<void> {
  await db.put("meta", { key, value });
}

export async function getMeta(db: OfflineDb, key: string): Promise<string | null> {
  const row = await db.get("meta", key);
  return row?.value ?? null;
}
