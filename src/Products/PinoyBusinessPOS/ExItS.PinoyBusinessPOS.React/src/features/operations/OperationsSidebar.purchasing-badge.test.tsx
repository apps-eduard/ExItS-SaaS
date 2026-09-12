import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { OperationsSidebar } from "@/features/operations/OperationsSidebar";

const listPurchaseOrders = vi.fn();
const listIncomingOrders = vi.fn();

vi.mock("@/access/pos-capabilities", async () => {
  const actual = await vi.importActual<typeof import("@/access/pos-capabilities")>(
    "@/access/pos-capabilities",
  );
  return {
    ...actual,
    canViewPurchasing: () => true,
    canViewInventory: () => true,
    canManageInventory: () => true,
    canViewOrders: () => true,
    canViewTransfers: () => true,
    canSell: () => true,
  };
});

vi.mock("@/api/pos/pos-purchase-orders-client", async () => {
  const actual = await vi.importActual<typeof import("@/api/pos/pos-purchase-orders-client")>(
    "@/api/pos/pos-purchase-orders-client",
  );
  return {
    ...actual,
    listPurchaseOrders: (...args: unknown[]) => listPurchaseOrders(...args),
  };
});

vi.mock("@/api/pos/pos-connected-suppliers-client", () => ({
  listIncomingOrders: (...args: unknown[]) => listIncomingOrders(...args),
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      branchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      branchType: "Retail",
      experience: "operations",
      organizationDisplayName: "Demo",
      branchName: "Main",
    },
    sessionGrant: {
      accessToken: "t",
      capabilities: ["ViewPurchasing", "ViewInventory", "ViewOrders", "Sell"],
    },
  }),
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

function receivablePo() {
  return {
    purchaseOrderId: "11111111-1111-1111-1111-111111111111",
    organizationId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    branchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    supplierId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    supplierName: "Supplier",
    status: "Ordered",
    poNumber: "PO-1",
    orderDate: "2026-09-10",
    expectedDeliveryDate: null,
    notes: null,
    currencyCode: "PHP",
    createdAtUtc: "2026-09-10T00:00:00Z",
    updatedAtUtc: "2026-09-10T00:00:00Z",
    canReceiveConnected: true,
    needsProductSetup: false,
    lines: [
      {
        purchaseOrderLineId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
        lineNumber: 1,
        productId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
        productName: "Item",
        sku: "SKU",
        orderedQty: 2,
        receivedQty: 0,
        outstandingQty: 2,
        unitCost: 10,
        lineTotal: 20,
      },
    ],
  };
}

function renderSidebar(path = "/role/manager") {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="*" element={<OperationsSidebar />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("OperationsSidebar Purchasing activity badge", () => {
  beforeEach(() => {
    listPurchaseOrders.mockReset();
    listIncomingOrders.mockReset();
  });

  it("shows CountBadge when navigationCount > 0", async () => {
    listPurchaseOrders.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 40,
    });
    listIncomingOrders.mockResolvedValue([
      { connectedPurchaseOrderId: "a" },
      { connectedPurchaseOrderId: "b" },
    ]);

    renderSidebar();

    const link = await screen.findByTestId("ops-sidebar-purchasing");
    await waitFor(() => {
      expect(screen.getByTestId("ops-sidebar-purchasing-badge")).toHaveTextContent("2");
    });
    expect(link).toHaveAttribute("aria-label", "org.nav.purchasing, 2 items");
  });

  it("hides badge when navigationCount is 0", async () => {
    listPurchaseOrders.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 40,
    });
    listIncomingOrders.mockResolvedValue([]);

    renderSidebar();

    await screen.findByTestId("ops-sidebar-purchasing");
    await waitFor(() => {
      expect(listIncomingOrders).toHaveBeenCalled();
      expect(listPurchaseOrders).toHaveBeenCalled();
    });
    expect(screen.queryByTestId("ops-sidebar-purchasing-badge")).not.toBeInTheDocument();
  });

  it("hides badge while loading", () => {
    listPurchaseOrders.mockReturnValue(new Promise(() => undefined));
    listIncomingOrders.mockReturnValue(new Promise(() => undefined));

    renderSidebar();

    expect(screen.getByTestId("ops-sidebar-purchasing")).toBeInTheDocument();
    expect(screen.queryByTestId("ops-sidebar-purchasing-badge")).not.toBeInTheDocument();
  });

  it("hides badge on summary error", async () => {
    listPurchaseOrders.mockRejectedValue(new Error("boom"));
    listIncomingOrders.mockResolvedValue([{ connectedPurchaseOrderId: "a" }]);

    renderSidebar();

    await screen.findByTestId("ops-sidebar-purchasing");
    await waitFor(() => {
      expect(listPurchaseOrders).toHaveBeenCalled();
    });
    expect(screen.queryByTestId("ops-sidebar-purchasing-badge")).not.toBeInTheDocument();
  });

  it("keeps badge when Purchasing is the selected nav item", async () => {
    listPurchaseOrders.mockResolvedValue({
      items: [receivablePo()],
      totalCount: 5,
      page: 1,
      pageSize: 40,
    });
    listIncomingOrders.mockResolvedValue([{ connectedPurchaseOrderId: "a" }]);

    renderSidebar("/purchasing");

    const link = await screen.findByTestId("ops-sidebar-purchasing");
    expect(link).toHaveAttribute("aria-current", "page");
    await waitFor(() => {
      // incoming 1 + receivable 1 (not PO total 5)
      expect(screen.getByTestId("ops-sidebar-purchasing-badge")).toHaveTextContent("2");
    });
  });
});
