import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ReceiveRemainingQuantityTable } from "@/features/purchasing/ReceiveRemainingQuantityTable";
import type { RemainingDecisionRow } from "@/features/purchasing/receive-remaining-decision";

const baseRows: RemainingDecisionRow[] = [
  {
    productId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
    name: "Apple",
    sku: "PH-FRU-APPLE",
    remainingQty: 1,
    remainingLabel: "1 Kilogram",
    issueLabel: "Not delivered",
    remark: "short ship",
    remainingAction: null,
  },
  {
    productId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
    name: "Banana Lakatan",
    sku: "",
    remainingQty: 1,
    remainingLabel: "1 Kilogram",
    issueLabel: "Not delivered",
    remark: null,
    remainingAction: null,
  },
];

describe("ReceiveRemainingQuantityTable", () => {
  it("renders a table with apply-to-all and independent row decisions", async () => {
    const user = userEvent.setup();
    const onDecisionChange = vi.fn();
    const onApplyToAll = vi.fn();
    const { rerender } = render(
      <ReceiveRemainingQuantityTable
        title="Remaining quantity"
        summaryText="2 products · 2 units need a decision"
        applyToAllLabel="Apply to all"
        productColLabel="Product"
        remainingColLabel="Remaining"
        issueColLabel="Issue"
        decisionColLabel="Decision"
        replaceLaterLabel="Replace later"
        cancelRemainingLabel="Cancel remaining"
        rows={baseRows}
        highlightUnresolved={false}
        onDecisionChange={onDecisionChange}
        onApplyToAll={onApplyToAll}
      />,
    );

    expect(screen.getByTestId("receive-remaining-decisions")).toBeInTheDocument();
    expect(screen.getByRole("table")).toBeInTheDocument();
    expect(screen.queryByTestId("receive-remaining-aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("receive-remaining-decisions-apply-option-replace_later"));
    expect(onApplyToAll).toHaveBeenCalledWith("replace_later");

    await user.click(
      screen.getByTestId(
        "receive-remaining-decisions-choice-bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb-option-cancel_remaining",
      ),
    );
    expect(onDecisionChange).toHaveBeenCalledWith(
      "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
      "cancel_remaining",
    );

    rerender(
      <ReceiveRemainingQuantityTable
        title="Remaining quantity"
        summaryText="2 products · 2 units need a decision"
        applyToAllLabel="Apply to all"
        productColLabel="Product"
        remainingColLabel="Remaining"
        issueColLabel="Issue"
        decisionColLabel="Decision"
        replaceLaterLabel="Replace later"
        cancelRemainingLabel="Cancel remaining"
        rows={[
          { ...baseRows[0]!, remainingAction: "replace_later" },
          { ...baseRows[1]!, remainingAction: "cancel_remaining" },
        ]}
        highlightUnresolved
        onDecisionChange={onDecisionChange}
        onApplyToAll={onApplyToAll}
      />,
    );

    expect(
      screen.getByTestId(
        "receive-remaining-decisions-choice-aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa-option-replace_later",
      ),
    ).toHaveAttribute("data-selected", "true");
    expect(
      screen.getByTestId(
        "receive-remaining-decisions-choice-bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb-option-cancel_remaining",
      ),
    ).toHaveAttribute("data-selected", "true");
  });
});
