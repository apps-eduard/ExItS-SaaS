import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const SUPPLIERS_PATH = "/api/v1/pos/suppliers";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const posSupplierSchema = z.object({
  supplierId: guidSchema,
  organizationId: guidSchema,
  supplierCode: z.string(),
  name: z.string(),
  contactPerson: z.string().nullable().optional(),
  mobileNumber: z.string().nullable().optional(),
  telephoneNumber: z.string().nullable().optional(),
  email: z.string().nullable().optional(),
  addressLine1: z.string().nullable().optional(),
  addressLine2: z.string().nullable().optional(),
  cityMunicipality: z.string().nullable().optional(),
  province: z.string().nullable().optional(),
  postalCode: z.string().nullable().optional(),
  taxOrRegistrationNumber: z.string().nullable().optional(),
  notes: z.string().nullable().optional(),
  status: z.string(),
  connectionType: z.string(),
  connectedRelationshipId: guidSchema.nullable().optional(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
});

export const posSupplierPagedResultSchema = z.object({
  items: z.array(posSupplierSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type PosSupplier = z.infer<typeof posSupplierSchema>;
export type PosSupplierPagedResult = z.infer<typeof posSupplierPagedResultSchema>;

export type CreatePosSupplierInput = {
  name: string;
  contactPerson?: string | null;
  mobileNumber?: string | null;
  telephoneNumber?: string | null;
  email?: string | null;
  addressLine1?: string | null;
  addressLine2?: string | null;
  cityMunicipality?: string | null;
  province?: string | null;
  postalCode?: string | null;
  taxOrRegistrationNumber?: string | null;
  notes?: string | null;
};

export type UpdatePosSupplierInput = CreatePosSupplierInput & {
  expectedUpdatedAtUtc: string;
};

export type ListSuppliersOptions = {
  supplierCode?: string;
  name?: string;
  contactPerson?: string;
  email?: string;
  mobile?: string;
  taxOrRegistrationNumber?: string;
  status?: string;
  page?: number;
  pageSize?: number;
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

function supplierPath(supplierId: string, suffix = ""): string {
  return `${SUPPLIERS_PATH}/${supplierId}${suffix}`;
}

function trimOrNull(value: string | null | undefined): string | null {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}

function supplierBody(input: CreatePosSupplierInput) {
  return {
    name: input.name.trim(),
    contactPerson: trimOrNull(input.contactPerson),
    mobileNumber: trimOrNull(input.mobileNumber),
    telephoneNumber: trimOrNull(input.telephoneNumber),
    email: trimOrNull(input.email),
    addressLine1: trimOrNull(input.addressLine1),
    addressLine2: trimOrNull(input.addressLine2),
    cityMunicipality: trimOrNull(input.cityMunicipality),
    province: trimOrNull(input.province),
    postalCode: trimOrNull(input.postalCode),
    taxOrRegistrationNumber: trimOrNull(input.taxOrRegistrationNumber),
    notes: trimOrNull(input.notes),
  };
}

/**
 * Resolve list search: codes like SUP… go to supplierCode; otherwise name.
 */
export function resolveSupplierSearchParams(term: string): {
  supplierCode?: string;
  name?: string;
} {
  const trimmed = term.trim();
  if (!trimmed) {
    return {};
  }
  if (trimmed.toUpperCase().startsWith("SUP")) {
    return { supplierCode: trimmed };
  }
  return { name: trimmed };
}

/** True when connectionType is a connected-organization supplier (not manual/external). */
export function isConnectedSupplier(supplier: Pick<PosSupplier, "connectionType">): boolean {
  const type = supplier.connectionType.trim().toLowerCase();
  return type === "connectedorganization" || type === "connected";
}

/**
 * Supplier list/search.
 * Requires ViewSuppliers — Cashier lacks this capability.
 */
export async function listSuppliers(
  workspace: PosWorkspaceScope,
  options: ListSuppliersOptions = {},
  signal?: AbortSignal,
): Promise<PosSupplierPagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(SUPPLIERS_PATH, {
      supplierCode: options.supplierCode,
      name: options.name,
      contactPerson: options.contactPerson,
      email: options.email,
      mobile: options.mobile,
      taxOrRegistrationNumber: options.taxOrRegistrationNumber,
      status: options.status,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
    }),
  });
  return posSupplierPagedResultSchema.parse(raw);
}

export async function getSupplier(
  workspace: PosWorkspaceScope,
  supplierId: string,
  signal?: AbortSignal,
): Promise<PosSupplier> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: supplierPath(supplierId),
  });
  return posSupplierSchema.parse(raw);
}

export async function createSupplier(
  workspace: PosWorkspaceScope,
  input: CreatePosSupplierInput,
  signal?: AbortSignal,
): Promise<PosSupplier> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: SUPPLIERS_PATH,
    body: supplierBody(input),
  });
  return posSupplierSchema.parse(raw);
}

export async function updateSupplier(
  workspace: PosWorkspaceScope,
  supplierId: string,
  input: UpdatePosSupplierInput,
  signal?: AbortSignal,
): Promise<PosSupplier> {
  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: supplierPath(supplierId),
    body: {
      ...supplierBody(input),
      expectedUpdatedAtUtc: input.expectedUpdatedAtUtc,
    },
  });
  return posSupplierSchema.parse(raw);
}

export async function activateSupplier(
  workspace: PosWorkspaceScope,
  supplierId: string,
  signal?: AbortSignal,
): Promise<PosSupplier> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: supplierPath(supplierId, "/activate"),
  });
  return posSupplierSchema.parse(raw);
}

export async function deactivateSupplier(
  workspace: PosWorkspaceScope,
  supplierId: string,
  signal?: AbortSignal,
): Promise<PosSupplier> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: supplierPath(supplierId, "/deactivate"),
  });
  return posSupplierSchema.parse(raw);
}
