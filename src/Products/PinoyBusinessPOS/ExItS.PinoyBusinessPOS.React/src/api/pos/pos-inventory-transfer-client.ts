import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";

const PATH = "/api/v1/pos/inventory/transfers";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const INVENTORY_TRANSFER_STATUSES = [
  "Draft",
  "InTransit",
  "PartiallyReceived",
  "Received",
  "ClosedWithDiscrepancy",
  "Cancelled",
] as const;
export type InventoryTransferStatusCode = (typeof INVENTORY_TRANSFER_STATUSES)[number];

/** Exact backend InventoryTransferDiscrepancyReason codes. */
export const INVENTORY_TRANSFER_DISCREPANCY_REASONS = [
  "ShortShipment",
  "Damaged",
  "LostInTransit",
  "WrongItem",
  "Other",
] as const;
export type InventoryTransferDiscrepancyReasonCode =
  (typeof INVENTORY_TRANSFER_DISCREPANCY_REASONS)[number];

export const INVENTORY_TRANSFER_DIRECTIONS = ["outgoing", "incoming"] as const;
export type InventoryTransferDirection = (typeof INVENTORY_TRANSFER_DIRECTIONS)[number];

export const inventoryTransferLineDtoSchema = z.object({
  lineId: guidSchema,
  productId: guidSchema,
  productName: z.string(),
  unitOfMeasure: z.string(),
  lineNumber: z.number(),
  sentQty: z.number(),
  receivedQty: z.number(),
  outstandingQty: z.number().optional(),
  closedQty: z.number().optional(),
  waivedQty: z.number().optional(),
  differenceQty: z.number(),
  lineStatus: z.string(),
  discrepancyReason: z.string().nullable().optional(),
  discrepancyNote: z.string().nullable().optional(),
  sourceLotId: guidSchema.nullable().optional(),
  lotNumber: z.string().nullable().optional(),
  expirationDate: z.string().nullable().optional(),
  unitCostSnapshot: z.number().nullable().optional(),
  sku: z.string().nullable().optional(),
});

export const INVENTORY_TRANSFER_MISSING_DISPOSITIONS = [
  "ExpectedLater",
  "CloseMissing",
  "AcceptShortage",
] as const;
export const INVENTORY_TRANSFER_DISCREPANCY_FOLLOW_UPS = [
  "RequestReplacement",
  "AcceptShortage",
] as const;
export type InventoryTransferDiscrepancyFollowUpCode =
  (typeof INVENTORY_TRANSFER_DISCREPANCY_FOLLOW_UPS)[number];
export type InventoryTransferMissingDispositionCode =
  (typeof INVENTORY_TRANSFER_MISSING_DISPOSITIONS)[number];

export const inventoryTransferReceiptLineDtoSchema = z.object({
  receiptLineId: guidSchema,
  lineId: guidSchema,
  productId: guidSchema,
  quantityReceived: z.number(),
  quantityDamaged: z.number().optional(),
  quantityMissing: z.number().optional(),
  quantityOther: z.number().optional(),
  otherReasonCode: z.string().nullable().optional(),
  otherReasonNote: z.string().nullable().optional(),
  missingDisposition: z.string().nullable().optional(),
  damagedFollowUp: z.string().nullable().optional(),
  otherFollowUp: z.string().nullable().optional(),
  quantityWaived: z.number().optional(),
  note: z.string().nullable().optional(),
  actualReceivedProductId: guidSchema.nullable().optional(),
  otherCustodyDecision: z.string().nullable().optional(),
});

export const inventoryTransferReceiptDtoSchema = z.object({
  receiptId: guidSchema,
  sequence: z.number(),
  receivedAtUtc: z.string(),
  receivedBy: guidSchema,
  lines: z.array(inventoryTransferReceiptLineDtoSchema),
});

export const inventoryTransferDtoSchema = z.object({
  transferId: guidSchema,
  organizationId: guidSchema,
  stockRequestId: guidSchema.nullable().optional(),
  transferNumber: z.string().nullable().optional(),
  sourceBranchId: guidSchema,
  sourceBranchName: z.string().nullable().optional(),
  destinationBranchId: guidSchema,
  destinationBranchName: z.string().nullable().optional(),
  status: z.string(),
  notes: z.string().nullable().optional(),
  createdBy: guidSchema,
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  dispatchedAtUtc: z.string().nullable().optional(),
  dispatchedBy: guidSchema.nullable().optional(),
  receivedAtUtc: z.string().nullable().optional(),
  receivedBy: guidSchema.nullable().optional(),
  cancelledAtUtc: z.string().nullable().optional(),
  cancelledBy: guidSchema.nullable().optional(),
  closedAtUtc: z.string().nullable().optional(),
  closedBy: guidSchema.nullable().optional(),
  totalSentQty: z.number(),
  totalReceivedQty: z.number(),
  totalClosedQty: z.number().optional(),
  totalOutstandingQty: z.number().optional(),
  totalDifferenceQty: z.number(),
  receiptCount: z.number().optional(),
  lastReceiptAtUtc: z.string().nullable().optional(),
  receipts: z.array(inventoryTransferReceiptDtoSchema).optional(),
  lines: z.array(inventoryTransferLineDtoSchema),
  rootTransferId: guidSchema.nullable().optional(),
  replacementSequence: z.number().nullable().optional(),
  replacementReason: z.string().nullable().optional(),
  damageHandlingPolicy: z.string().optional(),
  familyMembers: z
    .array(
      z.object({
        transferId: guidSchema,
        transferNumber: z.string().nullable().optional(),
        status: z.string(),
        replacementSequence: z.number().nullable().optional(),
        isRoot: z.boolean(),
        totalSentQty: z.number(),
        totalReceivedQty: z.number(),
        totalOutstandingQty: z.number(),
        totalDamagedQty: z.number().optional(),
        totalMissingQty: z.number().optional(),
        totalOtherQty: z.number().optional(),
      }),
    )
    .nullable()
    .optional(),
  damageCustodies: z
    .array(
      z.object({
        custodyId: guidSchema,
        transferId: guidSchema,
        rootTransferId: guidSchema,
        receiptLineId: guidSchema,
        productId: guidSchema,
        quantity: z.number(),
        decision: z.string(),
        followUpIntent: z.string(),
        status: z.string(),
        heldBranchId: guidSchema,
        recoveredSellableQty: z.number(),
        confirmedDamagedQty: z.number(),
        waivedQty: z.number(),
        destinationRecoveredSellableQty: z.number(),
        replacementDemandQty: z.number(),
        createdAtUtc: z.string(),
        updatedAtUtc: z.string(),
        returnDispatchedAtUtc: z.string().nullable().optional(),
        returnReceivedAtUtc: z.string().nullable().optional(),
        inspectedAtUtc: z.string().nullable().optional(),
      }),
    )
    .nullable()
    .optional(),
  exceptionCustodies: z
    .array(
      z.object({
        custodyId: guidSchema,
        transferId: guidSchema,
        rootTransferId: guidSchema,
        receiptLineId: guidSchema,
        expectedProductId: guidSchema,
        actualProductId: guidSchema,
        quantity: z.number(),
        reasonCode: z.string(),
        decision: z.string(),
        followUpIntent: z.string(),
        status: z.string(),
        heldBranchId: guidSchema,
        recoveredSellableQty: z.number(),
        confirmedNonSellableQty: z.number(),
        replacementDemandQty: z.number(),
        createdAtUtc: z.string(),
        updatedAtUtc: z.string(),
        returnDispatchedAtUtc: z.string().nullable().optional(),
        returnReceivedAtUtc: z.string().nullable().optional(),
        inspectedAtUtc: z.string().nullable().optional(),
      }),
    )
    .nullable()
    .optional(),
  satisfiedAtDestinationQty: z.number().optional(),
  openInTransitQty: z.number().optional(),
  remainingToDispatchQty: z.number().optional(),
  waivedQty: z.number().optional(),
});

export const inventoryTransferListItemDtoSchema = z.object({
  transferId: guidSchema,
  stockRequestId: guidSchema.nullable().optional(),
  transferNumber: z.string().nullable().optional(),
  sourceBranchId: guidSchema,
  sourceBranchName: z.string().nullable().optional(),
  destinationBranchId: guidSchema,
  destinationBranchName: z.string().nullable().optional(),
  status: z.string(),
  lineCount: z.number(),
  totalSentQty: z.number(),
  totalReceivedQty: z.number(),
  totalDifferenceQty: z.number(),
  updatedAtUtc: z.string(),
  createdBy: guidSchema,
  dispatchedBy: guidSchema.nullable().optional(),
  receivedBy: guidSchema.nullable().optional(),
  cancelledBy: guidSchema.nullable().optional(),
});

export const inventoryTransferPagedResultSchema = z.object({
  items: z.array(inventoryTransferListItemDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type InventoryTransferLineDto = z.infer<typeof inventoryTransferLineDtoSchema>;
export type InventoryTransferReceiptLineDto = z.infer<typeof inventoryTransferReceiptLineDtoSchema>;
export type InventoryTransferReceiptDto = z.infer<typeof inventoryTransferReceiptDtoSchema>;
export type InventoryTransferDto = z.infer<typeof inventoryTransferDtoSchema>;
export type InventoryTransferListItemDto = z.infer<typeof inventoryTransferListItemDtoSchema>;
export type InventoryTransferPagedResult = z.infer<typeof inventoryTransferPagedResultSchema>;

export type InventoryTransferLineRequest = {
  productId: string;
  quantity: number;
  sourceLotId?: string | null;
};

export type CreateInventoryTransferRequest = {
  sourceBranchId: string;
  destinationBranchId: string;
  lines: InventoryTransferLineRequest[];
  notes?: string | null;
  /** Client-generated idempotency entity id. */
  operationId?: string | null;
};

export const INVENTORY_TRANSFER_DAMAGED_CUSTODY_DECISIONS = [
  "KeepAtDestination",
  "ReturnToSource",
] as const;
export type InventoryTransferDamagedCustodyDecisionCode =
  (typeof INVENTORY_TRANSFER_DAMAGED_CUSTODY_DECISIONS)[number];

export type InventoryTransferReceiveLineRequest = {
  productId: string;
  /** Good qty received this wave (alias: receivedQty). */
  goodQty?: number;
  receivedQty?: number;
  damagedQty?: number;
  missingQty?: number;
  otherQty?: number;
  otherReasonCode?: string | null;
  otherReasonNote?: string | null;
  missingDisposition?: InventoryTransferMissingDispositionCode | string | null;
  damagedFollowUp?: InventoryTransferDiscrepancyFollowUpCode | string | null;
  otherFollowUp?: InventoryTransferDiscrepancyFollowUpCode | string | null;
  damagedCustodyDecision?: InventoryTransferDamagedCustodyDecisionCode | string | null;
  otherCustodyDecision?: InventoryTransferDamagedCustodyDecisionCode | string | null;
  actualReceivedProductId?: string | null;
  discrepancyReason?: string | null;
  discrepancyNote?: string | null;
  lineId?: string | null;
};

export type ReceiveInventoryTransferRequest = {
  lines: InventoryTransferReceiveLineRequest[];
};

export type CloseRemainderInventoryTransferLineRequest = {
  lineId?: string | null;
  productId?: string | null;
  discrepancyReason: string;
  discrepancyNote?: string | null;
};

export type CloseRemainderInventoryTransferRequest = {
  lines?: CloseRemainderInventoryTransferLineRequest[] | null;
  discrepancyReason?: string | null;
  discrepancyNote?: string | null;
};

export type ListInventoryTransfersOptions = {
  page?: number;
  pageSize?: number;
  status?: string;
  transferNumber?: string;
  direction?: InventoryTransferDirection | string;
  sourceBranchId?: string;
  destinationBranchId?: string;
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

export async function listInventoryTransfers(
  workspace: PosWorkspaceScope,
  options: ListInventoryTransfersOptions = {},
  signal?: AbortSignal,
): Promise<InventoryTransferPagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(PATH, {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
      status: options.status,
      transferNumber: options.transferNumber,
      direction: options.direction,
      sourceBranchId: options.sourceBranchId,
      destinationBranchId: options.destinationBranchId,
    }),
  });
  return inventoryTransferPagedResultSchema.parse(raw);
}

export async function getInventoryTransfer(
  workspace: PosWorkspaceScope,
  transferId: string,
  signal?: AbortSignal,
): Promise<InventoryTransferDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${PATH}/${transferId}`,
  });
  return inventoryTransferDtoSchema.parse(raw);
}

export async function createInventoryTransfer(
  workspace: PosWorkspaceScope,
  body: CreateInventoryTransferRequest,
  signal?: AbortSignal,
): Promise<InventoryTransferDto> {
  const operationId = body.operationId?.trim() || crypto.randomUUID();
  const payload: Record<string, unknown> = {
    sourceBranchId: body.sourceBranchId,
    destinationBranchId: body.destinationBranchId,
    lines: body.lines.map((line) => {
      const entry: Record<string, unknown> = {
        productId: line.productId,
        quantity: line.quantity,
      };
      if (line.sourceLotId) {
        entry.sourceLotId = line.sourceLotId;
      }
      return entry;
    }),
  };
  const notes = trimOrUndef(body.notes);
  if (notes) {
    payload.notes = notes;
  }

  const headers = await buildPosMutationIdempotencyHeaders(
    operationId,
    JSON.stringify(payload),
    OFFLINE_OPERATION_TYPES.InventoryTransferCreate,
  );

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: PATH,
    body: payload,
    headers,
  });
  return inventoryTransferDtoSchema.parse(raw);
}

export async function dispatchInventoryTransfer(
  workspace: PosWorkspaceScope,
  transferId: string,
  signal?: AbortSignal,
): Promise<InventoryTransferDto> {
  const headers = await buildPosMutationIdempotencyHeaders(
    transferId,
    "{}",
    OFFLINE_OPERATION_TYPES.InventoryTransferDispatch,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${transferId}/dispatch`,
    body: {},
    headers,
  });
  return inventoryTransferDtoSchema.parse(raw);
}

export async function prepareInventoryTransferRemaining(
  workspace: PosWorkspaceScope,
  transferId: string,
  signal?: AbortSignal,
): Promise<InventoryTransferDto> {
  const headers = await buildPosMutationIdempotencyHeaders(
    transferId,
    "{}",
    OFFLINE_OPERATION_TYPES.InventoryTransferCreate,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${transferId}/prepare-remaining`,
    body: {},
    headers,
  });
  return inventoryTransferDtoSchema.parse(raw);
}

export async function receiveInventoryTransfer(
  workspace: PosWorkspaceScope,
  transferId: string,
  body: ReceiveInventoryTransferRequest,
  signal?: AbortSignal,
): Promise<InventoryTransferDto> {
  const payload = {
    lines: body.lines.map((line) => {
      const goodQty = line.goodQty ?? line.receivedQty ?? 0;
      const entry: Record<string, unknown> = {
        productId: line.productId,
        goodQty,
        receivedQty: goodQty,
      };
      if (line.lineId) {
        entry.lineId = line.lineId;
      }
      if (line.damagedQty != null && line.damagedQty > 0) {
        entry.damagedQty = line.damagedQty;
      }
      if (line.missingQty != null && line.missingQty > 0) {
        entry.missingQty = line.missingQty;
      }
      if (line.otherQty != null && line.otherQty > 0) {
        entry.otherQty = line.otherQty;
      }
      const otherCode = trimOrUndef(line.otherReasonCode ?? undefined);
      if (otherCode) {
        entry.otherReasonCode = otherCode;
      }
      const otherNote = trimOrUndef(line.otherReasonNote ?? undefined);
      if (otherNote) {
        entry.otherReasonNote = otherNote;
      }
      const disposition = trimOrUndef(line.missingDisposition ?? undefined);
      if (disposition) {
        entry.missingDisposition = disposition;
      }
      const damagedFollowUp = trimOrUndef(line.damagedFollowUp ?? undefined);
      if (damagedFollowUp) {
        entry.damagedFollowUp = damagedFollowUp;
      }
      const otherFollowUp = trimOrUndef(line.otherFollowUp ?? undefined);
      if (otherFollowUp) {
        entry.otherFollowUp = otherFollowUp;
      }
      const custodyDecision = trimOrUndef(line.damagedCustodyDecision ?? undefined);
      if (custodyDecision) {
        entry.damagedCustodyDecision = custodyDecision;
      }
      const otherCustody = trimOrUndef(line.otherCustodyDecision ?? undefined);
      if (otherCustody) {
        entry.otherCustodyDecision = otherCustody;
      }
      const actualProductId = trimOrUndef(line.actualReceivedProductId ?? undefined);
      if (actualProductId) {
        entry.actualReceivedProductId = actualProductId;
      }
      const reason = trimOrUndef(line.discrepancyReason);
      if (reason) {
        entry.discrepancyReason = reason;
      }
      const note = trimOrUndef(line.discrepancyNote);
      if (note) {
        entry.discrepancyNote = note;
      }
      return entry;
    }),
  };
  const headers = await buildPosMutationIdempotencyHeaders(
    transferId,
    JSON.stringify(payload),
    OFFLINE_OPERATION_TYPES.InventoryTransferReceive,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${transferId}/receive`,
    body: payload,
    headers,
  });
  return inventoryTransferDtoSchema.parse(raw);
}

export async function closeRemainderInventoryTransfer(
  workspace: PosWorkspaceScope,
  transferId: string,
  body: CloseRemainderInventoryTransferRequest,
  signal?: AbortSignal,
): Promise<InventoryTransferDto> {
  const payload: Record<string, unknown> = {};
  const transferReason = trimOrUndef(body.discrepancyReason);
  if (transferReason) {
    payload.discrepancyReason = transferReason;
  }
  const transferNote = trimOrUndef(body.discrepancyNote);
  if (transferNote) {
    payload.discrepancyNote = transferNote;
  }
  if (body.lines?.length) {
    payload.lines = body.lines.map((line) => {
      const entry: Record<string, unknown> = {
        discrepancyReason: line.discrepancyReason,
      };
      if (line.lineId) {
        entry.lineId = line.lineId;
      }
      if (line.productId) {
        entry.productId = line.productId;
      }
      const note = trimOrUndef(line.discrepancyNote);
      if (note) {
        entry.discrepancyNote = note;
      }
      return entry;
    });
  }

  const headers = await buildPosMutationIdempotencyHeaders(
    transferId,
    JSON.stringify(payload),
    OFFLINE_OPERATION_TYPES.InventoryTransferCloseRemainder,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${transferId}/close-remainder`,
    body: payload,
    headers,
  });
  return inventoryTransferDtoSchema.parse(raw);
}

export async function cancelInventoryTransfer(
  workspace: PosWorkspaceScope,
  transferId: string,
  signal?: AbortSignal,
): Promise<InventoryTransferDto> {
  const headers = await buildPosMutationIdempotencyHeaders(
    transferId,
    "{}",
    OFFLINE_OPERATION_TYPES.InventoryTransferCancel,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${transferId}/cancel`,
    body: {},
    headers,
  });
  return inventoryTransferDtoSchema.parse(raw);
}

const damageCustodyDtoSchema = z.object({
  custodyId: guidSchema,
  transferId: guidSchema,
  rootTransferId: guidSchema,
  receiptLineId: guidSchema,
  productId: guidSchema,
  quantity: z.number(),
  decision: z.string(),
  followUpIntent: z.string(),
  status: z.string(),
  heldBranchId: guidSchema,
  recoveredSellableQty: z.number(),
  confirmedDamagedQty: z.number(),
  waivedQty: z.number(),
  destinationRecoveredSellableQty: z.number(),
  replacementDemandQty: z.number(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  returnDispatchedAtUtc: z.string().nullable().optional(),
  returnReceivedAtUtc: z.string().nullable().optional(),
  inspectedAtUtc: z.string().nullable().optional(),
});

export type InventoryTransferDamageCustodyDto = z.infer<typeof damageCustodyDtoSchema>;

export const exceptionCustodyDtoSchema = z.object({
  custodyId: guidSchema,
  transferId: guidSchema,
  rootTransferId: guidSchema,
  receiptLineId: guidSchema,
  expectedProductId: guidSchema,
  actualProductId: guidSchema,
  quantity: z.number(),
  reasonCode: z.string(),
  decision: z.string(),
  followUpIntent: z.string(),
  status: z.string(),
  heldBranchId: guidSchema,
  recoveredSellableQty: z.number(),
  confirmedNonSellableQty: z.number(),
  replacementDemandQty: z.number(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  returnDispatchedAtUtc: z.string().nullable().optional(),
  returnReceivedAtUtc: z.string().nullable().optional(),
  inspectedAtUtc: z.string().nullable().optional(),
  expectedProductName: z.string().nullable().optional(),
  actualProductName: z.string().nullable().optional(),
});

export type InventoryTransferExceptionCustodyDto = z.infer<typeof exceptionCustodyDtoSchema>;

async function mutateDamageCustody(
  workspace: PosWorkspaceScope,
  custodyId: string,
  pathSuffix: string,
  body: Record<string, unknown>,
  operationType: string,
  signal?: AbortSignal,
): Promise<InventoryTransferDamageCustodyDto> {
  const payloadJson = JSON.stringify(body);
  const headers = await buildPosMutationIdempotencyHeaders(
    custodyId,
    payloadJson,
    operationType,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/damage-custodies/${custodyId}/${pathSuffix}`,
    body,
    headers,
  });
  return damageCustodyDtoSchema.parse(raw);
}

export async function dispatchInventoryTransferDamageReturn(
  workspace: PosWorkspaceScope,
  custodyId: string,
  signal?: AbortSignal,
): Promise<InventoryTransferDamageCustodyDto> {
  return mutateDamageCustody(
    workspace,
    custodyId,
    "dispatch-return",
    {},
    OFFLINE_OPERATION_TYPES.InventoryTransferDamageDispatchReturn,
    signal,
  );
}

export async function receiveInventoryTransferDamageReturn(
  workspace: PosWorkspaceScope,
  custodyId: string,
  signal?: AbortSignal,
): Promise<InventoryTransferDamageCustodyDto> {
  return mutateDamageCustody(
    workspace,
    custodyId,
    "receive-return",
    {},
    OFFLINE_OPERATION_TYPES.InventoryTransferDamageReceiveReturn,
    signal,
  );
}

export async function inspectInventoryTransferDamageCustody(
  workspace: PosWorkspaceScope,
  custodyId: string,
  body: { recoveredSellableQty: number; confirmedDamagedQty: number; followUpOverride?: string | null },
  signal?: AbortSignal,
): Promise<InventoryTransferDamageCustodyDto> {
  const payload: Record<string, unknown> = {
    recoveredSellableQty: body.recoveredSellableQty,
    confirmedDamagedQty: body.confirmedDamagedQty,
  };
  const followUp = trimOrUndef(body.followUpOverride ?? undefined);
  if (followUp) {
    payload.followUpOverride = followUp;
  }
  return mutateDamageCustody(
    workspace,
    custodyId,
    "inspect",
    payload,
    OFFLINE_OPERATION_TYPES.InventoryTransferDamageInspect,
    signal,
  );
}

async function mutateExceptionCustody(
  workspace: PosWorkspaceScope,
  custodyId: string,
  pathSuffix: string,
  body: Record<string, unknown>,
  operationType: string,
  signal?: AbortSignal,
): Promise<InventoryTransferExceptionCustodyDto> {
  const payloadJson = JSON.stringify(body);
  const headers = await buildPosMutationIdempotencyHeaders(
    custodyId,
    payloadJson,
    operationType,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/exception-custodies/${custodyId}/${pathSuffix}`,
    body,
    headers,
  });
  return exceptionCustodyDtoSchema.parse(raw);
}

export async function dispatchInventoryTransferExceptionReturn(
  workspace: PosWorkspaceScope,
  custodyId: string,
  signal?: AbortSignal,
): Promise<InventoryTransferExceptionCustodyDto> {
  return mutateExceptionCustody(
    workspace,
    custodyId,
    "dispatch-return",
    {},
    OFFLINE_OPERATION_TYPES.InventoryTransferExceptionDispatchReturn,
    signal,
  );
}

export async function receiveInventoryTransferExceptionReturn(
  workspace: PosWorkspaceScope,
  custodyId: string,
  signal?: AbortSignal,
): Promise<InventoryTransferExceptionCustodyDto> {
  return mutateExceptionCustody(
    workspace,
    custodyId,
    "receive-return",
    {},
    OFFLINE_OPERATION_TYPES.InventoryTransferExceptionReceiveReturn,
    signal,
  );
}

export async function inspectInventoryTransferExceptionCustody(
  workspace: PosWorkspaceScope,
  custodyId: string,
  body: { recoveredSellableQty: number; confirmedNonSellableQty: number },
  signal?: AbortSignal,
): Promise<InventoryTransferExceptionCustodyDto> {
  return mutateExceptionCustody(
    workspace,
    custodyId,
    "inspect",
    body,
    OFFLINE_OPERATION_TYPES.InventoryTransferExceptionInspect,
    signal,
  );
}
