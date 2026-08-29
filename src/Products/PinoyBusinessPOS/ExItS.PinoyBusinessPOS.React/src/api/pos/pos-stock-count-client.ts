import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const PATH = "/api/v1/pos/inventory/stock-counts";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const STOCK_COUNT_STATUSES = ["Draft", "InProgress", "Completed", "Cancelled"] as const;
export type StockCountStatusCode = (typeof STOCK_COUNT_STATUSES)[number];

/** Domain max line count — keep client "count all" bounded to the same limit. */
export const STOCK_COUNT_MAX_LINES = 500;

export const stockCountLineDtoSchema = z.object({
  lineId: guidSchema,
  productId: guidSchema,
  productName: z.string(),
  unitOfMeasure: z.string(),
  lineNumber: z.number(),
  systemOnHandSnapshot: z.number().nullable().optional(),
  countedQuantity: z.number().nullable().optional(),
  variance: z.number().nullable().optional(),
});

export const stockCountDtoSchema = z.object({
  stockCountId: guidSchema,
  organizationId: guidSchema,
  countNumber: z.string().nullable().optional(),
  title: z.string(),
  status: z.string(),
  countDate: z.string(),
  notes: z.string().nullable().optional(),
  startedAtUtc: z.string().nullable().optional(),
  startedBy: guidSchema.nullable().optional(),
  completedAtUtc: z.string().nullable().optional(),
  completedBy: guidSchema.nullable().optional(),
  cancelledAtUtc: z.string().nullable().optional(),
  cancelledBy: guidSchema.nullable().optional(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  lines: z.array(stockCountLineDtoSchema),
});

export const stockCountPagedResultSchema = z.object({
  items: z.array(stockCountDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type StockCountLineDto = z.infer<typeof stockCountLineDtoSchema>;
export type StockCountDto = z.infer<typeof stockCountDtoSchema>;
export type StockCountPagedResult = z.infer<typeof stockCountPagedResultSchema>;

export type CreateStockCountLineRequest = {
  productId: string;
  countedQuantity?: number | null;
};

export type CreateStockCountRequest = {
  title: string;
  lines: CreateStockCountLineRequest[];
  countDate?: string | null;
  notes?: string | null;
};

export type UpdateStockCountRequest = {
  lines: CreateStockCountLineRequest[];
  title?: string | null;
  countDate?: string | null;
  notes?: string | null;
};

export type ListStockCountsOptions = {
  page?: number;
  pageSize?: number;
  status?: string;
  countNumber?: string;
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

export async function listStockCounts(
  workspace: PosWorkspaceScope,
  options: ListStockCountsOptions = {},
  signal?: AbortSignal,
): Promise<StockCountPagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(PATH, {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
      status: options.status,
      countNumber: options.countNumber,
    }),
  });
  return stockCountPagedResultSchema.parse(raw);
}

export async function getStockCount(
  workspace: PosWorkspaceScope,
  stockCountId: string,
  signal?: AbortSignal,
): Promise<StockCountDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${PATH}/${stockCountId}`,
  });
  return stockCountDtoSchema.parse(raw);
}

export async function createStockCount(
  workspace: PosWorkspaceScope,
  body: CreateStockCountRequest,
  signal?: AbortSignal,
): Promise<StockCountDto> {
  const payload: Record<string, unknown> = {
    title: body.title.trim(),
    lines: body.lines.map((line) => {
      const entry: Record<string, unknown> = { productId: line.productId };
      if (line.countedQuantity != null) {
        entry.countedQuantity = line.countedQuantity;
      }
      return entry;
    }),
  };
  if (body.countDate) {
    payload.countDate = body.countDate;
  }
  const notes = trimOrUndef(body.notes);
  if (notes) {
    payload.notes = notes;
  }

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: PATH,
    body: payload,
  });
  return stockCountDtoSchema.parse(raw);
}

export async function updateStockCount(
  workspace: PosWorkspaceScope,
  stockCountId: string,
  body: UpdateStockCountRequest,
  signal?: AbortSignal,
): Promise<StockCountDto> {
  const payload: Record<string, unknown> = {
    lines: body.lines.map((line) => {
      const entry: Record<string, unknown> = { productId: line.productId };
      if (line.countedQuantity !== undefined) {
        entry.countedQuantity = line.countedQuantity;
      }
      return entry;
    }),
  };
  if (body.title != null) {
    payload.title = body.title.trim();
  }
  if (body.countDate != null) {
    payload.countDate = body.countDate;
  }
  if (body.notes !== undefined) {
    payload.notes = trimOrUndef(body.notes) ?? null;
  }

  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: `${PATH}/${stockCountId}`,
    body: payload,
  });
  return stockCountDtoSchema.parse(raw);
}

export async function startStockCount(
  workspace: PosWorkspaceScope,
  stockCountId: string,
  signal?: AbortSignal,
): Promise<StockCountDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${stockCountId}/start`,
  });
  return stockCountDtoSchema.parse(raw);
}

export async function completeStockCount(
  workspace: PosWorkspaceScope,
  stockCountId: string,
  signal?: AbortSignal,
): Promise<StockCountDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${stockCountId}/complete`,
  });
  return stockCountDtoSchema.parse(raw);
}

export async function cancelStockCount(
  workspace: PosWorkspaceScope,
  stockCountId: string,
  signal?: AbortSignal,
): Promise<StockCountDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${stockCountId}/cancel`,
  });
  return stockCountDtoSchema.parse(raw);
}
