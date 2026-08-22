export function formatGlobalCatalogInstant(value: string | undefined, language: string): string | null {
  if (!value) {
    return null;
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

export const globalCatalogControlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

/** Shared list chrome — keep Business Types / Categories / Products / Imports / Templates aligned. */
export const globalCatalogListShellClass = "grid gap-3";
export const globalCatalogFilterFormClass =
  "grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:items-end";
export const globalCatalogTableShellClass =
  "rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3";
export const globalCatalogMobileCardClass =
  "rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5";
export const globalCatalogDetailCardClass =
  "grid gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4 md:grid-cols-2";
export const globalCatalogFieldLabelClass =
  "grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted";

export function globalCatalogStatusTone(
  status: string,
): "success" | "warning" | "danger" | "neutral" {
  if (status === "Active" || status === "Published") {
    return "success";
  }
  if (status === "Draft") {
    return "warning";
  }
  if (status === "Inactive" || status === "Archived") {
    return "neutral";
  }
  return "neutral";
}

export function globalCatalogImportStatusTone(
  status: string,
): "success" | "warning" | "danger" | "neutral" {
  if (status === "Completed") {
    return "success";
  }
  if (status === "Validated") {
    return "neutral";
  }
  if (status === "Queued" || status === "Processing") {
    return "warning";
  }
  if (status === "CompletedWithWarnings") {
    return "warning";
  }
  if (status === "Failed") {
    return "danger";
  }
  return "neutral";
}

export function formatGlobalCatalogFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }
  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
