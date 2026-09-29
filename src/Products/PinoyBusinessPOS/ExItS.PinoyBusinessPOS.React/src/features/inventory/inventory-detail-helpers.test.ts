import { describe, expect, it } from "vitest";
import {
  formatBranchRollupMetricsLine,
  listNonZeroStockExceptions,
} from "@/features/inventory/inventory-detail-helpers";

describe("inventory-detail-helpers stock exceptions", () => {
  it("lists only non-zero exception rows", () => {
    const rows = listNonZeroStockExceptions({
      damagedQuantity: 5,
      inspectionHoldQuantity: 0,
      pendingReturnQuantity: 2,
      expiredQuantity: 0,
      salePolicyBlockedQuantity: 1,
      inTransitOutboundQuantity: 0,
      inTransitInboundQuantity: 4,
    });
    expect(rows.map((r) => r.kind)).toEqual([
      "damaged",
      "pendingReturn",
      "saleBlocked",
      "inTransitInbound",
    ]);
  });

  it("formats branch metrics with server available and exception suffixes", () => {
    const line = formatBranchRollupMetricsLine(
      {
        onHandQuantity: 40,
        availableQuantity: 31,
        damagedQuantity: 5,
        pendingReturnQuantity: 0,
      },
      {
        onHand: "on hand",
        available: "available",
        exceptions: {
          damaged: "Damaged",
          inspectionHold: "Inspection hold",
          pendingReturn: "Pending return",
          expired: "Expired",
          saleBlocked: "Sale blocked",
          inTransitOutbound: "In transit out",
          inTransitInbound: "In transit in",
        },
      },
    );
    expect(line).toBe("40 on hand · 31 available · 5 damaged");
  });
});
