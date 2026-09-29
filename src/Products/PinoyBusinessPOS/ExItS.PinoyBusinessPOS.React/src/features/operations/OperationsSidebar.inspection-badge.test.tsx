import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { OperationsSidebar } from "@/features/operations/OperationsSidebar";

const listAwaitingInspection = vi.fn();
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

vi.mock("@/api/pos/pos-inventory-transfer-client", async () => {
  const actual = await vi.importActual<typeof import("@/api/pos/pos-inventory-transfer-client")>(
    "@/api/pos/pos-inventory-transfer-client",
  );
  return {
    ...actual,
    listInventoryTransfersAwaitingInspection: (...args: unknown[]) =>
      listAwaitingInspection(...args),
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
      branchType: "Warehouse",
      experience: "operations",
      organizationDisplayName: "Demo",
      branchName: "Panay",
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

function renderSidebar(path = "/warehouse") {
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

describe("OperationsSidebar Inspection activity badge", () => {
  beforeEach(() => {
    listAwaitingInspection.mockReset();
    listPurchaseOrders.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 40,
    });
    listIncomingOrders.mockResolvedValue([]);
  });

  it("shows CountBadge when awaiting inspection count > 0", async () => {
    listAwaitingInspection.mockResolvedValue({
      items: [
        { custodyId: "a" },
        { custodyId: "b" },
        { custodyId: "c" },
      ],
      totalCount: 3,
    });

    renderSidebar();

    const link = await screen.findByTestId("ops-sidebar-awaiting-inspection");
    await waitFor(() => {
      expect(screen.getByTestId("ops-sidebar-awaiting-inspection-badge")).toHaveTextContent("3");
    });
    expect(link).toHaveAttribute("aria-label", "org.nav.awaitingInspection, 3 items");
    expect(link).toHaveAttribute("href", "/inventory/awaiting-inspection");
  });

  it("hides badge when count is 0", async () => {
    listAwaitingInspection.mockResolvedValue({ items: [], totalCount: 0 });

    renderSidebar();

    await screen.findByTestId("ops-sidebar-awaiting-inspection");
    await waitFor(() => {
      expect(listAwaitingInspection).toHaveBeenCalled();
    });
    expect(screen.queryByTestId("ops-sidebar-awaiting-inspection-badge")).not.toBeInTheDocument();
  });

  it("hides badge while loading", () => {
    listAwaitingInspection.mockReturnValue(new Promise(() => undefined));

    renderSidebar();

    expect(screen.getByTestId("ops-sidebar-awaiting-inspection")).toBeInTheDocument();
    expect(screen.queryByTestId("ops-sidebar-awaiting-inspection-badge")).not.toBeInTheDocument();
  });
});
