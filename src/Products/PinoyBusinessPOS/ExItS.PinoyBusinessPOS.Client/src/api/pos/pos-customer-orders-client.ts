import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const customerOrderLineSchema = z.object({
  lineId: guidSchema,
  productId: guidSchema,
  lineNumber: z.number(),
  nameSnapshot: z.string(),
  skuSnapshot: z.string().nullable().optional(),
  unitSnapshot: z.string(),
  quantity: z.number(),
  unitPrice: z.number(),
  discount: z.number(),
  lineTotal: z.number(),
});

export const customerOrderDeliverySchema = z.object({
  recipientName: z.string(),
  recipientPhone: z.string().nullable().optional(),
  addressLine1: z.string(),
  addressLine2: z.string().nullable().optional(),
  city: z.string().nullable().optional(),
  deliveryNotes: z.string().nullable().optional(),
  destinationLatitude: z.number(),
  destinationLongitude: z.number(),
  branchLatitudeSnapshot: z.number(),
  branchLongitudeSnapshot: z.number(),
  distanceKm: z.number(),
  minimumOrderAmountSnapshot: z.number(),
  baseDeliveryFeeSnapshot: z.number(),
  includedDistanceKmSnapshot: z.number(),
  additionalFeePerKmSnapshot: z.number(),
  maximumDeliveryDistanceKmSnapshot: z.number(),
  freeDeliveryThresholdSnapshot: z.number().nullable().optional(),
  distanceCharge: z.number(),
  finalDeliveryFee: z.number(),
  freeDeliveryApplied: z.boolean(),
});

export const customerOrderSchema = z.object({
  orderId: guidSchema,
  sellerOrganizationId: guidSchema,
  orderNumber: z.string(),
  status: z.string(),
  fulfillmentStatus: z.string(),
  paymentStatus: z.string(),
  paymentMethod: z.string(),
  fulfillmentType: z.string(),
  fulfillmentBranchId: guidSchema,
  branchNameSnapshot: z.string(),
  customerPartyType: z.string(),
  customerDisplayName: z.string(),
  customerPlatformUserId: guidSchema.nullable().optional(),
  customerBuyerOrganizationId: guidSchema.nullable().optional(),
  customerBuyerPublicOrganizationId: z.string().nullable().optional(),
  merchandiseSubtotal: z.number(),
  deliveryFee: z.number(),
  total: z.number(),
  stockReservationState: z.string(),
  rejectReason: z.string().nullable().optional(),
  rejectNotes: z.string().nullable().optional(),
  delivery: customerOrderDeliverySchema.nullable().optional(),
  lines: z.array(customerOrderLineSchema),
  createdAtUtc: z.string(),
  submittedAtUtc: z.string().nullable().optional(),
  acceptedAtUtc: z.string().nullable().optional(),
  readyAtUtc: z.string().nullable().optional(),
  readyBy: guidSchema.nullable().optional(),
  outForDeliveryAtUtc: z.string().nullable().optional(),
  outForDeliveryBy: guidSchema.nullable().optional(),
  deliveredAtUtc: z.string().nullable().optional(),
  deliveredBy: guidSchema.nullable().optional(),
  collectedAtUtc: z.string().nullable().optional(),
  collectedBy: guidSchema.nullable().optional(),
  completedAtUtc: z.string().nullable().optional(),
  updatedAtUtc: z.string(),
});

export const customerOrderListItemSchema = z.object({
  orderId: guidSchema,
  orderNumber: z.string(),
  status: z.string(),
  fulfillmentStatus: z.string(),
  fulfillmentType: z.string(),
  fulfillmentBranchId: guidSchema,
  branchNameSnapshot: z.string(),
  customerDisplayName: z.string(),
  total: z.number(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  lineCount: z.number(),
  sellerOrganizationId: guidSchema,
});

export const customerOrderPagedSchema = z.object({
  items: z.array(customerOrderListItemSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export const storefrontCategorySchema = z.object({
  categoryId: guidSchema,
  name: z.string(),
});

export const storefrontProductSchema = z.object({
  productId: guidSchema,
  name: z.string(),
  sku: z.string().nullable().optional(),
  unitOfMeasure: z.string(),
  categoryId: guidSchema.nullable().optional(),
  unitPrice: z.number(),
  isAvailable: z.boolean(),
  tracksInventory: z.boolean().optional().default(false),
  availableQuantity: z.number().nullable().optional(),
  availabilityStatus: z.string().optional().default("Untracked"),
  hasImage: z.boolean().optional().default(false),
  imageVersion: z.number().nullable().optional(),
  imageSource: z.string().optional().default("None"),
});

export const storefrontBranchSchema = z.object({
  branchId: guidSchema,
  name: z.string(),
  pickupEnabled: z.boolean(),
  deliveryEnabled: z.boolean(),
  customerOrderingOperational: z.boolean(),
  pickupOperational: z.boolean(),
  deliveryOperational: z.boolean(),
  onlineOrdersPaused: z.boolean(),
  storeStatusMessage: z.string().nullable().optional(),
});

export const customerStorefrontSchema = z.object({
  organizationId: guidSchema,
  organizationDisplayName: z.string(),
  canCustomerOrder: z.boolean(),
  canCustomerDelivery: z.boolean(),
  categories: z.array(storefrontCategorySchema),
  products: z.array(storefrontProductSchema),
  productTotalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
  branches: z.array(storefrontBranchSchema),
});

export const quoteDeliverySchema = z.object({
  available: z.boolean(),
  unavailableReason: z.string().nullable().optional(),
  distanceKm: z.number(),
  extraDistanceKm: z.number(),
  distanceCharge: z.number(),
  deliveryFee: z.number(),
  freeDeliveryApplied: z.boolean(),
  minimumOrderAmount: z.number(),
  maximumDeliveryDistanceKm: z.number(),
});

export type CustomerOrderDto = z.infer<typeof customerOrderSchema>;
export type CustomerOrderListItemDto = z.infer<typeof customerOrderListItemSchema>;
export type CustomerOrderPagedResult = z.infer<typeof customerOrderPagedSchema>;
export type CustomerStorefrontDto = z.infer<typeof customerStorefrontSchema>;
export type CustomerStorefrontProductDto = z.infer<typeof storefrontProductSchema>;
export type CustomerStorefrontBranchDto = z.infer<typeof storefrontBranchSchema>;
export type QuoteCustomerOrderDeliveryDto = z.infer<typeof quoteDeliverySchema>;

export type PlaceCustomerOrderLineRequest = {
  productId: string;
  quantity: number;
  discount?: number;
};

export type PlaceCustomerOrderDeliveryRequest = {
  recipientName: string;
  recipientPhone?: string | null;
  addressLine1: string;
  addressLine2?: string | null;
  city?: string | null;
  deliveryNotes?: string | null;
  destinationLatitude: number;
  destinationLongitude: number;
};

export type PlaceCustomerOrderRequest = {
  fulfillmentType: string;
  fulfillmentBranchId: string;
  customerPartyType: string;
  customerDisplayName: string;
  customerPlatformUserId?: string | null;
  customerBuyerOrganizationId?: string | null;
  customerBuyerPublicOrganizationId?: string | null;
  lines: PlaceCustomerOrderLineRequest[];
  delivery?: PlaceCustomerOrderDeliveryRequest | null;
  clientOrderId?: string | null;
  idempotencyKey?: string | null;
  paymentMethod?: string | null;
};

export type QuoteCustomerOrderDeliveryRequest = {
  fulfillmentBranchId: string;
  merchandiseSubtotal: number;
  destinationLatitude: number;
  destinationLongitude: number;
};

export type RejectCustomerOrderRequest = {
  reason: string;
  notes?: string | null;
};

function sellerPath(organizationId: string): string {
  return `/api/v1/pos/organizations/${organizationId}/customer-orders`;
}

function customerSellerPath(sellerOrganizationId: string): string {
  return `/api/v1/pos/customer-orders/organizations/${sellerOrganizationId}`;
}

function appendQuery(params: Record<string, string | number | undefined | null>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === "") {
      continue;
    }
    search.set(key, String(value));
  }
  const qs = search.toString();
  return qs ? `?${qs}` : "";
}

/** Seller org scope header — required for seller APIs; reused as path seller id for buyer APIs. */
export function sellerWorkspace(
  organizationId: string,
  branchId?: string | null,
): PosWorkspaceScope {
  return { organizationId, branchId: branchId ?? undefined };
}

export async function listSellerCustomerOrders(
  workspace: PosWorkspaceScope,
  options: {
    status?: string;
    fulfillmentType?: string;
    branchId?: string;
    orderNumber?: string;
    page?: number;
    pageSize?: number;
  } = {},
  signal?: AbortSignal,
): Promise<CustomerOrderPagedResult> {
  const raw = await posRequest<unknown>({
    workspace,
    path: `${sellerPath(workspace.organizationId)}${appendQuery({
      status: options.status,
      fulfillmentType: options.fulfillmentType,
      branchId: options.branchId,
      orderNumber: options.orderNumber,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
    })}`,
    signal,
  });
  return customerOrderPagedSchema.parse(raw);
}

export async function getSellerCustomerOrder(
  workspace: PosWorkspaceScope,
  orderId: string,
  signal?: AbortSignal,
): Promise<CustomerOrderDto> {
  const raw = await posRequest<unknown>({
    workspace,
    path: `${sellerPath(workspace.organizationId)}/${orderId}`,
    signal,
  });
  return customerOrderSchema.parse(raw);
}

export async function acceptSellerCustomerOrder(
  workspace: PosWorkspaceScope,
  orderId: string,
): Promise<CustomerOrderDto> {
  const headers = await buildPosMutationIdempotencyHeaders(
    orderId,
    "{}",
    OFFLINE_OPERATION_TYPES.CustomerOrderAccept,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    path: `${sellerPath(workspace.organizationId)}/${orderId}/accept`,
    body: {},
    headers,
  });
  return customerOrderSchema.parse(raw);
}

export async function rejectSellerCustomerOrder(
  workspace: PosWorkspaceScope,
  orderId: string,
  request: RejectCustomerOrderRequest,
): Promise<CustomerOrderDto> {
  const json = JSON.stringify(request);
  const headers = await buildPosMutationIdempotencyHeaders(
    orderId,
    json,
    OFFLINE_OPERATION_TYPES.CustomerOrderReject,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    path: `${sellerPath(workspace.organizationId)}/${orderId}/reject`,
    body: request,
    headers,
  });
  return customerOrderSchema.parse(raw);
}

export async function completeSellerCustomerOrder(
  workspace: PosWorkspaceScope,
  orderId: string,
): Promise<CustomerOrderDto> {
  const headers = await buildPosMutationIdempotencyHeaders(
    orderId,
    "{}",
    OFFLINE_OPERATION_TYPES.CustomerOrderComplete,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    path: `${sellerPath(workspace.organizationId)}/${orderId}/complete`,
    body: {},
    headers,
  });
  return customerOrderSchema.parse(raw);
}

async function postSellerFulfillment(
  workspace: PosWorkspaceScope,
  orderId: string,
  action: string,
): Promise<CustomerOrderDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    path: `${sellerPath(workspace.organizationId)}/${orderId}/${action}`,
    body: {},
  });
  return customerOrderSchema.parse(raw);
}

export const startPreparingSellerCustomerOrder = (w: PosWorkspaceScope, id: string) =>
  postSellerFulfillment(w, id, "start-preparing");
export const markReadySellerCustomerOrder = (w: PosWorkspaceScope, id: string) =>
  postSellerFulfillment(w, id, "mark-ready");
export const markOutForDeliverySellerCustomerOrder = (w: PosWorkspaceScope, id: string) =>
  postSellerFulfillment(w, id, "mark-out-for-delivery");
export const markDeliveredSellerCustomerOrder = (w: PosWorkspaceScope, id: string) =>
  postSellerFulfillment(w, id, "mark-delivered");
export const markCollectedSellerCustomerOrder = (w: PosWorkspaceScope, id: string) =>
  postSellerFulfillment(w, id, "mark-collected");

export async function listMyCustomerOrders(
  workspace: PosWorkspaceScope,
  options: {
    partyType?: string;
    buyerOrganizationId?: string;
    page?: number;
    pageSize?: number;
  } = {},
  signal?: AbortSignal,
): Promise<CustomerOrderPagedResult> {
  const raw = await posRequest<unknown>({
    workspace,
    path: `/api/v1/pos/customer-orders/mine${appendQuery({
      partyType: options.partyType ?? "Personal",
      buyerOrganizationId: options.buyerOrganizationId,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
    })}`,
    signal,
  });
  return customerOrderPagedSchema.parse(raw);
}

export async function getMyCustomerOrder(
  workspace: PosWorkspaceScope,
  orderId: string,
  options: { partyType?: string; buyerOrganizationId?: string } = {},
  signal?: AbortSignal,
): Promise<CustomerOrderDto> {
  const raw = await posRequest<unknown>({
    workspace,
    path: `/api/v1/pos/customer-orders/mine/${orderId}${appendQuery({
      partyType: options.partyType ?? "Personal",
      buyerOrganizationId: options.buyerOrganizationId,
    })}`,
    signal,
  });
  return customerOrderSchema.parse(raw);
}

export async function getCustomerStorefront(
  workspace: PosWorkspaceScope,
  sellerOrganizationId: string,
  options: {
    search?: string;
    categoryId?: string;
    page?: number;
    pageSize?: number;
    fulfillmentBranchId?: string;
  } = {},
  signal?: AbortSignal,
): Promise<CustomerStorefrontDto> {
  const raw = await posRequest<unknown>({
    workspace,
    path: `${customerSellerPath(sellerOrganizationId)}/storefront${appendQuery({
      search: options.search,
      categoryId: options.categoryId,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 40,
      fulfillmentBranchId: options.fulfillmentBranchId,
    })}`,
    signal,
  });
  return customerStorefrontSchema.parse(raw);
}

export async function quoteCustomerDelivery(
  workspace: PosWorkspaceScope,
  sellerOrganizationId: string,
  request: QuoteCustomerOrderDeliveryRequest,
): Promise<QuoteCustomerOrderDeliveryDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    path: `${customerSellerPath(sellerOrganizationId)}/quote-delivery`,
    body: request,
  });
  return quoteDeliverySchema.parse(raw);
}

export async function placeCustomerOrder(
  workspace: PosWorkspaceScope,
  sellerOrganizationId: string,
  request: PlaceCustomerOrderRequest,
): Promise<CustomerOrderDto> {
  let headers: Record<string, string> | undefined;
  if (request.clientOrderId) {
    const json = JSON.stringify(request);
    headers = await buildPosMutationIdempotencyHeaders(
      request.clientOrderId,
      json,
      OFFLINE_OPERATION_TYPES.CustomerOrderPlace,
    );
  }
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    path: customerSellerPath(sellerOrganizationId),
    body: request,
    headers,
  });
  return customerOrderSchema.parse(raw);
}

export async function cancelMyCustomerOrder(
  workspace: PosWorkspaceScope,
  sellerOrganizationId: string,
  orderId: string,
): Promise<CustomerOrderDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    path: `${customerSellerPath(sellerOrganizationId)}/${orderId}/cancel`,
    body: {},
  });
  return customerOrderSchema.parse(raw);
}

export const INSUFFICIENT_STOCK_ERROR = "pos.inventory.insufficient_stock";

export function isInsufficientStockError(error: unknown): boolean {
  if (!error || typeof error !== "object") {
    return false;
  }
  const code =
    "errorCode" in error && typeof error.errorCode === "string"
      ? error.errorCode
      : "problem" in error &&
          error.problem &&
          typeof error.problem === "object" &&
          "errorCode" in error.problem &&
          typeof (error.problem as { errorCode?: unknown }).errorCode === "string"
        ? (error.problem as { errorCode: string }).errorCode
        : undefined;
  return code === INSUFFICIENT_STOCK_ERROR;
}
