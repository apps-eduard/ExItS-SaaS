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

function appendQuery(path: string, params: Record<string, string | number | undefined>): string {
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
  body: { openingQuantity?: number | null } = {},
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
