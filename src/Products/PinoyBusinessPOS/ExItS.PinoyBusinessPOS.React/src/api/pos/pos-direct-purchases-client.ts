import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const PATH = "/api/v1/pos/purchasing/direct-purchases";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const DIRECT_PURCHASE_HISTORY_SOURCE_TYPES = ["Local", "B2B"] as const;
export type DirectPurchaseHistorySourceType =
  (typeof DIRECT_PURCHASE_HISTORY_SOURCE_TYPES)[number];

export const directPurchaseHistoryItemSchema = z.object({
  sourceId: guidSchema,
  sourceType: z.enum(DIRECT_PURCHASE_HISTORY_SOURCE_TYPES),
  occurredAtUtc: z.string(),
  purchaseDate: z.string().nullable().optional(),
  sellerDisplayName: z.string(),
  sellerOrganizationId: guidSchema.nullable().optional(),
  sellerPublicOrganizationId: z.string().nullable().optional(),
  referenceNumber: z.string(),
  lineCount: z.number(),
  totalAmount: z.number(),
  status: z.string(),
  paymentMethod: z.string().nullable().optional(),
  sellerStoreDisplayName: z.string().nullable().optional(),
});

export const directPurchaseHistoryPagedSchema = z.object({
  items: z.array(directPurchaseHistoryItemSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export const directPurchaseB2bLineSchema = z.object({
  lineNumber: z.number(),
  productNameSnapshot: z.string(),
  skuSnapshot: z.string().nullable().optional(),
  barcodeSnapshot: z.string().nullable().optional(),
  quantity: z.number(),
  unitOfMeasure: z.string(),
  unitPrice: z.number(),
  lineDiscountAmount: z.number(),
  lineTotal: z.number(),
});

export const directPurchaseB2bDetailSchema = z.object({
  saleId: guidSchema,
  sellerOrganizationId: guidSchema,
  sellerPublicOrganizationId: z.string().nullable().optional(),
  sellerDisplayName: z.string(),
  sellerStoreDisplayName: z.string().nullable().optional(),
  saleNumber: z.string(),
  occurredAtUtc: z.string(),
  status: z.string(),
  paymentMethod: z.string(),
  subtotal: z.number(),
  discountTotal: z.number(),
  taxAmount: z.number(),
  totalAmount: z.number(),
  lines: z.array(directPurchaseB2bLineSchema),
});

export type DirectPurchaseHistoryItem = z.infer<typeof directPurchaseHistoryItemSchema>;
export type DirectPurchaseHistoryPaged = z.infer<typeof directPurchaseHistoryPagedSchema>;
export type DirectPurchaseB2bDetail = z.infer<typeof directPurchaseB2bDetailSchema>;

export type ListDirectPurchasesQuery = {
  sourceType?: DirectPurchaseHistorySourceType | "All";
  fromDate?: string;
  toDate?: string;
  search?: string;
  status?: "Completed" | "Voided" | "AwaitingPayment" | "All";
  page?: number;
  pageSize?: number;
};

function buildQuery(params: ListDirectPurchasesQuery): string {
  const search = new URLSearchParams();
  if (params.sourceType && params.sourceType !== "All") {
    search.set("sourceType", params.sourceType);
  }
  if (params.fromDate) search.set("fromDate", params.fromDate);
  if (params.toDate) search.set("toDate", params.toDate);
  if (params.search?.trim()) search.set("search", params.search.trim());
  if (params.status && params.status !== "All") search.set("status", params.status);
  if (params.page != null) search.set("page", String(params.page));
  if (params.pageSize != null) search.set("pageSize", String(params.pageSize));
  const qs = search.toString();
  return qs ? `${PATH}?${qs}` : PATH;
}

export async function listDirectPurchases(
  workspace: PosWorkspaceScope,
  params: ListDirectPurchasesQuery = {},
  signal?: AbortSignal,
): Promise<DirectPurchaseHistoryPaged> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: buildQuery(params),
  });
  return directPurchaseHistoryPagedSchema.parse(raw);
}

export async function getDirectPurchaseB2bDetail(
  workspace: PosWorkspaceScope,
  saleId: string,
  signal?: AbortSignal,
): Promise<DirectPurchaseB2bDetail> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${PATH}/b2b/${saleId}`,
  });
  return directPurchaseB2bDetailSchema.parse(raw);
}
