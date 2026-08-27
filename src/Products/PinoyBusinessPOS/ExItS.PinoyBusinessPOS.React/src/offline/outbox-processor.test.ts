import "fake-indexeddb/auto";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { OFFLINE_OPERATION_TYPES } from "@/api/pos/pos-mutation-idempotency";
import { PosApiError } from "@/api/pos/pos-http";
import { organizationScopeKey, openOfflineDatabase } from "@/offline/db";
import { enqueueOfflineCashSale } from "@/offline/cash-sale-offline";
import { drainOutbox, processNextOutboxOperation } from "@/offline/outbox-processor";
import { getOutboxCounts, listOutbox, setOperationState } from "@/offline/outbox";
import { enqueueEncryptedOperation } from "@/offline/outbox";
import { serializeQueuedRequest } from "@/offline/queued-request";
import { PERSONAL_OPERATION_TYPES } from "@/offline/server-dedupe-policy";
import { personalScopeKey } from "@/offline/db";
import { mockLeasedCheckoutLine, mockPriceAuthority } from "@/test/mock-price-authority";

vi.mock("@/api/pos/pos-http", async () => {
  const actual = await vi.importActual<typeof import("@/api/pos/pos-http")>("@/api/pos/pos-http");
  return {
    ...actual,
    posRequest: vi.fn(),
  };
});

vi.mock("@/api/platform/platform-http", async () => {
  const actual = await vi.importActual<typeof import("@/api/platform/platform-http")>(
    "@/api/platform/platform-http",
  );
  return {
    ...actual,
    platformRequest: vi.fn(),
  };
});

import { posRequest } from "@/api/pos/pos-http";
import { platformRequest } from "@/api/platform/platform-http";

const mockedPosRequest = vi.mocked(posRequest);
const mockedPlatformRequest = vi.mocked(platformRequest);

describe("outbox processor", () => {
  beforeEach(() => {
    mockedPosRequest.mockReset();
    mockedPlatformRequest.mockReset();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("replays a queued Cash sale with Idempotency-Key and marks Succeeded", async () => {
    const orgKey = organizationScopeKey({
      userId: "user-1",
      organizationId: "org-1",
      branchId: "branch-1",
      installationDeviceId: "install-1",
    });
    const db = await openOfflineDatabase("Organization", orgKey);
    const saleId = "11111111-1111-4111-8111-111111111111";

    await enqueueOfflineCashSale({
      db,
      scopeBinding: orgKey,
      userId: "user-1",
      organizationId: "org-1",
      branchId: "branch-1",
      installationDeviceId: "install-1",
      saleId,
      shiftId: "22222222-2222-4222-8222-222222222222",
      lines: [
        mockLeasedCheckoutLine(
          mockPriceAuthority({
            productId: "33333333-3333-4333-8333-333333333333",
            unitPrice: 100,
          }),
          1,
        ),
      ],
      amountTendered: 100,
    }, { allowOfflineEngine: true });

    mockedPosRequest.mockResolvedValueOnce({ saleId, total: 100 });

    const result = await processNextOutboxOperation(db, orgKey);
    expect(result).toEqual({ status: "succeeded", operationId: saleId });
    expect(mockedPosRequest).toHaveBeenCalledTimes(1);
    const call = mockedPosRequest.mock.calls[0]?.[0];
    expect(call?.path).toBe("/api/v1/pos/sales");
    expect(call?.headers?.["Idempotency-Key"]).toBe(saleId.replace(/-/g, "").toLowerCase());
    expect(call?.headers?.["X-Pos-Operation-Type"]).toBe(OFFLINE_OPERATION_TYPES.SaleCheckout);

    const [row] = await listOutbox(db);
    expect(row.queueState).toBe("Succeeded");
    expect(JSON.stringify(row)).not.toContain("amountTendered");
    db.close();
  });

  it("flags a Conflict when the server records a different total than the cashier collected", async () => {
    const orgKey = organizationScopeKey({
      userId: "user-mismatch",
      organizationId: "org-mismatch",
      branchId: "branch-mismatch",
      installationDeviceId: "install-mismatch",
    });
    const db = await openOfflineDatabase("Organization", orgKey);
    const saleId = "aaaaaaaa-1111-4111-8111-111111111111";

    await enqueueOfflineCashSale({
      db,
      scopeBinding: orgKey,
      userId: "user-mismatch",
      organizationId: "org-mismatch",
      branchId: "branch-mismatch",
      installationDeviceId: "install-mismatch",
      saleId,
      shiftId: "bbbbbbbb-2222-4222-8222-222222222222",
      lines: [
        mockLeasedCheckoutLine(
          mockPriceAuthority({
            productId: "cccccccc-3333-4333-8333-333333333333",
            unitPrice: 100,
          }),
          1,
        ),
      ],
      amountTendered: 100,
    }, { allowOfflineEngine: true });

    // 80 is what live repricing would have recorded. The customer paid 100.
    mockedPosRequest.mockResolvedValueOnce({ saleId, total: 80 });

    const result = await processNextOutboxOperation(db, orgKey);
    expect(result).toMatchObject({ status: "failed", queueState: "Conflict" });

    const [row] = await listOutbox(db);
    expect(row.queueState).toBe("Conflict");
    expect(row.failureCode).toBe("offline.sale.total_mismatch");
    db.close();
  });

  it("auto-retries ambiguous Personal create when server entity-id dedupe is available", async () => {
    const personalKey = personalScopeKey("user-p");
    const db = await openOfflineDatabase("Personal", personalKey);
    await enqueueEncryptedOperation({
      db,
      scopeKind: "Personal",
      scopeBinding: personalKey,
      userId: "user-p",
      productDomain: "personal.utang",
      operationType: PERSONAL_OPERATION_TYPES.ContactCreate,
      operationId: "op-contact-1",
      idempotencyKey: "opcontact1",
      plaintextJson: serializeQueuedRequest({
        api: "platform",
        method: "POST",
        path: "/api/v1/personal/utang/contacts",
        body: { contactId: "local-contact-1", displayName: "Ana" },
      }),
      entityLocalId: "local-contact-1",
    });

    mockedPlatformRequest.mockRejectedValueOnce(new Error("Failed to fetch"));

    const result = await processNextOutboxOperation(db, personalKey);
    expect(result.status).toBe("failed");
    if (result.status === "failed") {
      expect(result.queueState).toBe("RetryableFailure");
    }
    const [row] = await listOutbox(db);
    expect(row.failureCode).toBe("offline.ambiguous_transport");
    db.close();
  });

  it("marks 403 as BlockedByAccess", async () => {
    const orgKey = organizationScopeKey({
      userId: "user-2",
      organizationId: "org-2",
      branchId: "branch-2",
      installationDeviceId: "install-2",
    });
    const db = await openOfflineDatabase("Organization", orgKey);
    const saleId = "44444444-4444-4444-8444-444444444444";
    await enqueueOfflineCashSale({
      db,
      scopeBinding: orgKey,
      userId: "user-2",
      organizationId: "org-2",
      branchId: "branch-2",
      installationDeviceId: "install-2",
      saleId,
      shiftId: "55555555-5555-4555-8555-555555555555",
      lines: [
        mockLeasedCheckoutLine(
          mockPriceAuthority({ productId: "66666666-6666-4666-8666-666666666666", unitPrice: 50 }),
          1,
        ),
      ],
      amountTendered: 50,
    }, { allowOfflineEngine: true });

    mockedPosRequest.mockRejectedValueOnce(
      new PosApiError(403, { title: "Forbidden", status: 403 }),
    );

    const result = await processNextOutboxOperation(db, orgKey);
    expect(result).toMatchObject({ status: "failed", queueState: "BlockedByAccess" });
    db.close();
  });

  it("recovers abandoned Syncing then drains", async () => {
    const orgKey = organizationScopeKey({
      userId: "user-3",
      organizationId: "org-3",
      branchId: "branch-3",
      installationDeviceId: "install-3",
    });
    const db = await openOfflineDatabase("Organization", orgKey);
    const saleId = "77777777-7777-4777-8777-777777777777";
    await enqueueOfflineCashSale({
      db,
      scopeBinding: orgKey,
      userId: "user-3",
      organizationId: "org-3",
      branchId: "branch-3",
      installationDeviceId: "install-3",
      saleId,
      shiftId: "88888888-8888-4888-8888-888888888888",
      lines: [
        mockLeasedCheckoutLine(
          mockPriceAuthority({ productId: "99999999-9999-4999-8999-999999999999", unitPrice: 10 }),
          2,
        ),
      ],
      amountTendered: 20,
    }, { allowOfflineEngine: true });
    await setOperationState(db, saleId, { queueState: "Syncing" });
    mockedPosRequest.mockResolvedValueOnce({ saleId, total: 20 });

    const drained = await drainOutbox(db, orgKey, 5);
    expect(drained.succeeded).toBe(1);
    expect(isFullySyncedLike(await getOutboxCounts(db))).toBe(true);
    db.close();
  });
});

function isFullySyncedLike(counts: Awaited<ReturnType<typeof getOutboxCounts>>): boolean {
  return (
    counts.pending === 0 &&
    counts.syncing === 0 &&
    counts.retryableFailure === 0 &&
    counts.permanentFailure === 0 &&
    counts.conflict === 0 &&
    counts.blockedByAccess === 0
  );
}
