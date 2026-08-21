import {
  OFFLINE_OPERATION_TYPES,
  posIdempotencyKeyForEntity,
} from "@/api/pos/pos-mutation-idempotency";
import {
  buildCheckoutSalePayload,
  type CheckoutSaleLineRequest,
  type CommercialDiscountIntentRequest,
  type SalePriceOverrideIntentRequest,
} from "@/api/pos/pos-sales-client";
import type { OfflineDb } from "@/offline/db";
import { enqueueEncryptedOperation } from "@/offline/outbox";
import type { OfflineOperationRecord } from "@/offline/types";

/**
 * Offline Cash sale enqueue (RMAP-21D).
 *
 * Cash is the only offline-capable checkout method. GCash and Utang stay online-only because
 * they need a provider reference or a live customer credit decision. Discounts and price
 * overrides stay online-only because the server owns every money calculation and the
 * capability check behind them — this client must never compute an authorized price offline.
 */

export const POS_SALE_PRODUCT_DOMAIN = "pos.sale";

export type OfflineCashSaleRejectionCode =
  | "offline.sale.discount_not_supported"
  | "offline.sale.price_override_not_supported"
  | "offline.sale.customer_not_supported"
  | "offline.sale.lines_required"
  | "offline.sale.shift_required"
  | "offline.sale.tender_invalid";

export class OfflineCashSaleRejectedError extends Error {
  readonly code: OfflineCashSaleRejectionCode;

  constructor(code: OfflineCashSaleRejectionCode, message: string) {
    super(message);
    this.name = "OfflineCashSaleRejectedError";
    this.code = code;
  }
}

export type EnqueueOfflineCashSaleInput = {
  db: OfflineDb;
  /** Scope binding material for the payload envelope key (organization scope key). */
  scopeBinding: string;
  userId: string;
  organizationId: string;
  branchId: string;
  installationDeviceId: string;
  posDeviceId?: string | null;
  saleId: string;
  shiftId: string;
  lines: ReadonlyArray<CheckoutSaleLineRequest>;
  amountTendered: number;
  /** Present only so an offline attempt carrying them is rejected instead of silently dropped. */
  discounts?: ReadonlyArray<CommercialDiscountIntentRequest>;
  priceOverrides?: ReadonlyArray<SalePriceOverrideIntentRequest>;
  customerId?: string | null;
};

function assertOfflineCashSaleAllowed(input: EnqueueOfflineCashSaleInput): void {
  if (input.discounts && input.discounts.length > 0) {
    throw new OfflineCashSaleRejectedError(
      "offline.sale.discount_not_supported",
      "Commercial discounts require an online checkout.",
    );
  }
  if (input.priceOverrides && input.priceOverrides.length > 0) {
    throw new OfflineCashSaleRejectedError(
      "offline.sale.price_override_not_supported",
      "Price overrides require an online checkout.",
    );
  }
  if (input.customerId) {
    throw new OfflineCashSaleRejectedError(
      "offline.sale.customer_not_supported",
      "Attaching a customer requires an online checkout.",
    );
  }
  if (input.lines.length === 0) {
    throw new OfflineCashSaleRejectedError(
      "offline.sale.lines_required",
      "An offline sale needs at least one line.",
    );
  }
  if (!input.shiftId.trim()) {
    throw new OfflineCashSaleRejectedError(
      "offline.sale.shift_required",
      "An offline sale needs the shift it belongs to.",
    );
  }
  if (!Number.isFinite(input.amountTendered) || input.amountTendered < 0) {
    throw new OfflineCashSaleRejectedError(
      "offline.sale.tender_invalid",
      "Cash tendered must be a non-negative amount.",
    );
  }
}

/**
 * Encrypt and queue one Cash sale. The plaintext envelope is the same body the online
 * checkout posts, and the idempotency key is derived from saleId, so replaying the queued
 * operation cannot double-record the sale.
 *
 * Callers should refresh the Connection & Sync counts (`useOfflineSync().refreshCounts`)
 * once this resolves.
 */
export async function enqueueOfflineCashSale(
  input: EnqueueOfflineCashSaleInput,
): Promise<OfflineOperationRecord> {
  assertOfflineCashSaleAllowed(input);

  const payload = buildCheckoutSalePayload({
    lines: [...input.lines],
    paymentMethod: "Cash",
    saleId: input.saleId,
    shiftId: input.shiftId,
    amountTendered: Number(input.amountTendered.toFixed(2)),
  });

  return enqueueEncryptedOperation({
    db: input.db,
    scopeKind: "Organization",
    scopeBinding: input.scopeBinding,
    userId: input.userId,
    organizationId: input.organizationId,
    branchId: input.branchId,
    installationDeviceId: input.installationDeviceId,
    posDeviceId: input.posDeviceId ?? null,
    productDomain: POS_SALE_PRODUCT_DOMAIN,
    operationType: OFFLINE_OPERATION_TYPES.SaleCheckout,
    operationId: input.saleId,
    idempotencyKey: posIdempotencyKeyForEntity(input.saleId),
    plaintextJson: JSON.stringify(payload),
    entityLocalId: input.saleId,
  });
}
