import { describe, expect, it } from "vitest";
import type { CommercialPlanDto } from "@/api/platform/commercial-plans-client";
import {
  annualSavingsPercent,
  billingCycleDiscountPercentFromQuotes,
  buildPlanCompareRows,
  getPlanBillingQuote,
  getPlanDisplayMeta,
  getServerBillingQuote,
  planPriceForCycle,
  resolvePlanCtaKind,
} from "@/features/personal/start-business/plan-selection-meta";
import { buildPlanPaymentSummaryText } from "@/features/personal/start-business/PlanPaymentSummary";
import { en } from "@/i18n/locales/en";
import type { MessageKey } from "@/i18n/messages";

function plan(partial: Partial<CommercialPlanDto> & Pick<CommercialPlanDto, "code" | "displayName">): CommercialPlanDto {
  return {
    id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    productCode: "pinoy-business-pos",
    status: "Active",
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    productId: null,
    productDisplayName: null,
    planKey: partial.code,
    description: null,
    maxBranches: 1,
    maxActiveStaff: 3,
    maxActivePosDevices: 1,
    maxActiveBusinessTypes: 1,
    maxAreas: 0,
    customerCreditEnabled: true,
    advancedReportsEnabled: false,
    exportEnabled: false,
    trialAllowed: true,
    defaultTrialDays: 14,
    sortOrder: 10,
    monthlyPrice: 299,
    annualPrice: 2990,
    currencyCode: "PHP",
    billingQuotes: [],
    ...partial,
  };
}

describe("plan-selection-meta", () => {
  it("marks Growth as most popular and Pro+ as complete", () => {
    expect(getPlanDisplayMeta(plan({ code: "growth", displayName: "Growth" })).badge).toBe(
      "most_popular",
    );
    expect(getPlanDisplayMeta(plan({ code: "pro-plus", displayName: "Pro+" })).badge).toBe(
      "complete",
    );
  });

  it("does not invent annual discount percent without server quotes", () => {
    expect(
      annualSavingsPercent(
        plan({ code: "starter", displayName: "Starter", monthlyPrice: 299, annualPrice: 2990 }),
      ),
    ).toBeNull();
    expect(
      getPlanBillingQuote(
        plan({ code: "starter", displayName: "Starter", monthlyPrice: 299, annualPrice: 2990 }),
        "Annual",
      )?.discountPercent,
    ).toBe(0);
  });

  it("reads prepaid quotes and billing toggle discounts from server billingQuotes only", () => {
    const quoted = plan({
      code: "pro",
      displayName: "Pro",
      monthlyPrice: 1499,
      annualPrice: 14990,
      billingQuotes: [
        {
          billingCycle: "Quarterly",
          periodMonths: 3,
          baseAmount: 4497,
          discountPercent: 5,
          discountAmount: 224.85,
          finalAmount: 4272.15,
          equivalentMonthlyAmount: 1424.05,
          currencyCode: "PHP",
        },
        {
          billingCycle: "SixMonths",
          periodMonths: 6,
          baseAmount: 8994,
          discountPercent: 10,
          discountAmount: 899.4,
          finalAmount: 8094.6,
          equivalentMonthlyAmount: 1349.1,
          currencyCode: "PHP",
        },
      ],
    });
    expect(getServerBillingQuote(quoted, "Quarterly")?.finalAmount).toBe(4272.15);
    expect(getPlanBillingQuote(quoted, "Quarterly")?.finalAmount).toBe(4272.15);
    expect(planPriceForCycle(quoted, "Quarterly")).toBe(4272.15);
    expect(billingCycleDiscountPercentFromQuotes([quoted], "Quarterly")).toBe(5);
    expect(billingCycleDiscountPercentFromQuotes([quoted], "SixMonths")).toBe(10);
    expect(billingCycleDiscountPercentFromQuotes([quoted], "Monthly")).toBeNull();
  });

  it("builds compact payment summary lines by entitlement tier", () => {
    const t = (key: MessageKey) => en[key];
    expect(buildPlanPaymentSummaryText("starter", t)).toBe("Cash · GCash · Utang");
    expect(buildPlanPaymentSummaryText("growth", t)).toBe("Cash · GCash · Utang");
    expect(buildPlanPaymentSummaryText("pro", t)).toContain("Bank transfer");
    expect(buildPlanPaymentSummaryText("pro-plus", t)).toContain("Online payments");
  });

  it("resolves current / upgrade / downgrade CTAs from sort order", () => {
    expect(resolvePlanCtaKind("growth", "growth", 20, 20)).toBe("current");
    expect(resolvePlanCtaKind("pro", "growth", 30, 20)).toBe("upgrade");
    expect(resolvePlanCtaKind("starter", "growth", 10, 20)).toBe("downgrade");
    expect(resolvePlanCtaKind("starter", null, 10, null)).toBe("choose");
  });

  it("builds compare rows with capacities and warehouse flags", () => {
    const plans = [
      plan({
        code: "starter",
        displayName: "Starter",
        maxBranches: 1,
        maxAreas: 0,
        customerCreditEnabled: true,
      }),
      plan({
        code: "pro",
        displayName: "Pro",
        maxBranches: 10,
        maxAreas: 3,
        advancedReportsEnabled: true,
        exportEnabled: true,
        sortOrder: 30,
      }),
    ];
    const rows = buildPlanCompareRows(plans);
    const branches = rows.find((r) => r.id === "branches")!;
    expect(branches.values.starter).toBe("1");
    expect(branches.values.pro).toBe("10");
    expect(rows.find((r) => r.id === "warehouse")!.values.pro).toBe(true);
    expect(rows.find((r) => r.id === "warehouse")!.values.starter).toBe(false);
    expect(rows.find((r) => r.id === "utang")!.values.starter).toBe(true);
  });
});
