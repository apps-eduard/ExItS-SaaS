import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { ManagerRetailHome } from "@/features/role/ManagerRetailHome";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import * as supplyRoutesClient from "@/api/pos/pos-supply-routes-client";
import * as stockRequestsClient from "@/api/pos/pos-stock-requests-client";

const navigateMock = vi.fn();
const showToastMock = vi.fn();

const workspaceState = vi.hoisted(() => ({
  grant: {
    productAccessAllowed: true,
    mappedPosRoleCode: "StoreManager",
    productLocalRoleCode: "StoreManager",
    organizationManagementAuthority: false,
  } as PosSessionGrantFacts,
  branches: [
    {
      branchId: "22222222-2222-2222-2222-222222222222",
      name: "Main Branch",
      secondaryLine: "",
      isPrimary: true,
      isActive: true,
      branchType: "Retail",
    },
  ] as Array<{
    branchId: string;
    name: string;
    secondaryLine: string;
    isPrimary: boolean;
    isActive: boolean;
    branchType: string;
  }>,
}));

vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");
  return {
    ...actual,
    useNavigate: () => navigateMock,
  };
});

vi.mock("@/components/exits/ToastProvider", () => ({
  useToast: () => ({ showToast: showToastMock }),
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/selling/SellingModeProvider", () => ({
  useSellingMode: () => ({ enter: vi.fn() }),
}));

vi.mock("@/features/shifts/ShiftContextProvider", () => ({
  useShiftContext: () => ({
    currentShift: null,
    hasOpenShift: false,
    loading: false,
    errorMessage: null,
    denied: false,
    readiness: { ready: false },
    refresh: vi.fn(),
  }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "11111111-1111-1111-1111-111111111111",
      organizationDisplayName: "Test Org",
      branchId: "22222222-2222-2222-2222-222222222222",
      branchName: "Main Branch",
      branchType: "Retail",
      experience: "operations",
    },
    sessionGrant: workspaceState.grant,
    workspaces: [
      {
        organizationId: "11111111-1111-1111-1111-111111111111",
        displayName: "Test Org",
        branches: workspaceState.branches,
      },
    ],
  }),
}));

vi.mock("@/api/pos/pos-reporting-client", () => ({
  getDashboard: vi.fn(async () => ({
    fromDate: "2026-09-04",
    toDate: "2026-09-04",
    completedSalesTotal: 0,
    completedSaleCount: 0,
    cashSalesTotal: 0,
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
  })),
  getManagementOverview: vi.fn(async () => ({
    businessDate: "2026-09-04",
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
  })),
}));

vi.mock("@/api/pos/pos-customer-orders-client", () => ({
  sellerWorkspace: (organizationId: string, branchId?: string | null) => ({
    organizationId,
    branchId: branchId ?? undefined,
  }),
  listSellerCustomerOrders: vi.fn(async () => ({
    items: [],
    page: 1,
    pageSize: 1,
    totalCount: 0,
  })),
}));

vi.mock("@/api/pos/pos-inventory-transfer-client", () => ({
  listInventoryTransfers: vi.fn(async () => ({
    items: [],
    page: 1,
    pageSize: 40,
    totalCount: 0,
  })),
}));

vi.mock("@/api/pos/pos-purchase-orders-client", () => ({
  listPurchaseOrders: vi.fn(async () => ({
    items: [],
    page: 1,
    pageSize: 40,
    totalCount: 0,
  })),
  isReceivablePurchaseOrderStatus: () => false,
}));

function renderHome() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ManagerRetailHome />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("ManagerRetailHome warehouse quick action", () => {
  beforeEach(() => {
    navigateMock.mockReset();
    showToastMock.mockReset();
    workspaceState.grant = {
      productAccessAllowed: true,
      mappedPosRoleCode: "StoreManager",
      productLocalRoleCode: "StoreManager",
      organizationManagementAuthority: false,
    };
    workspaceState.branches = [
      {
        branchId: "22222222-2222-2222-2222-222222222222",
        name: "Main Branch",
        secondaryLine: "",
        isPrimary: true,
        isActive: true,
        branchType: "Retail",
      },
    ];
    vi.spyOn(stockRequestsClient, "getOutgoingStockRequestSummary").mockResolvedValue({
      submittedCount: 0,
      inProgressCount: 0,
      inTransitCount: 4,
      recent: [],
    });
  });

  it("always shows Warehouse action and badges in-transit count", async () => {
    vi.spyOn(supplyRoutesClient, "listSupplyRoutesByDestination").mockResolvedValue([]);
    renderHome();
    await waitFor(() => {
      expect(screen.getByTestId("manager-action-warehouse")).toBeInTheDocument();
    });
    expect(await screen.findByTestId("manager-action-warehouse-badge")).toHaveTextContent("4");
  });

  it("toasts when no warehouse exists", async () => {
    workspaceState.grant.organizationManagementAuthority = true;
    vi.spyOn(supplyRoutesClient, "listSupplyRoutesByDestination").mockResolvedValue([]);
    renderHome();
    await waitFor(() => {
      expect(screen.getByTestId("manager-action-warehouse")).toBeInTheDocument();
    });
    fireEvent.click(screen.getByTestId("manager-action-warehouse"));
    await waitFor(() => {
      expect(showToastMock).toHaveBeenCalled();
    });
    expect(showToastMock.mock.calls[0]?.[0]).toMatchObject({
      title: "warehouse.toast.noWarehouse.title",
      action: { href: "/org/branches" },
    });
    expect(navigateMock).not.toHaveBeenCalled();
  });

  it("navigates to /warehouse when supply is ready", async () => {
    const whId = "33333333-3333-3333-3333-333333333333";
    workspaceState.branches.push({
      branchId: whId,
      name: "Supply WH",
      secondaryLine: "",
      isPrimary: false,
      isActive: true,
      branchType: "Warehouse",
    });
    vi.spyOn(supplyRoutesClient, "listSupplyRoutesByDestination").mockResolvedValue([
      {
        routeId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        organizationId: "11111111-1111-1111-1111-111111111111",
        sourceLocationId: whId,
        destinationLocationId: "22222222-2222-2222-2222-222222222222",
        isPreferred: true,
        isActive: true,
        createdAtUtc: "2026-01-01T00:00:00Z",
        updatedAtUtc: "2026-01-01T00:00:00Z",
      },
    ]);
    renderHome();
    await waitFor(() => {
      expect(screen.getByTestId("manager-action-warehouse")).toBeInTheDocument();
    });
    fireEvent.click(screen.getByTestId("manager-action-warehouse"));
    await waitFor(() => {
      expect(navigateMock).toHaveBeenCalledWith("/warehouse");
    });
  });
});
