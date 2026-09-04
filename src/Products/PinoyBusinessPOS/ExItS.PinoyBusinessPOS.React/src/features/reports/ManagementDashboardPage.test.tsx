import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { ManagementDashboardPage } from "@/features/reports/ManagementDashboardPage";
import * as posReportingClient from "@/api/pos/pos-reporting-client";
import * as platformAuthClient from "@/api/platform/platform-auth-client";
import {
  TEST_BRANCH_A_ID,
  TEST_BRANCH_B_ID,
  TEST_ORG_A_ID,
} from "@/test/session-context";

const getManagementOverview = vi.spyOn(posReportingClient, "getManagementOverview");
const getDashboard = vi.spyOn(posReportingClient, "getDashboard");
const getSalesByProductReport = vi.spyOn(posReportingClient, "getSalesByProductReport");
const getProfitabilityReport = vi.spyOn(posReportingClient, "getProfitabilityReport");
const getUtangReport = vi.spyOn(posReportingClient, "getUtangReport");
const listOrganizationBranches = vi.spyOn(platformAuthClient, "listOrganizationBranches");

function makeOverviewPayload(overrides: Partial<ReturnType<typeof emptyOverview>> = {}) {
  return { ...emptyOverview(), ...overrides };
}

function emptyOverview() {
  return {
    businessDate: "2026-08-30",
    todaySalesTotal: 0,
    todaySaleCount: 0,
    todayCashSalesTotal: 0,
    todayUtangSalesTotal: 0,
    todayPaymentsReceived: 0,
    openUtangOutstanding: 0,
    lowStockProductCount: 0,
    expiredLotCount: 0,
    nearExpiryLotCount: 0,
    pendingTransferCount: 0,
    openShiftCount: 0,
    activeRegisterCount: 0,
  };
}

function makeDashboardPayload(
  total: number,
  overrides: Record<string, unknown> = {},
) {
  return {
    fromDate: "2026-08-30",
    toDate: "2026-08-30",
    completedSalesTotal: total,
    completedSaleCount: total > 0 ? 1 : 0,
    cashSalesTotal: total,
    manualGCashSalesTotal: 0,
    utangSalesTotal: 0,
    activeCustomerUtangOutstanding: 0,
    overdueUtangAmount: 0,
    recordedExpenseTotal: 0,
    lowStockProductCount: 0,
    voidedSaleCount: 0,
    voidedExpenseCount: 0,
    salesByDay: [{ date: "2026-08-30", amount: total, count: total > 0 ? 1 : 0 }],
    expensesByDay: [],
    paymentMethodBreakdown:
      total > 0 ? [{ paymentMethod: "Cash", amount: total, count: 1 }] : [],
    salesCountByDay: [{ date: "2026-08-30", amount: 0, count: total > 0 ? 1 : 0 }],
    salesTotalComparison: null,
    expenseTotalComparison: null,
    commercialDiscountTotal: 0,
    preDiscountGrossSales: total,
    ...overrides,
  };
}

function activeDashboard() {
  return makeDashboardPayload(48250, {
    completedSaleCount: 192,
    cashSalesTotal: 30000,
    manualGCashSalesTotal: 12000,
    utangSalesTotal: 6250,
    activeCustomerUtangOutstanding: 38400,
    overdueUtangAmount: 12200,
    recordedExpenseTotal: 2100,
    lowStockProductCount: 12,
    voidedSaleCount: 2,
    salesByDay: [
      { date: "2026-08-24", amount: 4200, count: 18 },
      { date: "2026-08-25", amount: 6100, count: 24 },
      { date: "2026-08-26", amount: 5800, count: 22 },
      { date: "2026-08-27", amount: 7200, count: 28 },
      { date: "2026-08-28", amount: 6900, count: 26 },
      { date: "2026-08-29", amount: 8800, count: 34 },
      { date: "2026-08-30", amount: 9250, count: 40 },
    ],
    paymentMethodBreakdown: [
      { paymentMethod: "Cash", amount: 30000, count: 110 },
      { paymentMethod: "ManualGCash", amount: 12000, count: 48 },
      { paymentMethod: "Utang", amount: 6250, count: 34 },
    ],
    salesTotalComparison: {
      comparisonFromDate: "2026-08-17",
      comparisonToDate: "2026-08-23",
      absoluteChange: 5200,
      percentageChange: 12.4,
      percentageAvailable: true,
    },
  });
}

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: TEST_ORG_A_ID,
      organizationDisplayName: "Kizy Store",
      branchId: TEST_BRANCH_A_ID,
      branchName: "Main Branch",
      experience: "manage_business" as const,
    },
    sessionGrant: {
      accessToken: "token",
      productAccessAllowed: true,
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
    },
  }),
}));

function renderDashboardPage() {
  return render(
    <AppProviders>
      <MemoryRouter>
        <ManagementDashboardPage />
      </MemoryRouter>
    </AppProviders>,
  );
}

function mockBranches() {
  listOrganizationBranches.mockResolvedValue({
    ok: true,
    branches: [
      {
        id: TEST_BRANCH_A_ID,
        organizationId: TEST_ORG_A_ID,
        code: "MAIN",
        name: "Main Branch",
        isPrimary: true,
        status: "Active",
      },
      {
        id: TEST_BRANCH_B_ID,
        organizationId: TEST_ORG_A_ID,
        code: "SEC",
        name: "Second Branch",
        isPrimary: false,
        status: "Active",
      },
    ],
  });
}

function mockUnavailableProfit() {
  getProfitabilityReport.mockResolvedValue({
    fromDate: "2026-08-30",
    toDate: "2026-08-30",
    branchId: null,
    netSales: 0,
    cogsStatus: "Unavailable",
    knownCogs: 0,
    totalCogs: null,
    grossProfit: null,
    grossMarginPercent: null,
    completedSaleCount: 0,
    completeCostSaleCount: 0,
    partialCostSaleCount: 0,
    unavailableCostSaleCount: 0,
    wasteLossKnownCost: 0,
    wasteLossCostStatus: "Unavailable",
    stockUseKnownCost: 0,
    stockUseCostStatus: "Unavailable",
    costCompletenessPercent: 0,
    commercialDiscountTotal: 0,
  });
}

describe("ManagementDashboardPage V2 visual composition", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockBranches();
    mockUnavailableProfit();
    getUtangReport.mockResolvedValue({
      fromDate: "2026-08-30",
      toDate: "2026-08-30",
      activeCustomerOutstanding: 0,
      overdueAmount: 0,
      customersWithBalances: 0,
      customersWithOverdue: 0,
      creditsRecordedInPeriod: 0,
      creditsRecordedCount: 0,
      repaymentsRecordedInPeriod: 0,
      repaymentsRecordedCount: 0,
      productBasedUtangSalesInPeriod: 0,
      productBasedUtangSaleCount: 0,
    });
    getSalesByProductReport.mockResolvedValue({
      fromDate: "2026-08-30",
      toDate: "2026-08-30",
      rows: [],
    });
  });

  it("EMPTY: avoids giant charts and misleading gauges", async () => {
    getManagementOverview.mockResolvedValue(makeOverviewPayload());
    getDashboard.mockResolvedValue(makeDashboardPayload(0));

    renderDashboardPage();

    await waitFor(() => {
      expect(screen.getByTestId("kpi-period-sales")).toBeInTheDocument();
    });

    expect(screen.getByTestId("dashboard-sales-trend-empty")).toBeInTheDocument();
    expect(screen.queryByTestId("dashboard-sales-trend-chart")).not.toBeInTheDocument();
    expect(screen.getByTestId("dashboard-payment-mix-empty")).toBeInTheDocument();
    expect(screen.queryByTestId("dashboard-payment-mix")).not.toBeInTheDocument();
    expect(screen.getByTestId("dashboard-utang-clear")).toBeInTheDocument();
    expect(screen.queryByTestId("dashboard-utang-radial")).not.toBeInTheDocument();
    expect(screen.getByTestId("dashboard-inventory-clear")).toBeInTheDocument();
    expect(screen.queryByTestId("dashboard-inventory-health")).not.toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId("dashboard-top-products-empty")).toBeInTheDocument();
    });
    expect(screen.queryByText(/change % n\/a/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Today\?s/)).not.toBeInTheDocument();
    expect(document.body.textContent).not.toMatch(/Today\?s/);
  });

  it("ACTIVE: renders charts and omits duplicate organization overview walls", async () => {
    getManagementOverview.mockResolvedValue(
      makeOverviewPayload({
        todaySalesTotal: 9250,
        todaySaleCount: 40,
        lowStockProductCount: 12,
        nearExpiryLotCount: 8,
        expiredLotCount: 2,
        openUtangOutstanding: 38400,
        openShiftCount: 2,
        activeRegisterCount: 3,
      }),
    );
    getDashboard.mockResolvedValue(activeDashboard());
    getSalesByProductReport.mockResolvedValue({
      fromDate: "2026-08-24",
      toDate: "2026-08-30",
      rows: [
        {
          productId: "11111111-1111-1111-1111-111111111111",
          productName: "Coke 1.5L",
          unitOfMeasure: "pc",
          sellingMode: "Unit",
          quantitySold: 40,
          quantityReturned: 0,
          netQuantity: 40,
          grossSaleAmount: 8200,
          refundAmount: 0,
          netAmount: 8200,
          preDiscountGrossSaleAmount: 8200,
          commercialDiscountAmount: 0,
        },
        {
          productId: "22222222-2222-2222-2222-222222222222",
          productName: "Rice",
          unitOfMeasure: "kg",
          sellingMode: "Unit",
          quantitySold: 30,
          quantityReturned: 0,
          netQuantity: 30,
          grossSaleAmount: 6500,
          refundAmount: 0,
          netAmount: 6500,
          preDiscountGrossSaleAmount: 6500,
          commercialDiscountAmount: 0,
        },
      ],
    });
    getUtangReport.mockResolvedValue({
      fromDate: "2026-08-24",
      toDate: "2026-08-30",
      activeCustomerOutstanding: 38400,
      overdueAmount: 12200,
      customersWithBalances: 18,
      customersWithOverdue: 6,
      creditsRecordedInPeriod: 6250,
      creditsRecordedCount: 34,
      repaymentsRecordedInPeriod: 4100,
      repaymentsRecordedCount: 12,
      productBasedUtangSalesInPeriod: 6250,
      productBasedUtangSaleCount: 34,
    });

    renderDashboardPage();

    await waitFor(() => {
      expect(screen.getByTestId("dashboard-sales-trend-chart")).toBeInTheDocument();
      expect(screen.getByTestId("dashboard-top-products-chart")).toBeInTheDocument();
    });

    expect(screen.getByTestId("dashboard-payment-mix")).toBeInTheDocument();
    expect(screen.getByTestId("dashboard-utang-radial")).toBeInTheDocument();
    expect(screen.getByTestId("dashboard-inventory-health")).toBeInTheDocument();
    expect(screen.getByTestId("dashboard-comparison-trend")).toHaveTextContent("12.4%");
    expect(screen.getByTestId("dashboard-toolbar")).toBeInTheDocument();
    expect(screen.queryByTestId("report-filters")).not.toBeInTheDocument();

    const orgSections = screen.getAllByTestId("dashboard-organization-overview");
    expect(orgSections).toHaveLength(1);

    expect(screen.getByTestId("scope-period-sales")).toHaveTextContent("Main Branch");
    expect(screen.getByTestId("scope-operations")).toHaveTextContent("Organization");

    const orgWideMatches = document.body.textContent?.match(/Organization-wide/g) ?? [];
    expect(orgWideMatches.length).toBe(0);
  });

  it("keeps branch filter authority and refreshes branch sales", async () => {
    getManagementOverview.mockResolvedValue(makeOverviewPayload({ todaySalesTotal: 900 }));
    getDashboard.mockImplementation(async (_workspace, _range, _signal, branchId) => {
      const total =
        branchId === TEST_BRANCH_B_ID ? 2000 : branchId === TEST_BRANCH_A_ID ? 1000 : 3000;
      return makeDashboardPayload(total);
    });

    renderDashboardPage();

    await waitFor(() => {
      expect(screen.getByTestId("kpi-period-sales")).toHaveTextContent("1,000");
    });

    await userEvent.selectOptions(screen.getByTestId("report-scope-select"), TEST_BRANCH_B_ID);

    await waitFor(() => {
      expect(screen.getByTestId("kpi-period-sales")).toHaveTextContent("2,000");
    });

    expect(within(screen.getByTestId("dashboard-branch-performance")).getByTestId("scope-period-sales")).toHaveTextContent(
      "Second Branch",
    );

    expect(getDashboard).toHaveBeenCalledWith(
      expect.objectContaining({ branchId: TEST_BRANCH_A_ID }),
      expect.any(Object),
      expect.any(AbortSignal),
      TEST_BRANCH_B_ID,
    );

    expect(screen.getByTestId("kpi-period-expenses")).toHaveAttribute(
      "data-metric-scope",
      "organization",
    );
    expect(screen.getByTestId("dashboard-scope-filter-note")).toBeInTheDocument();
  });

  it("uses compact branch comparison CTA instead of instructional analytics slot", async () => {
    getManagementOverview.mockResolvedValue(makeOverviewPayload());
    getDashboard.mockResolvedValue(makeDashboardPayload(0));

    renderDashboardPage();

    await waitFor(() => {
      expect(screen.getByTestId("dashboard-branch-ranking-unavailable")).toBeInTheDocument();
    });

    expect(screen.getByTestId("dashboard-compare-branches")).toBeInTheDocument();
    expect(screen.queryByText(/Select All branches \(Owner\/Manager\)/i)).not.toBeInTheDocument();
  });
});
