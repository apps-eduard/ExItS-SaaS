import { z } from "zod";
import { POS_PRODUCT_CODE } from "@/api/platform/browser-session";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

export const planBillingQuoteSchema = z.object({
  billingCycle: z.string(),
  periodMonths: z.number().int(),
  baseAmount: z.number(),
  discountPercent: z.number(),
  discountAmount: z.number(),
  finalAmount: z.number(),
  equivalentMonthlyAmount: z.number(),
  currencyCode: z.string(),
});

export type PlanBillingQuoteDto = z.infer<typeof planBillingQuoteSchema>;

export const commercialPlanSchema = z.object({
  id: guidSchema,
  productCode: z.string(),
  code: z.string(),
  displayName: z.string(),
  status: z.string(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  productId: guidSchema.nullable().optional().default(null),
  productDisplayName: z.string().nullable().optional().default(null),
  planKey: z.string().nullable().optional().default(null),
  description: z.string().nullable().optional().default(null),
  maxBranches: z.number().int(),
  maxActiveStaff: z.number().int(),
  maxActivePosDevices: z.number().int(),
  maxActiveBusinessTypes: z.number().int(),
  customerCreditEnabled: z.boolean(),
  advancedReportsEnabled: z.boolean(),
  exportEnabled: z.boolean(),
  trialAllowed: z.boolean(),
  defaultTrialDays: z.number().int(),
  sortOrder: z.number().int(),
  monthlyPrice: z.number(),
  annualPrice: z.number(),
  currencyCode: z.string(),
  maxAreas: z.number().int().default(0),
  billingQuotes: z.array(planBillingQuoteSchema).optional().default([]),
});

export type CommercialPlanDto = z.infer<typeof commercialPlanSchema>;

function normalizePlan(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    id: pick(r, "id", "Id"),
    productCode: pick(r, "productCode", "ProductCode"),
    code: pick(r, "code", "Code"),
    displayName: pick(r, "displayName", "DisplayName"),
    status: pick(r, "status", "Status"),
    createdAtUtc: pick(r, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: pick(r, "updatedAtUtc", "UpdatedAtUtc"),
    productId: pick(r, "productId", "ProductId") ?? null,
    productDisplayName: pick(r, "productDisplayName", "ProductDisplayName") ?? null,
    planKey: pick(r, "planKey", "PlanKey") ?? null,
    description: pick(r, "description", "Description") ?? null,
    maxBranches: Number(pick(r, "maxBranches", "MaxBranches") ?? 1),
    maxActiveStaff: Number(pick(r, "maxActiveStaff", "MaxActiveStaff") ?? 3),
    maxActivePosDevices: Number(pick(r, "maxActivePosDevices", "MaxActivePosDevices") ?? 1),
    maxActiveBusinessTypes: Number(
      pick(r, "maxActiveBusinessTypes", "MaxActiveBusinessTypes") ?? 1,
    ),
    customerCreditEnabled: Boolean(pick(r, "customerCreditEnabled", "CustomerCreditEnabled")),
    advancedReportsEnabled: Boolean(pick(r, "advancedReportsEnabled", "AdvancedReportsEnabled")),
    exportEnabled: Boolean(pick(r, "exportEnabled", "ExportEnabled")),
    trialAllowed: Boolean(pick(r, "trialAllowed", "TrialAllowed") ?? true),
    defaultTrialDays: Number(pick(r, "defaultTrialDays", "DefaultTrialDays") ?? 14),
    sortOrder: Number(pick(r, "sortOrder", "SortOrder") ?? 100),
    monthlyPrice: Number(pick(r, "monthlyPrice", "MonthlyPrice") ?? 0),
    annualPrice: Number(pick(r, "annualPrice", "AnnualPrice") ?? 0),
    currencyCode: String(pick(r, "currencyCode", "CurrencyCode") ?? "PHP"),
    maxAreas: Number(pick(r, "maxAreas", "MaxAreas") ?? 0),
    billingQuotes: normalizeBillingQuotes(pick(r, "billingQuotes", "BillingQuotes")),
  };
}

function normalizeBillingQuotes(raw: unknown): PlanBillingQuoteDto[] {
  if (!Array.isArray(raw)) {
    return [];
  }
  return raw
    .map((item) => {
      if (!item || typeof item !== "object") {
        return null;
      }
      const q = item as Record<string, unknown>;
      const parsed = planBillingQuoteSchema.safeParse({
        billingCycle: String(pick(q, "billingCycle", "BillingCycle") ?? ""),
        periodMonths: Number(pick(q, "periodMonths", "PeriodMonths") ?? 0),
        baseAmount: Number(pick(q, "baseAmount", "BaseAmount") ?? 0),
        discountPercent: Number(pick(q, "discountPercent", "DiscountPercent") ?? 0),
        discountAmount: Number(pick(q, "discountAmount", "DiscountAmount") ?? 0),
        finalAmount: Number(pick(q, "finalAmount", "FinalAmount") ?? 0),
        equivalentMonthlyAmount: Number(
          pick(q, "equivalentMonthlyAmount", "EquivalentMonthlyAmount") ?? 0,
        ),
        currencyCode: String(pick(q, "currencyCode", "CurrencyCode") ?? "PHP"),
      });
      return parsed.success ? parsed.data : null;
    })
    .filter((q): q is PlanBillingQuoteDto => q != null);
}

export async function listCommercialPlans(
  productCode: string = POS_PRODUCT_CODE,
  signal?: AbortSignal,
): Promise<CommercialPlanDto[]> {
  const query = new URLSearchParams({ productCode });
  const raw = await platformRequest<unknown>({
    path: `/api/v1/commercial/plans?${query.toString()}`,
    signal,
  });
  const items = Array.isArray(raw) ? raw : [];
  return items
    .map((item) => commercialPlanSchema.parse(normalizePlan(item)))
    .filter((p) => p.status.localeCompare("Active", undefined, { sensitivity: "accent" }) === 0)
    .sort((a, b) => a.sortOrder - b.sortOrder || a.displayName.localeCompare(b.displayName));
}

export function findCommercialPlan(
  plans: CommercialPlanDto[],
  planKey: string,
): CommercialPlanDto | undefined {
  const key = planKey.trim();
  return plans.find(
    (p) =>
      (p.planKey && p.planKey.localeCompare(key, undefined, { sensitivity: "accent" }) === 0) ||
      p.code.localeCompare(key, undefined, { sensitivity: "accent" }) === 0,
  );
}
