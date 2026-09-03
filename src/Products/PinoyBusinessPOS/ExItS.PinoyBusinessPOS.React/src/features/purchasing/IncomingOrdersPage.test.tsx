import { beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { catalogs } from "@/i18n/messages";
import { IncomingOrderDetailPage } from "@/features/purchasing/IncomingOrderDetailPage";
import { IncomingOrdersListPage } from "@/features/purchasing/IncomingOrdersListPage";

const orgId = "22222222-2222-4222-8222-222222222222";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const cpoId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const productId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    organizationDisplayName: "Mica Store",
    branchId,
    branchName: "Iloilo",
    experience: "operations" as const,
  },
  sessionGrant: {
    productAccessAllowed: true,
    membershipRole: "OrganizationOwner",
    productLocalRoleCode: "Owner",
    mappedPosRoleCode: "Owner",
  } as Record<string, unknown>,
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceMock,
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: keyof typeof catalogs.en) => catalogs.en[key] ?? String(key),
  }),
}));

const listIncomingOrders = vi.fn();
const getIncomingOrder = vi.fn();
const acceptIncomingOrder = vi.fn();
const declineIncomingOrder = vi.fn();
const prepareIncomingOrder = vi.fn();
const fulfillIncomingOrder = vi.fn();

vi.mock("@/api/pos/pos-connected-suppliers-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-connected-suppliers-client")>();
  return {
    ...actual,
    listIncomingOrders: (...args: unknown[]) => listIncomingOrders(...args),
    getIncomingOrder: (...args: unknown[]) => getIncomingOrder(...args),
    acceptIncomingOrder: (...args: unknown[]) => acceptIncomingOrder(...args),
    declineIncomingOrder: (...args: unknown[]) => declineIncomingOrder(...args),
    prepareIncomingOrder: (...args: unknown[]) => prepareIncomingOrder(...args),
    fulfillIncomingOrder: (...args: unknown[]) => fulfillIncomingOrder(...args),
  };
});

function pendingOrder(status = "New") {
  return {
    connectedPurchaseOrderId: cpoId,
    relationshipId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
    buyerOrganizationId: "11111111-1111-4111-8111-111111111111",
    supplierOrganizationId: orgId,
    buyerPurchaseOrderId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
    buyerPoNumber: "PO-000123",
    orderDate: "2026-09-04",
    notes: null,
    status,
    totalAmount: 240,
    createdAtUtc: "2026-09-04T00:00:00Z",
    updatedAtUtc: "2026-09-04T00:00:00Z",
    lines: [
      {
        productId,
        nameSnapshot: "Bottled Water 500ml",
        skuSnapshot: "PH-BEV-WATER-500",
        qty: 20,
        unitPriceSnapshot: 12,
        lineTotal: 240,
        unitOfMeasureCode: "Piece",
      },
    ],
    displayStatus: status,
    buyerDisplayName: "Paul Store",
    supplierBranchName: "Iloilo",
    paymentTerm: "Cash",
    paymentTermLabel: "Cash",
  };
}

function renderList() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, networkMode: "always" } },
  });
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={["/purchasing/incoming-orders"]}>
        <Routes>
          <Route path="/purchasing/incoming-orders" element={<IncomingOrdersListPage />} />
          <Route
            path="/purchasing/incoming-orders/:connectedPurchaseOrderId"
            element={<IncomingOrderDetailPage />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

function renderDetail() {
  const client = new QueryClient({
    defaultOptions: {
      queries: { retry: false, networkMode: "always" },
      mutations: { networkMode: "always" },
    },
  });
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[`/purchasing/incoming-orders/${cpoId}`]}>
        <Routes>
          <Route
            path="/purchasing/incoming-orders/:connectedPurchaseOrderId"
            element={<IncomingOrderDetailPage />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("IncomingOrders React flow", () => {
  beforeEach(() => {
    listIncomingOrders.mockReset();
    getIncomingOrder.mockReset();
    acceptIncomingOrder.mockReset();
    declineIncomingOrder.mockReset();
    prepareIncomingOrder.mockReset();
    fulfillIncomingOrder.mockReset();
    listIncomingOrders.mockResolvedValue([pendingOrder()]);
    getIncomingOrder.mockResolvedValue(pendingOrder());
    acceptIncomingOrder.mockResolvedValue(pendingOrder("Accepted"));
    declineIncomingOrder.mockResolvedValue(pendingOrder("Declined"));
    prepareIncomingOrder.mockResolvedValue(pendingOrder("Preparing"));
    fulfillIncomingOrder.mockResolvedValue(pendingOrder("Fulfilled"));
  });

  it("lists pending incoming POs separately from connection requests", async () => {
    renderList();
    await waitFor(() => expect(screen.getByTestId(`incoming-order-row-${cpoId}`)).toBeInTheDocument());
    expect(listIncomingOrders).toHaveBeenCalledWith(
      expect.objectContaining({ organizationId: orgId }),
      { status: "New" },
      expect.anything(),
    );
    expect(screen.getByText("PO-000123")).toBeInTheDocument();
    expect(screen.getByText(/Paul Store/)).toBeInTheDocument();
    expect(screen.queryByText("Incoming connection requests")).not.toBeInTheDocument();
    expect(screen.getByTestId("incoming-orders-filter-pending")).toHaveAttribute("aria-selected", "true");
  });

  it("accepts a pending order and hides accept/decline", async () => {
    const user = userEvent.setup();
    getIncomingOrder
      .mockResolvedValueOnce(pendingOrder("New"))
      .mockResolvedValueOnce(pendingOrder("Accepted"));
    renderDetail();
    await waitFor(() => screen.getByTestId("incoming-order-accept"));
    expect(screen.getByText("20 × ₱12 = ₱240")).toBeInTheDocument();
    expect(screen.getByTestId("incoming-order-total-amount")).toHaveTextContent("₱240.00");

    await user.click(screen.getByTestId("incoming-order-accept"));
    await waitFor(() => expect(acceptIncomingOrder).toHaveBeenCalled());
    await waitFor(() => expect(screen.queryByTestId("incoming-order-accept")).not.toBeInTheDocument());
    expect(screen.getByTestId("incoming-order-prepare")).toBeInTheDocument();
    expect(acceptIncomingOrder.mock.calls[0]![1]).toBe(cpoId);
  });

  it("declines with optional reason", async () => {
    const user = userEvent.setup();
    getIncomingOrder
      .mockResolvedValueOnce(pendingOrder("New"))
      .mockResolvedValueOnce(pendingOrder("Declined"));
    renderDetail();
    await waitFor(() => screen.getByTestId("incoming-order-decline"));
    await user.click(screen.getByTestId("incoming-order-decline"));
    await user.selectOptions(screen.getByTestId("incoming-order-decline-reason"), "OutOfStock");
    await user.click(screen.getByTestId("incoming-order-decline-confirm"));
    await waitFor(() => expect(declineIncomingOrder).toHaveBeenCalled());
    expect(declineIncomingOrder.mock.calls[0]![2]).toEqual({
      declineReason: "OutOfStock",
      declineNote: null,
    });
    await waitFor(() => expect(screen.queryByTestId("incoming-order-decline")).not.toBeInTheDocument());
  });

  it("supports prepare then fulfill", async () => {
    const user = userEvent.setup();
    getIncomingOrder
      .mockResolvedValueOnce(pendingOrder("Accepted"))
      .mockResolvedValueOnce(pendingOrder("Preparing"))
      .mockResolvedValueOnce(pendingOrder("Fulfilled"));
    renderDetail();
    await waitFor(() => screen.getByTestId("incoming-order-prepare"));
    await user.click(screen.getByTestId("incoming-order-prepare"));
    await waitFor(() => screen.getByTestId("incoming-order-fulfill"));
    await user.click(screen.getByTestId("incoming-order-fulfill"));
    await waitFor(() => expect(fulfillIncomingOrder).toHaveBeenCalled());
    await waitFor(() => expect(screen.queryByTestId("incoming-order-fulfill")).not.toBeInTheDocument());
  });
});
