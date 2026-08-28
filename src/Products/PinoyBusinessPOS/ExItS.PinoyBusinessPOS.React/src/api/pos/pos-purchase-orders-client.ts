import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";

const PURCHASE_ORDERS_PATH = "/api/v1/pos/purchase-orders";
const GOODS_RECEIPTS_PATH = "/api/v1/pos/goods-receipts";

/** Paths that touch inventory stock. PO create/submit/cancel/accept must never hit these. */
export const STOCK_TOUCHING_PATH_MARKERS = [
  "/receive",
  "/goods-receipts",
  "/direct-purchase-receipts",
  "/inventory",
  "/stock-counts",
] as const;

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const posPurchaseOrderLineDtoSchema = z.object({
  lineId: guidSchema,
  productId: guidSchema,
  lineNumber: z.number(),
  nameSnapshot: z.string().nullable().optional(),
  uomSnapshot: z.string().nullable().optional(),
  orderedQty: z.number(),
  unitPurchaseCost: z.number(),
  lineTotal: z.number(),
  receivedQty: z.number(),
  outstandingQty: z.number(),
  lineNotes: z.string().nullable().optional(),
  closedShortQty: z.number().optional(),
  tracksExpiration: z.boolean().optional(),
});

export const connectedPurchaseOrderLineDtoSchema = z
  .object({
    lineNumber: z.number().optional(),
    productId: guidSchema.optional(),
    nameSnapshot: z.string().nullable().optional(),
    orderedQty: z.number().optional(),
    unitPurchaseCost: z.number().optional(),
    proposedOrderedQty: z.number().nullable().optional(),
    proposedUnitPurchaseCost: z.number().nullable().optional(),
  })
  .passthrough();

export const posPurchaseOrderDtoSchema = z.object({
  purchaseOrderId: guidSchema,
  organizationId: guidSchema,
  poNumber: z.string().nullable().optional(),
  supplierId: guidSchema,
  status: z.string(),
  orderDate: z.string(),
  expectedDeliveryDate: z.string().nullable().optional(),
  supplierReference: z.string().nullable().optional(),
  notes: z.string().nullable().optional(),
  orderedAtUtc: z.string().nullable().optional(),
  orderedBy: guidSchema.nullable().optional(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  lines: z.array(posPurchaseOrderLineDtoSchema),
  displayStatus: z.string().optional(),
  connectedStatus: z.string().nullable().optional(),
  connectedPurchaseOrderId: guidSchema.nullable().optional(),
  supplierAcceptedAtUtc: z.string().nullable().optional(),
  supplierDeclinedAtUtc: z.string().nullable().optional(),
  supplierPreparingAtUtc: z.string().nullable().optional(),
  supplierFulfilledAtUtc: z.string().nullable().optional(),
  withdrawnAtUtc: z.string().nullable().optional(),
  declineReason: z.string().nullable().optional(),
  declineNote: z.string().nullable().optional(),
  hasReceivingIssues: z.boolean().optional(),
  canWithdrawConnected: z.boolean().optional(),
  canReceiveConnected: z.boolean().optional(),
  paymentTerm: z.string().optional(),
  paymentTermLabel: z.string().optional(),
  proposedTotalAmount: z.number().nullable().optional(),
  confirmedTotalAmount: z.number().nullable().optional(),
  connectedLines: z.array(connectedPurchaseOrderLineDtoSchema).nullable().optional(),
  changesProposedAtUtc: z.string().nullable().optional(),
  supplierName: z.string().nullable().optional(),
});

export const posGoodsReceiptLineDtoSchema = z.object({
  lineId: guidSchema,
  purchaseOrderLineId: guidSchema,
  productId: guidSchema,
  lineNumber: z.number(),
  nameSnapshot: z.string(),
  uomSnapshot: z.string(),
  quantityReceived: z.number(),
  unitPurchaseCostSnapshot: z.number(),
  lineTotalSnapshot: z.number(),
  inventoryMovementId: guidSchema.nullable().optional(),
  damagedQty: z.number().optional(),
  rejectedQty: z.number().optional(),
  shortClosedQty: z.number().optional(),
  discrepancyKind: z.string().optional(),
  discrepancyNote: z.string().nullable().optional(),
  receivedQty: z.number().optional(),
  expiryDate: z.string().nullable().optional(),
  lotNumber: z.string().nullable().optional(),
});

export const posGoodsReceiptDtoSchema = z.object({
  goodsReceiptId: guidSchema,
  organizationId: guidSchema,
  purchaseOrderId: guidSchema,
  supplierId: guidSchema,
  grnNumber: z.string(),
  receivedDate: z.string(),
  deliveryReference: z.string().nullable().optional(),
  notes: z.string().nullable().optional(),
  receivedAtUtc: z.string(),
  receivedBy: guidSchema,
  lines: z.array(posGoodsReceiptLineDtoSchema),
});

export const posPurchaseOrderPagedResultSchema = z.object({
  items: z.array(posPurchaseOrderDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type PosPurchaseOrderLineDto = z.infer<typeof posPurchaseOrderLineDtoSchema>;
export type PosPurchaseOrderDto = z.infer<typeof posPurchaseOrderDtoSchema>;
export type PosGoodsReceiptLineDto = z.infer<typeof posGoodsReceiptLineDtoSchema>;
export type PosGoodsReceiptDto = z.infer<typeof posGoodsReceiptDtoSchema>;
export type PosPurchaseOrderPagedResult = z.infer<typeof posPurchaseOrderPagedResultSchema>;

export type CreatePurchaseOrderLineRequest = {
  productId: string;
  orderedQty: number;
  unitPurchaseCost: number;
  lineNotes?: string | null;
  purchaseUnitId?: string | null;
};

export type CreatePurchaseOrderRequest = {
  supplierId: string;
  orderDate: string;
  lines: CreatePurchaseOrderLineRequest[];
  expectedDeliveryDate?: string | null;
  supplierReference?: string | null;
  notes?: string | null;
  paymentTerm?: string | null;
  purchaseOrderId?: string | null;
};

export type UpdatePurchaseOrderRequest = CreatePurchaseOrderRequest & {
  expectedUpdatedAtUtc: string;
};

export type ReceivePurchaseOrderLineRequest = {
  productId: string;
  receiveQty: number;
  damagedQty?: number;
  rejectedQty?: number;
  shortClosedQty?: number;
  discrepancyKind?: string | null;
  discrepancyNote?: string | null;
  expiryDate?: string | null;
  lotNumber?: string | null;
};

export type ReceivePurchaseOrderRequest = {
  lines: ReceivePurchaseOrderLineRequest[];
  goodsReceiptId?: string | null;
  receivedDate?: string | null;
  deliveryReference?: string | null;
  notes?: string | null;
};

export type ListPurchaseOrdersOptions = {
  status?: string;
  supplierId?: string;
  poNumber?: string;
  fromOrderDate?: string;
  toOrderDate?: string;
  page?: number;
  pageSize?: number;
};

function appendQuery(
  path: string,
  params: Record<string, string | number | boolean | undefined>,
): string {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") {
      query.set(key, String(value));
    }
  }
  const serialized = query.toString();
  return serialized ? `${path}?${serialized}` : path;
}

function trimOrUndef(value: string | null | undefined): string | undefined {
  const trimmed = value?.trim();
  return trimmed ? trimmed : undefined;
}

function serializeCreateLines(lines: CreatePurchaseOrderLineRequest[]): Record<string, unknown>[] {
  return lines.map((line) => {
    const entry: Record<string, unknown> = {
      productId: line.productId,
      orderedQty: line.orderedQty,
      unitPurchaseCost: line.unitPurchaseCost,
    };
    const notes = trimOrUndef(line.lineNotes);
    if (notes) {
      entry.lineNotes = notes;
    }
    if (line.purchaseUnitId) {
      entry.purchaseUnitId = line.purchaseUnitId;
    }
    return entry;
  });
}

function serializeCreateBody(body: CreatePurchaseOrderRequest): Record<string, unknown> {
  const payload: Record<string, unknown> = {
    supplierId: body.supplierId,
    orderDate: body.orderDate,
    lines: serializeCreateLines(body.lines),
  };
  if (body.expectedDeliveryDate) {
    payload.expectedDeliveryDate = body.expectedDeliveryDate;
  }
  const ref = trimOrUndef(body.supplierReference);
  if (ref) {
    payload.supplierReference = ref;
  }
  const notes = trimOrUndef(body.notes);
  if (notes) {
    payload.notes = notes;
  }
  if (body.paymentTerm) {
    payload.paymentTerm = body.paymentTerm;
  }
  if (body.purchaseOrderId) {
    payload.purchaseOrderId = body.purchaseOrderId;
  }
  return payload;
}

function serializeReceiveBody(body: ReceivePurchaseOrderRequest): Record<string, unknown> {
  const payload: Record<string, unknown> = {
    lines: body.lines.map((line) => {
      const entry: Record<string, unknown> = {
        productId: line.productId,
        receiveQty: line.receiveQty,
      };
      if (line.damagedQty !== undefined) {
        entry.damagedQty = line.damagedQty;
      }
      if (line.rejectedQty !== undefined) {
        entry.rejectedQty = line.rejectedQty;
      }
      if (line.shortClosedQty !== undefined) {
        entry.shortClosedQty = line.shortClosedQty;
      }
      if (line.discrepancyKind) {
        entry.discrepancyKind = line.discrepancyKind;
      }
      const note = trimOrUndef(line.discrepancyNote);
      if (note) {
        entry.discrepancyNote = note;
      }
      const expiry = trimOrUndef(line.expiryDate);
      if (expiry) {
        entry.expiryDate = expiry;
      }
      const lot = trimOrUndef(line.lotNumber);
      if (lot) {
        entry.lotNumber = lot;
      }
      return entry;
    }),
  };
  if (body.goodsReceiptId) {
    payload.goodsReceiptId = body.goodsReceiptId;
  }
  if (body.receivedDate) {
    payload.receivedDate = body.receivedDate;
  }
  const delivery = trimOrUndef(body.deliveryReference);
  if (delivery) {
    payload.deliveryReference = delivery;
  }
  const notes = trimOrUndef(body.notes);
  if (notes) {
    payload.notes = notes;
  }
  return payload;
}

export function assertNotStockTouchingUrl(url: string): void {
  const lower = url.toLowerCase();
  for (const marker of STOCK_TOUCHING_PATH_MARKERS) {
    if (lower.includes(marker)) {
      throw new Error(`Unexpected stock-touching URL: ${url}`);
    }
  }
}

/** Receivable PO statuses for the receipts hub. */
export function isReceivablePurchaseOrderStatus(status: string): boolean {
  return status === "Ordered" || status === "PartiallyReceived";
}

export function isPurchaseOrderReceivable(po: PosPurchaseOrderDto): boolean {
  return (
    (po.canReceiveConnected ?? true) &&
    isReceivablePurchaseOrderStatus(po.status) &&
    po.lines.some((line) => line.outstandingQty > 0)
  );
}

export async function listPurchaseOrders(
  workspace: PosWorkspaceScope,
  options: ListPurchaseOrdersOptions = {},
  signal?: AbortSignal,
): Promise<PosPurchaseOrderPagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(PURCHASE_ORDERS_PATH, {
      status: options.status,
      supplierId: options.supplierId,
      poNumber: options.poNumber,
      fromOrderDate: options.fromOrderDate,
      toOrderDate: options.toOrderDate,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
    }),
  });
  return posPurchaseOrderPagedResultSchema.parse(raw);
}

export async function getPurchaseOrder(
  workspace: PosWorkspaceScope,
  purchaseOrderId: string,
  signal?: AbortSignal,
): Promise<PosPurchaseOrderDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${PURCHASE_ORDERS_PATH}/${purchaseOrderId}`,
  });
  return posPurchaseOrderDtoSchema.parse(raw);
}

export async function createPurchaseOrder(
  workspace: PosWorkspaceScope,
  body: CreatePurchaseOrderRequest,
  signal?: AbortSignal,
): Promise<PosPurchaseOrderDto> {
  if (!body.purchaseOrderId) {
    throw new Error("purchaseOrderId is required for create purchase order idempotency.");
  }
  const payload = serializeCreateBody(body);
  const headers = await buildPosMutationIdempotencyHeaders(
    body.purchaseOrderId,
    JSON.stringify(payload),
    OFFLINE_OPERATION_TYPES.PurchaseOrderCreate,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: PURCHASE_ORDERS_PATH,
    body: payload,
    headers,
  });
  return posPurchaseOrderDtoSchema.parse(raw);
}

export async function updatePurchaseOrder(
  workspace: PosWorkspaceScope,
  purchaseOrderId: string,
  body: UpdatePurchaseOrderRequest,
  signal?: AbortSignal,
): Promise<PosPurchaseOrderDto> {
  const payload = {
    ...serializeCreateBody(body),
    expectedUpdatedAtUtc: body.expectedUpdatedAtUtc,
  };
  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: `${PURCHASE_ORDERS_PATH}/${purchaseOrderId}`,
    body: payload,
  });
  return posPurchaseOrderDtoSchema.parse(raw);
}

export async function submitPurchaseOrder(
  workspace: PosWorkspaceScope,
  purchaseOrderId: string,
  signal?: AbortSignal,
): Promise<PosPurchaseOrderDto> {
  const headers = await buildPosMutationIdempotencyHeaders(
    purchaseOrderId,
    "{}",
    OFFLINE_OPERATION_TYPES.PurchaseOrderSubmit,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PURCHASE_ORDERS_PATH}/${purchaseOrderId}/submit`,
    body: {},
    headers,
  });
  return posPurchaseOrderDtoSchema.parse(raw);
}

export async function cancelPurchaseOrder(
  workspace: PosWorkspaceScope,
  purchaseOrderId: string,
  signal?: AbortSignal,
): Promise<PosPurchaseOrderDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PURCHASE_ORDERS_PATH}/${purchaseOrderId}/cancel`,
  });
  return posPurchaseOrderDtoSchema.parse(raw);
}

export async function acceptConnectedPurchaseOrderChanges(
  workspace: PosWorkspaceScope,
  purchaseOrderId: string,
  signal?: AbortSignal,
): Promise<PosPurchaseOrderDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PURCHASE_ORDERS_PATH}/${purchaseOrderId}/accept-changes`,
  });
  return posPurchaseOrderDtoSchema.parse(raw);
}

/**
 * Goods receipt — the only PO client method that increases inventory.
 * Always send a client-generated goodsReceiptId for idempotency (MAUI pattern).
 */
export async function receivePurchaseOrder(
  workspace: PosWorkspaceScope,
  purchaseOrderId: string,
  body: ReceivePurchaseOrderRequest,
  signal?: AbortSignal,
): Promise<PosGoodsReceiptDto> {
  const goodsReceiptId = body.goodsReceiptId ?? crypto.randomUUID();
  const payload = serializeReceiveBody({ ...body, goodsReceiptId });
  const headers = await buildPosMutationIdempotencyHeaders(
    goodsReceiptId,
    JSON.stringify(payload),
    OFFLINE_OPERATION_TYPES.PurchaseOrderReceive,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PURCHASE_ORDERS_PATH}/${purchaseOrderId}/receive`,
    body: payload,
    headers,
  });
  return posGoodsReceiptDtoSchema.parse(raw);
}

export async function getGoodsReceipt(
  workspace: PosWorkspaceScope,
  goodsReceiptId: string,
  signal?: AbortSignal,
): Promise<PosGoodsReceiptDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${GOODS_RECEIPTS_PATH}/${goodsReceiptId}`,
  });
  return posGoodsReceiptDtoSchema.parse(raw);
}

/** Client methods that must never be treated as stock-increasing. */
export const NON_STOCK_PURCHASE_ORDER_METHODS = [
  "listPurchaseOrders",
  "getPurchaseOrder",
  "createPurchaseOrder",
  "updatePurchaseOrder",
  "submitPurchaseOrder",
  "cancelPurchaseOrder",
  "acceptConnectedPurchaseOrderChanges",
  "getGoodsReceipt",
] as const;

export const STOCK_TOUCHING_PURCHASE_ORDER_METHODS = ["receivePurchaseOrder"] as const;
