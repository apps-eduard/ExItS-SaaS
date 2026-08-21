import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const CUSTOMERS_PATH = "/api/v1/pos/customers";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const posCustomerListItemSchema = z.object({
  customerId: guidSchema,
  organizationId: guidSchema,
  displayName: z.string(),
  mobileNumber: z.string().nullable().optional(),
  address: z.string().nullable().optional(),
  notes: z.string().nullable().optional(),
  status: z.string(),
  platformBusinessCustomerId: guidSchema.nullable().optional(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  linkedPersonalPublicUserId: z.string().nullable().optional(),
  linkedBuyerOrganizationId: guidSchema.nullable().optional(),
  linkedBuyerPublicOrganizationId: z.string().nullable().optional(),
});

export const posCustomerPagedResultSchema = z.object({
  items: z.array(posCustomerListItemSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type PosCustomerListItem = z.infer<typeof posCustomerListItemSchema>;
export type PosCustomerPagedResult = z.infer<typeof posCustomerPagedResultSchema>;

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

/**
 * Minimal Active-customer list/search for Utang picker (RMAP-12).
 * Requires ViewCustomersAndHistory — Cashier lacks this capability.
 */
export async function listCustomers(
  workspace: PosWorkspaceScope,
  options: {
    status?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  } = {},
  signal?: AbortSignal,
): Promise<PosCustomerPagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(CUSTOMERS_PATH, {
      status: options.status ?? "Active",
      search: options.search,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
    }),
  });
  return posCustomerPagedResultSchema.parse(raw);
}
