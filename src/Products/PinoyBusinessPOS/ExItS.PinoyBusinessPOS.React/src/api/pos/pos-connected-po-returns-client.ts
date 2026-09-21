import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import { returnBatchDtoSchema, type ReturnBatchDto } from "@/api/pos/pos-return-batches-client";

const CONNECTED_PO_RETURNS_PATH = "/api/v1/pos/connected-po-returns";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const connectedPoReturnableLineDtoSchema = z.object({
  purchaseOrderLineId: guidSchema,
  productId: guidSchema.nullable().optional(),
  supplierProductId: guidSchema.nullable().optional(),
  productName: z.string(),
  unitOfMeasure: z.string(),
  unitPurchaseCost: z.number(),
  receivedQuantity: z.number(),
  alreadyReturnedQuantity: z.number(),
  returnableQuantity: z.number(),
  returnsAllowed: z.boolean().optional().default(true),
  returnWindowDays: z.number().int().nullable().optional(),
  earliestReturnExpiresAtUtc: z.string().nullable().optional(),
  latestReturnExpiresAtUtc: z.string().nullable().optional(),
  policySource: z.string().nullable().optional(),
  lineBlockedReason: z.string().nullable().optional(),
});

export const connectedPoReturnEligibilityDtoSchema = z.object({
  purchaseOrderId: guidSchema,
  connectedPurchaseOrderId: guidSchema.nullable().optional(),
  poNumber: z.string().nullable().optional(),
  purchaseOrderStatus: z.string(),
  canRequestReturn: z.boolean(),
  blockedReason: z.string().nullable().optional(),
  buyerOrganizationId: guidSchema.nullable().optional(),
  sellerOrganizationId: guidSchema.nullable().optional(),
  paymentTiming: z.string().nullable().optional(),
  goodReceivedValue: z.number(),
  amountPaid: z.number(),
  lines: z.array(connectedPoReturnableLineDtoSchema),
  returns: z.array(returnBatchDtoSchema),
});

export const requestConnectedPoReturnLineRequestSchema = z.object({
  purchaseOrderLineId: guidSchema,
  quantity: z.number().positive(),
});

export const requestConnectedPoReturnBatchRequestSchema = z.object({
  purchaseOrderId: guidSchema,
  reason: z.string().min(1),
  lines: z.array(requestConnectedPoReturnLineRequestSchema).min(1),
  notes: z.string().optional(),
  returnBatchId: guidSchema.optional(),
});

export type ConnectedPoReturnableLineDto = z.infer<typeof connectedPoReturnableLineDtoSchema>;
export type ConnectedPoReturnEligibilityDto = z.infer<typeof connectedPoReturnEligibilityDtoSchema>;
export type RequestConnectedPoReturnLineRequest = z.infer<
  typeof requestConnectedPoReturnLineRequestSchema
>;
export type RequestConnectedPoReturnBatchRequest = z.infer<
  typeof requestConnectedPoReturnBatchRequestSchema
>;

export async function getConnectedPoReturnEligibility(
  workspace: PosWorkspaceScope,
  purchaseOrderId: string,
  signal?: AbortSignal,
): Promise<ConnectedPoReturnEligibilityDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${CONNECTED_PO_RETURNS_PATH}/eligibility/${purchaseOrderId}`,
  });
  return connectedPoReturnEligibilityDtoSchema.parse(raw);
}

export async function listConnectedPoReturnsByPurchaseOrder(
  workspace: PosWorkspaceScope,
  purchaseOrderId: string,
  signal?: AbortSignal,
): Promise<ReturnBatchDto[]> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${CONNECTED_PO_RETURNS_PATH}/by-purchase-order/${purchaseOrderId}`,
  });
  return z.array(returnBatchDtoSchema).parse(raw);
}

export async function listConnectedPoReturnSellerInbox(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<ReturnBatchDto[]> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${CONNECTED_PO_RETURNS_PATH}/seller-inbox`,
  });
  return z.array(returnBatchDtoSchema).parse(raw);
}

export async function requestConnectedPoReturnBatch(
  workspace: PosWorkspaceScope,
  body: RequestConnectedPoReturnBatchRequest,
  signal?: AbortSignal,
): Promise<ReturnBatchDto> {
  const validated = requestConnectedPoReturnBatchRequestSchema.parse(body);
  const payload: Record<string, unknown> = {
    purchaseOrderId: validated.purchaseOrderId,
    reason: validated.reason.trim(),
    lines: validated.lines.map((line) => ({
      purchaseOrderLineId: line.purchaseOrderLineId,
      quantity: line.quantity,
    })),
  };
  if (validated.notes?.trim()) {
    payload.notes = validated.notes.trim();
  }
  if (validated.returnBatchId) {
    payload.returnBatchId = validated.returnBatchId;
  }

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: CONNECTED_PO_RETURNS_PATH,
    body: payload,
  });
  return returnBatchDtoSchema.parse(raw);
}

export async function receiveConnectedPoReturnBatch(
  workspace: PosWorkspaceScope,
  returnBatchId: string,
  expectedUpdatedAtUtc?: string,
  signal?: AbortSignal,
): Promise<ReturnBatchDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${CONNECTED_PO_RETURNS_PATH}/${returnBatchId}/receive`,
    body: expectedUpdatedAtUtc ? { expectedUpdatedAtUtc } : {},
  });
  return returnBatchDtoSchema.parse(raw);
}

export async function finalizeConnectedPoReturnBatch(
  workspace: PosWorkspaceScope,
  returnBatchId: string,
  expectedUpdatedAtUtc: string,
  signal?: AbortSignal,
): Promise<ReturnBatchDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${CONNECTED_PO_RETURNS_PATH}/${returnBatchId}/finalize`,
    body: { expectedUpdatedAtUtc },
  });
  return returnBatchDtoSchema.parse(raw);
}
