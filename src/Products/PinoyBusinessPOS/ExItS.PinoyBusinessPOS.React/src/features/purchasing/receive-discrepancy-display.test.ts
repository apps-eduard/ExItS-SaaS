import { describe, expect, it } from "vitest";
import {
  formatReceiveDiscrepancySummary,
  isReceiveDiscrepancyClassified,
  lineNeedsReceiveDiscrepancyClassification,
} from "@/features/purchasing/receive-discrepancy-display";

const labels = {
  damaged: "Damaged",
  notDelivered: "Not delivered",
  otherReasons: { WrongItem: "Wrong item", Other: "Other" },
  otherFallback: "Other",
};

describe("receive-discrepancy-display", () => {
  it("requires note + complete split before classified", () => {
    const line = {
      outstandingQty: 1.5,
      goodText: "0.5",
      damagedText: "0",
      notDeliveredText: "1",
      otherText: "0",
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

  it("formats mixed damaged, not delivered, and other compactly", () => {
    const summary = formatReceiveDiscrepancySummary(
      {
        outstandingQty: 2,
        goodText: "0",
        damagedText: "0.5",
        notDeliveredText: "0.5",
        otherText: "1",
        otherReasonCode: "WrongItem",
        remarksText: "Mixed",
        uom: "Kg",
      },
      labels,
    );
    expect(summary).toBe("0.5 damaged · 0.5 not delivered · 1 wrong item");
  });

  it("requires other reason and custom description when other qty present", () => {
    const line = {
      outstandingQty: 2,
      goodText: "0",
      damagedText: "0",
      notDeliveredText: "0",
      otherText: "2",
      otherReasonCode: "",
      otherReasonText: "",
      remarksText: "Note",
      uom: "Kg",
    };
    expect(isReceiveDiscrepancyClassified(line)).toBe(false);
    line.otherReasonCode = "Other";
    expect(isReceiveDiscrepancyClassified(line)).toBe(false);
    line.otherReasonText = "Supplier sent different SKU";
    expect(isReceiveDiscrepancyClassified(line)).toBe(true);
  });
});
