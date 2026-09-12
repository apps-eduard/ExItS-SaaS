export const UI_STANDARDS_VIEW_STORAGE_KEY = "exits.uiStandards.view.v1";

export type UiStandardsViewMode = "classic" | "simple";

export const UI_STANDARDS_DEFAULT_VIEW: UiStandardsViewMode = "classic";

/** Legacy Simple V2 storage/query values normalize to Simple. */
export function normalizeUiStandardsView(value: unknown): UiStandardsViewMode | null {
  if (value === "classic") return "classic";
  if (value === "simple" || value === "simple-v2") return "simple";
  return null;
}

export function isUiStandardsViewMode(value: unknown): value is UiStandardsViewMode {
  return value === "classic" || value === "simple";
}

export function readUiStandardsView(): UiStandardsViewMode {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return UI_STANDARDS_DEFAULT_VIEW;
  }
  try {
    const raw = window.localStorage.getItem(UI_STANDARDS_VIEW_STORAGE_KEY);
    return normalizeUiStandardsView(raw) ?? UI_STANDARDS_DEFAULT_VIEW;
  } catch {
    return UI_STANDARDS_DEFAULT_VIEW;
  }
}

export function writeUiStandardsView(view: UiStandardsViewMode): void {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") return;
  try {
    window.localStorage.setItem(UI_STANDARDS_VIEW_STORAGE_KEY, view);
  } catch {
    // Ignore quota / private-mode failures.
  }
}

export function parseUiStandardsViewParam(search: string): UiStandardsViewMode | null {
  const params = new URLSearchParams(search.startsWith("?") ? search.slice(1) : search);
  return normalizeUiStandardsView(params.get("view"));
}
