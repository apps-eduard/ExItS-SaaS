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

describe("PurchaseOrderReceivePage payment context", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("shows prepaid GCash reference read-only and does not require a new reference", async () => {
    const user = userEvent.setup();
    vi.spyOn(poClient, "getPurchaseOrder").mockResolvedValue({
      purchaseOrderId,
      organizationId: orgId,
      poNumber: "PO-PREPAY-1",
      supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      supplierName: "ABC Trading",
      status: "PartiallyReceived",
      displayStatus: "Ready to receive",
      orderDate: "2026-09-04",
      createdAtUtc: "2026-09-04T08:00:00Z",
      updatedAtUtc: "2026-09-18T08:00:00Z",
      paymentTerm: "ManualGCash",
      paymentTermLabel: "GCash",
      paymentTiming: "PayBeforeFulfillment",
      paymentTimingLabel: "Pay in advance",
      confirmedTotalAmount: 850,
      amountPaidSnapshot: 850,
      financialSettlementStatus: "Settled",
      financiallySettledAtUtc: "2026-09-17T10:00:00Z",
      buyerPrepaymentReference: "GC-123456",
      buyerPrepaymentMethod: "ManualGCash",
      supplierFulfilledAtUtc: "2026-09-18T09:00:00Z",
      canReceiveConnected: true,
      lines: [
        {
          lineId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          productId,
          lineNumber: 1,
          nameSnapshot: "Apple",
          skuSnapshot: "PH-FRU-APPLE",
          uomSnapshot: "Kilogram",
          orderedQty: 2,
          unitPurchaseCost: 200,
          lineTotal: 400,
          receivedQty: 0,
          outstandingQty: 2,
          tracksExpiration: false,
          isInventoryTracked: true,
        },
        {
          lineId: "11111111-1111-4111-8111-111111111111",
          productId: "22222222-2222-4222-8222-222222222222",
          lineNumber: 2,
          nameSnapshot: "Battery AA Pack",
          skuSnapshot: "PH-GEN-BATTERY-AA",
          uomSnapshot: "Pack",
          orderedQty: 2,
          unitPurchaseCost: 65,
          lineTotal: 130,
          receivedQty: 0,
          outstandingQty: 2,
          tracksExpiration: false,
          isInventoryTracked: true,
        },
      ],
    } as never);

    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/purchasing/${purchaseOrderId}/receive`]}>
          <Routes>
            <Route path="/purchasing/:purchaseOrderId/receive" element={<PurchaseOrderReceivePage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("receive-lines-table")).toBeInTheDocument();
    });

    expect(screen.getByText("SKU")).toBeInTheDocument();
    expect(screen.getByText("PH-FRU-APPLE")).toBeInTheDocument();
    expect(screen.getByTestId(`receive-line-total-${productId}`)).toHaveTextContent("400.00");
    expect(screen.getByTestId("receive-order-value")).toHaveTextContent("850.00");
    expect(screen.getByTestId("receive-this-receipt-value")).toHaveTextContent("530.00");

    expect(screen.getByTestId("receive-payment-section")).toHaveTextContent("Prepayment");
    expect(screen.getByTestId("receive-payment-prepaid-reference")).toHaveTextContent("GC-123456");
    expect(screen.queryByTestId("receive-payment-gcash-ref")).not.toBeInTheDocument();
    expect(screen.getByTestId("receive-context-payment-status")).toHaveTextContent("Paid / Settled");

    await user.click(screen.getByTestId("receive-review"));
    await waitFor(() => {
      expect(screen.getByTestId("receive-review-summary")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("receive-error")).not.toBeInTheDocument();
    expect(screen.queryByTestId("receive-payment-gcash-ref")).not.toBeInTheDocument();
  });

  it("does not show GCash reference for PayOnDelivery Cash", async () => {
    vi.spyOn(poClient, "getPurchaseOrder").mockResolvedValue({
      purchaseOrderId,
      organizationId: orgId,
      poNumber: "PO-COD-1",
      supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      status: "Ordered",
      orderDate: "2026-09-04",
      createdAtUtc: "2026-09-04T08:00:00Z",
      updatedAtUtc: "2026-09-04T08:00:00Z",
      paymentTerm: "Cash",
      paymentTermLabel: "Cash",
      paymentTiming: "PayOnDeliveryOrReceipt",
      paymentTimingLabel: "Pay on delivery",
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

    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/purchasing/${purchaseOrderId}/receive`]}>
          <Routes>
            <Route path="/purchasing/:purchaseOrderId/receive" element={<PurchaseOrderReceivePage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("receive-payment-section")).toBeInTheDocument();
    });
    expect(screen.getByTestId("receive-payment-section")).toHaveTextContent("Payment at receipt");
    expect(screen.queryByTestId("receive-payment-gcash-ref")).not.toBeInTheDocument();
  });

  it("shows supplier credit read-only with no settlement inputs", async () => {
    vi.spyOn(poClient, "getPurchaseOrder").mockResolvedValue({
      purchaseOrderId,
      organizationId: orgId,
      poNumber: "PO-UTANG-1",
      supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      status: "Ordered",
      orderDate: "2026-09-04",
      createdAtUtc: "2026-09-04T08:00:00Z",
      updatedAtUtc: "2026-09-04T08:00:00Z",
      paymentTerm: "Utang",
      paymentTermLabel: "Utang / Credit",
      paymentTiming: "SupplierCredit",
      paymentTimingLabel: "Supplier Utang",
      lines: [
        {
          lineId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
          productId,
          lineNumber: 1,
          nameSnapshot: "Bath Soap",
          uomSnapshot: "Case",
          orderedQty: 5,
          unitPurchaseCost: 100,
          lineTotal: 500,
          receivedQty: 0,
          outstandingQty: 5,
          tracksExpiration: false,
          isInventoryTracked: true,
        },
      ],
    } as never);

    render(
      <AppProviders>
        <MemoryRouter initialEntries={[`/purchasing/${purchaseOrderId}/receive`]}>
          <Routes>
            <Route path="/purchasing/:purchaseOrderId/receive" element={<PurchaseOrderReceivePage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("receive-payment-supplier-credit")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("receive-payment-gcash-ref")).not.toBeInTheDocument();
    expect(screen.queryByTestId("receive-payment-check-number")).not.toBeInTheDocument();
  });
});
