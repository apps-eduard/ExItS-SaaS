import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it } from "vitest";
import { openOfflineDatabase, organizationScopeKey } from "@/offline/db";
import { deriveScopeKeyFromBinding } from "@/offline/crypto";
import { enrollOfflinePinAndDek } from "@/offline/local-store-key";
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

describe("FIX02 migration all-or-nothing", () => {
  beforeEach(() => {
    window.localStorage.clear();
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

    await enrollOfflinePinAndDek(USER, PIN);
    const result = await migrateLegacyLocalStoreToFix02(db, scope, USER);
    expect(result.ok).toBe(false);
    expect(await isFix02MigrationComplete(db)).toBe(false);
    expect(await getMeta(db, FIX02_MIGRATION_META_KEY)).not.toBe("1");
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
});
