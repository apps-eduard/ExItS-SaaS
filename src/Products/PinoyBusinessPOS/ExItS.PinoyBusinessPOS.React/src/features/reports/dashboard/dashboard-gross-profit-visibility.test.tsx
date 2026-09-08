import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import type { ReactNode } from "react";
import type { PosProfitabilityReportDto } from "@/api/pos/pos-reporting-client";
import { AppProviders } from "@/app/providers";
import { DashboardGrossProfitCard } from "@/features/reports/dashboard/DashboardGrossProfitCard";

const here = dirname(fileURLToPath(import.meta.url));

vi.mock("recharts", async () => {
  const actual = await vi.importActual<typeof import("recharts")>("recharts");
  return {
    ...actual,
    ResponsiveContainer: ({ children }: { children?: ReactNode }) => (
      <div data-testid="recharts-responsive">{children}</div>
    ),
  };
});

function baseProfit(
  overrides: Partial<PosProfitabilityReportDto> = {},
): PosProfitabilityReportDto {
  return {
    fromDate: "2026-09-01",
    toDate: "2026-09-08",
    branchId: null,
    netSales: 10500,
    cogsStatus: "Complete",
    knownCogs: 7050,
    totalCogs: 7050,
    grossProfit: 3450,
    grossMarginPercent: 32.8,
    completedSaleCount: 10,
    completeCostSaleCount: 10,
    partialCostSaleCount: 0,
    unavailableCostSaleCount: 0,
    wasteLossKnownCost: 0,
    wasteLossCostStatus: "Unavailable",
    stockUseKnownCost: 0,
    stockUseCostStatus: "Unavailable",
    costCompletenessPercent: 100,
    commercialDiscountTotal: 0,
    ...overrides,
  };
}

function renderCard(data: PosProfitabilityReportDto | undefined, loading = false) {
  return render(
    <AppProviders>
      <MemoryRouter>
        <DashboardGrossProfitCard
          data={data}
          loading={loading}
          scopeLabel="Main Branch"
          animationKey="test"
        />
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("POS-DASHBOARD-GROSS-PROFIT-VISIBILITY-FIX-V1", () => {
  it("Complete COGS renders gross profit and margin (not hidden)", () => {
    renderCard(baseProfit());
    expect(screen.getByTestId("dashboard-gross-profit")).toBeInTheDocument();
    expect(screen.getByTestId("dashboard-gross-profit-complete")).toBeInTheDocument();
    expect(screen.getByTestId("dashboard-gross-profit-status")).toHaveAttribute(
      "data-cogs-status",
      "Complete",
    );
    expect(screen.getByTestId("dashboard-gross-margin-radial")).toBeInTheDocument();
    expect(screen.getByTestId("dashboard-gross-profit-coverage")).toHaveTextContent("100%");
    expect(screen.queryByTestId("dashboard-gross-profit-partial")).not.toBeInTheDocument();
  });

  it("Partial COGS still renders card without false profit", () => {
    renderCard(
      baseProfit({
        cogsStatus: "Partial",
        knownCogs: 4250,
        totalCogs: null,
        grossProfit: null,
        grossMarginPercent: null,
        completeCostSaleCount: 8,
        partialCostSaleCount: 1,
        unavailableCostSaleCount: 1,
        costCompletenessPercent: 80,
      }),
    );
    expect(screen.getByTestId("dashboard-gross-profit")).toBeInTheDocument();
    expect(screen.getByTestId("dashboard-gross-profit-partial")).toBeInTheDocument();
    expect(screen.getByTestId("dashboard-gross-profit-status")).toHaveAttribute(
      "data-cogs-status",
      "Partial",
    );
    expect(screen.queryByTestId("dashboard-gross-margin-radial")).not.toBeInTheDocument();
    expect(screen.queryByTestId("dashboard-gross-profit-complete")).not.toBeInTheDocument();
    expect(screen.getByTestId("dashboard-gross-profit-coverage")).toHaveTextContent("80%");
    expect(screen.getByTestId("dashboard-gross-profit-sales-costed")).toHaveTextContent("8 of 10");
    expect(screen.getByTestId("dashboard-gross-profit-incomplete-count")).toHaveTextContent("2");
    expect(screen.getByTestId("dashboard-gross-profit-partial").textContent).not.toMatch(/₱0\.00/);
    expect(screen.getByTestId("dashboard-view-profitability")).toHaveAttribute(
      "href",
      "/reports/operational/profitability",
    );
  });

  it("Unavailable COGS still renders card without fake zero profit", () => {
    renderCard(
      baseProfit({
        cogsStatus: "Unavailable",
        knownCogs: 0,
        totalCogs: null,
        grossProfit: null,
        grossMarginPercent: null,
        completedSaleCount: 3,
        completeCostSaleCount: 0,
        unavailableCostSaleCount: 3,
        costCompletenessPercent: 0,
        netSales: 2500,
      }),
    );
    expect(screen.getByTestId("dashboard-gross-profit")).toBeInTheDocument();
    expect(screen.getByTestId("dashboard-gross-profit-unavailable")).toBeInTheDocument();
    expect(screen.getByTestId("dashboard-gross-profit-status")).toHaveAttribute(
      "data-cogs-status",
      "Unavailable",
    );
    expect(screen.queryByTestId("dashboard-gross-margin-radial")).not.toBeInTheDocument();
    expect(screen.getByTestId("dashboard-gross-profit-coverage")).toHaveTextContent("0%");
  });

  it("ManagementDashboardPage always mounts Gross Profit card when profitability loads", () => {
    const source = readFileSync(resolve(here, "../ManagementDashboardPage.tsx"), "utf8");
    expect(source).toContain("DashboardGrossProfitCard");
    expect(source).not.toContain("grossProfitAvailable");
    expect(source).toContain("profitabilityQuery.isSuccess || profitabilityQuery.isFetching");
  });
});
