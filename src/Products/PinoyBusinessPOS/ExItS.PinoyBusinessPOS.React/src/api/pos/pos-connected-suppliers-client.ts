import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

/**
 * Connected-supplier API client for `/api/v1/pos/connected-suppliers/*`.
 *
 * INVARIANTS (do not violate in callers):
 * - EXPOSABLE ≠ SHARED: accepting a connection never auto-shares products.
 * - Share / expose / link / connection APIs never mutate inventory quantities.
 * - Do not invent a second organization-link model — use relationships + links only.
 */
const PATH = "/api/v1/pos/connected-suppliers";

/** Inventory mutation paths — share/link clients must never call these. */
export const INVENTORY_MUTATION_PATH_MARKERS = [
  "/api/v1/pos/inventory/",
  "/stock-counts",
  "/receive",
  "/goods-receipt",
] as const;

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

const isoDateSchema = z.string();

export const connectedSupplierRelationshipSchema = z.object({
  relationshipId: guidSchema,
  buyerOrganizationId: guidSchema,
  supplierOrganizationId: guidSchema,
  status: z.string(),
  requestedAtUtc: isoDateSchema,
  requestedByUserId: guidSchema.nullable().optional(),
  respondedAtUtc: isoDateSchema.nullable().optional(),
  respondedByUserId: guidSchema.nullable().optional(),
  disconnectedAtUtc: isoDateSchema.nullable().optional(),
  createdAtUtc: isoDateSchema,
  updatedAtUtc: isoDateSchema,
  counterpartyDisplayName: z.string().nullable().optional(),
  counterpartyPublicOrganizationId: z.string().nullable().optional(),
  catalogSharingMode: z.string().optional().default("SelectedOnly"),
  customerDiscountPercent: z.number().nullable().optional().default(null),
});

export const connectionCatalogSettingsSchema = z.object({
  relationshipId: guidSchema,
  catalogSharingMode: z.string(),
  customerDiscountPercent: z.number().nullable().optional().default(null),
  eligibleCount: z.number(),
  sharedCount: z.number(),
  excludedCount: z.number(),
  overrideCount: z.number(),
});

export const supplierProductExposureSchema = z.object({
  exposureId: guidSchema,
  supplierOrganizationId: guidSchema,
  productId: guidSchema,
  skuSnapshot: z.string().nullable().optional(),
  nameSnapshot: z.string(),
  categoryNameSnapshot: z.string().nullable().optional(),
  unitOfMeasureCode: z.string(),
  supplierOrderPrice: z.number(),
  isOrderable: z.boolean(),
  isExposed: z.boolean(),
  syncVersion: z.number(),
  createdAtUtc: isoDateSchema,
  updatedAtUtc: isoDateSchema,
  effectiveSupplierOrderPrice: z.number().nullable().optional(),
});

export const connectedBuyerProductShareSchema = z.object({
  shareId: guidSchema,
  relationshipId: guidSchema,
  buyerOrganizationId: guidSchema,
  supplierOrganizationId: guidSchema,
  supplierProductId: guidSchema,
  isShared: z.boolean(),
  buyerSpecificPoPrice: z.number().nullable().optional(),
  effectiveSupplierOrderPrice: z.number().nullable().optional(),
  syncVersion: z.number(),
  createdAtUtc: isoDateSchema,
  updatedAtUtc: isoDateSchema,
  skuSnapshot: z.string().nullable().optional(),
  nameSnapshot: z.string().nullable().optional(),
  unitOfMeasureCode: z.string().nullable().optional(),
  sellingPrice: z.number().nullable().optional(),
  categoryNameSnapshot: z.string().nullable().optional(),
  defaultPoPrice: z.number().nullable().optional(),
  isBlockedFromConnectedBuyers: z.boolean().optional(),
});

export const buyerProductShareCategoryFacetSchema = z.object({
  categoryName: z.string().nullable().optional(),
  count: z.number(),
});

export const buyerProductShareQueryResultSchema = z.object({
  items: z.array(connectedBuyerProductShareSchema),
  matchingCount: z.number(),
  eligibleCount: z.number(),
  sharedCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
  categories: z.array(buyerProductShareCategoryFacetSchema),
  catalogSharingMode: z.string().optional().default("SelectedOnly"),
  customerDiscountPercent: z.number().nullable().optional().default(null),
});

export const missingDefaultPoProductSchema = z.object({
  productId: guidSchema,
  name: z.string(),
  sellingPrice: z.number(),
});

export const bulkBuyerProductShareMutationResultSchema = z.object({
  affectedCount: z.number(),
  needsDefaultPo: z.array(missingDefaultPoProductSchema).nullable().optional(),
});

export const buyerPricePreviewItemSchema = z.object({
  supplierProductId: guidSchema,
  name: z.string(),
  defaultPoPrice: z.number(),
  currentBuyerPrice: z.number().nullable().optional(),
  proposedBuyerPrice: z.number().nullable().optional(),
  proposedEffectivePrice: z.number(),
});

export const bulkBuyerPricingPreviewSchema = z.object({
  affectedCount: z.number(),
  truncated: z.boolean(),
  items: z.array(buyerPricePreviewItemSchema),
  needsDefaultPo: z.array(missingDefaultPoProductSchema).nullable().optional(),
});

export const buyerSupplierProductLinkSchema = z.object({
  linkId: guidSchema,
  relationshipId: guidSchema,
  buyerOrganizationId: guidSchema,
  supplierOrganizationId: guidSchema,
  buyerProductId: guidSchema,
  supplierProductId: guidSchema,
  supplierSkuSnapshot: z.string().nullable().optional(),
  supplierNameSnapshot: z.string(),
  unitOfMeasureCode: z.string(),
  lastKnownOrderPrice: z.number(),
  isActive: z.boolean(),
  syncVersion: z.number(),
  createdAtUtc: isoDateSchema,
  updatedAtUtc: isoDateSchema,
  buyerPurchaseUnitId: guidSchema.nullable().optional(),
  multiplierToBase: z.number().optional(),
  packageLabel: z.string().nullable().optional(),
});

export const catalogProductReadinessItemSchema = z.object({
  exposureId: guidSchema,
  supplierProductId: guidSchema,
  supplierName: z.string(),
  supplierSku: z.string().nullable().optional(),
  supplierBarcode: z.string().nullable().optional(),
  unitOfMeasureCode: z.string(),
  poPrice: z.number(),
  status: z.string(),
  canAutoLink: z.boolean(),
  candidateBuyerProductId: guidSchema.nullable().optional(),
  candidateBuyerProductName: z.string().nullable().optional(),
  nameMatched: z.boolean(),
  skuMatched: z.boolean(),
  barcodeMatched: z.boolean(),
  unitCompatible: z.boolean(),
  matchDetails: z.string(),
  linkedBuyerProductId: guidSchema.nullable().optional(),
});

export const catalogReadinessResultSchema = z.object({
  relationshipId: guidSchema,
  ready: z.number(),
  new: z.number(),
  review: z.number(),
  conflict: z.number(),
  items: z.array(catalogProductReadinessItemSchema),
});

export const buyerProductMatchCandidateSchema = z.object({
  productId: guidSchema,
  name: z.string(),
  sku: z.string().nullable().optional(),
  unitOfMeasure: z.string(),
  sellingPrice: z.number(),
  matchKind: z.string(),
});

export const suggestBuyerProductMatchesResultSchema = z.object({
  exposureId: guidSchema,
  supplierName: z.string(),
  supplierSku: z.string().nullable().optional(),
  unitOfMeasureCode: z.string(),
  poPrice: z.number(),
  candidates: z.array(buyerProductMatchCandidateSchema),
});

export const createBuyerProductAndLinkResultSchema = z.object({
  link: buyerSupplierProductLinkSchema,
  buyerProductId: guidSchema,
  buyerProductName: z.string(),
  buyerSku: z.string().nullable().optional(),
  buyerSellingPrice: z.number(),
  createdNewProduct: z.boolean(),
  alreadyLinked: z.boolean(),
});

export const pagedExposureResultSchema = z.object({
  items: z.array(supplierProductExposureSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type ConnectedSupplierRelationship = z.infer<typeof connectedSupplierRelationshipSchema>;
export type SupplierProductExposure = z.infer<typeof supplierProductExposureSchema>;
export type ConnectedBuyerProductShare = z.infer<typeof connectedBuyerProductShareSchema>;
export type BuyerProductShareQueryResult = z.infer<typeof buyerProductShareQueryResultSchema>;
export type BulkBuyerProductShareMutationResult = z.infer<
  typeof bulkBuyerProductShareMutationResultSchema
>;
export type BulkBuyerPricingPreview = z.infer<typeof bulkBuyerPricingPreviewSchema>;
export type BuyerSupplierProductLink = z.infer<typeof buyerSupplierProductLinkSchema>;
export type CatalogReadinessResult = z.infer<typeof catalogReadinessResultSchema>;
export type CatalogProductReadinessItem = z.infer<typeof catalogProductReadinessItemSchema>;
export type SuggestBuyerProductMatchesResult = z.infer<
  typeof suggestBuyerProductMatchesResultSchema
>;
export type CreateBuyerProductAndLinkResult = z.infer<typeof createBuyerProductAndLinkResultSchema>;
export type PagedExposureResult = z.infer<typeof pagedExposureResultSchema>;

export type ShareFilter = "all" | "shared" | "notShared" | "customPrice" | "blocked" | string;

export type SetBuyerProductShareItem = {
  supplierProductId: string;
  isShared: boolean;
  buyerSpecificPoPrice?: number | null;
  establishDefaultPoPrice?: number | null;
};

export type BulkShareMutationInput = {
  operation: "Share" | "Unshare" | string;
  productIds?: string[] | null;
  selectAllMatching?: boolean;
  query?: string | null;
  category?: string | null;
  shareFilter?: string | null;
  establishDefaultPoPrices?: Record<string, number> | null;
};

export type BulkPricingInput = {
  mode: "UseDefault" | "DiscountPercent" | "AdjustAmount" | "FixedPrice" | string;
  productIds?: string[] | null;
  selectAllMatching?: boolean;
  query?: string | null;
  category?: string | null;
  shareFilter?: string | null;
  percent?: number | null;
  amount?: number | null;
  fixedPrice?: number | null;
};

export type LinkProductInput = {
  buyerProductId: string;
  exposureId: string;
  buyerPurchaseUnitId?: string | null;
  multiplierToBase?: number | null;
  packageLabel?: string | null;
};

export type CreateBuyerProductAndLinkInput = {
  exposureId: string;
  name: string;
  unitOfMeasure: string;
  sellingPrice: number;
  sku?: string | null;
  description?: string | null;
  categoryId?: string | null;
};

export type ExposeProductInput = {
  productId: string;
  supplierOrderPrice: number;
  isOrderable?: boolean;
};

export type UpdateExposureInput = {
  supplierOrderPrice: number;
  isOrderable: boolean;
  isExposed: boolean;
};

function appendQuery(
  path: string,
  params: Record<string, string | number | boolean | undefined | null>,
): string {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== "") {
      query.set(key, String(value));
    }
  }
  const serialized = query.toString();
  return serialized ? `${path}?${serialized}` : path;
}

function relPath(relationshipId: string, suffix = ""): string {
  return `${PATH}/relationships/${relationshipId}${suffix}`;
}

/** True when relationship status is Active (case-insensitive). */
export function isRelationshipActive(
  relationship: Pick<ConnectedSupplierRelationship, "status">,
): boolean {
  return relationship.status.trim().toLowerCase() === "active";
}

/** True when relationship status is Pending. */
export function isRelationshipPending(
  relationship: Pick<ConnectedSupplierRelationship, "status">,
): boolean {
  return relationship.status.trim().toLowerCase() === "pending";
}

/**
 * Documents EXPOSABLE ≠ SHARED: share filter "shared" only returns IsShared=true rows.
 * Eligible/exposable products may appear under "notShared" or "all" without being shared.
 */
export function isShareFilterSharedOnly(shareFilter: string | null | undefined): boolean {
  return (shareFilter ?? "").trim().toLowerCase() === "shared";
}

/** Assert a URL is not an inventory mutation endpoint (share/link invariant helper). */
export function assertNotInventoryMutationUrl(url: string): void {
  const lower = url.toLowerCase();
  for (const marker of INVENTORY_MUTATION_PATH_MARKERS) {
    if (lower.includes(marker.toLowerCase())) {
      throw new Error(`Inventory mutation path forbidden on share/link: ${url}`);
    }
  }
}

// --- Relationships ---

export async function requestConnection(
  workspace: PosWorkspaceScope,
  input: {
    supplierPublicOrganizationIdOrQrPayload: string;
    supplierOrganizationId?: string | null;
  },
  signal?: AbortSignal,
): Promise<ConnectedSupplierRelationship> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/relationships/request`,
    body: {
      supplierPublicOrganizationIdOrQrPayload: input.supplierPublicOrganizationIdOrQrPayload.trim(),
      supplierOrganizationId: input.supplierOrganizationId ?? null,
    },
  });
  return connectedSupplierRelationshipSchema.parse(raw);
}

export async function approveConnection(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  options?: {
    catalogSharingMode?: "AllEligible" | "SelectedOnly";
    customerDiscountPercent?: number | null;
    confirmCatalogSharing?: boolean;
    signal?: AbortSignal;
  },
): Promise<ConnectedSupplierRelationship> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal: options?.signal,
    path: `${relPath(relationshipId, "/approve")}`,
    body: {
      catalogSharingMode: options?.catalogSharingMode ?? null,
      customerDiscountPercent: options?.customerDiscountPercent ?? null,
      confirmCatalogSharing: options?.confirmCatalogSharing ?? false,
    },
  });
  return connectedSupplierRelationshipSchema.parse(raw);
}

export async function getConnectionCatalogSettings(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  signal?: AbortSignal,
): Promise<z.infer<typeof connectionCatalogSettingsSchema>> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${relPath(relationshipId, "/catalog-settings")}`,
  });
  return connectionCatalogSettingsSchema.parse(raw);
}

export async function updateConnectionCatalogSettings(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  input: {
    catalogSharingMode: "AllEligible" | "SelectedOnly";
    customerDiscountPercent?: number | null;
    confirmModeChange?: boolean;
  },
  signal?: AbortSignal,
): Promise<z.infer<typeof connectionCatalogSettingsSchema>> {
  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: `${relPath(relationshipId, "/catalog-settings")}`,
    body: {
      catalogSharingMode: input.catalogSharingMode,
      customerDiscountPercent: input.customerDiscountPercent ?? null,
      confirmModeChange: input.confirmModeChange ?? false,
    },
  });
  return connectionCatalogSettingsSchema.parse(raw);
}

export async function declineConnection(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  signal?: AbortSignal,
): Promise<ConnectedSupplierRelationship> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${relPath(relationshipId, "/decline")}`,
    body: {},
  });
  return connectedSupplierRelationshipSchema.parse(raw);
}

export async function disconnectConnection(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  signal?: AbortSignal,
): Promise<ConnectedSupplierRelationship> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${relPath(relationshipId, "/disconnect")}`,
  });
  return connectedSupplierRelationshipSchema.parse(raw);
}

export async function listRelationships(
  workspace: PosWorkspaceScope,
  view: "buyer" | "supplier" = "buyer",
  signal?: AbortSignal,
): Promise<ConnectedSupplierRelationship[]> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(`${PATH}/relationships`, { view }),
  });
  return z.array(connectedSupplierRelationshipSchema).parse(raw);
}

// --- Exposures (L1) ---

export async function listExposures(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<SupplierProductExposure[]> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${PATH}/exposures`,
  });
  return z.array(supplierProductExposureSchema).parse(raw);
}

export async function exposeProduct(
  workspace: PosWorkspaceScope,
  input: ExposeProductInput,
  signal?: AbortSignal,
): Promise<SupplierProductExposure> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/exposures`,
    body: {
      productId: input.productId,
      supplierOrderPrice: input.supplierOrderPrice,
      isOrderable: input.isOrderable ?? true,
    },
  });
  return supplierProductExposureSchema.parse(raw);
}

export async function updateExposure(
  workspace: PosWorkspaceScope,
  exposureId: string,
  input: UpdateExposureInput,
  signal?: AbortSignal,
): Promise<SupplierProductExposure> {
  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: `${PATH}/exposures/${exposureId}`,
    body: input,
  });
  return supplierProductExposureSchema.parse(raw);
}

// --- Buyer product shares (L2) ---

export async function queryBuyerProductShares(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  options: {
    query?: string;
    category?: string;
    shareFilter?: ShareFilter;
    page?: number;
    pageSize?: number;
  } = {},
  signal?: AbortSignal,
): Promise<BuyerProductShareQueryResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(relPath(relationshipId, "/buyer-product-shares"), {
      query: options.query,
      category: options.category,
      shareFilter: options.shareFilter,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 25,
    }),
  });
  return buyerProductShareQueryResultSchema.parse(raw);
}

export async function listBuyerProductShares(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  signal?: AbortSignal,
): Promise<ConnectedBuyerProductShare[]> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: relPath(relationshipId, "/buyer-product-shares"),
  });
  return z.array(connectedBuyerProductShareSchema).parse(raw);
}

export async function setBuyerProductShares(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  products: SetBuyerProductShareItem[],
  signal?: AbortSignal,
): Promise<ConnectedBuyerProductShare[]> {
  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: relPath(relationshipId, "/buyer-product-shares"),
    body: { products },
  });
  return z.array(connectedBuyerProductShareSchema).parse(raw);
}

export async function confirmBuyerProductSharing(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  input: {
    productIds: string[];
    establishDefaultPoPrices?: Record<string, number> | null;
  },
  signal?: AbortSignal,
): Promise<ConnectedBuyerProductShare[]> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: relPath(relationshipId, "/buyer-product-shares/confirm"),
    body: {
      productIds: input.productIds,
      establishDefaultPoPrices: input.establishDefaultPoPrices ?? null,
    },
  });
  return z.array(connectedBuyerProductShareSchema).parse(raw);
}

export async function bulkMutateBuyerProductShares(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  input: BulkShareMutationInput,
  signal?: AbortSignal,
): Promise<BulkBuyerProductShareMutationResult> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: relPath(relationshipId, "/buyer-product-shares/bulk"),
    body: {
      operation: input.operation,
      productIds: input.productIds ?? null,
      selectAllMatching: input.selectAllMatching ?? false,
      query: input.query ?? null,
      category: input.category ?? null,
      shareFilter: input.shareFilter ?? null,
      establishDefaultPoPrices: input.establishDefaultPoPrices ?? null,
    },
  });
  return bulkBuyerProductShareMutationResultSchema.parse(raw);
}

export async function previewBuyerProductPricing(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  input: BulkPricingInput,
  signal?: AbortSignal,
): Promise<BulkBuyerPricingPreview> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: relPath(relationshipId, "/buyer-product-shares/pricing/preview"),
    body: {
      mode: input.mode,
      productIds: input.productIds ?? null,
      selectAllMatching: input.selectAllMatching ?? false,
      query: input.query ?? null,
      category: input.category ?? null,
      shareFilter: input.shareFilter ?? null,
      percent: input.percent ?? null,
      amount: input.amount ?? null,
      fixedPrice: input.fixedPrice ?? null,
    },
  });
  return bulkBuyerPricingPreviewSchema.parse(raw);
}

export async function applyBuyerProductPricing(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  input: BulkPricingInput,
  signal?: AbortSignal,
): Promise<BulkBuyerProductShareMutationResult> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: relPath(relationshipId, "/buyer-product-shares/pricing/apply"),
    body: {
      mode: input.mode,
      productIds: input.productIds ?? null,
      selectAllMatching: input.selectAllMatching ?? false,
      query: input.query ?? null,
      category: input.category ?? null,
      shareFilter: input.shareFilter ?? null,
      percent: input.percent ?? null,
      amount: input.amount ?? null,
      fixedPrice: input.fixedPrice ?? null,
    },
  });
  return bulkBuyerProductShareMutationResultSchema.parse(raw);
}

// --- Catalog / readiness / match ---

export async function searchExposedCatalog(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  options: {
    query?: string;
    category?: string;
    page?: number;
    pageSize?: number;
  } = {},
  signal?: AbortSignal,
): Promise<PagedExposureResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(relPath(relationshipId, "/catalog"), {
      query: options.query,
      category: options.category,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 25,
    }),
  });
  return pagedExposureResultSchema.parse(raw);
}

export async function classifyCatalogReadiness(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  signal?: AbortSignal,
): Promise<CatalogReadinessResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: relPath(relationshipId, "/catalog/readiness"),
  });
  return catalogReadinessResultSchema.parse(raw);
}

export async function suggestBuyerProductMatches(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  exposureId: string,
  signal?: AbortSignal,
): Promise<SuggestBuyerProductMatchesResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: relPath(relationshipId, `/catalog/${exposureId}/match-suggestions`),
  });
  return suggestBuyerProductMatchesResultSchema.parse(raw);
}

// --- Links ---

export async function listLinks(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  signal?: AbortSignal,
): Promise<BuyerSupplierProductLink[]> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: relPath(relationshipId, "/links"),
  });
  return z.array(buyerSupplierProductLinkSchema).parse(raw);
}

export async function linkProduct(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  input: LinkProductInput,
  signal?: AbortSignal,
): Promise<BuyerSupplierProductLink> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: relPath(relationshipId, "/links"),
    body: {
      buyerProductId: input.buyerProductId,
      exposureId: input.exposureId,
      buyerPurchaseUnitId: input.buyerPurchaseUnitId ?? null,
      multiplierToBase: input.multiplierToBase ?? null,
      packageLabel: input.packageLabel ?? null,
    },
  });
  return buyerSupplierProductLinkSchema.parse(raw);
}

export async function createBuyerProductAndLink(
  workspace: PosWorkspaceScope,
  relationshipId: string,
  input: CreateBuyerProductAndLinkInput,
  signal?: AbortSignal,
): Promise<CreateBuyerProductAndLinkResult> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: relPath(relationshipId, "/links/create-and-link"),
    body: {
      exposureId: input.exposureId,
      name: input.name.trim(),
      unitOfMeasure: input.unitOfMeasure,
      sellingPrice: input.sellingPrice,
      sku: input.sku?.trim() || null,
      description: input.description?.trim() || null,
      categoryId: input.categoryId ?? null,
    },
  });
  return createBuyerProductAndLinkResultSchema.parse(raw);
}

export async function unlinkProduct(
  workspace: PosWorkspaceScope,
  linkId: string,
  signal?: AbortSignal,
): Promise<unknown> {
  return posRequest<unknown>({
    method: "DELETE",
    workspace,
    signal,
    path: `${PATH}/links/${linkId}`,
  });
}

// --- Business customers (supplier projection over OrganizationConnection) ---

export const businessCustomerSchema = z.object({
  connectionId: guidSchema,
  supplierOrganizationId: guidSchema,
  buyerOrganizationId: guidSchema,
  organizationDisplayName: z.string(),
  organizationPublicId: z.string().nullable().optional(),
  relationshipStatus: z.string(),
  catalogSharingMode: z.string(),
  customerDiscountPercent: z.number().nullable().optional().default(null),
  eligibleCount: z.number(),
  sharedCount: z.number(),
  excludedCount: z.number(),
  overrideCount: z.number(),
  connectedSinceUtc: isoDateSchema.nullable().optional(),
  createdAtUtc: isoDateSchema,
  updatedAtUtc: isoDateSchema,
  displayNameIsLive: z.boolean().optional().default(false),
});

export type BusinessCustomer = z.infer<typeof businessCustomerSchema>;

export async function listBusinessCustomers(
  workspace: PosWorkspaceScope,
  options?: { search?: string; includeDisconnected?: boolean },
  signal?: AbortSignal,
): Promise<BusinessCustomer[]> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(`${PATH}/business-customers`, {
      search: options?.search,
      includeDisconnected: options?.includeDisconnected ? "true" : undefined,
    }),
  });
  return z.array(businessCustomerSchema).parse(raw);
}

export async function getBusinessCustomer(
  workspace: PosWorkspaceScope,
  connectionId: string,
  signal?: AbortSignal,
): Promise<BusinessCustomer> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${PATH}/business-customers/${connectionId}`,
  });
  return businessCustomerSchema.parse(raw);
}
