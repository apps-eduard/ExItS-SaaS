import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import type { ConnectedPurchaseOrderLine } from "@/api/pos/pos-connected-suppliers-client";
import { IncomingOrderFulfillmentProgress } from "@/features/purchasing/IncomingOrderFulfillmentProgress";

const appleId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const bananaId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";

function line(overrides: Partial<ConnectedPurchaseOrderLine> & Pick<ConnectedPurchaseOrderLine, "productId" | "nameSnapshot">): ConnectedPurchaseOrderLine {
  return {
    skuSnapshot: null,
    qty: 4,
    unitPriceSnapshot: 10,
    lineTotal: 40,
    unitOfMeasureCode: "Piece",
    availability: "Available",
    proposedLineTotal: 40,
    confirmedLineTotal: 40,
    orderedQty: 4,
    goodReceivedQty: 0,
    damagedQty: 0,
    missingQty: 0,
    cancelledRemainingQty: 0,
    outstandingQty: 4,
    remainingValue: 40,
    ...overrides,
  };
}

describe("IncomingOrderFulfillmentProgress", () => {
  it("renders cumulative fulfillment columns with a separate unit column", () => {
    render(
      <IncomingOrderFulfillmentProgress
        lines={[
          line({
            productId: appleId,
            nameSnapshot: "Apple",
            goodReceivedQty: 3,
            damagedQty: 1,
            outstandingQty: 1,
            remainingValue: 10,
          }),
          line({
            productId: bananaId,
            nameSnapshot: "Banana",
            unitPriceSnapshot: 8,
            lineTotal: 32,
            goodReceivedQty: 2,
            damagedQty: 0,
            outstandingQty: 2,
            remainingValue: 16,
          }),
        ]}
        title="Fulfillment progress"
        productLabel="Product"
        orderedLabel="Ordered"
        goodLabel="Good received"
        damagedLabel="Damaged"
        missingLabel="Missing / not delivered"
        unitLabel="Unit"
        unitCostLabel="Unit cost"
        remainingValueLabel="Remaining value"
      />,
    );

    expect(screen.getByTestId("incoming-order-fulfillment-progress")).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Good received" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Damaged" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Unit" })).toBeInTheDocument();
    expect(screen.queryByRole("columnheader", { name: "Outstanding" })).not.toBeInTheDocument();
    expect(screen.queryByRole("columnheader", { name: "Missing / not delivered" })).not.toBeInTheDocument();
  });

  it("shows missing column when any line has missing qty", () => {
    render(
      <IncomingOrderFulfillmentProgress
        lines={[
          line({
            productId: appleId,
            nameSnapshot: "Apple",
            goodReceivedQty: 3,
            missingQty: 1,
            outstandingQty: 1,
            remainingValue: 10,
          }),
        ]}
        title="Fulfillment progress"
        productLabel="Product"
        orderedLabel="Ordered"
        goodLabel="Good received"
        damagedLabel="Damaged"
        missingLabel="Missing / not delivered"
        unitLabel="Unit"
        unitCostLabel="Unit cost"
        remainingValueLabel="Remaining value"
      />,
    );

    expect(screen.getByRole("columnheader", { name: "Missing / not delivered" })).toBeInTheDocument();
  });
});
