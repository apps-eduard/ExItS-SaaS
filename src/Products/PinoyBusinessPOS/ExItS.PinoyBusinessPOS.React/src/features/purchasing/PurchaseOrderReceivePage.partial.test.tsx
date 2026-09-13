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

describe("PurchaseOrderReceivePage partial remaining decisions", () => {
  beforeEach(() => {
    vi.spyOn(poClient, "getPurchaseOrder").mockResolvedValue({
      purchaseOrderId,
      organizationId: orgId,
      poNumber: "PO-000301",
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
          orderedQty: 10,
          unitPurchaseCost: 100,
          lineTotal: 1000,
          receivedQty: 0,
          outstandingQty: 10,
          tracksExpiration: false,
          isInventoryTracked: true,
        },
      ],
    } as never);
  });

  it("shows remaining decisions on review and posts shortClosedQty when cancel selected", async () => {
    const receiveSpy = vi.spyOn(poClient, "receivePurchaseOrder").mockResolvedValue({
      goodsReceiptId: "12121212-1212-4121-8121-121212121212",
      organizationId: orgId,
      purchaseOrderId,
      supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      grnNumber: "GRN-000201",
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
          quantityReceived: 4,
          unitPurchaseCostSnapshot: 100,
          lineTotalSnapshot: 400,
          shortClosedQty: 6,
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

    const goodInput = screen.getByTestId(`receive-good-${productId}`);
    await user.clear(goodInput);
    await user.type(goodInput, "4");

    await user.click(screen.getByTestId("receive-review"));

    await waitFor(() => {
      expect(screen.getByTestId("receive-remaining-decisions")).toBeInTheDocument();
    });
    expect(screen.getByTestId(`receive-remaining-${productId}`)).toBeInTheDocument();
    expect(screen.getByText(/What should happen to the remaining 6 Case/)).toBeInTheDocument();
    expect(screen.getByTestId(`receive-deliver-later-${productId}`)).toBeChecked();

    await user.click(screen.getByTestId(`receive-cancel-remaining-${productId}`));
    await user.click(screen.getByTestId("receive-confirm"));

    await waitFor(() => {
      expect(receiveSpy).toHaveBeenCalled();
    });
    expect(receiveSpy.mock.calls[0]?.[2]).toMatchObject({
      lines: [
        expect.objectContaining({
          productId,
          receiveQty: 4,
          shortClosedQty: 6,
        }),
      ],
    });
  });
});
