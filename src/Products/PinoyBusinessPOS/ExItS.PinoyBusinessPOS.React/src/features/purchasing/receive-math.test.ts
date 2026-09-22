import { describe, expect, it } from "vitest";
import {
  buildReceivePlan,
  isClassificationComplete,
  receiveDiscrepancyQty,
  resolveDiscrepancyKind,
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
        otherQty: 0,
        cancelRemaining: false,
      },
    ]);
    expect(plan.ok).toBe(true);
    if (plan.ok) {
      expect(plan.lines[0]?.shortClosedQty).toBe(0);
      expect(plan.lines[0]?.remainingAction).toBeNull();
    }
  });

  it("requires damaged + not delivered + other to equal discrepancy", () => {
    expect(receiveDiscrepancyQty(5, 3)).toBe(2);
    expect(isClassificationComplete(2, 0.5, 1, 0.5)).toBe(true);
    expect(isClassificationComplete(2, 1, 0, 0)).toBe(false);

    const incomplete = buildReceivePlan([
      {
        productId,
        outstandingQty: 5,
        goodQty: 3,
        damagedQty: 1,
        notDeliveredQty: 0,
        otherQty: 0,
        cancelRemaining: false,
      },
    ]);
    expect(incomplete.ok).toBe(false);
    if (!incomplete.ok) {
      expect(incomplete.error).toBe("classification_incomplete");
    }
  });

  it("supports all damaged, all missing, mixed, and other", () => {
    const damaged = buildReceivePlan([
      {
        productId,
        outstandingQty: 5,
        goodQty: 3,
        damagedQty: 2,
        notDeliveredQty: 0,
        otherQty: 0,
        cancelRemaining: false,
      },
    ]);
    expect(damaged.ok).toBe(true);
    if (damaged.ok) {
      expect(damaged.lines[0]?.damagedQty).toBe(2);
      expect(damaged.lines[0]?.rejectedQty).toBe(0);
      expect(damaged.lines[0]?.otherQty).toBe(0);
    }

    const missing = buildReceivePlan([
      {
        productId,
        outstandingQty: 5,
        goodQty: 3,
        damagedQty: 0,
        notDeliveredQty: 2,
        otherQty: 0,
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
        otherQty: 0,
        cancelRemaining: true,
      },
    ]);
    expect(split.ok).toBe(true);
    if (split.ok) {
      expect(split.lines[0]?.discrepancyKind).toBe("Other");
    }

    const withOther = buildReceivePlan([
      {
        productId,
        outstandingQty: 5,
        goodQty: 3,
        damagedQty: 1,
        notDeliveredQty: 0,
        otherQty: 1,
        cancelRemaining: false,
      },
    ]);
    expect(withOther.ok).toBe(true);
    if (withOther.ok) {
      expect(withOther.lines[0]?.otherQty).toBe(1);
      expect(withOther.lines[0]?.discrepancyKind).toBe("Other");
    }
    expect(resolveDiscrepancyKind(1, 0, 1)).toBe("Other");
    expect(resolveDiscrepancyKind(2, 0, 0)).toBe("Damaged");
  });
});
