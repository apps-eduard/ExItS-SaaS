import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as poClient from "@/api/pos/pos-purchase-orders-client";
import { PurchaseOrderDetailPage } from "@/features/purchasing/PurchaseOrderDetailPage";

const purchaseOrderId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const workspace = {
  organizationId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
  branchId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
  branchName: "Main",
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: workspace,
    sessionGrant: { capabilities: ["Purchasing.View", "Purchasing.Manage"] },
  }),
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
  subscribeBrowserOnline: (onChange: (online: boolean) => void) => {
    onChange(true);
    return () => undefined;
  },
}));

vi.mock("@/features/actors/useActorDirectory", () => ({
  useActorDirectory: () => ({
    resolve: (id?: string | null) =>
      id ? { actorId: id, displayName: "Maria Santos", actorStatus: "Active" } : null,
    isResolving: false,
    sortedIds: [],
    isLoading: false,
    isFetching: false,
    data: [],
  }),
}));

describe("PurchaseOrderDetailPage timeline drawer", () => {
  beforeEach(() => {
    vi.spyOn(poClient, "getPurchaseOrder").mockResolvedValue({
      purchaseOrderId,
      organizationId: workspace.organizationId,
      poNumber: "PO-1001",
      supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      supplierName: "Acme Supply",
      status: "Ordered",
      displayStatus: "Ordered",
      paymentTerm: "Cash",
      paymentTermLabel: "Cash",
      orderDate: "2026-08-27",
      orderedBy: "11111111-1111-4111-8111-111111111111",
      orderedAtUtc: "2026-08-27T09:00:00Z",
      createdAtUtc: "2026-08-27T08:00:00Z",
      updatedAtUtc: "2026-08-27T09:00:00Z",
      lines: [
        {
          lineId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          productId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
          lineNumber: 1,
          nameSnapshot: "Bath Soap",
          uomSnapshot: "Case",
          orderedQty: 2,
          unitPurchaseCost: 240,
          lineTotal: 480,
          receivedQty: 0,
          outstandingQty: 2,
        },
      ],
    } as never);
    vi.spyOn(poClient, "listGoodsReceiptsForPurchaseOrder").mockResolvedValue([]);
  });

  it("opens timeline in a drawer and closes without inline duplicate", async () => {
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/purchasing/orders/${purchaseOrderId}`]}>
          <Routes>
            <Route path="/purchasing/orders/:purchaseOrderId" element={<PurchaseOrderDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("po-timeline-open")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("po-activity-section")).not.toBeInTheDocument();
    expect(screen.queryByTestId("po-activity-timeline")).not.toBeInTheDocument();

    fireEvent.click(screen.getByTestId("po-timeline-open"));
    await waitFor(() => {
      expect(screen.getByTestId("po-timeline-drawer")).toBeInTheDocument();
      expect(screen.getByTestId("po-activity-timeline")).toBeInTheDocument();
    });

    const drawer = screen.getByTestId("po-timeline-drawer");
    expect(within(drawer).getByText("Purchase order timeline")).toBeInTheDocument();
    expect(within(drawer).getByText("PO-1001")).toBeInTheDocument();

    fireEvent.click(screen.getByTestId("po-timeline-drawer-close"));
    await waitFor(() => {
      expect(screen.queryByTestId("po-timeline-drawer")).not.toBeInTheDocument();
    });
    expect(screen.queryByTestId("po-activity-section")).not.toBeInTheDocument();
  });

  it("shows Cancelled with actor after local cancel fields are present", async () => {
    vi.spyOn(poClient, "getPurchaseOrder").mockResolvedValue({
      purchaseOrderId,
      organizationId: workspace.organizationId,
      poNumber: "PO-1001",
      supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      supplierName: "Acme Supply",
      status: "Cancelled",
      displayStatus: "Cancelled",
      paymentTerm: "Cash",
      paymentTermLabel: "Cash",
      orderDate: "2026-08-27",
      orderedBy: null,
      orderedAtUtc: null,
      cancelledAtUtc: "2026-08-27T10:08:00Z",
      cancelledByUserId: "11111111-1111-4111-8111-111111111111",
      createdAtUtc: "2026-08-27T08:00:00Z",
      updatedAtUtc: "2026-08-27T10:08:00Z",
      lines: [],
    } as never);

    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/purchasing/orders/${purchaseOrderId}`]}>
          <Routes>
            <Route path="/purchasing/orders/:purchaseOrderId" element={<PurchaseOrderDetailPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("po-timeline-open")).toBeInTheDocument();
    });
    fireEvent.click(screen.getByTestId("po-timeline-open"));

    const drawer = await screen.findByTestId("po-timeline-drawer");
    expect(within(drawer).getByText("Purchase order created")).toBeInTheDocument();
    expect(within(drawer).getByText("Cancelled")).toBeInTheDocument();
    expect(within(drawer).getByText("by")).toBeInTheDocument();
    expect(within(drawer).getByTestId("actor-attribution-name")).toHaveTextContent("Maria Santos");
    expect(within(drawer).queryByText("Withdrawn")).not.toBeInTheDocument();
  });
});
