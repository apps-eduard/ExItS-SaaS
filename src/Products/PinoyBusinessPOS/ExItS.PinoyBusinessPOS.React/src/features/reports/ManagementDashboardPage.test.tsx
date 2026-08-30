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
const listOrganizationBranches = vi.spyOn(platformAuthClient, "listOrganizationBranches");

function makeOverviewPayload() {
  return {
    businessDate: "2026-08-30",
    todaySalesTotal: 900,
    todaySaleCount: 3,
    todayCashSalesTotal: 700,
    todayUtangSalesTotal: 200,
    todayPaymentsReceived: 50,
    openUtangOutstanding: 500,
    lowStockProductCount: 2,
    expiredLotCount: 0,
    nearExpiryLotCount: 1,
    pendingTransferCount: 0,
    openShiftCount: 1,
    activeRegisterCount: 1,
  };
}

function makeDashboardPayload(total: number) {
  return {
    fromDate: "2026-08-30",
    toDate: "2026-08-30",
    completedSalesTotal: total,
    completedSaleCount: 1,
    cashSalesTotal: total,
    manualGCashSalesTotal: 0,
    utangSalesTotal: 0,
    activeCustomerUtangOutstanding: 500,
    overdueUtangAmount: 0,
    recordedExpenseTotal: 100,
    lowStockProductCount: 2,
    voidedSaleCount: 0,
    voidedExpenseCount: 0,
    salesByDay: [{ date: "2026-08-30", amount: total, count: 1 }],
    expensesByDay: [{ date: "2026-08-30", amount: 100, count: 1 }],
    paymentMethodBreakdown: [{ paymentMethod: "Cash", amount: total, count: 1 }],
    salesCountByDay: [{ date: "2026-08-30", amount: 0, count: 1 }],
    salesTotalComparison: null,
    expenseTotalComparison: null,
    commercialDiscountTotal: 0,
    preDiscountGrossSales: total,
  };
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

describe("ManagementDashboardPage scope clarity", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getManagementOverview.mockResolvedValue(makeOverviewPayload());
    getDashboard.mockImplementation(async (_workspace, _range, _signal, branchId) => {
      const total =
        branchId === TEST_BRANCH_B_ID ? 2000 : branchId === TEST_BRANCH_A_ID ? 1000 : 3000;
      return makeDashboardPayload(total);
    });
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
  });

  it("shows organization-wide scope on today overview cards", async () => {
    renderDashboardPage();

    await waitFor(() => {
      expect(screen.getByTestId("kpi-today-sales")).toBeInTheDocument();
    });

    const todaySales = screen.getByTestId("kpi-today-sales");
    expect(within(todaySales).getByTestId("scope-today-sales")).toHaveTextContent(
      "Organization-wide",
    );
    expect(todaySales).toHaveAttribute("data-metric-scope", "organization");
  });

  it("shows branch scope on period sales and organization scope on expenses", async () => {
    renderDashboardPage();

    await waitFor(() => {
      expect(screen.getByTestId("kpi-period-sales")).toBeInTheDocument();
    });

    const periodSales = screen.getByTestId("kpi-period-sales");
    expect(within(periodSales).getByTestId("scope-period-sales")).toHaveTextContent(
      "Branch: Main Branch",
    );
    expect(periodSales).toHaveAttribute("data-metric-scope", "branch");

    const expenses = screen.getByTestId("kpi-period-expenses");
    expect(within(expenses).getByTestId("scope-period-expenses")).toHaveTextContent(
      "Organization-wide",
    );
    expect(expenses).toHaveAttribute("data-metric-scope", "organization");
  });

  it("refreshes branch metrics when branch selection changes", async () => {
    renderDashboardPage();

    await waitFor(() => {
      expect(screen.getByTestId("kpi-period-sales")).toHaveTextContent("1,000");
    });

    await userEvent.selectOptions(screen.getByTestId("report-scope-select"), TEST_BRANCH_B_ID);

    await waitFor(() => {
      expect(screen.getByTestId("kpi-period-sales")).toHaveTextContent("2,000");
    });

    expect(within(screen.getByTestId("kpi-period-sales")).getByTestId("scope-period-sales")).toHaveTextContent(
      "Branch: Second Branch",
    );

    expect(getDashboard).toHaveBeenCalledWith(
      expect.objectContaining({ branchId: TEST_BRANCH_A_ID }),
      expect.any(Object),
      expect.any(AbortSignal),
      TEST_BRANCH_A_ID,
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
  });

  it("keeps organization overview section visible after branch switch", async () => {
    renderDashboardPage();

    await waitFor(() => {
      expect(screen.getByTestId("dashboard-organization-overview")).toBeInTheDocument();
    });

    await userEvent.selectOptions(screen.getByTestId("report-scope-select"), TEST_BRANCH_B_ID);

    await waitFor(() => {
      expect(screen.getByTestId("dashboard-organization-overview")).toBeInTheDocument();
    });

    expect(screen.getByTestId("dashboard-scope-filter-note")).toBeInTheDocument();
  });

  it("renders grouped branch performance and organization overview sections", async () => {
    renderDashboardPage();

    await waitFor(() => {
      expect(screen.getByTestId("dashboard-branch-performance")).toBeInTheDocument();
      expect(screen.getByTestId("dashboard-organization-overview")).toBeInTheDocument();
    });
  });
});
