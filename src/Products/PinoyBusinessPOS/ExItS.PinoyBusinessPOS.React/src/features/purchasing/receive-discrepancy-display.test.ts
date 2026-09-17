import { describe, expect, it } from "vitest";
import {
  formatReceiveDiscrepancySummary,
  isReceiveDiscrepancyClassified,
  lineNeedsReceiveDiscrepancyClassification,
} from "@/features/purchasing/receive-discrepancy-display";

const labels = { damaged: "Damaged", notDelivered: "Not delivered" };

describe("receive-discrepancy-display", () => {
  it("requires note + complete split before classified", () => {
    const line = {
      outstandingQty: 1.5,
      goodText: "0.5",
      damagedText: "0",
      notDeliveredText: "1",
      remarksText: "",
      uom: "Kg",
    };
    expect(lineNeedsReceiveDiscrepancyClassification(line)).toBe(true);
    expect(isReceiveDiscrepancyClassified(line)).toBe(false);
    expect(formatReceiveDiscrepancySummary(line, labels)).toBeNull();

    line.remarksText = "Short";
    expect(isReceiveDiscrepancyClassified(line)).toBe(true);
    expect(formatReceiveDiscrepancySummary(line, labels)).toMatch(/not delivered/i);
  });

  it("formats mixed damaged and not delivered compactly", () => {
    const summary = formatReceiveDiscrepancySummary(
      {
        outstandingQty: 1,
        goodText: "0",
        damagedText: "0.5",
        notDeliveredText: "0.5",
        remarksText: "Mixed",
        uom: "Kg",
      },
      labels,
    );
    expect(summary).toBe("0.5 damaged · 0.5 not delivered");
  });
});
