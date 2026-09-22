import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";

const RETURN_BATCHES_PATH = "/api/v1/pos/return-batches";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const RETURN_BATCH_STATUSES = [
  "AwaitingSellerReceipt",
  "PendingInspection",
  "ReadyForFinalize",
  "Finalized",
] as const;
export type ReturnBatchStatus = (typeof RETURN_BATCH_STATUSES)[number];

export const RETURN_BATCH_SOURCE_TYPES = ["Sale", "ConnectedPurchaseOrder"] as const;
export type ReturnBatchSourceType = (typeof RETURN_BATCH_SOURCE_TYPES)[number];

export const RETURN_BATCH_REFUND_STATUSES = [
  "None",
  "RefundDue",
  "Refunded",
  "ObligationReduced",
  "CreditReduced",
] as const;
export type ReturnBatchRefundStatus = (typeof RETURN_BATCH_REFUND_STATUSES)[number];

export const returnBatchLineDtoSchema = z.object({
  returnBatchLineId: guidSchema,
  saleLineId: guidSchema.nullable().optional(),
  purchaseOrderLineId: guidSchema.nullable().optional(),
  supplierProductId: guidSchema.nullable().optional(),
  productId: guidSchema,
  productNameSnapshot: z.string(),
  unitOfMeasure: z.string(),
  unitPriceSnapshot: z.number(),
  lineTotalSnapshot: z.number(),
  acceptedQuantity: z.number(),
  refundAmountSnapshot: z.number(),
  sellableQuantity: z.number().nullable().optional(),
  damagedQuantity: z.number().nullable().optional(),
  inspectionNote: z.string().nullable().optional(),
  classifiedAtUtc: z.string().nullable().optional(),
  classifiedBy: guidSchema.nullable().optional(),
});

export const returnBatchRefundDtoSchema = z.object({
  returnBatchRefundId: guidSchema,
  amount: z.number(),
  method: z.string(),
  reference: z.string().nullable().optional(),
  note: z.string().nullable().optional(),
  clientRefundId: z.string().nullable().optional(),
  createdAtUtc: z.string(),
  createdBy: guidSchema,
});

export const returnBatchTimelineEventDtoSchema = z.object({
  returnBatchAuditEventId: guidSchema,
  eventType: z.string(),
  payloadJson: z.string(),
  createdAtUtc: z.string(),
  createdBy: guidSchema,
});

export const returnBatchFinancialSummaryDtoSchema = z.object({
  originalSaleTotal: z.number(),
  acceptedReturnValue: z.number(),
  amountPreviouslyPaid: z.number(),
  obligationReduced: z.number(),
  remainingAmountDue: z.number(),
  refundDue: z.number(),
  refunded: z.number(),
  refundRemaining: z.number(),
  returnedQuantity: z.number(),
  sellableQuantity: z.number(),
  damagedQuantity: z.number(),
});

export const returnBatchDtoSchema = z.object({
  returnBatchId: guidSchema,
  organizationId: guidSchema,
  saleId: guidSchema.nullable().optional(),
  sourceType: z.enum(RETURN_BATCH_SOURCE_TYPES).default("Sale"),
  purchaseOrderId: guidSchema.nullable().optional(),
  connectedPurchaseOrderId: guidSchema.nullable().optional(),
  buyerOrganizationId: guidSchema.nullable().optional(),
  sellerOrganizationId: guidSchema.nullable().optional(),
  buyerBranchId: guidSchema.nullable().optional(),
  sellerBranchId: guidSchema.nullable().optional(),
  paymentTiming: z.string().nullable().optional(),
  poNumberSnapshot: z.string().nullable().optional(),
  sellerReceivedAtUtc: z.string().nullable().optional(),
  sellerReceivedBy: guidSchema.nullable().optional(),
  branchId: guidSchema.nullable().optional(),
  batchNumber: z.string(),
  status: z.enum(RETURN_BATCH_STATUSES),
  refundStatus: z.enum(RETURN_BATCH_REFUND_STATUSES),
  acceptedReturnValue: z.number(),
  refundDueAmount: z.number(),
  refundedAmount: z.number(),
  saleReturnId: guidSchema.nullable().optional(),
  reason: z.string(),
  notes: z.string().nullable().optional(),
  createdAtUtc: z.string(),
  createdBy: guidSchema,
  updatedAtUtc: z.string(),
  finalizedAtUtc: z.string().nullable().optional(),
  finalizedBy: guidSchema.nullable().optional(),
  financialSummary: returnBatchFinancialSummaryDtoSchema,
  lines: z.array(returnBatchLineDtoSchema),
  refunds: z.array(returnBatchRefundDtoSchema),
});

export const returnBatchReviewPreviewDtoSchema = z.object({
  returnBatchId: guidSchema,
  batchNumber: z.string(),
  status: z.enum(RETURN_BATCH_STATUSES),
  refundStatus: z.enum(RETURN_BATCH_REFUND_STATUSES),
  acceptedReturnValue: z.number(),
  refundDueAmount: z.number(),
  refundedAmount: z.number(),
  financialSummary: returnBatchFinancialSummaryDtoSchema,
  lines: z.array(returnBatchLineDtoSchema),
});

export const acceptReturnBatchLineRequestSchema = z.object({
  saleLineId: guidSchema,
  acceptedQuantity: z.number().positive(),
});

export const acceptReturnBatchRequestSchema = z.object({
  saleId: guidSchema,
  reason: z.string().min(1),
  lines: z.array(acceptReturnBatchLineRequestSchema).min(1),
  notes: z.string().optional(),
  returnBatchId: guidSchema.optional(),
});

export const classifyReturnBatchLineRequestSchema = z.object({
  sellableQuantity: z.number().nonnegative(),
  damagedQuantity: z.number().nonnegative(),
  inspectionNote: z.string().optional(),
  expectedUpdatedAtUtc: z.string().optional(),
});

export const finalizeReturnBatchRequestSchema = z.object({
  expectedUpdatedAtUtc: z.string(),
});

export const recordReturnBatchRefundRequestSchema = z.object({
  amount: z.number().positive(),
  method: z.string().min(1),
  reference: z.string().optional(),
  note: z.string().optional(),
  clientRefundId: z.string().optional(),
});

export type ReturnBatchLineDto = z.infer<typeof returnBatchLineDtoSchema>;
export type ReturnBatchRefundDto = z.infer<typeof returnBatchRefundDtoSchema>;
export type ReturnBatchTimelineEventDto = z.infer<typeof returnBatchTimelineEventDtoSchema>;
export type ReturnBatchFinancialSummaryDto = z.infer<typeof returnBatchFinancialSummaryDtoSchema>;
export type ReturnBatchDto = z.infer<typeof returnBatchDtoSchema>;
export type ReturnBatchReviewPreviewDto = z.infer<typeof returnBatchReviewPreviewDtoSchema>;
export type AcceptReturnBatchLineRequest = z.infer<typeof acceptReturnBatchLineRequestSchema>;
export type AcceptReturnBatchRequest = z.infer<typeof acceptReturnBatchRequestSchema>;
export type ClassifyReturnBatchLineRequest = z.infer<typeof classifyReturnBatchLineRequestSchema>;
export type FinalizeReturnBatchRequest = z.infer<typeof finalizeReturnBatchRequestSchema>;
export type RecordReturnBatchRefundRequest = z.infer<typeof recordReturnBatchRefundRequestSchema>;

function appendQuery(path: string, params: Record<string, string | number | boolean | undefined>): string {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") {
      query.set(key, String(value));
    }
  }
  const serialized = query.toString();
  return serialized ? `${path}?${serialized}` : path;
}

export async function acceptReturnBatch(
  workspace: PosWorkspaceScope,
  body: AcceptReturnBatchRequest,
  signal?: AbortSignal,
): Promise<ReturnBatchDto> {
  const validated = acceptReturnBatchRequestSchema.parse(body);
  const payload: Record<string, unknown> = {
    saleId: validated.saleId,
    reason: validated.reason.trim(),
    lines: validated.lines.map((line) => ({
      saleLineId: line.saleLineId,
      acceptedQuantity: line.acceptedQuantity,
    })),
  };
  if (validated.notes?.trim()) {
    payload.notes = validated.notes.trim();
  }
  if (validated.returnBatchId) {
    payload.returnBatchId = validated.returnBatchId;
  }

  const headers = validated.returnBatchId
    ? await buildPosMutationIdempotencyHeaders(
        validated.returnBatchId,
        JSON.stringify(payload),
        OFFLINE_OPERATION_TYPES.SaleReturnCreate,
      )
    : undefined;

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: RETURN_BATCHES_PATH,
    body: payload,
    headers,
  });
  return returnBatchDtoSchema.parse(raw);
}

export async function getReturnBatch(
  workspace: PosWorkspaceScope,
  returnBatchId: string,
  signal?: AbortSignal,
): Promise<ReturnBatchDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${RETURN_BATCHES_PATH}/${returnBatchId}`,
  });
  return returnBatchDtoSchema.parse(raw);
}

export async function listReturnBatches(
  workspace: PosWorkspaceScope,
  options: { saleId: string },
  signal?: AbortSignal,
): Promise<ReturnBatchDto[]> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(RETURN_BATCHES_PATH, { saleId: options.saleId }),
  });
  return z.array(returnBatchDtoSchema).parse(raw);
}

export async function classifyReturnBatchLine(
  workspace: PosWorkspaceScope,
  returnBatchId: string,
  returnBatchLineId: string,
  body: ClassifyReturnBatchLineRequest,
  signal?: AbortSignal,
): Promise<ReturnBatchDto> {
  const validated = classifyReturnBatchLineRequestSchema.parse(body);
  const payload: Record<string, unknown> = {
    sellableQuantity: validated.sellableQuantity,
    damagedQuantity: validated.damagedQuantity,
  };
  if (validated.inspectionNote?.trim()) {
    payload.inspectionNote = validated.inspectionNote.trim();
  }
  if (validated.expectedUpdatedAtUtc?.trim()) {
    payload.expectedUpdatedAtUtc = validated.expectedUpdatedAtUtc.trim();
  }

  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: `${RETURN_BATCHES_PATH}/${returnBatchId}/lines/${returnBatchLineId}/classification`,
    body: payload,
  });
  return returnBatchDtoSchema.parse(raw);
}

export async function getReturnBatchReviewPreview(
  workspace: PosWorkspaceScope,
  returnBatchId: string,
  signal?: AbortSignal,
): Promise<ReturnBatchReviewPreviewDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${RETURN_BATCHES_PATH}/${returnBatchId}/review`,
  });
  return returnBatchReviewPreviewDtoSchema.parse(raw);
}

export async function finalizeReturnBatch(
  workspace: PosWorkspaceScope,
  returnBatchId: string,
  body: FinalizeReturnBatchRequest,
  signal?: AbortSignal,
): Promise<ReturnBatchDto> {
  const validated = finalizeReturnBatchRequestSchema.parse(body);
  const payload = {
    expectedUpdatedAtUtc: validated.expectedUpdatedAtUtc,
  };
  const headers = await buildPosMutationIdempotencyHeaders(
    returnBatchId,
    JSON.stringify(payload),
    OFFLINE_OPERATION_TYPES.SaleReturnFinalize,
  );

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${RETURN_BATCHES_PATH}/${returnBatchId}/finalize`,
    body: payload,
    headers,
  });
  return returnBatchDtoSchema.parse(raw);
}

export async function listReturnBatchTimeline(
  workspace: PosWorkspaceScope,
  returnBatchId: string,
  signal?: AbortSignal,
): Promise<ReturnBatchTimelineEventDto[]> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${RETURN_BATCHES_PATH}/${returnBatchId}/timeline`,
  });
  return z.array(returnBatchTimelineEventDtoSchema).parse(raw);
}

export async function recordReturnBatchRefund(
  workspace: PosWorkspaceScope,
  returnBatchId: string,
  body: RecordReturnBatchRefundRequest,
  signal?: AbortSignal,
): Promise<ReturnBatchDto> {
  const validated = recordReturnBatchRefundRequestSchema.parse(body);
  const payload: Record<string, unknown> = {
    amount: validated.amount,
    method: validated.method.trim(),
  };
  if (validated.reference?.trim()) {
    payload.reference = validated.reference.trim();
  }
  if (validated.note?.trim()) {
    payload.note = validated.note.trim();
  }
  if (validated.clientRefundId?.trim()) {
    payload.clientRefundId = validated.clientRefundId.trim();
  }

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${RETURN_BATCHES_PATH}/${returnBatchId}/refunds`,
    body: payload,
  });
  return returnBatchDtoSchema.parse(raw);
}
