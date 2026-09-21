import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as poClient from "@/api/pos/pos-purchase-orders-client";
import { PurchaseOrderReceivePage } from "@/features/purchasing/PurchaseOrderReceivePage";

const purchaseOrderId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const productId = "ffffffff-ffff-4fff-8fff-ffffffffffff";
const productIdBanana = "99999999-9999-4999-8999-999999999999";
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

async function classifyLine(
  user: ReturnType<typeof userEvent.setup>,
  id: string,
  goodQty: string,
  note: string,
) {
  await user.click(screen.getByTestId(`receive-line-edit-menu-${id}`));
  const goodInput = await screen.findByTestId(`receive-good-${id}`);
  await user.clear(goodInput);
  await user.type(goodInput, goodQty);
  await user.click(screen.getByTestId(`receive-good-edit-save-${id}`));
  await waitFor(() => {
    expect(screen.getByTestId("receive-discrepancy-dialog")).toBeInTheDocument();
  });
  await user.click(screen.getByTestId(`receive-discrepancy-all-not-delivered-${id}`));
  await user.type(screen.getByTestId(`receive-discrepancy-remarks-${id}`), note);
  await user.click(screen.getByTestId("receive-discrepancy-confirm"));
  await waitFor(() => {
    expect(screen.queryByTestId("receive-discrepancy-dialog")).not.toBeInTheDocument();
  });
}

describe("PurchaseOrderReceivePage discrepancy classification", () => {
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

  it("skips discrepancy dialog when full outstanding is received", async () => {
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
    await waitFor(() => {
      expect(screen.queryByTestId("receive-discrepancy-dialog")).not.toBeInTheDocument();
      expect(screen.getByTestId("receive-confirm")).toBeInTheDocument();
      expect(screen.getByTestId("receive-review-summary")).toBeInTheDocument();
    });
  });

  it("saves classification without entering review, then reviews and posts", async () => {
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

    await classifyLine(user, productId, "4", "Short shipment");

    expect(screen.getByTestId("receive-lines-table")).toBeInTheDocument();
    expect(screen.queryByTestId("receive-review-summary")).not.toBeInTheDocument();
    expect(screen.getByTestId(`receive-discrepancy-summary-${productId}`)).toHaveTextContent(
      /not delivered/i,
    );
    expect(receiveSpy).not.toHaveBeenCalled();

    await user.click(screen.getByTestId("receive-review"));
    await waitFor(() => {
      expect(screen.getByTestId("receive-review-summary")).toBeInTheDocument();
      expect(screen.getByTestId("receive-remaining-decisions")).toBeInTheDocument();
    });
    const remaining = screen.getByTestId("receive-remaining-decisions");
    expect(within(remaining).getByRole("table")).toBeInTheDocument();
    expect(
      screen.getByTestId(`receive-remaining-decisions-choice-${productId}-option-replace_later`),
    ).toHaveAttribute("data-selected", "false");
    await user.click(
      screen.getByTestId(`receive-remaining-decisions-choice-${productId}-option-cancel_remaining`),
    );
    await user.click(screen.getByTestId("receive-confirm"));

    await waitFor(() => {
      expect(receiveSpy).toHaveBeenCalled();
    });
    expect(receiveSpy.mock.calls[0]?.[2]).toMatchObject({
      lines: [
        expect.objectContaining({
          productId,
          receiveQty: 4,
          damagedQty: 0,
          rejectedQty: 6,
          shortClosedQty: 6,
        }),
      ],
    });
  });

  it("blocks review while a discrepancy is unclassified", async () => {
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

    await user.click(screen.getByTestId(`receive-line-edit-menu-${productId}`));
    const goodInput = await screen.findByTestId(`receive-good-${productId}`);
    await user.clear(goodInput);
    await user.type(goodInput, "4");
    await user.click(screen.getByTestId(`receive-good-edit-save-${productId}`));
    await waitFor(() => {
      expect(screen.getByTestId("receive-discrepancy-dialog")).toBeInTheDocument();
    });
    await user.click(screen.getByTestId("receive-discrepancy-cancel"));
    await waitFor(() => {
      expect(screen.queryByTestId("receive-discrepancy-dialog")).not.toBeInTheDocument();
    });

    expect(screen.getByTestId(`receive-discrepancy-summary-${productId}`)).toHaveTextContent(
      /needs classification/i,
    );

    await user.click(screen.getByTestId("receive-review"));
    await waitFor(() => {
      expect(screen.getByText(/Classify all receipt discrepancies before reviewing/i)).toBeInTheDocument();
    });
    expect(screen.queryByTestId("receive-review-summary")).not.toBeInTheDocument();
    expect(screen.getByTestId("receive-lines-table")).toBeInTheDocument();
  });

  it("supports classifying one line then editing another", async () => {
    vi.spyOn(poClient, "getPurchaseOrder").mockResolvedValue({
      purchaseOrderId,
      organizationId: orgId,
      poNumber: "PO-000302",
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
          nameSnapshot: "Apple",
          uomSnapshot: "Kg",
          orderedQty: 1.5,
          unitPurchaseCost: 100,
          lineTotal: 150,
          receivedQty: 0,
          outstandingQty: 1.5,
          tracksExpiration: false,
          isInventoryTracked: true,
        },
        {
          lineId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaa01",
          productId: productIdBanana,
          lineNumber: 2,
          nameSnapshot: "Banana Lakatan",
          uomSnapshot: "Kg",
          orderedQty: 1,
          unitPurchaseCost: 50,
          lineTotal: 50,
          receivedQty: 0,
          outstandingQty: 1,
          tracksExpiration: false,
          isInventoryTracked: true,
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

    await classifyLine(user, productId, "0.5", "Apple short");
    expect(screen.getByTestId(`receive-line-edit-menu-${productIdBanana}`)).toBeEnabled();
    await user.click(screen.getByTestId(`receive-line-edit-menu-${productIdBanana}`));
    expect(await screen.findByTestId(`receive-good-${productIdBanana}`)).toBeInTheDocument();
  });

  it("keeps receive now read-only until pencil edit starts", async () => {
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

    expect(screen.queryByTestId(`receive-good-${productId}`)).not.toBeInTheDocument();
    expect(screen.getByTestId(`receive-line-edit-menu-${productId}`)).toBeInTheDocument();
  });
});
