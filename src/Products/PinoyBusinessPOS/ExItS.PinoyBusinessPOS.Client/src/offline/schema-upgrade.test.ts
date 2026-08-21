import "fake-indexeddb/auto";
import { openDB } from "idb";
import { describe, expect, it } from "vitest";
import { openOfflineDatabase, organizationScopeKey } from "@/offline/db";
import { listOutbox } from "@/offline/outbox";
import { OFFLINE_SCHEMA_VERSION } from "@/offline/types";

/**
 * The offline database name must not carry the schema version. If it did, every schema bump
 * would silently strand already queued money under a name nothing opens again.
 */
describe("RMAP-21E offline schema upgrade", () => {
  it("upgrades an older database in place and keeps queued operations", async () => {
    const scopeBinding = organizationScopeKey({
      userId: "user-upgrade",
      organizationId: "11111111-1111-1111-1111-111111111111",
      branchId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      installationDeviceId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
    });
    const name = `exits-offline-Organization-${scopeBinding}`;

    // Stand up the previous shape and queue one operation into it.
    const legacy = await openDB(name, OFFLINE_SCHEMA_VERSION - 1, {
      upgrade(db) {
        const outbox = db.createObjectStore("outbox", { keyPath: "operationId" });
        outbox.createIndex("byState", "queueState");
        outbox.createIndex("byCreatedAt", "createdAt");
        outbox.createIndex("byDependsOn", "dependsOnOperationId");
      },
    });
    await legacy.put("outbox", {
      operationId: "legacy-operation",
      queueState: "Pending",
      createdAt: "2026-08-20T00:00:00Z",
      dependsOnOperationId: null,
    });
    legacy.close();

    const upgraded = await openOfflineDatabase("Organization", scopeBinding);
    expect(upgraded.version).toBe(OFFLINE_SCHEMA_VERSION);
    expect([...upgraded.objectStoreNames]).toContain("customers");

    const queued = await listOutbox(upgraded);
    expect(queued.map((row) => row.operationId)).toEqual(["legacy-operation"]);
    upgraded.close();
  });
});
