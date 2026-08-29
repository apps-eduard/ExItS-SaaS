import { describe, expect, it } from "vitest";
import {
  posOperationalOverviewDtoSchema,
  posSalesByProductReportDtoSchema,
  posSalesSummaryReportDtoSchema,
} from "@/api/pos/pos-reporting-client";

describe("discount reporting client schemas", () => {
  it("parses sales summary with pre-discount and commercial discount totals", () => {
    const dto = posSalesSummaryReportDtoSchema.parse({
      fromDate: "2026-08-01",
      toDate: "2026-08-31",
      completedGrossSales: 900,
      voidedSales: 0,
      completedReturnsRefunds: 0,
      netSales: 900,
      completedTransactionCount: 1,
      averageTransactionValue: 900,
      preDiscountGrossSales: 1000,
      commercialDiscountTotal: 100,
      netSubtotal: 900,
      taxAmount: 0,
    });
    expect(dto.preDiscountGrossSales).toBe(1000);
    expect(dto.commercialDiscountTotal).toBe(100);
    expect(dto.completedGrossSales).toBe(900);
    expect(dto.netSales).toBe(900);
    expect(dto.netSales).not.toBe(800);
  });

  it("defaults missing discount fields to zero for compatibility", () => {
    const dto = posOperationalOverviewDtoSchema.parse({
      fromDate: "2026-08-01",
      toDate: "2026-08-31",
      completedGrossSales: 100,
      voidedSales: 0,
      refunds: 0,
      netSales: 100,
      completedTransactionCount: 1,
      averageTransactionValue: 100,
    });
    expect(dto.commercialDiscountTotal).toBe(0);
    expect(dto.preDiscountGrossSales).toBe(0);
  });

  it("parses product rows with pre-discount and discount amounts", () => {
    const dto = posSalesByProductReportDtoSchema.parse({
      fromDate: "2026-08-01",
      toDate: "2026-08-31",
      rows: [
        {
          productId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          productName: "Milk",
          unitOfMeasure: "Piece",
          sellingMode: "PerItem",
          quantitySold: 10,
          quantityReturned: 0,
          netQuantity: 10,
          grossSaleAmount: 900,
          refundAmount: 0,
          netAmount: 900,
          preDiscountGrossSaleAmount: 1000,
          commercialDiscountAmount: 100,
        },
      ],
    });
    expect(dto.rows[0]?.preDiscountGrossSaleAmount).toBe(1000);
    expect(dto.rows[0]?.commercialDiscountAmount).toBe(100);
    expect(dto.rows[0]?.grossSaleAmount).toBe(900);
  });
});
