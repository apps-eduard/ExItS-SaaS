import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { PurchasingHubPage } from "@/features/purchasing/PurchasingHubPage";

const listPurchaseOrders = vi.fn();
const listIncomingOrders = vi.fn();
const listDirectPurchases = vi.fn();
const listSuppliers = vi.fn();

vi.mock("@/access/pos-capabilities", () => ({
  canViewPurchasing: () => true,
  canManagePurchasing: () => true,
  canViewInventory: () => true,
  canManageInventory: () => true,
  canViewSuppliers: () => true,
}));

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

vi.mock("@/api/pos/pos-direct-purchases-client", () => ({
  listDirectPurchases: (...args: unknown[]) => listDirectPurchases(...args),
}));

vi.mock("@/api/pos/pos-suppliers-client", () => ({
  listSuppliers: (...args: unknown[]) => listSuppliers(...args),
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      branchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    },
    sessionGrant: { capabilities: ["ViewPurchasing", "ManagePurchasing", "ViewInventory", "ManageInventory", "ViewSuppliers"] },
  }),
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

function renderPage() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={["/purchasing"]}>
        <Routes>
          <Route path="/purchasing" element={<PurchasingHubPage />} />
          <Route path="/purchasing/orders" element={<div>orders-page</div>} />
          <Route path="/purchasing/incoming-orders" element={<div>incoming-page</div>} />
          <Route path="/purchasing/receipts" element={<div>receipts-page</div>} />
          <Route path="/purchasing/direct-purchases" element={<div>direct-page</div>} />
          <Route path="/suppliers" element={<div>suppliers-page</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("PurchasingHubPage browse tabs", () => {
  beforeEach(() => {
    listPurchaseOrders.mockReset();
    listIncomingOrders.mockReset();
    listDirectPurchases.mockReset();
    listSuppliers.mockReset();

    listPurchaseOrders.mockResolvedValue({
      items: [
        {
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
        },
      ],
      totalCount: 12,
      page: 1,
      pageSize: 40,
    });
    listIncomingOrders.mockResolvedValue([{ connectedPurchaseOrderId: "a" }, { connectedPurchaseOrderId: "b" }]);
    listDirectPurchases.mockResolvedValue({ items: [], totalCount: 4, page: 1, pageSize: 1 });
    listSuppliers.mockResolvedValue({ items: [], totalCount: 9, page: 1, pageSize: 1 });
  });

  function stubMatchMedia(matchesMin768: boolean) {
    Object.defineProperty(window, "matchMedia", {
      writable: true,
      value: (query: string) => ({
        matches: matchesMin768 && query.includes("min-width: 768px"),
        media: query,
        onchange: null,
        addListener: () => undefined,
        removeListener: () => undefined,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        dispatchEvent: () => false,
      }),
    });
  }

  it("uses mobile pill bar: continuous outer, solid active, scrollable single row", async () => {
    stubMatchMedia(false);
    renderPage();

    const toolbar = await screen.findByTestId("purchasing-toolbar-list");
    expect(toolbar).toHaveAttribute("data-variant", "pillBar");
    expect(toolbar).toHaveAttribute("data-active-treatment", "solid");
    expect(toolbar).toHaveAttribute("data-layout", "content");
    expect(toolbar.className).toMatch(/overflow-x-auto/);
    expect(toolbar.className).toMatch(/flex-nowrap/);
    expect(toolbar.className).toMatch(/rounded-full/);
    expect(toolbar.className).toMatch(/shadow-\[var\(--exits-shadow-sm\)\]/);
    expect(toolbar.className).toMatch(/bg-\[var\(--exits-surface\)\]/);
    expect(toolbar.className).toMatch(/exits-tabs__trigger\]:text-\[var\(--exits-text\)\]/);

    expect(screen.getByTestId("purchasing-orders")).toBeInTheDocument();
    expect(screen.getByTestId("purchasing-incoming-orders")).toBeInTheDocument();

    await waitFor(() => {
      expect(within(screen.getByTestId("purchasing-orders")).getByText("12")).toBeInTheDocument();
      expect(within(screen.getByTestId("purchasing-incoming-orders")).getByText("2")).toBeInTheDocument();
      expect(within(screen.getByTestId("purchasing-receipts")).getByText("1")).toBeInTheDocument();
    });
  });

  it("keeps soft tabs on wider viewports", async () => {
    stubMatchMedia(true);
    renderPage();

    const toolbar = await screen.findByTestId("purchasing-toolbar-list");
    expect(toolbar).toHaveAttribute("data-variant", "soft");
    expect(toolbar.className).toMatch(/overflow-x-auto/);
  });

  it("navigates from tab selection while preserving destinations", async () => {
    stubMatchMedia(false);
    const user = userEvent.setup();
    renderPage();

    await screen.findByTestId("purchasing-orders");
    await user.click(screen.getByTestId("purchasing-incoming-orders"));
    expect(await screen.findByText("incoming-page")).toBeInTheDocument();
  });
});
