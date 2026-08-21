import "fake-indexeddb/auto";
import { describe, expect, it } from "vitest";
import type { PosCustomerCreditSummary, PosCustomerListItem } from "@/api/pos/pos-customers-client";
import {
  cacheCustomerCreditSummary,
  cacheCustomers,
  filterCachedCustomers,
  getCachedCustomer,
  getCachedCustomerCreditSummary,
  listCachedCustomers,
} from "@/offline/customer-cache";
import { openOfflineDatabase, organizationScopeKey } from "@/offline/db";

const organizationId = "11111111-1111-1111-1111-111111111111";

function scopeKey(userId: string): string {
  return organizationScopeKey({
    userId,
    organizationId,
    branchId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
    installationDeviceId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
  });
}

async function openScoped(userId: string) {
  const scopeBinding = scopeKey(userId);
  const db = await openOfflineDatabase("Organization", scopeBinding);
  return { db, scopeBinding };
}

function customer(id: string, displayName: string, mobile = "09171234567"): PosCustomerListItem {
  return {
    customerId: id,
    organizationId,
    displayName,
    mobileNumber: mobile,
    address: "Manila",
    notes: null,
    status: "Active",
    createdAtUtc: "2026-08-01T00:00:00Z",
    updatedAtUtc: "2026-08-02T00:00:00Z",
  };
}

function summary(id: string, outstanding: number): PosCustomerCreditSummary {
  return {
    customerId: id,
    organizationId,
    outstandingAmount: outstanding,
    activeEntryCount: 1,
    totalEntryCount: 1,
  };
}

describe("RMAP-21E Business customer cache", () => {
  it("fails closed to no customers before any write-through", async () => {
    const { db, scopeBinding } = await openScoped("user-cache-empty");
    expect(await listCachedCustomers(db, scopeBinding)).toEqual([]);
    expect(await getCachedCustomer(db, scopeBinding, "missing")).toBeNull();
    expect(await getCachedCustomerCreditSummary(db, scopeBinding, "missing")).toBeNull();
    db.close();
  });

  it("round-trips a cached customer and outstanding balance", async () => {
    const { db, scopeBinding } = await openScoped("user-cache-roundtrip");
    const id = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

    await cacheCustomers(db, scopeBinding, [customer(id, "Juan Dela Cruz")]);
    await cacheCustomerCreditSummary(db, scopeBinding, summary(id, 18.5));

    await expect(getCachedCustomer(db, scopeBinding, id)).resolves.toMatchObject({
      displayName: "Juan Dela Cruz",
      mobileNumber: "09171234567",
    });
    await expect(getCachedCustomerCreditSummary(db, scopeBinding, id)).resolves.toMatchObject({
      outstandingAmount: 18.5,
    });
    db.close();
  });

  it("keeps customer identity out of plaintext at rest", async () => {
    const { db, scopeBinding } = await openScoped("user-cache-encrypted");
    const id = "cccccccc-cccc-4ccc-8ccc-ccccccccccc1";
    await cacheCustomers(db, scopeBinding, [customer(id, "Maria Santos", "09998887777")]);

    const row = await db.get("customers", id);
    expect(row).toBeDefined();
    const serialized = JSON.stringify({
      ...row,
      ciphertext: [...new Uint8Array(row!.ciphertext)],
      iv: [...new Uint8Array(row!.iv)],
    });
    expect(serialized).not.toContain("Maria Santos");
    expect(serialized).not.toContain("09998887777");
    // Routing and lifecycle columns stay readable so the store can be indexed and merged.
    expect(row!.status).toBe("Active");
    expect(row!.organizationId).toBe(organizationId);
    db.close();
  });

  it("refuses to decrypt another scope's cached customer", async () => {
    const { db, scopeBinding } = await openScoped("user-cache-scope-a");
    const id = "cccccccc-cccc-4ccc-8ccc-ccccccccccc2";
    await cacheCustomers(db, scopeBinding, [customer(id, "Scoped Customer")]);

    const otherScope = scopeKey("user-cache-scope-b");
    expect(await getCachedCustomer(db, otherScope, id)).toBeNull();
    expect(await listCachedCustomers(db, otherScope)).toEqual([]);
    db.close();
  });

  it("merges pages instead of treating a filtered page as a deletion", async () => {
    const { db, scopeBinding } = await openScoped("user-cache-merge");
    const first = "cccccccc-cccc-4ccc-8ccc-ccccccccccc3";
    const second = "cccccccc-cccc-4ccc-8ccc-ccccccccccc4";

    await cacheCustomers(db, scopeBinding, [customer(first, "Ana")]);
    await cacheCustomers(db, scopeBinding, [customer(second, "Ben")]);

    const cached = await listCachedCustomers(db, scopeBinding);
    expect(cached.map((row) => row.displayName).sort()).toEqual(["Ana", "Ben"]);
    db.close();
  });

  it("searches the cached list by name and mobile, and honours the status filter", () => {
    const rows = [
      customer("id-1", "Ana Reyes", "09170000001"),
      { ...customer("id-2", "Ben Cruz", "09170000002"), status: "Inactive" },
    ];

    expect(filterCachedCustomers(rows, { search: "ana" }).map((row) => row.customerId)).toEqual([
      "id-1",
    ]);
    expect(filterCachedCustomers(rows, { search: "0000002" }).map((row) => row.customerId)).toEqual(
      ["id-2"],
    );
    expect(filterCachedCustomers(rows, { status: "Active" }).map((row) => row.customerId)).toEqual([
      "id-1",
    ]);
    expect(filterCachedCustomers(rows, {}).length).toBe(2);
  });
});
