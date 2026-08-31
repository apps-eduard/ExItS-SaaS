import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

export const businessCustomerSchema = z.object({
  id: guidSchema,
  organizationId: guidSchema,
  displayName: z.string(),
  email: z.string().nullable().optional().default(null),
  phone: z.string().nullable().optional().default(null),
  notes: z.string().nullable().optional().default(null),
  owningProductCode: z.string().nullable().optional().default(null),
  status: z.string(),
  linkedUserIdentityId: guidSchema.nullable().optional().default(null),
  isOrganizationStaff: z.boolean().optional().default(false),
  isCreditCustomer: z.boolean().optional().default(false),
  allowDeliveryBeyondNormalDistance: z.boolean().default(false),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
});

export type BusinessCustomerDto = z.infer<typeof businessCustomerSchema>;

function normalizeBusinessCustomer(raw: unknown): BusinessCustomerDto {
  const r = (raw ?? {}) as Record<string, unknown>;
  return businessCustomerSchema.parse({
    id: pick(r, "id", "Id"),
    organizationId: pick(r, "organizationId", "OrganizationId"),
    displayName: pick(r, "displayName", "DisplayName"),
    email: pick(r, "email", "Email") ?? null,
    phone: pick(r, "phone", "Phone") ?? null,
    notes: pick(r, "notes", "Notes") ?? null,
    owningProductCode: pick(r, "owningProductCode", "OwningProductCode") ?? null,
    status: pick(r, "status", "Status"),
    linkedUserIdentityId: pick(r, "linkedUserIdentityId", "LinkedUserIdentityId") ?? null,
    isOrganizationStaff: pick(r, "isOrganizationStaff", "IsOrganizationStaff") ?? false,
    isCreditCustomer: pick(r, "isCreditCustomer", "IsCreditCustomer") ?? false,
    allowDeliveryBeyondNormalDistance:
      pick(r, "allowDeliveryBeyondNormalDistance", "AllowDeliveryBeyondNormalDistance") ?? false,
    createdAtUtc: pick(r, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: pick(r, "updatedAtUtc", "UpdatedAtUtc"),
  });
}

export async function getOrganizationBusinessCustomer(
  organizationId: string,
  platformBusinessCustomerId: string,
  signal?: AbortSignal,
): Promise<BusinessCustomerDto> {
  const raw = await platformRequest<unknown>({
    path: `/api/v1/organizations/${organizationId}/customers/${platformBusinessCustomerId}`,
    signal,
  });
  return normalizeBusinessCustomer(raw);
}

export async function listOrganizationBusinessCustomers(
  organizationId: string,
  options?: { page?: number; pageSize?: number; signal?: AbortSignal },
): Promise<{ items: BusinessCustomerDto[]; totalCount: number }> {
  const page = options?.page ?? 1;
  const pageSize = options?.pageSize ?? 100;
  const raw = await platformRequest<unknown>({
    path: `/api/v1/organizations/${organizationId}/customers?page=${page}&pageSize=${pageSize}`,
    signal: options?.signal,
  });
  const body = (raw ?? {}) as Record<string, unknown>;
  const itemsRaw = (pick(body, "items", "Items") as unknown[]) ?? [];
  const totalCount = Number(pick(body, "totalCount", "TotalCount") ?? itemsRaw.length);
  return {
    items: itemsRaw.map(normalizeBusinessCustomer),
    totalCount,
  };
}

export async function updateBusinessCustomerDeliveryPreferences(
  organizationId: string,
  platformBusinessCustomerId: string,
  allowDeliveryBeyondNormalDistance: boolean,
  signal?: AbortSignal,
): Promise<BusinessCustomerDto> {
  const raw = await platformRequest<unknown>({
    method: "PATCH",
    path: `/api/v1/organizations/${organizationId}/customers/${platformBusinessCustomerId}/delivery-preferences`,
    body: { allowDeliveryBeyondNormalDistance },
    signal,
  });
  return normalizeBusinessCustomer(raw);
}
