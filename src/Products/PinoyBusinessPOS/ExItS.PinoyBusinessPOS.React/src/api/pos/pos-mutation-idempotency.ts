/**
 * Mirrors MAUI PosMutationIdempotencyHelper used by sales/returns/PO clients.
 * Headers are optional on the server; when present both key + payload hash are required.
 */

import { sha256 as nobleSha256 } from "@noble/hashes/sha256";
import { bytesToHex } from "@noble/hashes/utils";
import { isWebCryptoSubtleAvailable } from "@/lib/web-crypto-capability";

export const IDEMPOTENCY_KEY_HEADER = "Idempotency-Key";
export const PAYLOAD_HASH_HEADER = "X-Pos-Payload-Hash";
export const OPERATION_ID_HEADER = "X-Pos-Operation-Id";
export const OPERATION_TYPE_HEADER = "X-Pos-Operation-Type";

export const OFFLINE_OPERATION_TYPES = {
  SaleCheckout: "sale.checkout",
  SaleReturnCreate: "sale_return.create",
  CustomerCreate: "customer.create",
  CustomerUpdate: "customer.update",
  RepaymentCreate: "repayment.create",
  PurchaseOrderCreate: "purchase_order.create",
  PurchaseOrderSubmit: "purchase_order.submit",
  PurchaseOrderReceive: "purchase_order.receive",
  GoodsReceiptVoid: "goods_receipt.void",
  DirectPurchaseReceiptVoid: "direct_purchase_receipt.void",
  InventoryAdjustment: "inventory.adjustment",
  StockUse: "inventory.stock_use",
  WasteLoss: "inventory.waste_loss",
  ProductionRun: "inventory.production_run",
  InventoryTransferCreate: "inventory_transfer.create",
  InventoryTransferDispatch: "inventory_transfer.dispatch",
  InventoryTransferReceive: "inventory_transfer.receive",
  InventoryTransferCancel: "inventory_transfer.cancel",
  CustomerOrderPlace: "customer_order.place",
  CustomerOrderAccept: "customer_order.accept",
  CustomerOrderReject: "customer_order.reject",
  CustomerOrderComplete: "customer_order.complete",
  /** Online-only expense create — mirrors server PosMutationIdempotencyHelper expense.create. */
  ExpenseCreate: "expense.create",
} as const;

function guidToN(guid: string): string {
  return guid.replace(/-/g, "").toLowerCase();
}

function guidToD(guid: string): string {
  return guid.toLowerCase();
}

function sha256HexFallback(payload: string): string {
  // Standards-correct SHA-256 for non-secure contexts (e.g. plain HTTP Tailscale LV)
  // where crypto.subtle is undefined. Must match Web Crypto output exactly.
  return bytesToHex(nobleSha256(new TextEncoder().encode(payload)));
}

export async function sha256Hex(payload: string): Promise<string> {
  if (isWebCryptoSubtleAvailable()) {
    const data = new TextEncoder().encode(payload);
    const digest = await crypto.subtle.digest("SHA-256", data);
    return Array.from(new Uint8Array(digest))
      .map((b) => b.toString(16).padStart(2, "0"))
      .join("");
  }
  return sha256HexFallback(payload);
}

/**
 * Idempotency key for an entity id, in the same `N` format the server matches.
 * Offline queued operations must reuse this so a replayed sale is deduplicated.
 */
export function posIdempotencyKeyForEntity(entityId: string): string {
  return guidToN(entityId);
}

/** Build idempotency headers matching MAUI PosMutationIdempotencyHelper.BuildHeaders(entityId, json, opType). */
export async function buildPosMutationIdempotencyHeaders(
  entityId: string,
  payloadJson: string,
  operationType: string,
): Promise<Record<string, string>> {
  const hash = await sha256Hex(payloadJson);
  return {
    [IDEMPOTENCY_KEY_HEADER]: guidToN(entityId),
    [PAYLOAD_HASH_HEADER]: hash,
    [OPERATION_ID_HEADER]: guidToD(entityId),
    [OPERATION_TYPE_HEADER]: operationType,
  };
}
