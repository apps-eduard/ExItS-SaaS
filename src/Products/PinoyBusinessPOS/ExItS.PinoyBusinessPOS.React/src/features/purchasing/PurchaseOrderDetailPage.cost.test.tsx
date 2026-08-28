import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as poClient from "@/api/pos/pos-purchase-orders-client";
import { PurchaseOrderDetailPage } from "@/features/purchasing/PurchaseOrderDetailPage";
import { formatPeso } from "@/lib/format-money";

const purchaseOrderId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const workspace = {
  organizationId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
  branchId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
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
      id === "11111111-1111-4111-8111-111111111111"
        ? { actorId: id, displayName: "Maria Santos", actorStatus: "Active" }
        : id
          ? { actorId: id, displayName: "Juan Dela Cruz", actorStatus: "Active" }
          : null,
    isResolving: false,
    sortedIds: [],
    isLoading: false,
    isFetching: false,
    data: [],
  }),
}));

describe("PurchaseOrderDetailPage cost and receipt history", () => {
  beforeEach(() => {
    vi.spyOn(poClient, "getPurchaseOrder").mockResolvedValue({
      purchaseOrderId,
      organizationId: workspace.organizationId,
      poNumber: "PO-000102",
      supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      supplierName: "ABC Trading",
      status: "PartiallyReceived",
      orderDate: "2026-08-28",
      orderedAtUtc: "2026-08-28T08:00:00Z",
      orderedBy: "11111111-1111-4111-8111-111111111111",
      createdAtUtc: "2026-08-28T08:00:00Z",
      updatedAtUtc: "2026-08-28T12:00:00Z",
      paymentTerm: "Cash",
      paymentTermLabel: "Cash",
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
          receivedQty: 1,
          outstandingQty: 1,
          tracksExpiration: true,
        },
      ],
    } as never);

    vi.spyOn(poClient, "listGoodsReceiptsForPurchaseOrder").mockResolvedValue([
      {
        goodsReceiptId: "12121212-1212-4121-8121-121212121212",
        organizationId: workspace.organizationId,
        purchaseOrderId,
        supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        grnNumber: "GRN-000051",
        receivedDate: "2026-08-28",
        deliveryReference: "DR-9912",
        notes: null,
        receivedAtUtc: "2026-08-28T11:40:00Z",
        receivedBy: "22222222-2222-4222-8222-222222222222",
        lines: [
          {
            lineId: "33333333-3333-4333-8333-333333333333",
            purchaseOrderLineId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
            productId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
            lineNumber: 1,
            nameSnapshot: "Bath Soap",
            uomSnapshot: "Case",
            quantityReceived: 1,
            unitPurchaseCostSnapshot: 240,
            lineTotalSnapshot: 240,
            damagedQty: 0,
            rejectedQty: 0,
            shortClosedQty: 0,
            discrepancyKind: "None",
            discrepancyNote: null,
            expiryDate: "2027-12-30",
            lotNumber: "LOT-A123",
          },
        ],
      },
      {
        goodsReceiptId: "14141414-1414-4141-8141-141414141414",
        organizationId: workspace.organizationId,
        purchaseOrderId,
        supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        grnNumber: "GRN-000052",
        receivedDate: "2026-08-29",
        deliveryReference: null,
        notes: null,
        receivedAtUtc: "2026-08-29T09:00:00Z",
        receivedBy: "11111111-1111-4111-8111-111111111111",
        lines: [
          {
            lineId: "35353535-3535-4353-8353-353535353535",
            purchaseOrderLineId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
            productId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
            lineNumber: 1,
            nameSnapshot: "Bath Soap",
            uomSnapshot: "Case",
            quantityReceived: 1,
            unitPurchaseCostSnapshot: 240,
            lineTotalSnapshot: 240,
            damagedQty: 0,
            rejectedQty: 0,
            shortClosedQty: 1,
            discrepancyKind: "Short",
            discrepancyNote: "Supplier will replace next delivery",
            expiryDate: "2027-03-15",
            lotNumber: null,
          },
        ],
      },
    ] as never);
  });

  it("shows PO purchase-unit cost, order total, and separate receipt history cards", async () => {
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

    expect(screen.getByTestId("po-order-total")).toHaveTextContent(formatPeso(480));
    expect(screen.getAllByText(formatPeso(240)).length).toBeGreaterThanOrEqual(1);
    expect(screen.getByTestId("po-receipt-GRN-000051")).toBeInTheDocument();
    expect(screen.getByTestId("po-receipt-GRN-000052")).toBeInTheDocument();
    expect(screen.getByTestId("po-receipt-value-12121212-1212-4121-8121-121212121212")).toHaveTextContent(
      formatPeso(240),
    );
    expect(screen.getByText("DR-9912")).toBeInTheDocument();
    expect(screen.getByText("LOT-A123")).toBeInTheDocument();
    expect(screen.getByText(/Short:/)).toBeInTheDocument();
    expect(screen.getByText(/Supplier will replace next delivery/)).toBeInTheDocument();
    expect(screen.queryByText(/Damaged: 0/)).not.toBeInTheDocument();
    expect(screen.getByText("Juan Dela Cruz")).toBeInTheDocument();
    expect(screen.getAllByText("Maria Santos").length).toBeGreaterThanOrEqual(1);
  });
});
