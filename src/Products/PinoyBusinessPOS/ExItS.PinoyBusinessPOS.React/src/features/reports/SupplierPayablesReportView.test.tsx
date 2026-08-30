import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { AppProviders } from "@/app/providers";
import type { PosSupplierPayableReportDto } from "@/api/pos/pos-supplier-payables-client";
import { SupplierPayablesReportView } from "@/features/reports/SupplierPayablesReportView";

const report: PosSupplierPayableReportDto = {
  asOfDate: "2026-08-30",
  summary: {
    outstandingTotal: 800,
    overdueTotal: 200,
    openCount: 1,
    partiallyPaidCount: 1,
    paidCount: 1,
    voidedCount: 0,
  },
  suppliers: [
    {
      supplierId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      supplierName: "Fresh Farms",
      outstandingBalance: 800,
      overdueBalance: 200,
      openPayables: 2,
      oldestDueDate: "2026-08-01",
    },
  ],
  payables: [
    {
      payableId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      supplierId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      supplierName: "Fresh Farms",
      sourceType: "GoodsReceipt",
      sourceId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      originalAmount: 1000,
      paidAtReceiptAmount: 200,
      paidAmount: 200,
      balance: 800,
      status: "Open",
      dueDate: "2026-08-01",
      isOverdue: true,
      createdAtUtc: "2026-08-20T00:00:00Z",
    },
  ],
};

describe("SupplierPayablesReportView", () => {
  it("renders as-of summary, supplier balances, and payable detail", () => {
    render(
      <AppProviders>
        <SupplierPayablesReportView report={report} />
      </AppProviders>,
    );

    expect(screen.getByTestId("supplier-payables-as-of")).toHaveTextContent("2026-08-30");
    expect(screen.getByTestId("supplier-payables-open-count")).toHaveTextContent("1");
    expect(screen.getByTestId("supplier-payables-supplier-list")).toHaveTextContent("Fresh Farms");
    expect(screen.getByTestId("supplier-payables-table")).toHaveTextContent("Goods Receipt");
  });
});
