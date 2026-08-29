import { describe, expect, it } from "vitest";
import { posProductProfitabilityReportDtoSchema } from "@/api/pos/pos-reporting-client";

describe("product profitability client schema", () => {
  it("parses ranked rows and hides incomplete gross profit as null", () => {
    const dto = posProductProfitabilityReportDtoSchema.parse({
      fromDate: "2026-08-01",
      toDate: "2026-08-31",
      branchId: null,
      rankBy: "grossProfitDesc",
      rows: [
        {
          productId: "11111111-1111-1111-1111-111111111111",
          productName: "Milk",
          sku: "M1",
          unitOfMeasure: "Piece",
          quantitySold: 10,
          quantityReturned: 0,
          netQuantity: 10,
          salesBeforeDiscounts: 1000,
          commercialDiscounts: 100,
          netSales: 900,
          refundAmount: 0,
          knownCogs: 300,
          cogsStatus: "Complete",
          grossProfit: 600,
          grossMarginPercent: 66.7,
          costCompletenessPercent: 100,
        },
        {
          productId: "22222222-2222-2222-2222-222222222222",
          productName: "Partial",
          unitOfMeasure: "Piece",
          quantitySold: 5,
          quantityReturned: 0,
          netQuantity: 5,
          salesBeforeDiscounts: 500,
          commercialDiscounts: 0,
          netSales: 500,
          refundAmount: 0,
          knownCogs: 100,
          cogsStatus: "Partial",
          grossProfit: null,
          grossMarginPercent: null,
          costCompletenessPercent: 40,
        },
      ],
    });

    expect(dto.rows[0]?.grossProfit).toBe(600);
    expect(dto.rows[0]?.commercialDiscounts).toBe(100);
    expect(dto.rows[1]?.grossProfit).toBeNull();
    expect(dto.rankBy).toBe("grossProfitDesc");
  });
});
