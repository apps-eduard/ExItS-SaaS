import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

const isoDateTimeSchema = z.string().min(1);

const POLICY_PATH = "/api/v1/pos/inventory/expiry-sale-policy";

export const EXPIRY_SALE_POLICY_SOURCES = [
  "OrganizationDefault",
  "OrganizationCategory",
  "Branch",
  "BranchCategory",
] as const;

export type ExpirySalePolicySource = (typeof EXPIRY_SALE_POLICY_SOURCES)[number];

export const expirySalePolicySettingDtoSchema = z.object({
  stopSellingDaysBeforeExpiry: z.number().int(),
  updatedAtUtc: isoDateTimeSchema.nullable().optional(),
  updatedBy: guidSchema.nullable().optional(),
  isExplicit: z.boolean(),
});

export const expirySalePolicyCategoryOverrideDtoSchema = z.object({
  categoryId: guidSchema,
  stopSellingDaysBeforeExpiry: z.number().int(),
  updatedAtUtc: isoDateTimeSchema,
  updatedBy: guidSchema,
});

export const effectiveExpirySalePolicyDtoSchema = z.object({
  stopSellingDaysBeforeExpiry: z.number().int(),
  source: z.string(),
  organizationDefaultDays: z.number().int(),
  organizationCategoryDays: z.number().int().nullable().optional(),
  branchDefaultDays: z.number().int().nullable().optional(),
  branchCategoryDays: z.number().int().nullable().optional(),
});

export const upsertExpirySalePolicyRequestSchema = z.object({
  stopSellingDaysBeforeExpiry: z.number().int().min(0).max(365),
});

export type ExpirySalePolicySettingDto = z.infer<typeof expirySalePolicySettingDtoSchema>;
export type ExpirySalePolicyCategoryOverrideDto = z.infer<
  typeof expirySalePolicyCategoryOverrideDtoSchema
>;
export type EffectiveExpirySalePolicyDto = z.infer<typeof effectiveExpirySalePolicyDtoSchema>;
export type UpsertExpirySalePolicyRequest = z.infer<typeof upsertExpirySalePolicyRequestSchema>;

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
  const qs = query.toString();
  return qs ? `${path}?${qs}` : path;
}

export async function getOrganizationExpirySalePolicy(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<ExpirySalePolicySettingDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${POLICY_PATH}/organization`,
  });
  return expirySalePolicySettingDtoSchema.parse(raw);
}

export async function putOrganizationExpirySalePolicy(
  workspace: PosWorkspaceScope,
  body: UpsertExpirySalePolicyRequest,
  signal?: AbortSignal,
): Promise<ExpirySalePolicySettingDto> {
  const payload = upsertExpirySalePolicyRequestSchema.parse(body);
  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: `${POLICY_PATH}/organization`,
    body: payload,
  });
  return expirySalePolicySettingDtoSchema.parse(raw);
}

export async function listOrganizationCategoryExpirySalePolicies(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<ExpirySalePolicyCategoryOverrideDto[]> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${POLICY_PATH}/organization/categories`,
  });
  return z.array(expirySalePolicyCategoryOverrideDtoSchema).parse(raw);
}

export async function putOrganizationCategoryExpirySalePolicy(
  workspace: PosWorkspaceScope,
  categoryId: string,
  body: UpsertExpirySalePolicyRequest,
  signal?: AbortSignal,
): Promise<ExpirySalePolicyCategoryOverrideDto> {
  const payload = upsertExpirySalePolicyRequestSchema.parse(body);
  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: `${POLICY_PATH}/organization/categories/${categoryId}`,
    body: payload,
  });
  return expirySalePolicyCategoryOverrideDtoSchema.parse(raw);
}

export async function deleteOrganizationCategoryExpirySalePolicy(
  workspace: PosWorkspaceScope,
  categoryId: string,
  signal?: AbortSignal,
): Promise<void> {
  await posRequest<unknown>({
    method: "DELETE",
    workspace,
    signal,
    path: `${POLICY_PATH}/organization/categories/${categoryId}`,
  });
}

export async function getBranchExpirySalePolicy(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<ExpirySalePolicySettingDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${POLICY_PATH}/branch`,
  });
  return expirySalePolicySettingDtoSchema.parse(raw);
}

export async function putBranchExpirySalePolicy(
  workspace: PosWorkspaceScope,
  body: UpsertExpirySalePolicyRequest,
  signal?: AbortSignal,
): Promise<ExpirySalePolicySettingDto> {
  const payload = upsertExpirySalePolicyRequestSchema.parse(body);
  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: `${POLICY_PATH}/branch`,
    body: payload,
  });
  return expirySalePolicySettingDtoSchema.parse(raw);
}

export async function deleteBranchExpirySalePolicy(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<void> {
  await posRequest<unknown>({
    method: "DELETE",
    workspace,
    signal,
    path: `${POLICY_PATH}/branch`,
  });
}

export async function listBranchCategoryExpirySalePolicies(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<ExpirySalePolicyCategoryOverrideDto[]> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${POLICY_PATH}/branch/categories`,
  });
  return z.array(expirySalePolicyCategoryOverrideDtoSchema).parse(raw);
}

export async function putBranchCategoryExpirySalePolicy(
  workspace: PosWorkspaceScope,
  categoryId: string,
  body: UpsertExpirySalePolicyRequest,
  signal?: AbortSignal,
): Promise<ExpirySalePolicyCategoryOverrideDto> {
  const payload = upsertExpirySalePolicyRequestSchema.parse(body);
  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: `${POLICY_PATH}/branch/categories/${categoryId}`,
    body: payload,
  });
  return expirySalePolicyCategoryOverrideDtoSchema.parse(raw);
}

export async function deleteBranchCategoryExpirySalePolicy(
  workspace: PosWorkspaceScope,
  categoryId: string,
  signal?: AbortSignal,
): Promise<void> {
  await posRequest<unknown>({
    method: "DELETE",
    workspace,
    signal,
    path: `${POLICY_PATH}/branch/categories/${categoryId}`,
  });
}

export async function getEffectiveExpirySalePolicy(
  workspace: PosWorkspaceScope,
  categoryId?: string | null,
  signal?: AbortSignal,
): Promise<EffectiveExpirySalePolicyDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(`${POLICY_PATH}/effective`, {
      categoryId: categoryId ?? undefined,
    }),
  });
  return effectiveExpirySalePolicyDtoSchema.parse(raw);
}
