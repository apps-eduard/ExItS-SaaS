import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { ManagerHomePage } from "@/features/role/ManagerHomePage";
import {
  canCreateSale,
  canEnterManagerRoleHome,
  type PosSessionGrantFacts,
} from "@/access/pos-capabilities";

const workspaceState = vi.hoisted(() => ({
  branchType: "Retail" as string,
  branchId: "22222222-2222-2222-2222-222222222222",
  branchName: "Main Branch",
  grant: {
    productAccessAllowed: true,
    mappedPosRoleCode: "StoreManager",
    productLocalRoleCode: "StoreManager",
  } as PosSessionGrantFacts,
  hasOpenShift: false,
  currentShift: null as null | {
    shiftId: string;
    shiftNumber: string;
    registerCode: string;
    registerName: string;
  },
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string, vars?: Record<string, string | number>) => {
      if (!vars) return key;
      return Object.entries(vars).reduce(
        (acc, [k, v]) => acc.replace(`{${k}}`, String(v)),
        key,
      );
    },
  }),
}));

vi.mock("@/selling/SellingModeProvider", () => ({
  useSellingMode: () => ({ enter: vi.fn() }),
}));

vi.mock("@/features/shifts/ShiftContextProvider", () => ({
  useShiftContext: () => ({
    currentShift: workspaceState.currentShift,
    hasOpenShift: workspaceState.hasOpenShift,
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
      branchId: workspaceState.branchId,
      branchName: workspaceState.branchName,
      branchType: workspaceState.branchType,
      experience: "operations",
    },
    sessionGrant: workspaceState.grant,
  }),
}));

vi.mock("@/api/pos/pos-reporting-client", () => ({
  getDashboard: vi.fn(async () => ({
    fromDate: "2026-09-04",
    toDate: "2026-09-04",
    completedSalesTotal: 1500,
    completedSaleCount: 7,
    cashSalesTotal: 1000,
    manualGCashSalesTotal: 500,
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
    todaySalesTotal: 1500,
    todaySaleCount: 7,
    todayCashSalesTotal: 1000,
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
        <ManagerHomePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("ManagerHomePage", () => {
  beforeEach(() => {
    workspaceState.branchType = "Retail";
    workspaceState.branchId = "22222222-2222-2222-2222-222222222222";
    workspaceState.branchName = "Main Branch";
    workspaceState.grant = {
      productAccessAllowed: true,
      mappedPosRoleCode: "StoreManager",
      productLocalRoleCode: "StoreManager",
    };
    workspaceState.hasOpenShift = false;
    workspaceState.currentShift = null;
  });

  it("authority: StoreManager may enter Manager Home; pure OrgAdmin and Cashier may not", () => {
    expect(canEnterManagerRoleHome(workspaceState.grant)).toBe(true);
    expect(
      canEnterManagerRoleHome({
        productAccessAllowed: false,
        membershipRole: "OrganizationAdministrator",
      }),
    ).toBe(false);
    expect(
      canEnterManagerRoleHome({
        productAccessAllowed: true,
        mappedPosRoleCode: "Cashier",
        productLocalRoleCode: "Cashier",
      }),
    ).toBe(false);
    expect(
      canEnterManagerRoleHome({
        productAccessAllowed: true,
        membershipRole: "Owner",
        mappedPosRoleCode: "Owner",
      }),
    ).toBe(true);
  });

  it("renders retail command center with today metrics and no Devices/Branches admin shortcuts", async () => {
    renderHome();
    expect(screen.getByTestId("manager-home")).toHaveAttribute("data-home-variant", "retail");
    await waitFor(() => {
      expect(screen.getByTestId("manager-today-sales")).toBeInTheDocument();
    });
    expect(screen.getByTestId("manager-today-transactions")).toBeInTheDocument();
    expect(screen.getByTestId("manager-attention-healthy")).toBeInTheDocument();
    expect(screen.queryByTestId("role-devices")).not.toBeInTheDocument();
    expect(screen.queryByText(/Authorized devices/i)).not.toBeInTheDocument();
    expect(screen.queryByTestId("open-branches")).not.toBeInTheDocument();
    expect(screen.queryByTestId("manager-action-sell")).toBeInTheDocument();
    expect(canCreateSale(workspaceState.grant, "Retail")).toBe(true);
  });

  it("retail Start selling is gated by canCreateSale", async () => {
    workspaceState.grant = {
      productAccessAllowed: true,
      mappedPosRoleCode: "ReportingUser",
      productLocalRoleCode: "ReportingUser",
    };
    // ReportingUser typically cannot sell — if capability denies, sell tile absent
    renderHome();
    await waitFor(() => {
      expect(screen.getByTestId("manager-home")).toBeInTheDocument();
    });
    if (!canCreateSale(workspaceState.grant, "Retail")) {
      expect(screen.queryByTestId("manager-action-sell")).not.toBeInTheDocument();
    }
  });

  it("renders warehouse composition without Start selling", async () => {
    workspaceState.branchType = "Warehouse";
    workspaceState.branchName = "Central Warehouse";
    renderHome();
    expect(screen.getByTestId("manager-home")).toHaveAttribute("data-home-variant", "warehouse");
    await waitFor(() => {
      expect(screen.getByTestId("manager-home-today")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("manager-action-sell")).not.toBeInTheDocument();
    expect(screen.queryByTestId("manager-today-sales")).not.toBeInTheDocument();
    expect(canCreateSale(workspaceState.grant, "Warehouse")).toBe(false);
  });

  it("uses current branch identity in the header subtitle", async () => {
    workspaceState.branchName = "Branch B";
    renderHome();
    await waitFor(() => {
      expect(screen.getByText("Branch B")).toBeInTheDocument();
    });
  });

  it("highlights Manager role chip on retail home", async () => {
    renderHome();
    await waitFor(() => {
      expect(screen.getByTestId("manager-home")).toBeInTheDocument();
    });
    const badge = screen.getByTestId("manager-home-badge");
    expect(badge.querySelector(".manager-home-role-chip")).not.toBeNull();
    expect(badge).toHaveTextContent("role.managerBadge");
  });

  it("polishes retail action cards: neutral sell, chevrons, shift sixth action, insight cards", async () => {
    renderHome();
    await waitFor(() => {
      expect(screen.getByTestId("manager-action-sell")).toBeInTheDocument();
    });

    const sell = screen.getByTestId("manager-action-sell");
    expect(sell.className).toContain("manager-action-card");
    expect(sell.className).not.toContain("role-action-tile--primary");
    expect(sell.className).not.toMatch(/\bbg-primary\b/);
    expect(sell.querySelector(".manager-action-card__chevron")).toBeTruthy();

    expect(screen.getByTestId("manager-action-shift")).toHaveTextContent(
      "managerHome.shift.openAction",
    );
    expect(screen.getByTestId("manager-action-shift")).toHaveAttribute("href", "/shifts/open");
    expect(screen.queryByTestId("manager-shift-view")).not.toBeInTheDocument();
    expect(screen.queryByTestId("manager-shift-open")).not.toBeInTheDocument();

    const dashboard = screen.getByTestId("manager-insight-dashboard");
    const reports = screen.getByTestId("manager-insight-reports");
    expect(dashboard.className).toContain("manager-action-card");
    expect(reports.className).toContain("manager-action-card");
    expect(dashboard).toHaveAttribute("href", "/dashboard");
    expect(reports).toHaveAttribute("href", "/reports");
    expect(dashboard.querySelector(".manager-action-card__chevron")).toBeTruthy();
    expect(reports.querySelector(".manager-action-card__chevron")).toBeTruthy();

    expect(screen.getByTestId("manager-home-quick-actions").querySelectorAll(".manager-action-card"))
      .toHaveLength(6);
  });

  it("shows View shift quick action when shift is open and removes floating link", async () => {
    workspaceState.hasOpenShift = true;
    workspaceState.currentShift = {
      shiftId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      shiftNumber: "SHIFT-20260901-000001",
      registerCode: "REG-000001",
      registerName: "PWA-0001",
    };
    renderHome();
    await waitFor(() => {
      expect(screen.getByTestId("manager-action-shift")).toBeInTheDocument();
    });
    expect(screen.getByTestId("manager-action-shift")).toHaveTextContent("managerHome.shift.view");
    expect(screen.getByTestId("manager-action-shift")).toHaveAttribute(
      "href",
      "/shifts/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    );
    expect(screen.queryByTestId("manager-shift-view")).not.toBeInTheDocument();

    const shiftMetric = screen.getByTestId("manager-today-shift");
    expect(shiftMetric).toHaveAttribute("data-value-scale", "restrained");
    expect(shiftMetric.className).not.toMatch(/exits-alert-surface/);
    expect(shiftMetric).toHaveTextContent("SHIFT-20260901-000001");
    expect(shiftMetric.querySelector(".exits-type-kpi")).toBeNull();
    expect(shiftMetric.querySelector(".manager-metric-value--restrained")).not.toBeNull();
    expect(screen.getByTestId("manager-today-register")).toHaveAttribute(
      "data-value-scale",
      "restrained",
    );
    expect(screen.getByTestId("manager-today-register")).toHaveTextContent(
      "REG-000001 — PWA-0001",
    );
    expect(screen.getByTestId("manager-today-register").querySelector(".exits-type-kpi")).toBeNull();
    expect(
      screen.getByTestId("manager-today-register").querySelector(".manager-metric-value--restrained"),
    ).not.toBeNull();
    expect(screen.getByTestId("manager-today-sales").querySelector(".exits-type-kpi")).toBeTruthy();
  });
});
