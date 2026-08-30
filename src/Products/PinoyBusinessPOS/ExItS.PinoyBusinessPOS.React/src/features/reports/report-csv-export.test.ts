import { describe, expect, it, vi, afterEach } from "vitest";
import {
  buildOperationalReportExport,
  buildProductProfitabilityCsvTable,
  resolveReportExportScopeLabel,
} from "@/features/reports/report-csv-export";
import * as reportingClient from "@/api/pos/pos-reporting-client";
import type { PosProductProfitabilityRowDto } from "@/api/pos/pos-reporting-client";
import { FEATURE_STORE_EXPORT, canExportData } from "@/access/pos-capabilities";
import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "22222222-2222-2222-2222-222222222222",
};

function grant(partial: Partial<SessionGrantResponse> = {}): SessionGrantResponse {
  return {
    accessToken: "token",
    productAccessAllowed: true,
    ...partial,
  };
}

function profitabilityRow(
  overrides: Partial<PosProductProfitabilityRowDto> = {},
): PosProductProfitabilityRowDto {
  return {
    productId: "33333333-3333-3333-3333-333333333333",
    productName: "Milk",
    sku: "MLK-1",
    unitOfMeasure: "pc",
    quantitySold: 10,
    quantityReturned: 1,
    netQuantity: 9,
    salesBeforeDiscounts: 100,
    commercialDiscounts: 5,
    netSales: 95,
    refundAmount: 0,
    knownCogs: 40,
    cogsStatus: "Partial",
    totalCogs: null,
    grossProfit: null,
    grossMarginPercent: null,
    costCompletenessPercent: 40,
    ...overrides,
  };
}

describe("report csv export builders", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("keeps null COGS/profit blank instead of zero", () => {
    const table = buildProductProfitabilityCsvTable([profitabilityRow()]);
    const row = table.rows[0]!;
    const totalCogsIndex = table.headers.indexOf("Total COGS");
    const grossProfitIndex = table.headers.indexOf("Gross Profit");
    const marginIndex = table.headers.indexOf("Margin %");
    expect(row[totalCogsIndex]).toBe("");
    expect(row[grossProfitIndex]).toBe("");
    expect(row[marginIndex]).toBe("");
  });

  it("reflects branch scope and date range in filename and metadata", async () => {
    vi.spyOn(reportingClient, "getSalesByProductReport").mockResolvedValue({
      fromDate: "2026-08-01",
      toDate: "2026-08-30",
      rows: [
        {
          productId: "p1",
          productName: "Bread",
          unitOfMeasure: "pc",
          sellingMode: "Unit",
          quantitySold: 2,
          quantityReturned: 0,
          netQuantity: 2,
          grossSaleAmount: 50,
          refundAmount: 0,
          netAmount: 50,
          preDiscountGrossSaleAmount: 50,
          commercialDiscountAmount: 0,
        },
      ],
    });

    const result = await buildOperationalReportExport({
      kind: "sales-by-product",
      workspace,
      range: { fromDate: "2026-08-01", toDate: "2026-08-30" },
      reportBranchId: workspace.branchId,
      scope: {
        organizationName: "Kizzy Store",
        scopeLabel: "Main Branch",
        fromDate: "2026-08-01",
        toDate: "2026-08-30",
      },
    });

    expect(result.filename).toBe("sales-by-product_main-branch_2026-08-01_2026-08-30.csv");
    expect(result.csvText).toContain("Scope,Main Branch");
    expect(result.csvText).toContain("From,2026-08-01");
    expect(result.csvText).toContain("To,2026-08-30");
    expect(result.csvText).toContain("Bread");
    expect(reportingClient.getSalesByProductReport).toHaveBeenCalledWith(
      workspace,
      { fromDate: "2026-08-01", toDate: "2026-08-30" },
      undefined,
      workspace.branchId,
    );
  });

  it("exports all-branches scope without a branch id", async () => {
    vi.spyOn(reportingClient, "getSalesByPaymentReport").mockResolvedValue({
      fromDate: "2026-08-01",
      toDate: "2026-08-30",
      rows: [
        {
          paymentMethod: "Cash",
          grossCompleted: 100,
          voided: 0,
          refunded: 0,
          net: 100,
          preDiscountGross: 100,
          commercialDiscountTotal: 0,
        },
      ],
    });

    const result = await buildOperationalReportExport({
      kind: "sales-by-payment",
      workspace,
      range: { fromDate: "2026-08-01", toDate: "2026-08-30" },
      reportBranchId: undefined,
      scope: {
        scopeLabel: "all-branches",
        fromDate: "2026-08-01",
        toDate: "2026-08-30",
      },
    });

    expect(result.filename).toContain("all-branches");
    expect(reportingClient.getSalesByPaymentReport).toHaveBeenCalledWith(
      workspace,
      { fromDate: "2026-08-01", toDate: "2026-08-30" },
      undefined,
      undefined,
    );
  });

  it("resolves export scope labels for branch and all modes", () => {
    expect(
      resolveReportExportScopeLabel({
        scopeMode: "organization_only",
        selection: { mode: "current" },
        currentBranchName: "Main",
      }),
    ).toBe("all-branches");
    expect(
      resolveReportExportScopeLabel({
        scopeMode: "branch",
        selection: { mode: "all" },
        currentBranchName: "Main",
      }),
    ).toBe("all-branches");
    expect(
      resolveReportExportScopeLabel({
        scopeMode: "branch",
        selection: { mode: "current" },
        currentBranchName: "Main Branch",
      }),
    ).toBe("Main Branch");
  });

  it("maps supplier-payables rows into CSV columns", async () => {
    vi.spyOn(reportingClient, "getSupplierPayablesReport").mockResolvedValue([
      {
        payableId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        supplierId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        supplierName: "Fresh Farms",
        sourceType: "GoodsReceipt",
        sourceId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        originalAmount: 1000,
        paidAtReceiptAmount: 200,
        paidAmount: 100,
        balance: 700,
        status: "PartiallyPaid",
        dueDate: "2026-09-15",
        isOverdue: true,
        createdAtUtc: "2026-08-20T00:00:00Z",
      },
    ]);

    const result = await buildOperationalReportExport({
      kind: "supplier-payables",
      workspace,
      range: { fromDate: "2026-08-01", toDate: "2026-08-30" },
      scope: {
        organizationName: "Kizy Store",
        scopeLabel: "all-branches",
      },
    });

    expect(result.filename).toContain("supplier-payables");
    expect(result.csvText).toContain("Fresh Farms");
    expect(result.csvText).toContain("GoodsReceipt");
    expect(result.csvText).toContain("700");
    expect(result.csvText).toContain("true");
    expect(reportingClient.getSupplierPayablesReport).toHaveBeenCalledWith(
      workspace,
      { outstandingOnly: true },
      undefined,
    );
  });
});

describe("canExportData entitlement", () => {
  it("requires store-export feature code", () => {
    expect(canExportData(grant({ featureCodes: [FEATURE_STORE_EXPORT] }))).toBe(true);
    expect(canExportData(grant({ featureCodes: [] }))).toBe(false);
    expect(canExportData(grant({ mappedPosRoleCode: "Owner" }))).toBe(false);
  });
});
