import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

export const personalProductAffiliationSchema = z.object({
  productCode: z.string(),
  productDisplayName: z.string(),
  organizationId: guidSchema.nullable().optional().default(null),
  organizationDisplayName: z.string().nullable().optional().default(null),
  membershipRole: z.string().nullable().optional().default(null),
  roleDisplay: z.string().nullable().optional().default(null),
  planKey: z.string().nullable().optional().default(null),
  planDisplayName: z.string().nullable().optional().default(null),
  subscriptionStatus: z.string().nullable().optional().default(null),
  trialEndUtc: z.string().nullable().optional().default(null),
  canManageBilling: z.boolean(),
});

export type PersonalProductAffiliationDto = z.infer<typeof personalProductAffiliationSchema>;

function normalize(raw: unknown): Record<string, unknown> {
  const row = raw && typeof raw === "object" ? (raw as Record<string, unknown>) : {};
  return {
    productCode: pick(row, "productCode", "ProductCode"),
    productDisplayName: pick(row, "productDisplayName", "ProductDisplayName"),
    organizationId: pick(row, "organizationId", "OrganizationId") ?? null,
    organizationDisplayName:
      pick(row, "organizationDisplayName", "OrganizationDisplayName") ?? null,
    membershipRole: pick(row, "membershipRole", "MembershipRole") ?? null,
    roleDisplay: pick(row, "roleDisplay", "RoleDisplay") ?? null,
    planKey: pick(row, "planKey", "PlanKey") ?? null,
    planDisplayName: pick(row, "planDisplayName", "PlanDisplayName") ?? null,
    subscriptionStatus: pick(row, "subscriptionStatus", "SubscriptionStatus") ?? null,
    trialEndUtc: pick(row, "trialEndUtc", "TrialEndUtc") ?? null,
    canManageBilling: Boolean(pick(row, "canManageBilling", "CanManageBilling")),
  };
}

export async function listPersonalProductAffiliations(
  signal?: AbortSignal,
): Promise<PersonalProductAffiliationDto[]> {
  const raw = await platformRequest<unknown>({
    path: "/api/v1/personal/product-affiliations",
    signal,
  });
  const items = Array.isArray(raw) ? raw : [];
  return items.map((item) => personalProductAffiliationSchema.parse(normalize(item)));
}

export function affiliationCanOpen(status: string | null | undefined): boolean {
  return status === "Active" || status === "Trialing" || status === "GracePeriod" || status === "PastDue";
}

export function affiliationNeedsPlan(status: string | null | undefined, hasOrganization: boolean): boolean {
  if (!hasOrganization) return false;
  return (
    !status
    || status === "Cancelled"
    || status === "Expired"
    || status === "Suspended"
    || status === "Inactive"
  );
}
