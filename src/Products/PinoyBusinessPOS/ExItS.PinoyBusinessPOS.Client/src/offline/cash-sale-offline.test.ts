import "fake-indexeddb/auto";
import { describe, expect, it } from "vitest";
import { posIdempotencyKeyForEntity } from "@/api/pos/pos-mutation-idempotency";
import { enqueueOfflineCashSale, OfflineCashSaleRejectedError } from "@/offline/cash-sale-offline";
import { decryptPayload, deriveScopeKeyFromBinding } from "@/offline/crypto";
import { openOfflineDatabase, organizationScopeKey } from "@/offline/db";
import { listOutbox, listSafeOutboxMetadata } from "@/offline/outbox";
import { OFFLINE_SCHEMA_VERSION } from "@/offline/types";

const productId = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const shiftId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const organizationId = "11111111-1111-4111-8111-111111111111";
const branchId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const installationDeviceId = "22222222-2222-4222-8222-222222222222";

async function openScopedDb(userId: string) {
  const scopeBinding = organizationScopeKey({
    userId,
    organizationId,
    branchId,
    installationDeviceId,
  });
  const db = await openOfflineDatabase("Organization", scopeBinding);
  return { db, scopeBinding };
}

function baseInput(
  db: Awaited<ReturnType<typeof openScopedDb>>["db"],
  scopeBinding: string,
  userId: string,
  saleId: string,
) {
  return {
    db,
    scopeBinding,
    userId,
    organizationId,
    branchId,
    installationDeviceId,
    saleId,
    shiftId,
    lines: [{ productId, quantity: 2 }],
    amountTendered: 50,
  };
}

describe("RMAP-21D offline Cash sale enqueue", () => {
  it("keeps the Sell stores intact after the RMAP-21F schema bump", async () => {
    expect(OFFLINE_SCHEMA_VERSION).toBe(4);
    const { db } = await openScopedDb("user-schema");
    expect([...db.objectStoreNames].sort()).toEqual([
      "catalogCategories",
      "catalogProducts",
      "customerCredit",
      "customers",
      "entityMap",
      "meta",
      "outbox",
      "personalContacts",
      "personalEntries",
      "personalRelationships",
      "sellReadiness",
    ]);
    db.close();
  });

  it("encrypts the Cash payload, queues it Pending, and reuses the saleId idempotency key", async () => {
    const userId = "user-cash";
    const saleId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
    const { db, scopeBinding } = await openScopedDb(userId);

    const record = await enqueueOfflineCashSale(baseInput(db, scopeBinding, userId, saleId));

    expect(record.queueState).toBe("Pending");
    expect(record.operationType).toBe("sale.checkout");
    expect(record.operationId).toBe(saleId);
    expect(record.idempotencyKey).toBe(posIdempotencyKeyForEntity(saleId));
    expect(record.organizationId).toBe(organizationId);
    expect(record.branchId).toBe(branchId);
    expect(record.installationDeviceId).toBe(installationDeviceId);

    const [row] = await listOutbox(db);
    expect(new TextDecoder().decode(new Uint8Array(row.ciphertext))).not.toContain(productId);
    expect(JSON.stringify(await listSafeOutboxMetadata(db))).not.toContain(productId);

    const key = await deriveScopeKeyFromBinding(scopeBinding);
    const plaintext = await decryptPayload(
      key,
      { ciphertext: row.ciphertext, iv: row.iv },
      `Organization|sale.checkout|${saleId}`,
    );
    expect(JSON.parse(new TextDecoder().decode(plaintext))).toEqual({
      lines: [{ productId, quantity: 2 }],
      paymentMethod: "Cash",
      saleId,
      shiftId,
      amountTendered: 50,
    });
    db.close();
  });

  it("rejects a discount intent instead of dropping it", async () => {
    const userId = "user-discount";
    const saleId = "dddddddd-dddd-4ddd-8ddd-ddddddddddd1";
    const { db, scopeBinding } = await openScopedDb(userId);

    await expect(
      enqueueOfflineCashSale({
        ...baseInput(db, scopeBinding, userId, saleId),
        discounts: [{ scope: "Sale", method: "Percentage", value: 10, reason: "Regular buyer" }],
      }),
    ).rejects.toMatchObject({ code: "offline.sale.discount_not_supported" });

    expect(await listOutbox(db)).toHaveLength(0);
    db.close();
  });

  it("rejects a price override intent instead of dropping it", async () => {
    const userId = "user-override";
    const saleId = "dddddddd-dddd-4ddd-8ddd-ddddddddddd2";
    const { db, scopeBinding } = await openScopedDb(userId);

    const attempt = enqueueOfflineCashSale({
      ...baseInput(db, scopeBinding, userId, saleId),
      priceOverrides: [{ requestedUnitPrice: 90, reason: "Price match", lineNumber: 1 }],
    });

    await expect(attempt).rejects.toBeInstanceOf(OfflineCashSaleRejectedError);
    await expect(attempt).rejects.toMatchObject({
      code: "offline.sale.price_override_not_supported",
    });
    expect(await listOutbox(db)).toHaveLength(0);
    db.close();
  });

  it("rejects a missing shift, empty lines, and a negative tender", async () => {
    const userId = "user-invalid";
    const saleId = "dddddddd-dddd-4ddd-8ddd-ddddddddddd3";
    const { db, scopeBinding } = await openScopedDb(userId);
    const base = baseInput(db, scopeBinding, userId, saleId);

    await expect(enqueueOfflineCashSale({ ...base, shiftId: "  " })).rejects.toMatchObject({
      code: "offline.sale.shift_required",
    });
    await expect(enqueueOfflineCashSale({ ...base, lines: [] })).rejects.toMatchObject({
      code: "offline.sale.lines_required",
    });
    await expect(enqueueOfflineCashSale({ ...base, amountTendered: -1 })).rejects.toMatchObject({
      code: "offline.sale.tender_invalid",
    });

    expect(await listOutbox(db)).toHaveLength(0);
    db.close();
  });
});
