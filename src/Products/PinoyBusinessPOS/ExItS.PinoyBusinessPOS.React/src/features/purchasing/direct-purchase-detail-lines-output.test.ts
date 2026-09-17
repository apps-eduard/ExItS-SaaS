import { describe, expect, it } from "vitest";
import { buildDirectPurchaseDetailLinesExportModel } from "@/features/purchasing/direct-purchase-detail-lines-output";

describe("direct-purchase-detail-lines-output", () => {
  it("builds export model with sanitized filename and row payload", () => {
    const model = buildDirectPurchaseDetailLinesExportModel({
      receiptNumber: "DPR-20260913-000001",
      filterLabel: "All lines",
      rows: [
        {
          product: "Apple",
          quantity: 10,
          uom: "Kilogram",
          unitCost: 150,
          lineTotal: 1500,
          expiry: "2026-09-20",
          lot: "",
        },
      ],
    });

    expect(model.filenameBase).toContain("Direct-purchase-");
    expect(model.receiptNumber).toBe("DPR-20260913-000001");
    expect(model.filterLabel).toBe("All lines");
    expect(model.rows).toHaveLength(1);
    expect(model.rows[0]?.product).toBe("Apple");
    expect(model.rows[0]?.lineTotal).toBe(1500);
  });
});
