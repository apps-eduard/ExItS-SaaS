import { openDB, type DBSchema, type IDBPDatabase } from "idb";
import {
  OFFLINE_SCHEMA_VERSION,
  type OfflineOperationRecord,
  type OfflineScopeKind,
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
}

export type OfflineDb = IDBPDatabase<OfflineDbSchema>;

function databaseName(scope: OfflineScopeKind, scopeKey: string): string {
  // Separate DBs per Personal user vs Organization context — never share.
  return `exits-offline-v${OFFLINE_SCHEMA_VERSION}-${scope}-${scopeKey}`;
}

export function personalScopeKey(userId: string, accountProfileId: string): string {
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
  return openDB<OfflineDbSchema>(name, OFFLINE_SCHEMA_VERSION, {
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
    },
  });
}

export async function putMeta(db: OfflineDb, key: string, value: string): Promise<void> {
  await db.put("meta", { key, value });
}

export async function getMeta(db: OfflineDb, key: string): Promise<string | null> {
  const row = await db.get("meta", key);
  return row?.value ?? null;
}
