import { jsPDF } from "jspdf";
import autoTable from "jspdf-autotable";
import * as XLSX from "xlsx";
import type { PeopleRowModel } from "@/features/personal/people-status";
import { buildCsvWithMetadata, downloadCsvFile } from "@/lib/csv";
import { downloadBlob } from "@/lib/download-blob";

export type PeopleTableExportLabels = {
  name: string;
  contact: string;
  email: string;
  status: string;
  lent: string;
  owe: string;
};

export type PeopleTableExportModel = {
  rows: Array<{
    name: string;
    contact: string;
    email: string;
    status: string;
    lent: string;
    owe: string;
  }>;
  exportedAt: string;
};

export function buildPeopleTableExportModel(
  rows: readonly PeopleRowModel[],
  statusLabel: (status: PeopleRowModel["connectionStatus"]) => string,
): PeopleTableExportModel {
  return {
    exportedAt: new Date().toISOString(),
    rows: rows.map((row) => ({
      name: row.contact.displayName,
      contact: row.contact.phone?.trim() || "",
      email: row.contact.email?.trim() || "",
      status: statusLabel(row.connectionStatus),
      lent: String(row.lentTotal),
      owe: String(row.oweTotal),
    })),
  };
}

function headers(labels: PeopleTableExportLabels): string[] {
  return [labels.name, labels.contact, labels.email, labels.status, labels.lent, labels.owe];
}

function bodyRows(model: PeopleTableExportModel): string[][] {
  return model.rows.map((row) => [row.name, row.contact, row.email, row.status, row.lent, row.owe]);
}

export function downloadPeopleTableCsv(model: PeopleTableExportModel, labels: PeopleTableExportLabels): void {
  const text = buildCsvWithMetadata(
    [
      ["People", String(model.rows.length)],
      ["Exported at", model.exportedAt],
    ],
    { headers: headers(labels), rows: bodyRows(model) },
  );
  downloadCsvFile("people.csv", text);
}

export function downloadPeopleTableXlsx(model: PeopleTableExportModel, labels: PeopleTableExportLabels): void {
  const workbook = XLSX.utils.book_new();
  const sheetRows: string[][] = [
    ["People", String(model.rows.length)],
    ["Exported at", model.exportedAt],
    [],
    headers(labels),
    ...bodyRows(model),
  ];
  const sheet = XLSX.utils.aoa_to_sheet(sheetRows);
  XLSX.utils.book_append_sheet(workbook, sheet, "People");
  const buffer = XLSX.write(workbook, { bookType: "xlsx", type: "array" }) as ArrayBuffer;
  downloadBlob(
    "people.xlsx",
    buffer,
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  );
}

export function downloadPeopleTablePdf(model: PeopleTableExportModel, labels: PeopleTableExportLabels): void {
  const doc = new jsPDF({ orientation: "landscape", unit: "pt", format: "a4" });
  doc.setFontSize(14);
  doc.text("People", 40, 40);
  doc.setFontSize(10);
  doc.text(`Rows: ${model.rows.length}`, 40, 58);
  autoTable(doc, {
    startY: 74,
    head: [headers(labels)],
    body: bodyRows(model),
    styles: { fontSize: 9 },
  });
  const buffer = doc.output("arraybuffer");
  downloadBlob("people.pdf", buffer, "application/pdf");
}

export function printPeopleTable(): void {
  document.body.classList.add("exits-printing");
  window.print();
  window.setTimeout(() => {
    document.body.classList.remove("exits-printing");
  }, 300);
}
