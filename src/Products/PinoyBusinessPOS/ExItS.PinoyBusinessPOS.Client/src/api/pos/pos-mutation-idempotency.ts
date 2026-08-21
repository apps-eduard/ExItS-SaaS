/**
 * Mirrors MAUI PosMutationIdempotencyHelper used by sales/returns/PO clients.
 * Headers are optional on the server; when present both key + payload hash are required.
 */

export const IDEMPOTENCY_KEY_HEADER = "Idempotency-Key";
export const PAYLOAD_HASH_HEADER = "X-Pos-Payload-Hash";
export const OPERATION_ID_HEADER = "X-Pos-Operation-Id";
export const OPERATION_TYPE_HEADER = "X-Pos-Operation-Type";

export const OFFLINE_OPERATION_TYPES = {
  SaleCheckout: "sale.checkout",
  SaleReturnCreate: "sale_return.create",
  PurchaseOrderSubmit: "purchase_order.submit",
  PurchaseOrderReceive: "purchase_order.receive",
  CustomerOrderPlace: "customer_order.place",
  CustomerOrderAccept: "customer_order.accept",
  CustomerOrderReject: "customer_order.reject",
  CustomerOrderComplete: "customer_order.complete",
} as const;

function guidToN(guid: string): string {
  return guid.replace(/-/g, "").toLowerCase();
}

function guidToD(guid: string): string {
  return guid.toLowerCase();
}

export async function sha256Hex(payload: string): Promise<string> {
  const data = new TextEncoder().encode(payload);
  const digest = await crypto.subtle.digest("SHA-256", data);
  return Array.from(new Uint8Array(digest))
    .map((b) => b.toString(16).padStart(2, "0"))
    .join("");
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
