import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";

const DEFINITIONS_PATH = "/api/v1/pos/inventory/production/definitions";
const RUNS_PATH = "/api/v1/pos/inventory/production/runs";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const productionComponentDtoSchema = z.object({
  componentId: guidSchema,
  materialProductId: guidSchema,
  productUnitId: guidSchema.nullable().optional(),
  sortOrder: z.number(),
  quantityEntered: z.number(),
  multiplierToBase: z.number(),
  baseQuantity: z.number(),
});

export const productionDefinitionDtoSchema = z.object({
  productionDefinitionId: guidSchema,
  organizationId: guidSchema,
  name: z.string(),
  outputProductId: guidSchema,
  outputProductUnitId: guidSchema.nullable().optional(),
  outputQuantityEntered: z.number(),
  outputMultiplierToBase: z.number(),
  outputBaseQuantity: z.number(),
  status: z.string(),
  isActive: z.boolean(),
  revision: z.number(),
  createdByUserId: guidSchema,
  createdAtUtc: z.string(),
  updatedByUserId: guidSchema.nullable().optional(),
  updatedAtUtc: z.string().nullable().optional(),
  components: z.array(productionComponentDtoSchema),
});

export const productionDefinitionListItemDtoSchema = z.object({
  productionDefinitionId: guidSchema,
  name: z.string(),
  outputProductId: guidSchema,
  outputQuantityEntered: z.number(),
  outputBaseQuantity: z.number(),
  status: z.string(),
  isActive: z.boolean(),
  componentCount: z.number(),
  revision: z.number(),
  createdAtUtc: z.string(),
});

export const productionDefinitionPagedResultSchema = z.object({
  items: z.array(productionDefinitionListItemDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export const productionRunMaterialDtoSchema = z.object({
  materialId: guidSchema,
  materialProductId: guidSchema,
  productUnitId: guidSchema.nullable().optional(),
  lineNumber: z.number(),
  expectedQuantityEntered: z.number(),
  actualQuantityEntered: z.number(),
  multiplierToBase: z.number(),
  expectedBaseQuantity: z.number(),
  actualBaseQuantity: z.number(),
  nameSnapshot: z.string(),
  unitLabelSnapshot: z.string(),
  unitCostSnapshot: z.number().nullable().optional(),
  lineCostSnapshot: z.number().nullable().optional(),
  inventoryMovementId: guidSchema.nullable().optional(),
});

export const productionRunDtoSchema = z.object({
  productionRunId: guidSchema,
  organizationId: guidSchema,
  branchId: guidSchema.nullable().optional(),
  productionNumber: z.string(),
  referenceNumber: z.string().nullable().optional(),
  productionDefinitionId: guidSchema,
  productionDefinitionRevision: z.number(),
  productionDefinitionNameSnapshot: z.string(),
  outputProductId: guidSchema,
  outputProductUnitId: guidSchema.nullable().optional(),
  outputQuantityEntered: z.number(),
  outputMultiplierToBase: z.number(),
  outputBaseQuantity: z.number(),
  outputNameSnapshot: z.string(),
  outputUnitLabelSnapshot: z.string(),
  producedAtUtc: z.string(),
  outputExpirationDate: z.string().nullable().optional(),
  outputLotNumber: z.string().nullable().optional(),
  status: z.string(),
  costStatus: z.string(),
  totalMaterialCost: z.number().nullable().optional(),
  outputBaseUnitCost: z.number().nullable().optional(),
  notes: z.string().nullable().optional(),
  createdByUserId: guidSchema,
  createdAtUtc: z.string(),
  voidedByUserId: guidSchema.nullable().optional(),
  voidedAtUtc: z.string().nullable().optional(),
  outputInventoryMovementId: guidSchema.nullable().optional(),
  materials: z.array(productionRunMaterialDtoSchema),
});

export const productionRunListItemDtoSchema = z.object({
  productionRunId: guidSchema,
  productionNumber: z.string(),
  branchId: guidSchema.nullable().optional(),
  outputProductId: guidSchema,
  outputNameSnapshot: z.string(),
  outputBaseQuantity: z.number(),
  status: z.string(),
  costStatus: z.string(),
  totalMaterialCost: z.number().nullable().optional(),
  outputBaseUnitCost: z.number().nullable().optional(),
  producedAtUtc: z.string(),
  createdAtUtc: z.string(),
});

export const productionRunPagedResultSchema = z.object({
  items: z.array(productionRunListItemDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type ProductionComponentDto = z.infer<typeof productionComponentDtoSchema>;
export type ProductionDefinitionDto = z.infer<typeof productionDefinitionDtoSchema>;
export type ProductionDefinitionListItemDto = z.infer<
  typeof productionDefinitionListItemDtoSchema
>;
export type ProductionDefinitionPagedResult = z.infer<
  typeof productionDefinitionPagedResultSchema
>;
export type ProductionRunMaterialDto = z.infer<typeof productionRunMaterialDtoSchema>;
export type ProductionRunDto = z.infer<typeof productionRunDtoSchema>;
export type ProductionRunListItemDto = z.infer<typeof productionRunListItemDtoSchema>;
export type ProductionRunPagedResult = z.infer<typeof productionRunPagedResultSchema>;

export type CreateProductionComponentRequest = {
  materialProductId: string;
  quantity: number;
  productUnitId?: string | null;
  sortOrder?: number | null;
};

export type CreateProductionDefinitionRequest = {
  name: string;
  outputProductId: string;
  outputQuantity: number;
  components: CreateProductionComponentRequest[];
  outputProductUnitId?: string | null;
  productionDefinitionId?: string | null;
};

export type UpdateProductionDefinitionRequest = {
  name: string;
  outputProductId: string;
  outputQuantity: number;
  components: CreateProductionComponentRequest[];
  outputProductUnitId?: string | null;
};

export type CreateProductionRunMaterialOverrideRequest = {
  materialProductId: string;
  actualQuantity: number;
  productUnitId?: string | null;
};

export type CreateProductionRunRequest = {
  productionDefinitionId: string;
  outputQuantity: number;
  outputProductUnitId?: string | null;
  branchId?: string | null;
  referenceNumber?: string | null;
  notes?: string | null;
  producedAtUtc?: string | null;
  outputExpirationDate?: string | null;
  outputLotNumber?: string | null;
  materialOverrides?: CreateProductionRunMaterialOverrideRequest[] | null;
  productionRunId?: string | null;
};

export type ListProductionDefinitionsOptions = {
  page?: number;
  pageSize?: number;
  search?: string;
  outputProductId?: string;
  isActive?: boolean;
};

export type ListProductionRunsOptions = {
  page?: number;
  pageSize?: number;
  fromProducedAtUtc?: string;
  toProducedAtUtc?: string;
  status?: string;
  branchId?: string;
  outputProductId?: string;
  productionDefinitionId?: string;
  referenceNumber?: string;
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

export async function listProductionDefinitions(
  workspace: PosWorkspaceScope,
  options: ListProductionDefinitionsOptions = {},
  signal?: AbortSignal,
): Promise<ProductionDefinitionPagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(DEFINITIONS_PATH, {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
      search: options.search,
      outputProductId: options.outputProductId,
      isActive: options.isActive,
    }),
  });
  return productionDefinitionPagedResultSchema.parse(raw);
}

export async function getProductionDefinition(
  workspace: PosWorkspaceScope,
  definitionId: string,
  signal?: AbortSignal,
): Promise<ProductionDefinitionDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${DEFINITIONS_PATH}/${definitionId}`,
  });
  return productionDefinitionDtoSchema.parse(raw);
}

export async function createProductionDefinition(
  workspace: PosWorkspaceScope,
  body: CreateProductionDefinitionRequest,
  signal?: AbortSignal,
): Promise<ProductionDefinitionDto> {
  const payload: Record<string, unknown> = {
    name: body.name.trim(),
    outputProductId: body.outputProductId,
    outputQuantity: body.outputQuantity,
    components: body.components.map((c, index) => {
      const entry: Record<string, unknown> = {
        materialProductId: c.materialProductId,
        quantity: c.quantity,
        sortOrder: c.sortOrder ?? index,
      };
      if (c.productUnitId) {
        entry.productUnitId = c.productUnitId;
      }
      return entry;
    }),
  };
  if (body.outputProductUnitId) {
    payload.outputProductUnitId = body.outputProductUnitId;
  }
  if (body.productionDefinitionId) {
    payload.productionDefinitionId = body.productionDefinitionId;
  }

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: DEFINITIONS_PATH,
    body: payload,
  });
  return productionDefinitionDtoSchema.parse(raw);
}

export async function updateProductionDefinition(
  workspace: PosWorkspaceScope,
  definitionId: string,
  body: UpdateProductionDefinitionRequest,
  signal?: AbortSignal,
): Promise<ProductionDefinitionDto> {
  const payload: Record<string, unknown> = {
    name: body.name.trim(),
    outputProductId: body.outputProductId,
    outputQuantity: body.outputQuantity,
    components: body.components.map((c, index) => {
      const entry: Record<string, unknown> = {
        materialProductId: c.materialProductId,
        quantity: c.quantity,
        sortOrder: c.sortOrder ?? index,
      };
      if (c.productUnitId) {
        entry.productUnitId = c.productUnitId;
      }
      return entry;
    }),
  };
  if (body.outputProductUnitId) {
    payload.outputProductUnitId = body.outputProductUnitId;
  }

  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: `${DEFINITIONS_PATH}/${definitionId}`,
    body: payload,
  });
  return productionDefinitionDtoSchema.parse(raw);
}

export async function setProductionDefinitionActive(
  workspace: PosWorkspaceScope,
  definitionId: string,
  isActive: boolean,
  signal?: AbortSignal,
): Promise<ProductionDefinitionDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${DEFINITIONS_PATH}/${definitionId}/set-active`,
    body: { isActive },
  });
  return productionDefinitionDtoSchema.parse(raw);
}

export async function listProductionRuns(
  workspace: PosWorkspaceScope,
  options: ListProductionRunsOptions = {},
  signal?: AbortSignal,
): Promise<ProductionRunPagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(RUNS_PATH, {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
      fromProducedAtUtc: options.fromProducedAtUtc,
      toProducedAtUtc: options.toProducedAtUtc,
      status: options.status,
      branchId: options.branchId,
      outputProductId: options.outputProductId,
      productionDefinitionId: options.productionDefinitionId,
      referenceNumber: options.referenceNumber,
    }),
  });
  return productionRunPagedResultSchema.parse(raw);
}

export async function getProductionRun(
  workspace: PosWorkspaceScope,
  productionRunId: string,
  signal?: AbortSignal,
): Promise<ProductionRunDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${RUNS_PATH}/${productionRunId}`,
  });
  return productionRunDtoSchema.parse(raw);
}

/**
 * Creates a production run (consumes materials, posts output). Online-only mutation.
 * Idempotency via headers when productionRunId is provided (or auto-generated).
 */
export async function createProductionRun(
  workspace: PosWorkspaceScope,
  body: CreateProductionRunRequest,
  signal?: AbortSignal,
): Promise<ProductionRunDto> {
  const productionRunId = body.productionRunId?.trim() || crypto.randomUUID();
  const payload: Record<string, unknown> = {
    productionDefinitionId: body.productionDefinitionId,
    outputQuantity: body.outputQuantity,
    productionRunId,
  };
  if (body.outputProductUnitId) {
    payload.outputProductUnitId = body.outputProductUnitId;
  }
  if (body.branchId) {
    payload.branchId = body.branchId;
  }
  const reference = trimOrUndef(body.referenceNumber);
  if (reference) {
    payload.referenceNumber = reference;
  }
  const notes = trimOrUndef(body.notes);
  if (notes) {
    payload.notes = notes;
  }
  if (body.producedAtUtc) {
    payload.producedAtUtc = body.producedAtUtc;
  }
  if (body.outputExpirationDate) {
    payload.outputExpirationDate = body.outputExpirationDate;
  }
  const lot = trimOrUndef(body.outputLotNumber);
  if (lot) {
    payload.outputLotNumber = lot;
  }
  if (body.materialOverrides && body.materialOverrides.length > 0) {
    payload.materialOverrides = body.materialOverrides.map((ov) => {
      const entry: Record<string, unknown> = {
        materialProductId: ov.materialProductId,
        actualQuantity: ov.actualQuantity,
      };
      if (ov.productUnitId) {
        entry.productUnitId = ov.productUnitId;
      }
      return entry;
    });
  }

  const headers = await buildPosMutationIdempotencyHeaders(
    productionRunId,
    JSON.stringify(payload),
    OFFLINE_OPERATION_TYPES.ProductionRun,
  );

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: RUNS_PATH,
    body: payload,
    headers,
  });
  return productionRunDtoSchema.parse(raw);
}

export async function voidProductionRun(
  workspace: PosWorkspaceScope,
  productionRunId: string,
  signal?: AbortSignal,
): Promise<ProductionRunDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${RUNS_PATH}/${productionRunId}/void`,
  });
  return productionRunDtoSchema.parse(raw);
}
