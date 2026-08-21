import "fake-indexeddb/auto";
import { openDB } from "idb";
import { describe, expect, it } from "vitest";
import {
  assertOfflineScope,
  getMeta,
  openOfflineDatabase,
  organizationScopeKey,
  personalScopeKey,
} from "@/offline/db";
import { listOutbox } from "@/offline/outbox";
import { OFFLINE_SCHEMA_VERSION, OFFLINE_SCOPE_KIND_META_KEY } from "@/offline/types";

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
    expect([...upgraded.objectStoreNames]).toContain("personalContacts");

    const queued = await listOutbox(upgraded);
    expect(queued.map((row) => row.operationId)).toEqual(["legacy-operation"]);
    upgraded.close();
  });
});

describe("RMAP-21F offline scope stamp", () => {
  it("stamps each database with its own scope so a writer can fail closed", async () => {
    const personal = await openOfflineDatabase("Personal", personalScopeKey("scope-stamp-user"));
    const organization = await openOfflineDatabase(
      "Organization",
      organizationScopeKey({
        userId: "scope-stamp-user",
        organizationId: "22222222-2222-4222-8222-222222222222",
        branchId: "33333333-3333-4333-8333-333333333333",
        installationDeviceId: "44444444-4444-4444-8444-444444444444",
      }),
    );

    expect(await getMeta(personal, OFFLINE_SCOPE_KIND_META_KEY)).toBe("Personal");
    expect(await getMeta(organization, OFFLINE_SCOPE_KIND_META_KEY)).toBe("Organization");

    await expect(assertOfflineScope(personal, "Personal")).resolves.toBeUndefined();
    await expect(assertOfflineScope(personal, "Organization")).rejects.toThrow(/scope mismatch/i);
    await expect(assertOfflineScope(organization, "Personal")).rejects.toThrow(/scope mismatch/i);

    personal.close();
    organization.close();
  });
});
