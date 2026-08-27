import "fake-indexeddb/auto";
import { describe, expect, it } from "vitest";
import { posIdempotencyKeyForEntity } from "@/api/pos/pos-mutation-idempotency";
import { decryptPayload, deriveScopeKeyFromBinding } from "@/offline/crypto";
import {
  enqueueOfflineCustomerCreate,
  enqueueOfflineCustomerRepayment,
  enqueueOfflineCustomerUpdate,
  OfflineCustomerRejectedError,
} from "@/offline/customer-offline";
import { openOfflineDatabase, organizationScopeKey } from "@/offline/db";
import { listOutbox, listSafeOutboxMetadata } from "@/offline/outbox";
import { parseQueuedRequest } from "@/offline/queued-request";
import type { OfflineOperationRecord } from "@/offline/types";

const organizationId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const installationDeviceId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const customerId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

async function openScoped(userId: string) {
  const scopeBinding = organizationScopeKey({
    userId,
    organizationId,
    branchId,
    installationDeviceId,
  });
  const db = await openOfflineDatabase("Organization", scopeBinding);
  return { db, scopeBinding };
}

function scope(
  db: Awaited<ReturnType<typeof openOfflineDatabase>>,
  scopeBinding: string,
  userId: string,
) {
  return {
    db,
    scopeBinding,
    userId,
    organizationId,
    branchId,
    installationDeviceId,
    posDeviceId: null,
  };
}

async function decryptRequest(record: OfflineOperationRecord, scopeBinding: string) {
  const key = await deriveScopeKeyFromBinding(scopeBinding);
  const plaintext = await decryptPayload(
    key,
    { ciphertext: record.ciphertext, iv: record.iv },
    `${record.scopeKind}|${record.operationType}|${record.operationId}`,
  );
  return parseQueuedRequest(new TextDecoder().decode(plaintext));
}

describe("RMAP-21E offline Business customer queue", () => {
  it("queues a customer create keyed on the client-chosen customer id", async () => {
    const userId = "user-customer-create";
    const { db, scopeBinding } = await openScoped(userId);

    const record = await enqueueOfflineCustomerCreate({
      ...scope(db, scopeBinding, userId),
      customerId,
      customer: { displayName: "  Juan Dela Cruz  ", mobileNumber: "09171234567" },
    }, { allowOfflineEngine: true });

    expect(record.queueState).toBe("Pending");
    expect(record.operationType).toBe("customer.create");
    expect(record.idempotencyKey).toBe(posIdempotencyKeyForEntity(customerId));
    expect(record.entityLocalId).toBe(customerId);
    expect(record.scopeKind).toBe("Organization");

    const request = await decryptRequest(record, scopeBinding);
    expect(request).toMatchObject({ api: "pos", method: "POST", path: "/api/v1/pos/customers" });
    expect(request?.body).toMatchObject({
      displayName: "Juan Dela Cruz",
      mobileNumber: "09171234567",
      customerId,
    });
    db.close();
  });

  it("rejects an offline attempt to link a Business customer to an ExItS identity", async () => {
    const userId = "user-customer-link";
    const { db, scopeBinding } = await openScoped(userId);

    await expect(
      enqueueOfflineCustomerCreate({
        ...scope(db, scopeBinding, userId),
        customerId,
        customer: { displayName: "Linked Buyer" },
        platformBusinessCustomerId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      }, { allowOfflineEngine: true }),
    ).rejects.toBeInstanceOf(OfflineCustomerRejectedError);

    await expect(
      enqueueOfflineCustomerCreate({
        ...scope(db, scopeBinding, userId),
        customerId,
        customer: { displayName: "Linked Buyer" },
        linkedPersonalPublicUserId: "EXITS-PERSONAL-1",
      }, { allowOfflineEngine: true }),
    ).rejects.toMatchObject({ code: "offline.customer.identity_link_not_supported" });

    expect(await listOutbox(db)).toEqual([]);
    db.close();
  });

  it("rejects a nameless customer instead of queueing an unusable row", async () => {
    const userId = "user-customer-name";
    const { db, scopeBinding } = await openScoped(userId);

    await expect(
      enqueueOfflineCustomerCreate({
        ...scope(db, scopeBinding, userId),
        customerId,
        customer: { displayName: "   " },
      }, { allowOfflineEngine: true }),
    ).rejects.toMatchObject({ code: "offline.customer.display_name_required" });

    expect(await listOutbox(db)).toEqual([]);
    db.close();
  });

  it("keys two offline edits of one customer on separate idempotency keys", async () => {
    const userId = "user-customer-update";
    const { db, scopeBinding } = await openScoped(userId);
    const firstEdit = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1";
    const secondEdit = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee2";

    const first = await enqueueOfflineCustomerUpdate({
      ...scope(db, scopeBinding, userId),
      customerId,
      operationId: firstEdit,
      customer: { displayName: "Juan v2", expectedUpdatedAtUtc: "2026-08-02T00:00:00Z" },
    }, { allowOfflineEngine: true });
    const second = await enqueueOfflineCustomerUpdate({
      ...scope(db, scopeBinding, userId),
      customerId,
      operationId: secondEdit,
      customer: { displayName: "Juan v3", expectedUpdatedAtUtc: "2026-08-02T00:00:00Z" },
    }, { allowOfflineEngine: true });

    expect(first.idempotencyKey).not.toBe(second.idempotencyKey);
    expect(first.entityLocalId).toBe(customerId);
    expect(second.entityLocalId).toBe(customerId);
    expect((await listOutbox(db)).length).toBe(2);

    const request = await decryptRequest(second, scopeBinding);
    expect(request).toMatchObject({
      method: "PUT",
      path: `/api/v1/pos/customers/${customerId}`,
    });
    expect(request?.body).toMatchObject({
      displayName: "Juan v3",
      expectedUpdatedAtUtc: "2026-08-02T00:00:00Z",
    });
    db.close();
  });

  it("queues a repayment with a client-chosen repayment id so a replay records one payment", async () => {
    const userId = "user-repayment";
    const { db, scopeBinding } = await openScoped(userId);
    const repaymentId = "ffffffff-ffff-4fff-8fff-ffffffffffff";

    const record = await enqueueOfflineCustomerRepayment({
      ...scope(db, scopeBinding, userId),
      customerId,
      repaymentId,
      repayment: { amount: 10.005, remarks: "  Partial  " },
    }, { allowOfflineEngine: true });

    expect(record.operationType).toBe("repayment.create");
    expect(record.idempotencyKey).toBe(posIdempotencyKeyForEntity(repaymentId));

    const request = await decryptRequest(record, scopeBinding);
    expect(request).toMatchObject({
      method: "POST",
      path: `/api/v1/pos/customers/${customerId}/repayments`,
    });
    expect(request?.body).toMatchObject({ amount: 10.01, remarks: "Partial", repaymentId });
    db.close();
  });

  it("rejects a non-positive repayment", async () => {
    const userId = "user-repayment-invalid";
    const { db, scopeBinding } = await openScoped(userId);
    const repaymentId = "ffffffff-ffff-4fff-8fff-fffffffffff1";
    const base = {
      ...scope(db, scopeBinding, userId),
      customerId,
      repaymentId,
    };

    await expect(
      enqueueOfflineCustomerRepayment({ ...base, repayment: { amount: 0 } }, { allowOfflineEngine: true }),
    ).rejects.toMatchObject({ code: "offline.repayment.amount_invalid" });
    await expect(
      enqueueOfflineCustomerRepayment({ ...base, repayment: { amount: -5 } }, { allowOfflineEngine: true }),
    ).rejects.toMatchObject({ code: "offline.repayment.amount_invalid" });
    await expect(
      enqueueOfflineCustomerRepayment({
        ...base,
        customerId: "  ",
        repayment: { amount: 5 },
      }, { allowOfflineEngine: true }),
    ).rejects.toMatchObject({ code: "offline.repayment.customer_required" });

    expect(await listOutbox(db)).toEqual([]);
    db.close();
  });

  it("keeps queued customer and payment plaintext out of safe sync metadata", async () => {
    const userId = "user-customer-metadata";
    const { db, scopeBinding } = await openScoped(userId);

    await enqueueOfflineCustomerCreate({
      ...scope(db, scopeBinding, userId),
      customerId,
      customer: { displayName: "Sensitive Name", mobileNumber: "09991112222", notes: "Kapitbahay" },
    }, { allowOfflineEngine: true });

    const metadata = await listSafeOutboxMetadata(db);
    const serialized = JSON.stringify(metadata);
    expect(serialized).not.toContain("Sensitive Name");
    expect(serialized).not.toContain("09991112222");
    expect(serialized).not.toContain("Kapitbahay");
    expect(metadata[0]?.operationType).toBe("customer.create");
    db.close();
  });
});
