import { afterEach, describe, expect, it, vi } from "vitest";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";
import { findCommercialPlan, listCommercialPlans } from "@/api/platform/commercial-plans-client";

describe("commercial-plans-client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("normalizes PascalCase commercial plans and filters Active", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        status: 200,
        json: async () => [
          {
            Id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            ProductCode: "pinoy-business-pos",
            Code: "business",
            DisplayName: "Business",
            Status: "Active",
            CreatedAtUtc: "2026-01-01T00:00:00Z",
            UpdatedAtUtc: "2026-01-01T00:00:00Z",
            PlanKey: "business",
            MaxBranches: 3,
            MaxActiveStaff: 10,
            MaxActivePosDevices: 2,
            MaxActiveBusinessTypes: 2,
            CustomerCreditEnabled: true,
            AdvancedReportsEnabled: false,
            ExportEnabled: false,
            TrialAllowed: true,
            DefaultTrialDays: 14,
            SortOrder: 20,
            MonthlyPrice: 999,
            AnnualPrice: 9990,
            CurrencyCode: "PHP",
          },
          {
            Id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            ProductCode: "pinoy-business-pos",
            Code: "retired",
            DisplayName: "Retired",
            Status: "Retired",
            CreatedAtUtc: "2026-01-01T00:00:00Z",
            UpdatedAtUtc: "2026-01-01T00:00:00Z",
            PlanKey: "retired",
            MaxBranches: 1,
            MaxActiveStaff: 1,
            MaxActivePosDevices: 1,
            MaxActiveBusinessTypes: 1,
            CustomerCreditEnabled: false,
            AdvancedReportsEnabled: false,
            ExportEnabled: false,
            TrialAllowed: false,
            DefaultTrialDays: 0,
            SortOrder: 99,
            MonthlyPrice: 0,
            AnnualPrice: 0,
            CurrencyCode: "PHP",
          },
        ],
        text: async () => "",
      })),
    );

    const plans = await listCommercialPlans();
    expect(plans).toHaveLength(1);
    expect(plans[0]?.planKey).toBe("business");
    expect(plans[0]?.monthlyPrice).toBe(999);
    expect(findCommercialPlan(plans, "business")?.displayName).toBe("Business");
  });
});
