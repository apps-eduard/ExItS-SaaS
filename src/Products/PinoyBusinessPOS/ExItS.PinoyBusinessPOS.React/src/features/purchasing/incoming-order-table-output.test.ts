import { describe, expect, it } from "vitest";
import type { ConnectedPurchaseOrder, ConnectedPurchaseOrderLine } from "@/api/pos/pos-connected-suppliers-client";
import {
  buildIncomingOrderCsvText,
  buildIncomingOrderExportModel,
  buildIncomingOrderPdfArrayBuffer,
  buildIncomingOrderXlsxArrayBuffer,
  resolveIncomingOrderExportLines,
} from "@/features/purchasing/incoming-order-table-output";

const apple = {
  productId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
  nameSnapshot: "Apple",
  skuSnapshot: "PH-FRU-APPLE",
  qty: 5,
  unitPriceSnapshot: 180,
  lineTotal: 900,
  unitOfMeasureCode: "Kilogram",
  availability: "Available",
  proposedLineTotal: 900,
  confirmedLineTotal: 900,
} as ConnectedPurchaseOrderLine;

const banana = {
  productId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
  nameSnapshot: "Banana Lakatan",
  skuSnapshot: "PH-FRU-BANANA",
  qty: 5,
  unitPriceSnapshot: 76,
  lineTotal: 380,
  unitOfMeasureCode: "Kilogram",
  availability: "Available",
  proposedLineTotal: 380,
  confirmedLineTotal: 380,
} as ConnectedPurchaseOrderLine;

const order = {
  connectedPurchaseOrderId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
  relationshipId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
  buyerOrganizationId: "11111111-1111-4111-8111-111111111111",
  supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
  buyerPurchaseOrderId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
  buyerPoNumber: "PO-20260911-000001",
  orderDate: "2026-09-04",
  notes: null,
  status: "New",
  totalAmount: 1280,
  createdAtUtc: "2026-09-04T00:00:00Z",
  updatedAtUtc: "2026-09-04T00:00:00Z",
  lines: [apple, banana],
  displayStatus: "New",
  buyerDisplayName: "Paul Store",
  supplierBranchName: "Iloilo",
  paymentTerm: "Cash",
  paymentTermLabel: "Cash",
} as ConnectedPurchaseOrder;

describe("incoming-order-table-output", () => {
  it("exports matching filtered rows by default", () => {
    const resolved = resolveIncomingOrderExportLines([apple, banana], new Set());
    expect(resolved.scope).toBe("matching");
    expect(resolved.lines.map((line) => line.product)).toEqual(["Apple", "Banana Lakatan"]);
  });

  it("narrows export scope to selected matching rows", () => {
    const resolved = resolveIncomingOrderExportLines(
      [apple, banana],
      new Set([banana.productId]),
    );
    expect(resolved.scope).toBe("selected");
    expect(resolved.lines).toHaveLength(1);
    expect(resolved.lines[0]?.product).toBe("Banana Lakatan");
  });

  it("keeps authoritative order total while labeling selected lines separately", () => {
    const model = buildIncomingOrderExportModel(order, [apple, banana], new Set([apple.productId]));
    const csv = buildIncomingOrderCsvText(model);
    expect(model.orderTotal).toBe(1280);
    expect(model.selectedLinesTotal).toBe(900);
    expect(csv).toContain("Order total,1280");
    expect(csv).toContain("Selected lines total,900");
    expect(csv).toContain("Apple");
    expect(csv).not.toContain("Banana Lakatan");
  });

  it("builds real xlsx and pdf buffers with order context", () => {
    const model = buildIncomingOrderExportModel(order, [apple, banana], new Set());
    const xlsx = buildIncomingOrderXlsxArrayBuffer(model);
    const pdf = buildIncomingOrderPdfArrayBuffer(model);
    expect(xlsx.byteLength).toBeGreaterThan(100);
    // XLSX files are ZIP packages starting with PK
    expect(String.fromCharCode(...new Uint8Array(xlsx.slice(0, 2)))).toBe("PK");
    expect(pdf.byteLength).toBeGreaterThan(100);
    expect(String.fromCharCode(...new Uint8Array(pdf.slice(0, 5)))).toBe("%PDF-");
  });
});
