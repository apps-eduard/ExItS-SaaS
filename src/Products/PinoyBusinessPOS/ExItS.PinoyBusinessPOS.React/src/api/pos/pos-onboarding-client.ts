import { z } from "zod";
import { posRequest, type PosWorkspaceScope } from "@/api/pos/pos-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const onboardingStepStatusSchema = z.enum(["NotStarted", "Completed", "Skipped"]);
export const onboardingOverallStatusSchema = z.enum(["InProgress", "Completed", "FinishedLater"]);

export type OnboardingStepStatus = z.infer<typeof onboardingStepStatusSchema>;
export type OnboardingOverallStatus = z.infer<typeof onboardingOverallStatusSchema>;

export const organizationOnboardingProgressSchema = z.object({
  organizationId: guidSchema,
  organizationSetupStatus: onboardingStepStatusSchema,
  businessSetupStatus: onboardingStepStatusSchema,
  productTemplateStatus: onboardingStepStatusSchema,
  overallStatus: onboardingOverallStatusSchema,
  primaryBusinessTypeId: guidSchema.nullable().optional().default(null),
  updatedAtUtc: z.string(),
  createdAtUtc: z.string(),
});

export type OrganizationOnboardingProgressDto = z.infer<typeof organizationOnboardingProgressSchema>;

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

function emptyToNull(value: unknown): unknown {
  if (value == null) return null;
  if (typeof value === "string" && value.trim().length === 0) return null;
  return value;
}

function normalizeProgress(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    organizationId: pick(r, "organizationId", "OrganizationId"),
    organizationSetupStatus: pick(r, "organizationSetupStatus", "OrganizationSetupStatus"),
    businessSetupStatus: pick(r, "businessSetupStatus", "BusinessSetupStatus"),
    productTemplateStatus: pick(r, "productTemplateStatus", "ProductTemplateStatus"),
    overallStatus: pick(r, "overallStatus", "OverallStatus"),
    primaryBusinessTypeId: emptyToNull(pick(r, "primaryBusinessTypeId", "PrimaryBusinessTypeId")),
    updatedAtUtc: pick(r, "updatedAtUtc", "UpdatedAtUtc"),
    createdAtUtc: pick(r, "createdAtUtc", "CreatedAtUtc"),
  };
}

export async function getOnboardingProgress(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<OrganizationOnboardingProgressDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    path: "/api/v1/pos/onboarding/progress",
    signal,
  });
  return organizationOnboardingProgressSchema.parse(normalizeProgress(raw));
}

export async function ensureOnboardingProgress(
  workspace: PosWorkspaceScope,
  options: { primaryBusinessTypeId?: string | null } = {},
  signal?: AbortSignal,
): Promise<OrganizationOnboardingProgressDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    path: "/api/v1/pos/onboarding/progress/ensure",
    body: {
      primaryBusinessTypeId: options.primaryBusinessTypeId ?? null,
    },
    signal,
  });
  return organizationOnboardingProgressSchema.parse(normalizeProgress(raw));
}

export async function updateOnboardingProgress(
  workspace: PosWorkspaceScope,
  body: {
    organizationSetupStatus?: OnboardingStepStatus;
    businessSetupStatus?: OnboardingStepStatus;
    productTemplateStatus?: OnboardingStepStatus;
    overallStatus?: OnboardingOverallStatus;
    primaryBusinessTypeId?: string | null;
  },
  signal?: AbortSignal,
): Promise<OrganizationOnboardingProgressDto> {
  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    path: "/api/v1/pos/onboarding/progress",
    body,
    signal,
  });
  return organizationOnboardingProgressSchema.parse(normalizeProgress(raw));
}
