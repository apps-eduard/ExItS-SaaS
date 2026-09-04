import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as poClient from "@/api/pos/pos-purchase-orders-client";
import { PurchaseOrderReceivePage } from "@/features/purchasing/PurchaseOrderReceivePage";

const purchaseOrderId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const productId = "ffffffff-ffff-4fff-8fff-ffffffffffff";
const orgId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const branchId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: orgId,
      organizationDisplayName: "Test Org",
      branchId,
      branchName: "Main Branch",
      experience: "operations" as const,
    },
    sessionGrant: {
      productAccessAllowed: true,
      membershipRole: "OrganizationOwner",
      productLocalRoleCode: "Owner",
      mappedPosRoleCode: "Owner",
    },
  }),
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
  subscribeBrowserOnline: (onChange: (online: boolean) => void) => {
    onChange(true);
    return () => undefined;
  },
}));

describe("PurchaseOrderReceivePage tracking confirmation", () => {
  beforeEach(() => {
    vi.spyOn(poClient, "getPurchaseOrder").mockResolvedValue({
      purchaseOrderId,
      organizationId: orgId,
      poNumber: "PO-000201",
      supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      supplierName: "ABC Trading",
      status: "Ordered",
      orderDate: "2026-09-04",
      createdAtUtc: "2026-09-04T08:00:00Z",
      updatedAtUtc: "2026-09-04T08:00:00Z",
      paymentTerm: "Cash",
      paymentTermLabel: "Cash",
      lines: [
        {
          lineId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          productId,
          lineNumber: 1,
          nameSnapshot: "Bath Soap",
          uomSnapshot: "Case",
          orderedQty: 2,
          unitPurchaseCost: 100,
          lineTotal: 200,
          receivedQty: 0,
          outstandingQty: 2,
          tracksExpiration: false,
          isInventoryTracked: false,
        },
      ],
    } as never);
  });

  it("shows tracking confirmation copy and posts enableTrackingIfNeeded", async () => {
    const receiveSpy = vi.spyOn(poClient, "receivePurchaseOrder").mockResolvedValue({
      goodsReceiptId: "12121212-1212-4121-8121-121212121212",
      organizationId: orgId,
      purchaseOrderId,
      supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      grnNumber: "GRN-000101",
      receivedDate: "2026-09-04",
      deliveryReference: null,
      notes: null,
      receivedAtUtc: "2026-09-04T10:00:00Z",
      receivedBy: "11111111-1111-4111-8111-111111111111",
      lines: [
        {
          lineId: "33333333-3333-4333-8333-333333333333",
          purchaseOrderLineId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          productId,
          lineNumber: 1,
          nameSnapshot: "Bath Soap",
          uomSnapshot: "Case",
          quantityReceived: 2,
          unitPurchaseCostSnapshot: 100,
          lineTotalSnapshot: 200,
          inventoryTrackingEnabled: true,
          previousTrackedStock: null,
          newTrackedStock: 2,
        },
      ],
    } as never);

    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/purchasing/${purchaseOrderId}/receive`]}>
          <Routes>
            <Route
              path="/purchasing/:purchaseOrderId/receive"
              element={<PurchaseOrderReceivePage />}
            />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("purchase-order-receive-page")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("receive-review"));
    await user.click(screen.getByTestId("receive-confirm"));

    await waitFor(() => {
      expect(screen.getByTestId("receive-tracking-confirm")).toBeInTheDocument();
    });
    expect(screen.getByText("Inventory is not currently tracked.")).toBeInTheDocument();
    expect(screen.getByText("Enable inventory tracking")).toBeInTheDocument();
    expect(
      screen.getByText("Tracking starts with the quantity received in this receipt."),
    ).toBeInTheDocument();
    expect(screen.getByText(/Add stock to Main Branch/)).toBeInTheDocument();

    await user.click(screen.getByTestId("receive-confirm"));

    await waitFor(() => {
      expect(receiveSpy).toHaveBeenCalled();
    });
    expect(receiveSpy.mock.calls[0]?.[2]).toMatchObject({
      enableTrackingIfNeeded: true,
    });

    await waitFor(() => {
      expect(screen.getByTestId("receive-completed-panel")).toBeInTheDocument();
    });
    expect(screen.getByText("Inventory tracking enabled")).toBeInTheDocument();
    expect(screen.getByText("Receipt completed")).toBeInTheDocument();
  });
});
