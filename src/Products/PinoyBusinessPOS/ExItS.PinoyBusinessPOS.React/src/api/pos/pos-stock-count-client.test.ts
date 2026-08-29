import { describe, expect, it } from "vitest";
import {
  stockCountDtoSchema,
  stockCountPagedResultSchema,
} from "@/api/pos/pos-stock-count-client";

describe("pos-stock-count-client schemas", () => {
  it("parses a stock count detail DTO", () => {
    const parsed = stockCountDtoSchema.parse({
      stockCountId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      organizationId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      countNumber: "SC-1",
      title: "Monthly",
      status: "InProgress",
      countDate: "2026-08-29",
      notes: null,
      startedAtUtc: "2026-08-29T09:00:00Z",
      startedBy: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      completedAtUtc: null,
      completedBy: null,
      cancelledAtUtc: null,
      cancelledBy: null,
      createdAtUtc: "2026-08-29T08:00:00Z",
      updatedAtUtc: "2026-08-29T09:00:00Z",
      lines: [
        {
          lineId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
          productId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
          productName: "Coke",
          unitOfMeasure: "pcs",
          lineNumber: 1,
          systemOnHandSnapshot: 24,
          countedQuantity: 22,
          variance: -2,
        },
      ],
    });
    expect(parsed.lines[0]?.variance).toBe(-2);
  });

  it("parses paged list results", () => {
    const parsed = stockCountPagedResultSchema.parse({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
    expect(parsed.totalCount).toBe(0);
  });
});
