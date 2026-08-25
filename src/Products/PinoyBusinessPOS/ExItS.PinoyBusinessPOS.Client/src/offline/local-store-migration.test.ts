import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it } from "vitest";
import { openOfflineDatabase, organizationScopeKey } from "@/offline/db";
import { decryptPayload, deriveScopeKeyFromBinding } from "@/offline/crypto";
import { enrollOfflinePinAndDek } from "@/offline/local-store-key";
import { clearUnlockedDek } from "@/offline/offline-unlock-session";
import {
  FIX02_MIGRATION_META_KEY,
  isFix02MigrationComplete,
  migrateLegacyLocalStoreToFix02,
} from "@/offline/local-store-migration";
import { getMeta } from "@/offline/db";
import { putPriceAuthorities } from "@/offline/price-authority-cache";
import type { OfflinePriceAuthority } from "@/api/pos/pos-offline-price-authority-client";
import { enqueueEncryptedOperation } from "@/offline/outbox";
import { cacheCustomers, getCachedCustomer } from "@/offline/customer-cache";

const USER = "248935e9-e462-425f-88f5-a9255bf12748";
const ORG = "ca023f5b-925e-4aa5-a843-d48c4c06fa14";
const BRANCH = "742fb3f3-14f9-4bee-a94e-f5acccc7cbc5";
const PIN = "123456";

function testInstallation(suffix: string): string {
  return `22222222-2222-4222-8${suffix.padStart(3, "0").slice(-3)}-222222222222`;
}

function bufferFingerprint(buffer: ArrayBuffer): string {
  return Array.from(new Uint8Array(buffer)).join(",");
}

async function decryptLegacyOutboxPayload(
  db: Awaited<ReturnType<typeof openOfflineDatabase>>,
  legacyKey: CryptoKey,
  operationId: string,
  scopeKind: "Organization" = "Organization",
  operationType = "sale.checkout.cash",
): Promise<string> {
  const row = await db.get("outbox", operationId);
  expect(row).toBeTruthy();
  if (!row) {
    return "";
  }
  const plaintext = await decryptPayload(
    legacyKey,
    { ciphertext: row.ciphertext, iv: row.iv },
    `${scopeKind}|${operationType}|${operationId}`,
  );
  return new TextDecoder().decode(plaintext);
}

describe("FIX02 migration all-or-nothing", () => {
  beforeEach(() => {
    window.localStorage.clear();
    clearUnlockedDek();
  });

  it("fails safely when a legacy row cannot decrypt and leaves migration incomplete", async () => {
    const installationDeviceId = testInstallation("001");
    const scope = organizationScopeKey({
      userId: USER,
      organizationId: ORG,
      branchId: BRANCH,
      installationDeviceId,
    });
    const db = await openOfflineDatabase("Organization", scope);
    const legacyKey = await deriveScopeKeyFromBinding(scope);

    await enqueueEncryptedOperation({
      db,
      scopeKind: "Organization",
      scopeBinding: scope,
      userId: USER,
      organizationId: ORG,
      branchId: BRANCH,
      installationDeviceId,
      posDeviceId: "11111111-1111-1111-1111-111111111111",
      productDomain: "pos.sale",
      operationType: "sale.checkout.cash",
      operationId: "good-op",
      idempotencyKey: "good-sale",
      plaintextJson: JSON.stringify({ saleId: "good-sale", total: 10 }),
      cryptoKey: legacyKey,
    });

    const corruptRows = await db.getAll("outbox");
    const corrupt = corruptRows.find((row) => row.operationId === "good-op");
    expect(corrupt).toBeTruthy();
    if (corrupt) {
      corrupt.iv = corrupt.iv.slice(0, 4);
      await db.put("outbox", corrupt);
    }

    const beforeRows = await db.getAll("outbox");
    const beforeFingerprints = beforeRows.map((row) => ({
      operationId: row.operationId,
      ciphertext: bufferFingerprint(row.ciphertext),
      iv: bufferFingerprint(row.iv),
    }));

    await enrollOfflinePinAndDek(USER, PIN);
    const result = await migrateLegacyLocalStoreToFix02(db, scope, USER);
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.reason).toBe("partial_decrypt_failure");
    }
    expect(await isFix02MigrationComplete(db)).toBe(false);
    expect(await getMeta(db, FIX02_MIGRATION_META_KEY)).not.toBe("1");

    const afterRows = await db.getAll("outbox");
    expect(afterRows.map((row) => row.operationId)).toEqual(beforeRows.map((row) => row.operationId));
    for (const before of beforeFingerprints) {
      const after = afterRows.find((row) => row.operationId === before.operationId);
      expect(after).toBeTruthy();
      expect(bufferFingerprint(after!.ciphertext)).toBe(before.ciphertext);
      expect(bufferFingerprint(after!.iv)).toBe(before.iv);
    }
    db.close();
  });

  it("preserves pending sale and plaintext price lease after successful migration", async () => {
    const installationDeviceId = testInstallation("002");
    const scope = organizationScopeKey({
      userId: USER,
      organizationId: ORG,
      branchId: BRANCH,
      installationDeviceId,
    });
    const db = await openOfflineDatabase("Organization", scope);
    const legacyKey = await deriveScopeKeyFromBinding(scope);

    await enqueueEncryptedOperation({
      db,
      scopeKind: "Organization",
      scopeBinding: scope,
      userId: USER,
      organizationId: ORG,
      branchId: BRANCH,
      installationDeviceId,
      posDeviceId: "11111111-1111-1111-1111-111111111111",
      productDomain: "pos.sale",
      operationType: "sale.checkout.cash",
      operationId: "sale-op",
      idempotencyKey: "sale-1",
      plaintextJson: JSON.stringify({ saleId: "sale-1", total: 25 }),
      cryptoKey: legacyKey,
    });

    await cacheCustomers(db, scope, [
      {
        customerId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        organizationId: ORG,
        displayName: "Ana Customer",
        mobileNumber: "09171234567",
        address: "Manila",
        notes: null,
        status: "Active",
        createdAtUtc: "2026-01-01T12:00:00.000Z",
        updatedAtUtc: "2026-01-01T12:00:00.000Z",
      },
    ]);

    const authority: OfflinePriceAuthority = {
      authorityId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      organizationId: ORG,
      branchId: BRANCH,
      productId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      sellingUnitId: null,
      unitPrice: 25,
      unitOfMeasure: "Piece",
      sellingMode: "Retail",
      issuedAtUtc: "2026-01-01T12:00:00.000Z",
      expiresAtUtc: "2030-01-01T12:00:00.000Z",
      signature: "abc123",
    };
    await putPriceAuthorities(db, [authority]);

    await enrollOfflinePinAndDek(USER, PIN);
    const first = await migrateLegacyLocalStoreToFix02(db, scope, USER);
    expect(first.ok).toBe(true);
    const second = await migrateLegacyLocalStoreToFix02(db, scope, USER);
    expect(second.ok).toBe(true);
    expect(await isFix02MigrationComplete(db)).toBe(true);

    const lease = await db.get("priceAuthorities", `${authority.productId}::base`);
    expect(lease?.authority.unitPrice).toBe(25);

    const outbox = await db.getAll("outbox");
    expect(outbox.some((row) => row.operationId === "sale-op")).toBe(true);

    const customer = await getCachedCustomer(db, scope, "cccccccc-cccc-cccc-cccc-cccccccccccc");
    expect(customer?.displayName).toBe("Ana Customer");
    db.close();
  });

  it("aborts commit atomically and allows safe retry after simulated write failure", async () => {
    const installationDeviceId = testInstallation("003");
    const scope = organizationScopeKey({
      userId: USER,
      organizationId: ORG,
      branchId: BRANCH,
      installationDeviceId,
    });
    const db = await openOfflineDatabase("Organization", scope);
    const legacyKey = await deriveScopeKeyFromBinding(scope);

    await enqueueEncryptedOperation({
      db,
      scopeKind: "Organization",
      scopeBinding: scope,
      userId: USER,
      organizationId: ORG,
      branchId: BRANCH,
      installationDeviceId,
      posDeviceId: "11111111-1111-1111-1111-111111111111",
      productDomain: "pos.sale",
      operationType: "sale.checkout.cash",
      operationId: "retry-op",
      idempotencyKey: "retry-sale",
      plaintextJson: JSON.stringify({ saleId: "retry-sale", total: 42 }),
      cryptoKey: legacyKey,
    });

    await cacheCustomers(db, scope, [
      {
        customerId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
        organizationId: ORG,
        displayName: "Retry Customer",
        mobileNumber: "09181234567",
        address: "Cebu",
        notes: null,
        status: "Active",
        createdAtUtc: "2026-01-01T12:00:00.000Z",
        updatedAtUtc: "2026-01-01T12:00:00.000Z",
      },
    ]);

    const outboxBefore = await db.get("outbox", "retry-op");
    expect(outboxBefore).toBeTruthy();
    const outboxCiphertextBefore = bufferFingerprint(outboxBefore!.ciphertext);
    const outboxIvBefore = bufferFingerprint(outboxBefore!.iv);

    const customerBefore = await db.get("customers", "dddddddd-dddd-dddd-dddd-dddddddddddd");
    expect(customerBefore).toBeTruthy();
    const customerCiphertextBefore = bufferFingerprint(customerBefore!.ciphertext);
    const customerIvBefore = bufferFingerprint(customerBefore!.iv);

    await enrollOfflinePinAndDek(USER, PIN);
    const aborted = await migrateLegacyLocalStoreToFix02(db, scope, USER, {
      testAbortCommitOnStore: "customers",
    });
    expect(aborted.ok).toBe(false);
    if (!aborted.ok) {
      expect(aborted.reason).toBe("commit_failure");
    }
    expect(await isFix02MigrationComplete(db)).toBe(false);
    expect(await getMeta(db, FIX02_MIGRATION_META_KEY)).not.toBe("1");

    const outboxAfterAbort = await db.get("outbox", "retry-op");
    expect(outboxAfterAbort).toBeTruthy();
    expect(bufferFingerprint(outboxAfterAbort!.ciphertext)).toBe(outboxCiphertextBefore);
    expect(bufferFingerprint(outboxAfterAbort!.iv)).toBe(outboxIvBefore);

    const customerAfterAbort = await db.get("customers", "dddddddd-dddd-dddd-dddd-dddddddddddd");
    expect(customerAfterAbort).toBeTruthy();
    expect(bufferFingerprint(customerAfterAbort!.ciphertext)).toBe(customerCiphertextBefore);
    expect(bufferFingerprint(customerAfterAbort!.iv)).toBe(customerIvBefore);

    clearUnlockedDek();
    const legacyPayload = await decryptLegacyOutboxPayload(db, legacyKey, "retry-op");
    expect(legacyPayload).toContain("retry-sale");

    const legacyCustomer = await getCachedCustomer(db, scope, "dddddddd-dddd-dddd-dddd-dddddddddddd");
    expect(legacyCustomer?.displayName).toBe("Retry Customer");

    await enrollOfflinePinAndDek(USER, PIN);
    const retried = await migrateLegacyLocalStoreToFix02(db, scope, USER);
    expect(retried.ok).toBe(true);
    expect(await isFix02MigrationComplete(db)).toBe(true);

    const migratedCustomer = await getCachedCustomer(db, scope, "dddddddd-dddd-dddd-dddd-dddddddddddd");
    expect(migratedCustomer?.displayName).toBe("Retry Customer");
    db.close();
  });

  it("remains idempotent after migration completes", async () => {
    const installationDeviceId = testInstallation("004");
    const scope = organizationScopeKey({
      userId: USER,
      organizationId: ORG,
      branchId: BRANCH,
      installationDeviceId,
    });
    const db = await openOfflineDatabase("Organization", scope);
    const legacyKey = await deriveScopeKeyFromBinding(scope);

    await enqueueEncryptedOperation({
      db,
      scopeKind: "Organization",
      scopeBinding: scope,
      userId: USER,
      organizationId: ORG,
      branchId: BRANCH,
      installationDeviceId,
      posDeviceId: "11111111-1111-1111-1111-111111111111",
      productDomain: "pos.sale",
      operationType: "sale.checkout.cash",
      operationId: "idem-op",
      idempotencyKey: "idem-sale",
      plaintextJson: JSON.stringify({ saleId: "idem-sale", total: 5 }),
      cryptoKey: legacyKey,
    });

    await enrollOfflinePinAndDek(USER, PIN);
    expect((await migrateLegacyLocalStoreToFix02(db, scope, USER)).ok).toBe(true);
    expect((await migrateLegacyLocalStoreToFix02(db, scope, USER)).ok).toBe(true);
    expect(await isFix02MigrationComplete(db)).toBe(true);
    db.close();
  });
});
