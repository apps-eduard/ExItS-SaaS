import type { InventoryStockStatusRowDto } from "@/api/pos/pos-inventory-client";
import {
  buildCsvWithMetadata,
  buildReportCsvFilename,
  downloadCsvFile,
  type CsvTable,
} from "@/lib/csv";
import { downloadBlob } from "@/lib/download-blob";
import * as XLSX from "xlsx";

export const STOCK_STATUS_EXPORT_HEADERS = [
  "Product",
  "SKU",
  "Category",
  "Branch",
  "On hand",
  "Sellable",
  "Reserved",
  "Available",
  "Damaged",
  "Inspection hold",
  "Pending return",
  "Expired",
  "Sale blocked",
  "Incoming",
  "In transit out",
  "Unit",
  "Low stock",
] as const;

export function buildStockStatusCsvTable(rows: InventoryStockStatusRowDto[]): CsvTable {
  return {
    headers: [...STOCK_STATUS_EXPORT_HEADERS],
    rows: rows.map((row) => [
      row.productName,
      row.sku ?? "",
      row.categoryName ?? "",
      row.branchName,
      row.onHandQuantity,
      row.sellableQuantity,
      row.reservedQuantity,
      row.availableQuantity,
      row.damagedQuantity,
      row.inspectionHoldQuantity,
      row.pendingReturnQuantity,
      row.expiredQuantity,
      row.saleBlockedQuantity,
      row.inTransitInboundQuantity,
      row.inTransitOutboundQuantity,
      row.unitOfMeasure,
      row.isLowStock ? "Yes" : "No",
    ]),
  };
}

export function buildStockStatusExportMetadata(args: {
  organizationName?: string | null;
  scopeLabel: string;
  stockState: string;
  search?: string | null;
  generatedAtUtc: string;
}): Array<readonly [string, string]> {
  const metadata: Array<[string, string]> = [
    ["Report", "Stock status"],
  ];
  if (args.organizationName?.trim()) {
    metadata.push(["Organization", args.organizationName.trim()]);
  }
  metadata.push(["Scope", args.scopeLabel]);
  metadata.push(["Stock state", args.stockState]);
  if (args.search?.trim()) {
    metadata.push(["Search", args.search.trim()]);
  }
  metadata.push(["As of", "Current only"]);
  metadata.push(["GeneratedAtUtc", args.generatedAtUtc]);
  return metadata;
}

export function exportStockStatusCsv(args: {
  rows: InventoryStockStatusRowDto[];
  organizationName?: string | null;
  scopeLabel: string;
  stockState: string;
  search?: string | null;
  generatedAtUtc: string;
}): string {
  const metadata = buildStockStatusExportMetadata(args);
  const table = buildStockStatusCsvTable(args.rows);
  const csvText = buildCsvWithMetadata(metadata, table);
  const filename = buildReportCsvFilename({
    reportName: "stock-status",
    scopeLabel: args.scopeLabel,
  });
  downloadCsvFile(filename, csvText);
  return csvText;
}

export function exportStockStatusXlsx(args: {
  rows: InventoryStockStatusRowDto[];
  organizationName?: string | null;
  scopeLabel: string;
  stockState: string;
  search?: string | null;
  generatedAtUtc: string;
}): void {
  const metadata = buildStockStatusExportMetadata(args);
  const table = buildStockStatusCsvTable(args.rows);
  const sheetRows: (string | number)[][] = [
    ...metadata.map(([key, value]) => [key, value]),
    [],
    table.headers,
    ...table.rows.map((row) =>
      row.map((cell) => (cell == null ? "" : cell)),
    ),
  ];
  const sheet = XLSX.utils.aoa_to_sheet(sheetRows);
  const book = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(book, sheet, "Stock status");
  const buffer = XLSX.write(book, { bookType: "xlsx", type: "array" });
  const stamp = new Date().toISOString().slice(0, 10);
  downloadBlob(
    `stock-status-${stamp}.xlsx`,
    buffer,
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  );
}
