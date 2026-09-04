import { describe, expect, it } from "vitest";
import type { CommercialPlanDto } from "@/api/platform/commercial-plans-client";
import {
  annualSavingsPercent,
  buildPlanCompareRows,
  getPlanDisplayMeta,
  resolvePlanCtaKind,
} from "@/features/personal/start-business/plan-selection-meta";

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

  it("computes annual savings from catalog prices", () => {
    expect(
      annualSavingsPercent(
        plan({ code: "starter", displayName: "Starter", monthlyPrice: 299, annualPrice: 2990 }),
      ),
    ).toBe(17);
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
