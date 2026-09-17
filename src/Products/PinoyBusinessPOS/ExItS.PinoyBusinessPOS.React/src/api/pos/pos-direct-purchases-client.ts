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

export const directPurchaseSellerDocumentIdentitySchema = z.object({
  businessName: z.string().nullable().optional(),
  publicOrganizationId: z.string().nullable().optional(),
  logoUrl: z.string().nullable().optional(),
  address: z.string().nullable().optional(),
  phone: z.string().nullable().optional(),
  email: z.string().nullable().optional(),
  branchName: z.string().nullable().optional(),
  branchAddress: z.string().nullable().optional(),
  showLogo: z.boolean().optional().default(true),
  showBusinessName: z.boolean().optional().default(true),
  showBusinessAddress: z.boolean().optional().default(true),
  showBusinessPhone: z.boolean().optional().default(true),
  showBusinessEmail: z.boolean().optional().default(true),
  showBranchName: z.boolean().optional().default(true),
  showBranchAddress: z.boolean().optional().default(false),
  identitySource: z.string().optional().default("unknown"),
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
  buyerDisplayNameSnapshot: z.string().nullable().optional(),
  sellerDocumentIdentity: directPurchaseSellerDocumentIdentitySchema.nullable().optional(),
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
  if (!raw || typeof raw !== "object") {
    return directPurchaseB2bDetailSchema.parse(raw);
  }
  const r = raw as Record<string, unknown>;
  const sellerRaw = (r.sellerDocumentIdentity ?? r.SellerDocumentIdentity ?? null) as Record<
    string,
    unknown
  > | null;
  return directPurchaseB2bDetailSchema.parse({
    saleId: r.saleId ?? r.SaleId,
    sellerOrganizationId: r.sellerOrganizationId ?? r.SellerOrganizationId,
    sellerPublicOrganizationId: r.sellerPublicOrganizationId ?? r.SellerPublicOrganizationId ?? null,
    sellerDisplayName: r.sellerDisplayName ?? r.SellerDisplayName,
    sellerStoreDisplayName: r.sellerStoreDisplayName ?? r.SellerStoreDisplayName ?? null,
    saleNumber: r.saleNumber ?? r.SaleNumber,
    occurredAtUtc: r.occurredAtUtc ?? r.OccurredAtUtc,
    status: r.status ?? r.Status,
    paymentMethod: r.paymentMethod ?? r.PaymentMethod,
    subtotal: r.subtotal ?? r.Subtotal,
    discountTotal: r.discountTotal ?? r.DiscountTotal,
    taxAmount: r.taxAmount ?? r.TaxAmount,
    totalAmount: r.totalAmount ?? r.TotalAmount,
    lines: r.lines ?? r.Lines,
    buyerDisplayNameSnapshot: r.buyerDisplayNameSnapshot ?? r.BuyerDisplayNameSnapshot ?? null,
    sellerDocumentIdentity: sellerRaw
      ? {
          businessName: sellerRaw.businessName ?? sellerRaw.BusinessName ?? null,
          publicOrganizationId:
            sellerRaw.publicOrganizationId ?? sellerRaw.PublicOrganizationId ?? null,
          logoUrl: sellerRaw.logoUrl ?? sellerRaw.LogoUrl ?? null,
          address: sellerRaw.address ?? sellerRaw.Address ?? null,
          phone: sellerRaw.phone ?? sellerRaw.Phone ?? null,
          email: sellerRaw.email ?? sellerRaw.Email ?? null,
          branchName: sellerRaw.branchName ?? sellerRaw.BranchName ?? null,
          branchAddress: sellerRaw.branchAddress ?? sellerRaw.BranchAddress ?? null,
          showLogo: sellerRaw.showLogo ?? sellerRaw.ShowLogo,
          showBusinessName: sellerRaw.showBusinessName ?? sellerRaw.ShowBusinessName,
          showBusinessAddress: sellerRaw.showBusinessAddress ?? sellerRaw.ShowBusinessAddress,
          showBusinessPhone: sellerRaw.showBusinessPhone ?? sellerRaw.ShowBusinessPhone,
          showBusinessEmail: sellerRaw.showBusinessEmail ?? sellerRaw.ShowBusinessEmail,
          showBranchName: sellerRaw.showBranchName ?? sellerRaw.ShowBranchName,
          showBranchAddress: sellerRaw.showBranchAddress ?? sellerRaw.ShowBranchAddress,
          identitySource: sellerRaw.identitySource ?? sellerRaw.IdentitySource,
        }
      : null,
  });
}
