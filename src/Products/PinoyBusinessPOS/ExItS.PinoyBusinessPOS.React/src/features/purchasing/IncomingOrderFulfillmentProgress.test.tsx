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
  it("renders cumulative good/damaged/outstanding/remaining value for prepare remaining", () => {
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
            unitOfMeasureCode: "Kilogram",
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
        outstandingLabel="Outstanding"
        unitCostLabel="Unit cost"
        remainingValueLabel="Remaining value"
      />,
    );

    expect(screen.getByTestId("incoming-order-fulfillment-progress")).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Good received" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Damaged" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Outstanding" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Remaining value" })).toBeInTheDocument();
    expect(screen.queryByRole("columnheader", { name: "Unit" })).not.toBeInTheDocument();

    expect(screen.getByTestId(`incoming-order-fulfillment-progress-good-${appleId}`)).toHaveTextContent("3");
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-damaged-${appleId}`)).toHaveTextContent("1");
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-outstanding-${appleId}`)).toHaveTextContent("1");
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-remaining-value-${appleId}`)).toHaveTextContent(
      "₱10.00",
    );

    expect(screen.getByTestId(`incoming-order-fulfillment-progress-good-${bananaId}`)).toHaveTextContent("2");
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-damaged-${bananaId}`)).toHaveTextContent("0");
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-outstanding-${bananaId}`)).toHaveTextContent("2");
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-remaining-value-${bananaId}`)).toHaveTextContent(
      "₱16.00",
    );

    const appleRow = screen.getByTestId(`incoming-order-fulfillment-progress-row-${appleId}`);
    expect(appleRow).toHaveTextContent("pc");
    expect(appleRow).toHaveAttribute("data-outstanding", "true");
  });

  it("derives outstanding and remaining value when API fields are missing", () => {
    render(
      <IncomingOrderFulfillmentProgress
        lines={[
          line({
            productId: appleId,
            nameSnapshot: "Apple",
            orderedQty: 5,
            goodReceivedQty: 2,
            damagedQty: 1,
            cancelledRemainingQty: 0,
            outstandingQty: null,
            remainingValue: null,
            unitPriceSnapshot: 12,
          }),
        ]}
        title="Fulfillment progress"
        productLabel="Product"
        orderedLabel="Ordered"
        goodLabel="Good received"
        damagedLabel="Damaged"
        missingLabel="Missing / not delivered"
        outstandingLabel="Outstanding"
        unitCostLabel="Unit cost"
        remainingValueLabel="Remaining value"
      />,
    );

    // 5 − 2 − 0 = 3 outstanding; remaining value 3 × 12 = 36
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-outstanding-${appleId}`)).toHaveTextContent("3");
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-remaining-value-${appleId}`)).toHaveTextContent(
      "₱36.00",
    );
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-good-${appleId}`)).toHaveTextContent("2");
    expect(screen.getByTestId(`incoming-order-fulfillment-progress-damaged-${appleId}`)).toHaveTextContent("1");
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
        outstandingLabel="Outstanding"
        unitCostLabel="Unit cost"
        remainingValueLabel="Remaining value"
      />,
    );

    expect(screen.getByRole("columnheader", { name: "Missing / not delivered" })).toBeInTheDocument();
  });
});
