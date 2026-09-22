import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import type { IncomingOrderBuyerReceipt } from "@/api/pos/pos-connected-suppliers-client";
import { IncomingOrderBuyerReceipts } from "@/features/purchasing/IncomingOrderBuyerReceipts";

const navigateWithReturn = vi.fn();

vi.mock("@/navigation/smart-back", () => ({
  navigateWithReturn: (...args: unknown[]) => navigateWithReturn(...args),
}));

const cpoId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const grn1 = "11111111-1111-4111-8111-111111111111";
const grn2 = "22222222-2222-4222-8222-222222222222";

function receipt(overrides: Partial<IncomingOrderBuyerReceipt> & Pick<IncomingOrderBuyerReceipt, "goodsReceiptId" | "grnNumber">): IncomingOrderBuyerReceipt {
  return {
    receivedDate: "2026-09-17",
    receivedAtUtc: "2026-09-17T12:00:00Z",
    deliveryReference: "DRV-9",
    notes: "Handle with care",
    status: "Posted",
    goodQtyTotal: 5,
    damagedQtyTotal: 1,
    missingQtyTotal: 0,
    cancelledRemainingTotal: 0,
    lines: [],
    ...overrides,
  };
}

function renderReceipts(receipts: IncomingOrderBuyerReceipt[]) {
  return render(
    <MemoryRouter>
      <IncomingOrderBuyerReceipts
        receipts={receipts}
        buyerName="Paul Store"
        buyerLabel="Buyer"
        connectedPurchaseOrderId={cpoId}
        latestTitle="Latest goods receipt"
        historyTitle="Receipt history"
        viewDetailsLabel="View receipt details"
        goodLabel="Good received"
        damagedLabel="Damaged"
        missingLabel="Missing / not delivered"
        deliveryRefLabel="Delivery reference"
        notesLabel="Notes"
        loadMoreLabel="Load more"
        postedLabel="Posted"
        voidedLabel="Voided"
        emptyLabel="No buyer goods receipts yet."
      />
    </MemoryRouter>,
  );
}

describe("IncomingOrderBuyerReceipts", () => {
  beforeEach(() => {
    navigateWithReturn.mockClear();
  });

  it("renders latest receipt summary and navigates with smart back", async () => {
    const user = userEvent.setup();
    renderReceipts([
      receipt({ goodsReceiptId: grn1, grnNumber: "GRN-001", goodQtyTotal: 5, damagedQtyTotal: 1 }),
    ]);

    expect(screen.getByTestId("incoming-order-latest-receipt")).toBeInTheDocument();
    expect(screen.getByTestId("incoming-order-latest-receipt-meta")).toBeInTheDocument();
    expect(screen.getByText(/GRN-001/)).toBeInTheDocument();
    expect(screen.getByText("Paul Store")).toBeInTheDocument();
    expect(screen.getByText("DRV-9")).toBeInTheDocument();
    expect(screen.getByText("Handle with care")).toBeInTheDocument();
    expect(screen.queryByTestId("incoming-order-receipt-history")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("incoming-order-view-latest-receipt"));
    expect(navigateWithReturn).toHaveBeenCalled();
    expect(String(navigateWithReturn.mock.calls[0]![1])).toContain(
      `/purchasing/incoming-orders/${cpoId}/receipts/${grn1}`,
    );
  });

  it("renders clickable history when multiple receipts exist", async () => {
    const user = userEvent.setup();
    renderReceipts([
      receipt({
        goodsReceiptId: grn2,
        grnNumber: "GRN-002",
        receivedAtUtc: "2026-09-17T14:00:00Z",
        goodQtyTotal: 2,
        damagedQtyTotal: 0,
      }),
      receipt({
        goodsReceiptId: grn1,
        grnNumber: "GRN-001",
        receivedAtUtc: "2026-09-17T12:00:00Z",
        goodQtyTotal: 5,
        damagedQtyTotal: 1,
      }),
    ]);

    expect(screen.getByTestId("incoming-order-receipt-history")).toBeInTheDocument();
    await user.click(screen.getByTestId(`incoming-order-receipt-row-${grn1}`));
    expect(String(navigateWithReturn.mock.calls[0]![1])).toContain(
      `/purchasing/incoming-orders/${cpoId}/receipts/${grn1}`,
    );
  });
});
