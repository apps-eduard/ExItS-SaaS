import type { DataTableColumn } from "./types";

export type ExportFormat = "csv" | "xlsx";
export type ExportScope = "page" | "filtered" | "selected";

const FORMULA_PREFIX = /^[=+\-@\t\r]/;

/** Spreadsheets treat a leading =, +, -, or @ as a formula. Prefix those cells. */
export function guardSpreadsheetFormula(value: string) {
  return FORMULA_PREFIX.test(value) ? `'${value}` : value;
}

export function exportFilename(title: string, extension: ExportFormat, now = new Date()) {
  const slug = title
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `exits-${slug || "records"}-${year}-${month}-${day}.${extension}`;
}

export function rowsForExportScope<T>(
  scope: ExportScope,
  sortedRows: readonly T[],
  pageRows: readonly T[],
  selectedIds: readonly string[],
  getRowId: (row: T) => string,
) {
  if (scope === "page") return [...pageRows];
  if (scope === "filtered") return [...sortedRows];
  const selected = new Set(selectedIds);
  return sortedRows.filter((row) => selected.has(getRowId(row)));
}

type ExportCell =
  | { kind: "empty" }
  | { kind: "number"; value: number }
  | { kind: "date"; iso: string; serial: number }
  | { kind: "text"; text: string };

function isoDay(date: Date) {
  const year = date.getUTCFullYear();
  const month = String(date.getUTCMonth() + 1).padStart(2, "0");
  const day = String(date.getUTCDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function dateFromValue(value: string | number | Date) {
  if (value instanceof Date) return Number.isNaN(value.getTime()) ? null : value;
  if (typeof value !== "string" || !/^\d{4}-\d{2}-\d{2}/.test(value)) return null;
  const [year, month, day] = value.slice(0, 10).split("-").map(Number);
  if (!year || !month || !day) return null;
  const date = new Date(Date.UTC(year, month - 1, day));
  return Number.isNaN(date.getTime()) ? null : date;
}

/** Excel's day count, including the 1900 leap-year bug, so a date-only value stays on that day. */
function excelSerial(date: Date) {
  const utc = Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate());
  const epoch = Date.UTC(1899, 11, 30);
  return (utc - epoch) / 86400000;
}

function rawExportValue<T>(column: DataTableColumn<T>, row: T) {
  if (column.exportValue) return column.exportValue(row);
  return column.accessor(row);
}

export function exportCell<T>(column: DataTableColumn<T>, row: T): ExportCell {
  const raw = rawExportValue(column, row);
  if (raw == null || raw === "") return { kind: "empty" };
  if (typeof raw === "number") {
    return Number.isFinite(raw) ? { kind: "number", value: raw } : { kind: "empty" };
  }
  if (raw instanceof Date || column.sortType === "date") {
    const date = dateFromValue(raw);
    if (date) return { kind: "date", iso: isoDay(date), serial: excelSerial(date) };
  }
  return { kind: "text", text: String(raw) };
}

function csvField(value: string) {
  const safe = guardSpreadsheetFormula(value);
  if (/[",\n\r]/.test(safe)) return `"${safe.replace(/"/g, '""')}"`;
  return safe;
}

function csvText(cell: ExportCell) {
  if (cell.kind === "empty") return "";
  if (cell.kind === "number") return String(cell.value);
  if (cell.kind === "date") return cell.iso;
  return csvField(cell.text);
}

export function buildCsv<T>(columns: readonly DataTableColumn<T>[], rows: readonly T[]) {
  const header = columns.map((column) => csvField(column.header)).join(",");
  const body = rows.map((row) => columns.map((column) => csvText(exportCell(column, row))).join(","));
  return `\uFEFF${[header, ...body].join("\r\n")}`;
}

function escapeXml(value: string) {
  return value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function columnLetter(index: number) {
  let n = index + 1;
  let name = "";
  while (n > 0) {
    const rem = (n - 1) % 26;
    name = String.fromCharCode(65 + rem) + name;
    n = Math.floor((n - 1) / 26);
  }
  return name;
}

function sheetName(title: string) {
  const cleaned = title.replace(/[\\/*?:[\]]/g, " ").trim() || "Records";
  return cleaned.slice(0, 31);
}

function cellXml(ref: string, cell: ExportCell, header = false) {
  if (cell.kind === "empty") return `<c r="${ref}"/>`;
  if (cell.kind === "number") return `<c r="${ref}"><v>${cell.value}</v></c>`;
  if (cell.kind === "date") return `<c r="${ref}" s="1"><v>${cell.serial}</v></c>`;
  const text = escapeXml(guardSpreadsheetFormula(cell.text));
  const style = header ? ` s="2"` : "";
  return `<c r="${ref}" t="inlineStr"${style}><is><t xml:space="preserve">${text}</t></is></c>`;
}

function worksheetXml<T>(columns: readonly DataTableColumn<T>[], rows: readonly T[]) {
  const header = columns
    .map((column, index) => cellXml(`${columnLetter(index)}1`, { kind: "text", text: column.header }, true))
    .join("");
  const body = rows
    .map((row, rowIndex) => {
      const cells = columns
        .map((column, index) => cellXml(`${columnLetter(index)}${rowIndex + 2}`, exportCell(column, row)))
        .join("");
      return `<row r="${rowIndex + 2}">${cells}</row>`;
    })
    .join("");
  const last = `${columnLetter(Math.max(columns.length - 1, 0))}${rows.length + 1}`;
  return `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
<dimension ref="A1:${last}"/>
<sheetData>
<row r="1">${header}</row>
${body}
</sheetData>
</worksheet>`;
}

const CONTENT_TYPES = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
<Default Extension="xml" ContentType="application/xml"/>
<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
<Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
</Types>`;

const ROOT_RELS = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>`;

const WORKBOOK_RELS = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
<Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>`;

const STYLES = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
<numFmts count="1"><numFmt numFmtId="164" formatCode="yyyy-mm-dd"/></numFmts>
<fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts>
<fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>
<borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
<cellXfs count="3">
<xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
<xf numFmtId="164" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>
<xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/>
</cellXfs>
</styleSheet>`;

function workbookXml(title: string) {
  return `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
<sheets><sheet name="${escapeXml(sheetName(title))}" sheetId="1" r:id="rId1"/></sheets>
</workbook>`;
}

const CRC_TABLE = (() => {
  const table = new Uint32Array(256);
  for (let n = 0; n < 256; n += 1) {
    let c = n;
    for (let k = 0; k < 8; k += 1) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    table[n] = c >>> 0;
  }
  return table;
})();

function crc32(data: Uint8Array) {
  let c = 0xffffffff;
  for (const byte of data) c = CRC_TABLE[(c ^ byte) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
}

function concatBytes(parts: Uint8Array[]) {
  const size = parts.reduce((sum, part) => sum + part.length, 0);
  const out = new Uint8Array(size);
  let offset = 0;
  for (const part of parts) {
    out.set(part, offset);
    offset += part.length;
  }
  return out;
}

function le(size: 2 | 4, value: number) {
  const bytes = new Uint8Array(size);
  const view = new DataView(bytes.buffer);
  if (size === 2) view.setUint16(0, value, true);
  else view.setUint32(0, value, true);
  return bytes;
}

/** Uncompressed ZIP. Excel opens a stored .xlsx package. */
export function zipStore(files: ReadonlyArray<{ name: string; data: Uint8Array }>) {
  const encoder = new TextEncoder();
  const locals: Uint8Array[] = [];
  const centrals: Uint8Array[] = [];
  let offset = 0;
  for (const file of files) {
    const name = encoder.encode(file.name);
    const crc = crc32(file.data);
    const local = concatBytes([
      le(4, 0x04034b50),
      le(2, 20),
      le(2, 0),
      le(2, 0),
      le(2, 0),
      le(2, 0),
      le(4, crc),
      le(4, file.data.length),
      le(4, file.data.length),
      le(2, name.length),
      le(2, 0),
      name,
      file.data,
    ]);
    const central = concatBytes([
      le(4, 0x02014b50),
      le(2, 20),
      le(2, 20),
      le(2, 0),
      le(2, 0),
      le(2, 0),
      le(2, 0),
      le(4, crc),
      le(4, file.data.length),
      le(4, file.data.length),
      le(2, name.length),
      le(2, 0),
      le(2, 0),
      le(2, 0),
      le(2, 0),
      le(4, 0),
      le(4, offset),
      name,
    ]);
    locals.push(local);
    centrals.push(central);
    offset += local.length;
  }
  const centralDir = concatBytes(centrals);
  const end = concatBytes([
    le(4, 0x06054b50),
    le(2, 0),
    le(2, 0),
    le(2, files.length),
    le(2, files.length),
    le(4, centralDir.length),
    le(4, offset),
    le(2, 0),
  ]);
  return concatBytes([...locals, centralDir, end]);
}

function xmlFile(name: string, xml: string) {
  return { name, data: new TextEncoder().encode(xml) };
}

export function buildXlsx<T>(title: string, columns: readonly DataTableColumn<T>[], rows: readonly T[]) {
  return zipStore([
    xmlFile("[Content_Types].xml", CONTENT_TYPES),
    xmlFile("_rels/.rels", ROOT_RELS),
    xmlFile("xl/workbook.xml", workbookXml(title)),
    xmlFile("xl/_rels/workbook.xml.rels", WORKBOOK_RELS),
    xmlFile("xl/styles.xml", STYLES),
    xmlFile("xl/worksheets/sheet1.xml", worksheetXml(columns, rows)),
  ]);
}

export function downloadBlob(filename: string, blob: Blob) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  link.rel = "noopener";
  document.body.append(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}
