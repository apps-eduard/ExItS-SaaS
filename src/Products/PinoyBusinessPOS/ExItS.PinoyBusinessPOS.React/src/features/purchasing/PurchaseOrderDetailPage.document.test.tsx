import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
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
    resolve: () => null,
    isResolving: false,
    sortedIds: [],
    isLoading: false,
    isFetching: false,
    data: [],
  }),
}));

describe("PurchaseOrderDetailPage business document", () => {
  beforeEach(() => {
    vi.spyOn(window, "print").mockImplementation(() => undefined);
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

  it("keeps operational detail without inline A4 document preview", async () => {
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
      expect(screen.getByTestId("purchase-order-detail-page")).toBeInTheDocument();
    });

    expect(screen.queryByTestId("po-printable-document")).not.toBeInTheDocument();
    expect(screen.queryByTestId("po-document-preview")).not.toBeInTheDocument();
    expect(screen.getByTestId("po-print-host")).toBeInTheDocument();
    expect(within(screen.getByTestId("po-print-host")).getByTestId("po-business-document")).toBeInTheDocument();
    expect(screen.getByTestId("po-lines-table")).toBeInTheDocument();
  });

  it("opens canonical PO document in Preview and supports Print/PDF", async () => {
    const user = userEvent.setup();
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
      expect(screen.getByTestId("po-business-document-actions-preview")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("po-business-document-actions-preview"));

    const preview = await screen.findByTestId("po-document-preview");
    expect(within(preview).getByTestId("po-business-document")).toBeInTheDocument();
    expect(within(preview).getByTestId("business-document-title")).toBeInTheDocument();
    expect(screen.queryByTestId("po-print-host")).not.toBeInTheDocument();

    await user.click(within(preview).getByTestId("po-document-preview-print"));
    expect(window.print).toHaveBeenCalled();

    await user.click(within(preview).getByTestId("po-document-preview-pdf"));
    expect(window.print).toHaveBeenCalledTimes(2);
  });

  it("prints canonical document from header Print without opening Preview", async () => {
    const user = userEvent.setup();
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
      expect(screen.getByTestId("po-business-document-actions-print")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("po-business-document-actions-print"));
    expect(window.print).toHaveBeenCalled();
    expect(screen.getByTestId("po-print-host")).toBeInTheDocument();
    expect(screen.getAllByTestId("po-business-document")).toHaveLength(1);
  });
});
