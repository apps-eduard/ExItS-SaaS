import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { TransferReceiveFollowUpTable } from "@/features/inventory/TransferReceiveFollowUpTable";
import { buildTransferFollowUpRows } from "@/features/inventory/transfer-receive-follow-up";
import type { TransferReceiveLineEdit } from "@/features/inventory/inventory-transfer-receive-helpers";

function baseLine(overrides: Partial<TransferReceiveLineEdit> = {}): TransferReceiveLineEdit {
  return {
    lineId: "line-1",
    productId: "11111111-1111-1111-1111-111111111111",
    name: "Banana Lakatan",
    sku: "",
    uom: "Kg",
    sentQty: 10,
    receivedQty: 0,
    outstandingQty: 10,
    goodText: "6",
    damagedText: "0",
    notDeliveredText: "0",
    otherText: "0",
    otherReasonCode: "",
    otherReasonText: "",
    actualReceivedProductId: null,
    actualReceivedProductName: null,
    remarksText: "wrong item. battery",
    missingFollowUp: null,
    damagedFollowUp: null,
    otherFollowUp: null,
    damagedCustodyDecision: null,
    otherCustodyDecision: null,
    ...overrides,
  };
}

describe("TransferReceiveFollowUpTable", () => {
  it("locks ReturnToSource for force-return other reasons", () => {
    render(
      <TransferReceiveFollowUpTable
        title="Follow-up"
        summaryText="1 product"
        productColLabel="Product"
        qtyColLabel="Qty"
        issueColLabel="Issue"
        decisionColLabel="Decision"
        waitOriginalLabel="Wait"
        requestReplacementLabel="Replace"
        acceptShortageLabel="Accept"
        keepAtDestinationLabel="Keep"
        returnToSourceLabel="Return to source"
        allowCustodyDecision
        linkedStockRequest={false}
        rows={[
          {
            rowKey: "p-other",
            productId: "11111111-1111-1111-1111-111111111111",
            name: "Coke",
            sku: "",
            issueKind: "other",
            qty: 2,
            qtyLabel: "2",
            issueLabel: "Wrong variant",
            otherReasonCode: "WrongVariant",
            remark: null,
            actualReceivedProductName: "Apple Green",
            action: "request_replacement",
          },
        ]}
        highlightUnresolved={false}
        onDecisionChange={vi.fn()}
        otherCustodyDecisionByProductId={
          new Map([["11111111-1111-1111-1111-111111111111", "ReturnToSource"]])
        }
      />,
    );

    const lockedSelect = screen.getByTestId(
      "transfer-receive-follow-up-custody-other-11111111-1111-1111-1111-111111111111-locked",
    );
    expect(lockedSelect).toBeInTheDocument();
    expect(
      screen.getByTestId(
        "transfer-receive-follow-up-custody-other-11111111-1111-1111-1111-111111111111-locked-option-ReturnToSource",
      ),
    ).toHaveAttribute("data-selected", "true");
    expect(
      screen.getByTestId(
        "transfer-receive-follow-up-custody-other-11111111-1111-1111-1111-111111111111-locked-option-KeepAtDestination",
      ),
    ).toBeDisabled();
    expect(screen.getByTestId("transfer-receive-follow-up-issue-actual-p-other")).toHaveTextContent(
      "Apple Green",
    );
    expect(screen.queryByText("wrong item. battery")).not.toBeInTheDocument();
  });

  it("puts selected wrong item under Issue instead of remarks", () => {
    const rows = buildTransferFollowUpRows(
      [
        baseLine({
          otherText: "2",
          otherReasonCode: "WrongItem",
          actualReceivedProductId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
          actualReceivedProductName: "Battery AA",
          otherFollowUp: "request_replacement",
        }),
      ],
      {
        damaged: "Damaged",
        notDelivered: "Not delivered",
        otherReasons: { WrongItem: "Wrong item" },
        otherFallback: "Other",
      },
    );

    expect(rows).toHaveLength(1);
    expect(rows[0]!.issueLabel).toBe("Wrong item");
    expect(rows[0]!.remark).toBeNull();
    expect(rows[0]!.actualReceivedProductName).toBe("Battery AA");

    render(
      <TransferReceiveFollowUpTable
        title="Follow-up"
        summaryText="1 product"
        productColLabel="Product"
        qtyColLabel="Qty"
        issueColLabel="Issue"
        decisionColLabel="Decision"
        waitOriginalLabel="Wait"
        requestReplacementLabel="Replace"
        acceptShortageLabel="Accept"
        linkedStockRequest={false}
        rows={rows}
        highlightUnresolved={false}
        onDecisionChange={vi.fn()}
      />,
    );

    expect(screen.getByText("Wrong item")).toBeInTheDocument();
    expect(
      screen.getByTestId(`transfer-receive-follow-up-issue-actual-${rows[0]!.rowKey}`),
    ).toHaveTextContent("Battery AA");
    expect(screen.queryByText("wrong item. battery")).not.toBeInTheDocument();
  });
});
