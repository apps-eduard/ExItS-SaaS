import { useId, useState } from "react";
import { Button } from "../button/Button";
import { Dialog } from "../dialog/Dialog";
import { Radio } from "../radio/Radio";
import { ExIcon } from "../../icons/ExIcon";
import {
  buildCsv,
  buildXlsx,
  downloadBlob,
  exportFilename,
  rowsForExportScope,
  type ExportFormat,
  type ExportScope,
} from "./export";
import type { DataTableColumn } from "./types";
import styles from "./DataTable.module.scss";

const SCOPES: ReadonlyArray<{ id: ExportScope; label: string }> = [
  { id: "page", label: "Current page" },
  { id: "filtered", label: "All filtered records" },
  { id: "selected", label: "Selected records" },
];

type DataTableExportProps<T> = {
  title: string;
  columns: readonly DataTableColumn<T>[];
  sortedRows: readonly T[];
  pageRows: readonly T[];
  selectedIds: readonly string[];
  getRowId: (row: T) => string;
  disabled?: boolean;
  blockedReason?: string | null;
};

export function DataTableExport<T>({
  title,
  columns,
  sortedRows,
  pageRows,
  selectedIds,
  getRowId,
  disabled = false,
  blockedReason = null,
}: DataTableExportProps<T>) {
  const formatName = useId();
  const scopeName = useId();
  const [open, setOpen] = useState(false);
  const [format, setFormat] = useState<ExportFormat>("xlsx");
  const [scope, setScope] = useState<ExportScope>("page");
  const [preparing, setPreparing] = useState(false);
  const [message, setMessage] = useState("");

  const counts: Record<ExportScope, number> = {
    page: pageRows.length,
    filtered: sortedRows.length,
    selected: rowsForExportScope("selected", sortedRows, pageRows, selectedIds, getRowId).length,
  };
  const count = counts[scope];
  const formatLabel = format === "xlsx" ? "Excel" : "CSV";
  const scopeLabel = SCOPES.find((item) => item.id === scope)?.label ?? "Current page";
  const empty = count === 0;
  const summary = blockedReason
    ? blockedReason
    : empty
      ? "No records in this scope. Change the filters or select rows before exporting."
      : `${formatLabel} file with ${count.toLocaleString("en-US")} ${count === 1 ? "record" : "records"} from ${scopeLabel.toLowerCase()}.`;

  async function confirm() {
    if (blockedReason) {
      setMessage(blockedReason);
      return;
    }
    const exportRows = rowsForExportScope(scope, sortedRows, pageRows, selectedIds, getRowId);
    if (exportRows.length === 0 || columns.length === 0) {
      setMessage("No records to export.");
      return;
    }
    setPreparing(true);
    setMessage("");
    await new Promise((resolve) => window.setTimeout(resolve, 0));
    try {
      const filename = exportFilename(title, format);
      if (format === "csv") {
        downloadBlob(filename, new Blob([buildCsv(columns, exportRows)], { type: "text/csv;charset=utf-8" }));
      } else {
        const bytes = buildXlsx(title, columns, exportRows);
        downloadBlob(
          filename,
          new Blob([bytes], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }),
        );
      }
      setMessage(`Downloaded ${filename} with ${exportRows.length.toLocaleString("en-US")} ${exportRows.length === 1 ? "record" : "records"}.`);
    } catch {
      setMessage("The file could not be prepared. Nothing was downloaded.");
    } finally {
      setPreparing(false);
    }
  }

  return (
    <>
      <Button
        type="button"
        variant="secondary"
        icon={<ExIcon name="download" />}
        disabled={disabled}
        aria-haspopup="dialog"
        onClick={() => {
          setMessage("");
          setOpen(true);
        }}
      >
        Export
      </Button>
      {open ? (
        <Dialog
          open
          title="Export"
          description="Choose a file format and which records to include."
          size="sm"
          onClose={() => {
            if (!preparing) setOpen(false);
          }}
          footer={
            <>
              <Button type="button" variant="ghost" disabled={preparing} onClick={() => setOpen(false)}>
                Cancel
              </Button>
              <Button type="button" loading={preparing} disabled={empty || Boolean(blockedReason)} onClick={() => void confirm()}>
                {empty ? "Export" : `Export ${count.toLocaleString("en-US")}`}
              </Button>
            </>
          }
        >
          <form
            className={styles.exportForm}
            onSubmit={(event) => {
              event.preventDefault();
              void confirm();
            }}
          >
            <fieldset className={styles.choices}>
              <legend>File format</legend>
              <Radio name={formatName} value="xlsx" checked={format === "xlsx"} label="Excel (.xlsx)" onChange={() => setFormat("xlsx")} />
              <Radio name={formatName} value="csv" checked={format === "csv"} label="CSV (.csv)" onChange={() => setFormat("csv")} />
            </fieldset>
            <fieldset className={styles.choices}>
              <legend>Export scope</legend>
              {SCOPES.map((item) => {
                const itemCount = counts[item.id];
                const unavailable = item.id === "selected" && itemCount === 0;
                return (
                  <Radio
                    key={item.id}
                    name={scopeName}
                    value={item.id}
                    checked={scope === item.id}
                    disabled={unavailable}
                    label={`${item.label} (${itemCount.toLocaleString("en-US")})`}
                    onChange={() => setScope(item.id)}
                  />
                );
              })}
            </fieldset>
            <p className={styles.summary} role={message.startsWith("The file") || message.startsWith("No records") ? "alert" : "status"}>
              {message || summary}
            </p>
          </form>
        </Dialog>
      ) : null}
    </>
  );
}
