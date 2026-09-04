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

vi.mock("@/api/pos/pos-reporting-client", () => ({
  getManagementOverview: vi.fn(async () => ({
    businessDate: "2026-09-04",
    todaySalesTotal: 0,
    todaySaleCount: 0,
    todayCashSalesTotal: 0,
    todayUtangSalesTotal: 0,
    todayPaymentsReceived: 0,
    openUtangOutstanding: 0,
    lowStockProductCount: 2,
    expiredLotCount: 1,
    nearExpiryLotCount: 3,
    pendingTransferCount: 1,
    openShiftCount: 0,
    activeRegisterCount: 0,
  })),
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
  it("renders warehouse home with branch name and stock alerts", async () => {
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
    expect(screen.getByText("warehouse.title")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId("warehouse-alert-low-stock")).toHaveTextContent("2");
    });
    expect(screen.getByTestId("warehouse-quick-actions")).toBeInTheDocument();
  });
});
