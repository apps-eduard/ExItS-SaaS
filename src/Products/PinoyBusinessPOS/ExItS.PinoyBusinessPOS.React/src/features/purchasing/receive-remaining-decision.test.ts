import { describe, expect, it } from "vitest";
import {
  buildRemainingDecisionRows,
  countUnresolvedRemaining,
  formatRemainingIssueLabel,
  sumRemainingUnits,
} from "@/features/purchasing/receive-remaining-decision";

describe("receive-remaining-decision", () => {
  const labels = { damaged: "Damaged", notDelivered: "Not delivered" };

  it("builds rows only for outstanding remaining qty", () => {
    const rows = buildRemainingDecisionRows(
      [
        {
          productId: "a",
          name: "Apple",
          sku: "SKU-A",
          uom: "Kilogram",
          outstandingQty: 5,
          goodText: "4",
          damagedText: "0",
          notDeliveredText: "1",
          remarksText: "short",
          remainingAction: null,
        },
        {
          productId: "b",
          name: "Full",
          sku: "",
          uom: "Piece",
          outstandingQty: 2,
          goodText: "2",
          damagedText: "0",
          notDeliveredText: "0",
          remarksText: "",
          remainingAction: "replace_later",
        },
      ],
      labels,
    );
    expect(rows).toHaveLength(1);
    expect(rows[0]?.productId).toBe("a");
    expect(rows[0]?.remainingLabel).toMatch(/1/);
    expect(rows[0]?.issueLabel).toBe("Not delivered");
    expect(rows[0]?.remark).toBe("short");
  });

  it("formats combined issue labels", () => {
    expect(
      formatRemainingIssueLabel(
        {
          outstandingQty: 2,
          goodText: "0",
          damagedText: "1",
          notDeliveredText: "1",
          uom: "Piece",
        },
        labels,
      ),
    ).toBe("Damaged · Not delivered");
  });

  it("counts unresolved and remaining units", () => {
    const rows = buildRemainingDecisionRows(
      [
        {
          productId: "a",
          name: "A",
          sku: "",
          uom: "Piece",
          outstandingQty: 3,
          goodText: "1",
          damagedText: "0",
          notDeliveredText: "2",
          remarksText: "n",
          remainingAction: null,
        },
        {
          productId: "b",
          name: "B",
          sku: "",
          uom: "Piece",
          outstandingQty: 2,
          goodText: "1",
          damagedText: "0",
          notDeliveredText: "1",
          remarksText: "n",
          remainingAction: "cancel_remaining",
        },
      ],
      labels,
    );
    expect(countUnresolvedRemaining(rows)).toBe(1);
    expect(sumRemainingUnits(rows)).toBe(3);
  });
});
