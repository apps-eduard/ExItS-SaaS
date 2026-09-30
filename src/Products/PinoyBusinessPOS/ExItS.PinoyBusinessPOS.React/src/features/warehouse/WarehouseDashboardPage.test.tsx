import { describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { WarehouseDashboardPage } from "@/features/warehouse/WarehouseDashboardPage";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({
    session: { accountClass: "Organization" },
    refreshSession: vi.fn(),
  }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "11111111-1111-1111-1111-111111111111",
      organizationDisplayName: "Test Org",
      branchId: "22222222-2222-2222-2222-222222222222",
      branchName: "Central Warehouse",
      branchType: "Warehouse",
      experience: "operations",
    },
    sessionGrant: {
      accessToken: "token",
      productAccessAllowed: true,
      mappedPosRoleCode: "StoreManager",
      productLocalRoleCode: "StoreManager",
    },
  }),
}));

const getManagementOverview = vi.fn(async () => ({
  businessDate: "2026-09-04",
  todaySalesTotal: 0,
  todaySaleCount: 0,
  todayCashSalesTotal: 0,
  todayUtangSalesTotal: 0,
  todayPaymentsReceived: 0,
  openUtangOutstanding: 0,
  lowStockProductCount: 99,
  expiredLotCount: 99,
  nearExpiryLotCount: 99,
  pendingTransferCount: 1,
  openShiftCount: 0,
  activeRegisterCount: 0,
}));

vi.mock("@/api/pos/pos-reporting-client", () => ({
  getManagementOverview: (...args: unknown[]) => getManagementOverview(...args),
}));

const getInventoryAttentionSummary = vi.fn(async () => ({
  lowStockProductCount: 2,
  outOfStockProductCount: 0,
  expiredLotCount: 0,
  nearExpiryLotCount: 0,
}));

vi.mock("@/api/pos/pos-inventory-client", () => ({
  getInventoryAttentionSummary: (...args: unknown[]) => getInventoryAttentionSummary(...args),
}));

vi.mock("@/api/pos/pos-inventory-transfer-client", () => ({
  listInventoryTransfers: vi.fn(async () => ({
    items: [],
    page: 1,
    pageSize: 8,
    totalCount: 0,
  })),
}));

vi.mock("@/api/pos/pos-purchase-orders-client", () => ({
  listPurchaseOrders: vi.fn(async () => ({
    items: [],
    page: 1,
    pageSize: 20,
    totalCount: 0,
  })),
  isReceivablePurchaseOrderStatus: (status: string) =>
    status === "Ordered" || status === "PartiallyReceived",
}));

describe("WarehouseDashboardPage", () => {
  it("renders warehouse command center without Start selling", async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <WarehouseDashboardPage />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    expect(screen.getByTestId("warehouse-dashboard")).toBeInTheDocument();
    expect(screen.getByTestId("warehouse-dashboard")).toHaveAttribute(
      "data-home-variant",
      "warehouse",
    );
    expect(screen.getByText("managerHome.warehouseTitle")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId("manager-attention-low-stock")).toBeInTheDocument();
    });
    expect(screen.getByTestId("manager-home-quick-actions")).toBeInTheDocument();
    expect(screen.queryByTestId("manager-action-sell")).not.toBeInTheDocument();
  });

  it("uses branch attention summary for expiry — not management overview", async () => {
    getInventoryAttentionSummary.mockResolvedValueOnce({
      lowStockProductCount: 0,
      outOfStockProductCount: 0,
      expiredLotCount: 0,
      nearExpiryLotCount: 0,
    });

    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <WarehouseDashboardPage />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("manager-today-stock-alerts")).toBeInTheDocument();
    });

    expect(getInventoryAttentionSummary).toHaveBeenCalled();
    expect(getManagementOverview).not.toHaveBeenCalled();
    expect(screen.queryByTestId("manager-attention-expiry")).not.toBeInTheDocument();
    const stockAlerts = screen.getByTestId("manager-today-stock-alerts");
    expect(stockAlerts.textContent).toMatch(/0/);
    expect(stockAlerts.textContent).not.toMatch(/198|99/);
  });
});
