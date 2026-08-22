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

export function globalCatalogStatusTone(
  status: string,
): "success" | "warning" | "danger" | "neutral" {
  if (status === "Active") {
    return "success";
  }
  if (status === "Draft" || status === "Inactive") {
    return "warning";
  }
  if (status === "Archived") {
    return "danger";
  }
  return "neutral";
}
