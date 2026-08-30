import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { PosApiError } from "@/api/pos/pos-http";
import * as poClient from "@/api/pos/pos-purchase-orders-client";
import { PurchaseOrderDetailPage } from "@/features/purchasing/PurchaseOrderDetailPage";

const purchaseOrderId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const goodsReceiptId = "12121212-1212-4121-8121-121212121212";
const workspace = {
  organizationId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
  branchId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
};

const ownerGrant = {
  productAccessAllowed: true,
  mappedPosRoleCode: "Owner",
  productLocalRoleCode: "Owner",
  membershipRole: "OrganizationOwner",
  organizationManagementAuthority: true,
};

let sessionGrant: typeof ownerGrant = ownerGrant;

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: workspace,
    get sessionGrant() {
      return sessionGrant;
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

function postedReceipt(
  overrides: Partial<poClient.PosGoodsReceiptDto> = {},
): poClient.PosGoodsReceiptDto {
  return {
    goodsReceiptId,
    organizationId: workspace.organizationId,
    purchaseOrderId,
    supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
    grnNumber: "GRN-000051",
    receivedDate: "2026-08-28",
    deliveryReference: "DR-9912",
    notes: null,
    receivedAtUtc: "2026-08-28T11:40:00Z",
    receivedBy: "22222222-2222-4222-8222-222222222222",
    status: "Posted",
    voidedAtUtc: null,
    voidedByUserId: null,
    voidReason: null,
    lines: [
      {
        lineId: "33333333-3333-4333-8333-333333333333",
        purchaseOrderLineId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        productId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
        lineNumber: 1,
        nameSnapshot: "Bath Soap",
        uomSnapshot: "Case",
        quantityReceived: 2,
        unitPurchaseCostSnapshot: 240,
        lineTotalSnapshot: 480,
      },
    ],
    ...overrides,
  };
}

function basePo(): poClient.PosPurchaseOrderDto {
  return {
    purchaseOrderId,
    organizationId: workspace.organizationId,
    supplierId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
    supplierName: "ABC Trading",
    poNumber: "PO-0001",
    status: "Received",
    displayStatus: "Received",
    orderDate: "2026-08-27",
    orderedBy: "11111111-1111-4111-8111-111111111111",
    createdAtUtc: "2026-08-27T08:00:00Z",
    updatedAtUtc: "2026-08-28T11:40:00Z",
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
        receivedQty: 2,
        outstandingQty: 0,
      },
    ],
  } as poClient.PosPurchaseOrderDto;
}

function renderPage() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[`/purchasing/orders/${purchaseOrderId}`]}>
        <Routes>
          <Route path="/purchasing/orders/:purchaseOrderId" element={<PurchaseOrderDetailPage />} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("PurchaseOrderDetailPage receipt reversal", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    sessionGrant = ownerGrant;
    vi.spyOn(poClient, "getPurchaseOrder").mockResolvedValue(basePo());
    vi.spyOn(poClient, "listGoodsReceiptsForPurchaseOrder").mockResolvedValue([postedReceipt()]);
  });

  it("shows reverse action, confirms with reason, and refreshes voided state", async () => {
    const user = userEvent.setup();
    const voided = postedReceipt({
      status: "Voided",
      voidedAtUtc: "2026-08-30T10:00:00Z",
      voidedByUserId: "11111111-1111-4111-8111-111111111111",
      voidReason: "Wrong delivery",
    });
    const voidSpy = vi.spyOn(poClient, "voidGoodsReceipt").mockImplementation(async () => {
      vi.spyOn(poClient, "listGoodsReceiptsForPurchaseOrder").mockResolvedValue([voided]);
      vi.spyOn(poClient, "getPurchaseOrder").mockResolvedValue({
        ...basePo(),
        status: "Ordered",
        displayStatus: "Ordered",
        lines: [
          {
            ...basePo().lines[0]!,
            receivedQty: 0,
            outstandingQty: 2,
          },
        ],
      } as poClient.PosPurchaseOrderDto);
      return voided;
    });

    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId(`po-receipt-reverse-${goodsReceiptId}`)).toBeInTheDocument();
    });

    await user.click(screen.getByTestId(`po-receipt-reverse-${goodsReceiptId}`));
    expect(screen.getByTestId(`po-receipt-reverse-dialog-${goodsReceiptId}`)).toBeInTheDocument();
    expect(screen.getByText(/removes the received stock/i)).toBeInTheDocument();

    await user.type(
      screen.getByTestId(`po-receipt-reverse-reason-${goodsReceiptId}`),
      "Wrong delivery",
    );
    await user.click(screen.getByTestId(`po-receipt-reverse-confirm-${goodsReceiptId}`));

    await waitFor(() => {
      expect(voidSpy).toHaveBeenCalledWith(
        workspace,
        goodsReceiptId,
        expect.objectContaining({ reason: "Wrong delivery" }),
      );
    });

    await waitFor(() => {
      expect(screen.getByText("Reversed")).toBeInTheDocument();
      expect(screen.queryByTestId(`po-receipt-reverse-${goodsReceiptId}`)).not.toBeInTheDocument();
    });
  });

  it("hides reverse action for reporting-only users", async () => {
    sessionGrant = {
      productAccessAllowed: true,
      mappedPosRoleCode: "ReportingUser",
      productLocalRoleCode: "ReportingUser",
      membershipRole: "Member",
      organizationManagementAuthority: false,
    };

    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId("po-receipt-GRN-000051")).toBeInTheDocument();
    });
    expect(screen.queryByTestId(`po-receipt-reverse-${goodsReceiptId}`)).not.toBeInTheDocument();
  });

  it("disables reverse when already voided", async () => {
    vi.spyOn(poClient, "listGoodsReceiptsForPurchaseOrder").mockResolvedValue([
      postedReceipt({
        status: "Voided",
        voidedAtUtc: "2026-08-30T10:00:00Z",
        voidedByUserId: "11111111-1111-4111-8111-111111111111",
        voidReason: "Already reversed",
      }),
    ]);

    renderPage();

    await waitFor(() => {
      expect(screen.getByText("Reversed")).toBeInTheDocument();
    });
    expect(screen.queryByTestId(`po-receipt-reverse-${goodsReceiptId}`)).not.toBeInTheDocument();
    expect(screen.getByTestId(`po-receipt-void-reason-${goodsReceiptId}`)).toHaveTextContent(
      "Already reversed",
    );
  });

  it("shows insufficient-stock error from API", async () => {
    const user = userEvent.setup();
    vi.spyOn(poClient, "voidGoodsReceipt").mockRejectedValue(
      new PosApiError(409, {
        title: "Conflict",
        status: 409,
        detail: "Cannot reverse: stock from this receipt is no longer available.",
        errorCode: "pos.goods_receipt.void.insufficient_stock",
      }),
    );

    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId(`po-receipt-reverse-${goodsReceiptId}`)).toBeInTheDocument();
    });
    await user.click(screen.getByTestId(`po-receipt-reverse-${goodsReceiptId}`));
    await user.type(
      screen.getByTestId(`po-receipt-reverse-reason-${goodsReceiptId}`),
      "Try anyway",
    );
    await user.click(screen.getByTestId(`po-receipt-reverse-confirm-${goodsReceiptId}`));

    await waitFor(() => {
      expect(
        screen.getByText(/stock from this receipt is no longer available/i),
      ).toBeInTheDocument();
    });
  });

  it("shows friendly message when reverse is blocked by supplier payments", async () => {
    const user = userEvent.setup();
    vi.spyOn(poClient, "voidGoodsReceipt").mockRejectedValue(
      new PosApiError(409, {
        title: "Conflict",
        status: 409,
        detail: "raw code detail",
        errorCode: "pos.supplier_payable.void.blocked_by_payments",
      }),
    );

    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId(`po-receipt-reverse-${goodsReceiptId}`)).toBeInTheDocument();
    });
    await user.click(screen.getByTestId(`po-receipt-reverse-${goodsReceiptId}`));
    await user.type(
      screen.getByTestId(`po-receipt-reverse-reason-${goodsReceiptId}`),
      "Undo",
    );
    await user.click(screen.getByTestId(`po-receipt-reverse-confirm-${goodsReceiptId}`));

    await waitFor(() => {
      expect(
        screen.getByText(
          /cannot be reversed because supplier payments have already been recorded/i,
        ),
      ).toBeInTheDocument();
    });
    expect(screen.queryByText(/raw code detail/i)).not.toBeInTheDocument();
  });
});
