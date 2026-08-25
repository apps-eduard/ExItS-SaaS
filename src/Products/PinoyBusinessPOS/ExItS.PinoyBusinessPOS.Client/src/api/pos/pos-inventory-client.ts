import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const INVENTORY_PATH = "/api/v1/pos/inventory";

export type PosInventoryAccountDto = {
  productId: string;
  organizationId: string;
  name: string;
  unitOfMeasure: string;
  productStatus: string;
  isTracked: boolean;
  onHandQuantity: number;
  reorderLevel?: number | null;
  stockStatus: string;
  isLowStock: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  tracksExpiration?: boolean;
  expirationWarningDays?: number | null;
  sellableQuantity?: number | null;
  expiredQuantity?: number | null;
  nearExpiryQuantity?: number | null;
};

export type PosStockMovementDto = {
  movementId: string;
  productId: string;
  inventoryAccountId: string;
  movementType: string;
  quantityEffect: number;
  reason: string;
  sourceType: string;
  sourceId?: string | null;
  recordedAtUtc: string;
  recordedBy: string;
};

export type PosInventoryLotDto = {
  lotId: string;
  productId: string;
  branchId?: string | null;
  lotNumber?: string | null;
  expirationDate: string;
  quantityOnHand: number;
  expiryStatus: string;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type PosExpiringLotDto = {
  lotId: string;
  productId: string;
  productName: string;
  sku?: string | null;
  branchId?: string | null;
  lotNumber?: string | null;
  expirationDate: string;
  quantityOnHand: number;
  expiryStatus: string;
  warningDays: number;
};

export type PosInventoryPagedResult = {
  items: PosInventoryAccountDto[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type PosStockMovementPagedResult = {
  items: PosStockMovementDto[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type PosInventoryLotPagedResult = {
  items: PosInventoryLotDto[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type PosExpiringLotPagedResult = {
  items: PosExpiringLotDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  expiredCount: number;
  nearExpiryCount: number;
};

export type PosExpiryWindow = "Expired" | "Days7" | "Days14" | "Days30" | "Custom";

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

export function listInventory(
  workspace: PosWorkspaceScope,
  options: { search?: string; page?: number; pageSize?: number } = {},
  signal?: AbortSignal,
): Promise<PosInventoryPagedResult> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(INVENTORY_PATH, {
      search: options.search,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 50,
    }),
  });
}

export function getInventoryProduct(
  workspace: PosWorkspaceScope,
  productId: string,
  signal?: AbortSignal,
): Promise<PosInventoryAccountDto> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: `${INVENTORY_PATH}/${productId}`,
  });
}

export function enableInventoryTracking(
  workspace: PosWorkspaceScope,
  productId: string,
  body: {
    openingQuantity?: number | null;
    expirationDate?: string | null;
    lotNumber?: string | null;
  } = {},
  signal?: AbortSignal,
): Promise<PosInventoryAccountDto> {
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: `${INVENTORY_PATH}/${productId}/enable`,
    body,
  });
}

export function disableInventoryTracking(
  workspace: PosWorkspaceScope,
  productId: string,
  signal?: AbortSignal,
): Promise<PosInventoryAccountDto> {
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: `${INVENTORY_PATH}/${productId}/disable`,
  });
}

export function adjustInventoryStock(
  workspace: PosWorkspaceScope,
  productId: string,
  body: {
    direction: "In" | "Out";
    quantity: number;
    reason: string;
    productUnitId?: string | null;
    expirationDate?: string | null;
    lotNumber?: string | null;
    lotId?: string | null;
  },
  signal?: AbortSignal,
): Promise<PosInventoryAccountDto> {
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: `${INVENTORY_PATH}/${productId}/adjustments`,
    body,
  });
}

export function listInventoryMovements(
  workspace: PosWorkspaceScope,
  productId: string,
  options: { page?: number; pageSize?: number } = {},
  signal?: AbortSignal,
): Promise<PosStockMovementPagedResult> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(`${INVENTORY_PATH}/${productId}/movements`, {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 50,
    }),
  });
}

export function listProductLots(
  workspace: PosWorkspaceScope,
  productId: string,
  options: { includeDepleted?: boolean; page?: number; pageSize?: number } = {},
  signal?: AbortSignal,
): Promise<PosInventoryLotPagedResult> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(`${INVENTORY_PATH}/${productId}/lots`, {
      includeDepleted: options.includeDepleted ?? false,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 50,
    }),
  });
}

export function listExpiringLots(
  workspace: PosWorkspaceScope,
  options: {
    window?: PosExpiryWindow | string;
    fromDate?: string;
    toDate?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  } = {},
  signal?: AbortSignal,
): Promise<PosExpiringLotPagedResult> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(`${INVENTORY_PATH}/lots`, {
      window: options.window ?? "Days30",
      fromDate: options.fromDate,
      toDate: options.toDate,
      search: options.search,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 50,
    }),
  });
}
