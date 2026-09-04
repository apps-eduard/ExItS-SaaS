import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { PersonalExplorePosPage } from "@/features/personal/start-business/PersonalExplorePosPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

const plans = [
  {
    Id: "11111111-1111-1111-1111-111111111111",
    ProductCode: "pinoy-business-pos",
    Code: "starter",
    DisplayName: "Starter",
    Status: "Active",
    CreatedAtUtc: "2026-01-01T00:00:00Z",
    UpdatedAtUtc: "2026-01-01T00:00:00Z",
    PlanKey: "starter",
    MaxBranches: 1,
    MaxActiveStaff: 3,
    MaxActivePosDevices: 1,
    MaxActiveBusinessTypes: 1,
    MaxAreas: 0,
    CustomerCreditEnabled: true,
    AdvancedReportsEnabled: false,
    ExportEnabled: false,
    TrialAllowed: true,
    DefaultTrialDays: 14,
    SortOrder: 10,
    MonthlyPrice: 299,
    AnnualPrice: 2990,
    CurrencyCode: "PHP",
  },
  {
    Id: "22222222-2222-2222-2222-222222222222",
    ProductCode: "pinoy-business-pos",
    Code: "growth",
    DisplayName: "Growth",
    Status: "Active",
    CreatedAtUtc: "2026-01-01T00:00:00Z",
    UpdatedAtUtc: "2026-01-01T00:00:00Z",
    PlanKey: "growth",
    MaxBranches: 3,
    MaxActiveStaff: 10,
    MaxActivePosDevices: 3,
    MaxActiveBusinessTypes: 3,
    MaxAreas: 0,
    CustomerCreditEnabled: true,
    AdvancedReportsEnabled: false,
    ExportEnabled: false,
    TrialAllowed: true,
    DefaultTrialDays: 14,
    SortOrder: 20,
    MonthlyPrice: 699,
    AnnualPrice: 6990,
    CurrencyCode: "PHP",
  },
  {
    Id: "33333333-3333-3333-3333-333333333333",
    ProductCode: "pinoy-business-pos",
    Code: "pro",
    DisplayName: "Pro",
    Status: "Active",
    CreatedAtUtc: "2026-01-01T00:00:00Z",
    UpdatedAtUtc: "2026-01-01T00:00:00Z",
    PlanKey: "pro",
    MaxBranches: 10,
    MaxActiveStaff: 30,
    MaxActivePosDevices: 10,
    MaxActiveBusinessTypes: 6,
    MaxAreas: 3,
    CustomerCreditEnabled: true,
    AdvancedReportsEnabled: true,
    ExportEnabled: true,
    TrialAllowed: false,
    DefaultTrialDays: 0,
    SortOrder: 30,
    MonthlyPrice: 1499,
    AnnualPrice: 14990,
    CurrencyCode: "PHP",
  },
  {
    Id: "44444444-4444-4444-4444-444444444444",
    ProductCode: "pinoy-business-pos",
    Code: "pro-plus",
    DisplayName: "Pro+",
    Status: "Active",
    CreatedAtUtc: "2026-01-01T00:00:00Z",
    UpdatedAtUtc: "2026-01-01T00:00:00Z",
    PlanKey: "pro-plus",
    MaxBranches: 25,
    MaxActiveStaff: 75,
    MaxActivePosDevices: 25,
    MaxActiveBusinessTypes: 12,
    MaxAreas: 10,
    CustomerCreditEnabled: true,
    AdvancedReportsEnabled: true,
    ExportEnabled: true,
    TrialAllowed: false,
    DefaultTrialDays: 0,
    SortOrder: 40,
    MonthlyPrice: 2499,
    AnnualPrice: 24990,
    CurrencyCode: "PHP",
  },
];

function renderPage(currentPlanKey?: string) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter>
            <PersonalExplorePosPage currentPlanKey={currentPlanKey} />
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("PersonalExplorePosPage", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/api/v1/commercial/plans")) {
          return new Response(JSON.stringify(plans), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          });
        }
        return new Response("{}", { status: 404 });
      }),
    );
  });

  it("renders four plan cards with Growth Most Popular and Pro+ Complete", async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("explore-plan-starter")).toBeInTheDocument();
    });
    expect(screen.getByTestId("explore-plan-growth")).toBeInTheDocument();
    expect(screen.getByTestId("explore-plan-pro")).toBeInTheDocument();
    expect(screen.getByTestId("explore-plan-pro-plus")).toBeInTheDocument();
    expect(screen.getByTestId("explore-badge-most-popular")).toBeInTheDocument();
    expect(screen.getByTestId("explore-badge-complete")).toBeInTheDocument();
  });

  it("toggles monthly/annual prices from catalog", async () => {
    const user = userEvent.setup();
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId("explore-price-growth")).toHaveTextContent("699");
    });
    await user.click(screen.getByTestId("explore-billing-annual"));
    expect(screen.getByTestId("explore-price-growth")).toHaveTextContent("6,990");
  });

  it("shows current-plan CTA and opens compare matrix", async () => {
    const user = userEvent.setup();
    renderPage("growth");
    await waitFor(() => {
      expect(screen.getByTestId("explore-current-growth")).toBeInTheDocument();
    });
    expect(screen.getByTestId("explore-current-growth")).toHaveTextContent("Current plan");
    await user.click(screen.getByTestId("explore-compare-toggle"));
    expect(screen.getByTestId("explore-compare-matrix")).toBeInTheDocument();
  });
});
