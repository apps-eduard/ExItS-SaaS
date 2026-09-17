import { describe, expect, it } from "vitest";
import {
  buildReceivePlan,
  isClassificationComplete,
  receiveDiscrepancyQty,
} from "@/features/purchasing/receive-math";

const productId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

describe("receive discrepancy classification math", () => {
  it("defaults full good qty with no classification", () => {
    const plan = buildReceivePlan([
      {
        productId,
        outstandingQty: 5,
        goodQty: 5,
        damagedQty: 0,
        notDeliveredQty: 0,
        cancelRemaining: false,
      },
    ]);
    expect(plan.ok).toBe(true);
    if (plan.ok) {
      expect(plan.lines[0]?.shortClosedQty).toBe(0);
      expect(plan.lines[0]?.remainingAction).toBeNull();
    }
  });

  it("requires damaged + not delivered to equal discrepancy", () => {
    expect(receiveDiscrepancyQty(5, 3)).toBe(2);
    expect(isClassificationComplete(2, 0.5, 1.5)).toBe(true);
    expect(isClassificationComplete(2, 1, 0)).toBe(false);

    const incomplete = buildReceivePlan([
      {
        productId,
        outstandingQty: 5,
        goodQty: 3,
        damagedQty: 1,
        notDeliveredQty: 0,
        cancelRemaining: false,
      },
    ]);
    expect(incomplete.ok).toBe(false);
    if (!incomplete.ok) {
      expect(incomplete.error).toBe("classification_incomplete");
    }
  });

  it("supports all damaged, all not delivered, and manual split", () => {
    const damaged = buildReceivePlan([
      {
        productId,
        outstandingQty: 5,
        goodQty: 3,
        damagedQty: 2,
        notDeliveredQty: 0,
        cancelRemaining: false,
      },
    ]);
    expect(damaged.ok).toBe(true);
    if (damaged.ok) {
      expect(damaged.lines[0]?.damagedQty).toBe(2);
      expect(damaged.lines[0]?.rejectedQty).toBe(0);
      expect(damaged.lines[0]?.shortClosedQty).toBe(0);
      expect(damaged.lines[0]?.remainingAction).toBe("deliver_later");
    }

    const missing = buildReceivePlan([
      {
        productId,
        outstandingQty: 5,
        goodQty: 3,
        damagedQty: 0,
        notDeliveredQty: 2,
        cancelRemaining: false,
      },
    ]);
    expect(missing.ok).toBe(true);

    const split = buildReceivePlan([
      {
        productId,
        outstandingQty: 5,
        goodQty: 3,
        damagedQty: 0.5,
        notDeliveredQty: 1.5,
        cancelRemaining: true,
      },
    ]);
    expect(split.ok).toBe(true);
    if (split.ok) {
      expect(split.lines[0]?.shortClosedQty).toBe(2);
      expect(split.lines[0]?.remainingAfter).toBe(0);
      expect(split.lines[0]?.remainingAction).toBe("cancel_remaining");
      expect(split.lines[0]?.discrepancyKind).toBe("Other");
    }
  });
});
