import type {
  CommercialPlanDto,
  PlanBillingQuoteDto,
} from "@/api/platform/commercial-plans-client";
import type { MessageKey } from "@/i18n/messages";

export type PlanBillingCycle = "Monthly" | "Quarterly" | "SixMonths" | "Annual";

export const PLAN_BILLING_CYCLES: readonly PlanBillingCycle[] = [
  "Monthly",
  "Quarterly",
  "SixMonths",
  "Annual",
] as const;

export type PlanSelectionBadge = "most_popular" | "complete" | null;

export type PlanDisplayMeta = {
  planKey: string;
  taglineKey: MessageKey;
  badge: PlanSelectionBadge;
  /** Inclusive “Everything in X, plus…” baseline plan key, when not the lowest tier. */
  includesEverythingIn?: string;
  /** Differentiator bullets beyond capacity lines (commercial display metadata). */
  highlightKeys: MessageKey[];
  warehouseIncluded: boolean;
  areaManagementIncluded: boolean;
  orderingIncluded: boolean;
  advancedReportsIncluded: boolean;
  exportIncluded: boolean;
};

/** Central display metadata keyed by plan code — capacities/prices come from the API. */
export const POS_PLAN_DISPLAY_META: Readonly<Record<string, PlanDisplayMeta>> = {
  starter: {
    planKey: "starter",
    taglineKey: "personal.explore.tagline.starter",
    badge: null,
    highlightKeys: [
      "personal.explore.highlight.sellCatalog",
      "personal.explore.highlight.utang",
      "personal.explore.highlight.purchasing",
      "personal.explore.highlight.shiftsReports",
    ],
    warehouseIncluded: false,
    areaManagementIncluded: false,
    orderingIncluded: false,
    advancedReportsIncluded: false,
    exportIncluded: false,
  },
  growth: {
    planKey: "growth",
    taglineKey: "personal.explore.tagline.growth",
    badge: "most_popular",
    includesEverythingIn: "starter",
    highlightKeys: [
      "personal.explore.highlight.multiBranch",
      "personal.explore.highlight.ordering",
      "personal.explore.highlight.connectedSuppliers",
    ],
    warehouseIncluded: false,
    areaManagementIncluded: false,
    orderingIncluded: true,
    advancedReportsIncluded: false,
    exportIncluded: false,
  },
  pro: {
    planKey: "pro",
    taglineKey: "personal.explore.tagline.pro",
    badge: null,
    includesEverythingIn: "growth",
    highlightKeys: [
      "personal.explore.highlight.areas",
      "personal.explore.highlight.warehouse",
      "personal.explore.highlight.advancedReports",
    ],
    warehouseIncluded: true,
    areaManagementIncluded: true,
    orderingIncluded: true,
    advancedReportsIncluded: true,
    exportIncluded: true,
  },
  "pro-plus": {
    planKey: "pro-plus",
    taglineKey: "personal.explore.tagline.proPlus",
    badge: "complete",
    includesEverythingIn: "pro",
    highlightKeys: [
      "personal.explore.highlight.highestCapacities",
      "personal.explore.highlight.completeSet",
    ],
    warehouseIncluded: true,
    areaManagementIncluded: true,
    orderingIncluded: true,
    advancedReportsIncluded: true,
    exportIncluded: true,
  },
};

export function resolvePlanKey(plan: CommercialPlanDto): string {
  return (plan.planKey ?? plan.code).trim().toLowerCase();
}

export function getPlanDisplayMeta(plan: CommercialPlanDto): PlanDisplayMeta {
  const key = resolvePlanKey(plan);
  return (
    POS_PLAN_DISPLAY_META[key] ?? {
      planKey: key,
      taglineKey: "personal.explore.tagline.fallback",
      badge: null,
      highlightKeys: [],
      warehouseIncluded: false,
      areaManagementIncluded: (plan.maxAreas ?? 0) > 0,
      orderingIncluded: false,
      advancedReportsIncluded: plan.advancedReportsEnabled,
      exportIncluded: plan.exportEnabled,
    }
  );
}

export function parsePlanBillingCycle(raw: string | null | undefined): PlanBillingCycle {
  const value = (raw ?? "Monthly").trim();
  const match = PLAN_BILLING_CYCLES.find(
    (cycle) => cycle.localeCompare(value, undefined, { sensitivity: "accent" }) === 0,
  );
  return match ?? "Monthly";
}

/** Server-authored quote only — never invent discount fields client-side. */
export function getServerBillingQuote(
  plan: CommercialPlanDto,
  cycle: PlanBillingCycle,
): PlanBillingQuoteDto | null {
  return (
    plan.billingQuotes?.find(
      (q) => q.billingCycle.localeCompare(cycle, undefined, { sensitivity: "accent" }) === 0,
    ) ?? null
  );
}

/**
 * Prefer server-authored billingQuotes.
 * Amount fallbacks for Monthly/Annual list prices only; discountPercent/Amount stay 0 when invented.
 */
export function getPlanBillingQuote(
  plan: CommercialPlanDto,
  cycle: PlanBillingCycle,
): PlanBillingQuoteDto | null {
  const fromApi = getServerBillingQuote(plan, cycle);
  if (fromApi) {
    return fromApi;
  }

  // Fallback only when older catalog payloads omit quotes (Monthly / Annual list prices).
  if (cycle === "Monthly") {
    return {
      billingCycle: "Monthly",
      periodMonths: 1,
      baseAmount: plan.monthlyPrice,
      discountPercent: 0,
      discountAmount: 0,
      finalAmount: plan.monthlyPrice,
      equivalentMonthlyAmount: plan.monthlyPrice,
      currencyCode: plan.currencyCode,
    };
  }
  if (cycle === "Annual") {
    return {
      billingCycle: "Annual",
      periodMonths: 12,
      baseAmount: plan.monthlyPrice * 12,
      discountPercent: 0,
      discountAmount: 0,
      finalAmount: plan.annualPrice,
      equivalentMonthlyAmount: Math.round((plan.annualPrice / 12) * 100) / 100,
      currencyCode: plan.currencyCode,
    };
  }
  return null;
}

/** Discount percent for billing toggle labels — only from server quotes across plans. */
export function billingCycleDiscountPercentFromQuotes(
  plans: CommercialPlanDto[],
  cycle: PlanBillingCycle,
): number | null {
  for (const plan of plans) {
    const quote = getServerBillingQuote(plan, cycle);
    if (quote && quote.discountPercent > 0) {
      return Math.round(quote.discountPercent);
    }
  }
  return null;
}

export function planPriceForCycle(plan: CommercialPlanDto, cycle: PlanBillingCycle): number {
  return getPlanBillingQuote(plan, cycle)?.finalAmount ?? plan.monthlyPrice;
}

/** @deprecated Prefer getPlanBillingQuote(plan, "Annual").discountAmount */
export function annualSavingsAmount(plan: CommercialPlanDto): number | null {
  const quote = getPlanBillingQuote(plan, "Annual");
  if (!quote || quote.discountAmount <= 0) {
    return null;
  }
  return quote.discountAmount;
}

/** @deprecated Prefer getPlanBillingQuote(plan, "Annual").discountPercent */
export function annualSavingsPercent(plan: CommercialPlanDto): number | null {
  const quote = getPlanBillingQuote(plan, "Annual");
  if (!quote || quote.discountPercent <= 0) {
    return null;
  }
  return Math.round(quote.discountPercent);
}

export type PlanCompareRow = {
  id: string;
  labelKey: MessageKey;
  values: Record<string, string | boolean>;
};

export function buildPlanCompareRows(plans: CommercialPlanDto[]): PlanCompareRow[] {
  const keys = plans.map(resolvePlanKey);
  const valueBy = <T,>(pick: (p: CommercialPlanDto, meta: PlanDisplayMeta) => T): Record<string, T> => {
    const map: Record<string, T> = {};
    for (const plan of plans) {
      map[resolvePlanKey(plan)] = pick(plan, getPlanDisplayMeta(plan));
    }
    return map;
  };

  const rows: PlanCompareRow[] = [
    {
      id: "branches",
      labelKey: "personal.explore.compare.branches",
      values: valueBy((p) => String(p.maxBranches)),
    },
    {
      id: "staff",
      labelKey: "personal.explore.compare.staff",
      values: valueBy((p) => String(p.maxActiveStaff)),
    },
    {
      id: "devices",
      labelKey: "personal.explore.compare.devices",
      values: valueBy((p) => String(p.maxActivePosDevices)),
    },
    {
      id: "businessTypes",
      labelKey: "personal.explore.compare.businessTypes",
      values: valueBy((p) => String(p.maxActiveBusinessTypes)),
    },
    {
      id: "areas",
      labelKey: "personal.explore.compare.areas",
      values: valueBy((p) => (p.maxAreas > 0 ? String(p.maxAreas) : false)),
    },
    {
      id: "warehouse",
      labelKey: "personal.explore.compare.warehouse",
      values: valueBy((_, m) => m.warehouseIncluded),
    },
    {
      id: "utang",
      labelKey: "personal.explore.compare.utang",
      values: valueBy((p) => p.customerCreditEnabled),
    },
    {
      id: "ordering",
      labelKey: "personal.explore.compare.ordering",
      values: valueBy((_, m) => m.orderingIncluded),
    },
    {
      id: "reports",
      labelKey: "personal.explore.compare.reports",
      values: valueBy((p) => p.advancedReportsEnabled),
    },
    {
      id: "export",
      labelKey: "personal.explore.compare.export",
      values: valueBy((p) => p.exportEnabled),
    },
  ];

  return rows.map((row) => ({
    ...row,
    values: Object.fromEntries(keys.map((k) => [k, row.values[k] ?? false])),
  }));
}

export type PlanCtaKind = "current" | "upgrade" | "downgrade" | "choose";

export function resolvePlanCtaKind(
  planKey: string,
  currentPlanKey: string | null | undefined,
  planSortOrder: number,
  currentSortOrder: number | null | undefined,
): PlanCtaKind {
  if (!currentPlanKey) {
    return "choose";
  }
  if (planKey.localeCompare(currentPlanKey, undefined, { sensitivity: "accent" }) === 0) {
    return "current";
  }
  if (currentSortOrder != null && planSortOrder > currentSortOrder) {
    return "upgrade";
  }
  if (currentSortOrder != null && planSortOrder < currentSortOrder) {
    return "downgrade";
  }
  return "choose";
}
