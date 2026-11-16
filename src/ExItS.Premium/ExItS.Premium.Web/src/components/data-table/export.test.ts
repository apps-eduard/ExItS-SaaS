import { describe, expect, it } from "vitest";
import { buildCsv, buildXlsx, exportFilename, guardSpreadsheetFormula, rowsForExportScope, zipStore } from "./export";
import type { DataTableColumn } from "./types";

type Row = { id: string; name: string; amount: number; date: string; note: string };

const columns: DataTableColumn<Row>[] = [
  { id: "name", header: "Customer", accessor: (row) => row.name },
  { id: "amount", header: "Amount", accessor: (row) => row.amount, sortType: "number", align: "end" },
  { id: "date", header: "Date", accessor: (row) => row.date, sortType: "date" },
  {
    id: "note",
    header: "Note",
    accessor: (row) => row.note,
    exportValue: (row) => row.note,
  },
];

const rows: Row[] = [
  { id: "a", name: "Ana", amount: 12.5, date: "2026-01-02", note: "Hello, \"world\"" },
  { id: "b", name: "=CMD()", amount: 3, date: "2026-03-15", note: "Line one\nLine two" },
  { id: "c", name: "Cia", amount: 40, date: "2026-02-01", note: "Plain" },
];

function parseCsv(text: string) {
  const source = text.replace(/^\uFEFF/, "");
  const parsed: string[][] = [];
  let row: string[] = [];
  let field = "";
  let quoted = false;
  for (let index = 0; index < source.length; index += 1) {
    const char = source[index];
    if (quoted) {
      if (char === '"' && source[index + 1] === '"') {
        field += '"';
        index += 1;
      } else if (char === '"') quoted = false;
      else field += char;
    } else if (char === '"') quoted = true;
    else if (char === ",") {
      row.push(field);
      field = "";
    } else if (char === "\n") {
      row.push(field);
      parsed.push(row);
      row = [];
      field = "";
    } else if (char !== "\r") field += char;
  }
  if (field.length > 0 || row.length > 0) {
    row.push(field);
    parsed.push(row);
  }
  return parsed;
}

function unzipStore(bytes: Uint8Array) {
  const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
  const files = new Map<string, string>();
  let offset = 0;
  const decoder = new TextDecoder();
  while (offset + 30 <= bytes.length && view.getUint32(offset, true) === 0x04034b50) {
    const method = view.getUint16(offset + 8, true);
    const size = view.getUint32(offset + 18, true);
    const nameLength = view.getUint16(offset + 26, true);
    const extraLength = view.getUint16(offset + 28, true);
    const nameStart = offset + 30;
    const name = decoder.decode(bytes.subarray(nameStart, nameStart + nameLength));
    const dataStart = nameStart + nameLength + extraLength;
    if (method !== 0) throw new Error(`Unexpected compression for ${name}`);
    files.set(name, decoder.decode(bytes.subarray(dataStart, dataStart + size)));
    offset = dataStart + size;
  }
  return files;
}

describe("data table export", () => {
  it("builds a UTF-8 CSV with escaped cells and formula protection", () => {
    const csv = buildCsv(columns, rows);
    expect(csv.startsWith("\uFEFF")).toBe(true);
    expect(guardSpreadsheetFormula("=1+1")).toBe("'=1+1");
    const parsed = parseCsv(csv);
    expect(parsed[0]).toEqual(["Customer", "Amount", "Date", "Note"]);
    expect(parsed[1]).toEqual(["Ana", "12.5", "2026-01-02", "Hello, \"world\""]);
    expect(parsed[2]?.[0]).toBe("'=CMD()");
    expect(parsed[2]?.[1]).toBe("3");
    expect(parsed[2]?.[2]).toBe("2026-03-15");
    expect(parsed[2]?.[3]).toBe("Line one\nLine two");
    expect(parsed).toHaveLength(4);
  });

  it("builds a real xlsx package with numeric amounts and date cells", () => {
    const bytes = buildXlsx("Transactions", columns, [rows[0], rows[1]]);
    expect(bytes[0]).toBe(0x50);
    expect(bytes[1]).toBe(0x4b);
    const files = unzipStore(bytes);
    expect(files.has("xl/worksheets/sheet1.xml")).toBe(true);
    expect(files.get("[Content_Types].xml")).toContain("spreadsheetml.sheet.main+xml");
    const sheet = files.get("xl/worksheets/sheet1.xml") ?? "";
    expect(sheet).toContain("Customer");
    expect(sheet).toContain("<v>12.5</v>");
    expect(sheet).not.toContain("₱");
    const serial = (Date.UTC(2026, 0, 2) - Date.UTC(1899, 11, 30)) / 86400000;
    expect(sheet).toContain(`s="1"><v>${serial}</v>`);
    expect(sheet).toContain("'=CMD()");
    expect(files.get("xl/workbook.xml")).toContain('name="Transactions"');
  });

  it("limits rows to the page, the filtered list, or the selected ids in sort order", () => {
    expect(rowsForExportScope("page", rows, [rows[2]], ["a"], (row) => row.id).map((row) => row.id)).toEqual(["c"]);
    expect(rowsForExportScope("filtered", rows, [rows[2]], ["a"], (row) => row.id)).toHaveLength(3);
    expect(rowsForExportScope("selected", rows, rows, ["c", "a"], (row) => row.id).map((row) => row.id)).toEqual(["a", "c"]);
    expect(exportFilename("Transactions", "xlsx", new Date(2026, 10, 16))).toBe("exits-transactions-2026-11-16.xlsx");
    expect(exportFilename("Large ledger", "csv", new Date(2026, 0, 5))).toBe("exits-large-ledger-2026-01-05.csv");
  });

  it("stores zip entries that round-trip", () => {
    const packed = zipStore([{ name: "hello.txt", data: new TextEncoder().encode("ok") }]);
    expect(unzipStore(packed).get("hello.txt")).toBe("ok");
  });
});
