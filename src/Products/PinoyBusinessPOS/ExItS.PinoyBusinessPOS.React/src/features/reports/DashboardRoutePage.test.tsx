import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { DashboardRoutePage } from "@/features/reports/DashboardRoutePage";
import * as posReportingClient from "@/api/pos/pos-reporting-client";
import * as posInventoryClient from "@/api/pos/pos-inventory-client";
import * as posTransferClient from "@/api/pos/pos-inventory-transfer-client";
import * as posPurchaseOrdersClient from "@/api/pos/pos-purchase-orders-client";
import * as posStockRequestsClient from "@/api/pos/pos-stock-requests-client";
import * as platformAuthClient from "@/api/platform/platform-auth-client";
import { TEST_BRANCH_A_ID, TEST_ORG_A_ID } from "@/test/session-context";

const workspaceState = {
  branchType: "Retail" as "Retail" | "Warehouse",
  branchName: "Main Branch",
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: TEST_ORG_A_ID,
      organizationDisplayName: "Kizy Store",
      branchId: TEST_BRANCH_A_ID,
      branchName: workspaceState.branchName,
      branchType: workspaceState.branchType,
      experience: workspaceState.branchType === "Warehouse" ? "operations" : "manage_business",
    },
    sessionGrant: {
      accessToken: "token",
      productAccessAllowed: true,
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
    },
  }),
}));

vi.spyOn(posReportingClient, "getManagementOverview").mockResolvedValue({
  businessDate: "2026-09-05",
  todaySalesTotal: 100,
  todaySaleCount: 1,
  todayCashSalesTotal: 100,
  todayUtangSalesTotal: 0,
  todayPaymentsReceived: 0,
  openUtangOutstanding: 0,
  lowStockProductCount: 0,
  expiredLotCount: 0,
  nearExpiryLotCount: 0,
  pendingTransferCount: 0,
  openShiftCount: 0,
  activeRegisterCount: 0,
});

vi.spyOn(posReportingClient, "getDashboard").mockResolvedValue({
  fromDate: "2026-09-05",
  toDate: "2026-09-05",
  completedSalesTotal: 100,
  completedSaleCount: 1,
  cashSalesTotal: 100,
  manualGCashSalesTotal: 0,
  utangSalesTotal: 0,
  activeCustomerUtangOutstanding: 0,
  overdueUtangAmount: 0,
  recordedExpenseTotal: 0,
  lowStockProductCount: 0,
  voidedSaleCount: 0,
  voidedExpenseCount: 0,
  salesByDay: [],
  expensesByDay: [],
  paymentMethodBreakdown: [],
  salesCountByDay: [],
  salesTotalComparison: null,
  expenseTotalComparison: null,
  commercialDiscountTotal: 0,
  preDiscountGrossSales: 100,
});

vi.spyOn(posReportingClient, "getSalesByProductReport").mockResolvedValue({
  fromDate: "2026-09-05",
  toDate: "2026-09-05",
  rows: [],
});
vi.spyOn(posReportingClient, "getProfitabilityReport").mockResolvedValue({
  fromDate: "2026-09-05",
  toDate: "2026-09-05",
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
vi.spyOn(posReportingClient, "getUtangReport").mockResolvedValue({
  fromDate: "2026-09-05",
  toDate: "2026-09-05",
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
vi.spyOn(platformAuthClient, "listOrganizationBranches").mockResolvedValue({
  ok: true,
  branches: [],
});

vi.spyOn(posInventoryClient, "listInventory").mockResolvedValue({
  items: [
    {
      productId: TEST_BRANCH_A_ID,
      organizationId: TEST_ORG_A_ID,
      name: "Rice",
      unitOfMeasure: "sack",
      productStatus: "Active",
      isTracked: true,
      onHandQuantity: 10,
      stockStatus: "InStock",
      isLowStock: false,
      createdAtUtc: "2026-09-05T00:00:00Z",
      updatedAtUtc: "2026-09-05T00:00:00Z",
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 200,
});
vi.spyOn(posInventoryClient, "listExpiringLots").mockResolvedValue({
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 8,
  expiredCount: 0,
  nearExpiryCount: 0,
});
vi.spyOn(posTransferClient, "listInventoryTransfers").mockResolvedValue({
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 40,
});
vi.spyOn(posPurchaseOrdersClient, "listPurchaseOrders").mockResolvedValue({
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 40,
});
vi.spyOn(posStockRequestsClient, "listIncomingStockRequests").mockResolvedValue({
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 40,
});
vi.spyOn(posReportingClient, "getInventoryMovementsReport").mockResolvedValue({
  fromDate: "2026-09-05",
  toDate: "2026-09-05",
  movementCount: 0,
  byType: [],
  rows: [],
});
vi.spyOn(posReportingClient, "getPurchasingSummaryReport").mockResolvedValue({
  fromDate: "2026-09-05",
  toDate: "2026-09-05",
  orderCount: 0,
  orderedQuantity: 0,
  receivedQuantity: 0,
  outstandingQuantity: 0,
});

describe("DashboardRoutePage location-type routing", () => {
  beforeEach(() => {
    workspaceState.branchType = "Retail";
    workspaceState.branchName = "Main Branch";
  });

  it("renders Retail management dashboard for Retail workspace", async () => {
    render(
      <AppProviders>
        <MemoryRouter>
          <DashboardRoutePage />
        </MemoryRouter>
      </AppProviders>,
    );
    await waitFor(() => {
      expect(screen.getByTestId("management-dashboard-page")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("warehouse-management-dashboard")).not.toBeInTheDocument();
  });

  it("renders Warehouse dashboard without sales/payment metrics", async () => {
    workspaceState.branchType = "Warehouse";
    workspaceState.branchName = "Panay Warehouse";
    render(
      <AppProviders>
        <MemoryRouter>
          <DashboardRoutePage />
        </MemoryRouter>
      </AppProviders>,
    );
    await waitFor(() => {
      expect(screen.getByTestId("warehouse-management-dashboard")).toBeInTheDocument();
      expect(screen.getByTestId("warehouse-kpi-strip")).toBeInTheDocument();
      expect(screen.getByTestId("warehouse-transfers")).toBeInTheDocument();
      expect(screen.getByTestId("warehouse-replenishment")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("management-dashboard-page")).not.toBeInTheDocument();
    expect(screen.queryByTestId("report-scope-select")).not.toBeInTheDocument();
    expect(screen.queryByTestId("kpi-period-sales")).not.toBeInTheDocument();
    expect(screen.queryByTestId("dashboard-payment-mix")).not.toBeInTheDocument();
    expect(screen.queryByText(/GCash/i)).not.toBeInTheDocument();
  });

  it("shows purchasing for Owner grant on Warehouse dashboard", async () => {
    workspaceState.branchType = "Warehouse";
    render(
      <AppProviders>
        <MemoryRouter>
          <DashboardRoutePage />
        </MemoryRouter>
      </AppProviders>,
    );
    await waitFor(() => {
      expect(screen.getByTestId("warehouse-purchasing")).toBeInTheDocument();
    });
  });
});
