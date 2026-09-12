export const UI_STANDARDS_VIEW_STORAGE_KEY = "exits.uiStandards.view.v1";

export type UiStandardsViewMode = "classic" | "simple";

export const UI_STANDARDS_DEFAULT_VIEW: UiStandardsViewMode = "classic";

export function isUiStandardsViewMode(value: unknown): value is UiStandardsViewMode {
  return value === "classic" || value === "simple";
}

export function readUiStandardsView(): UiStandardsViewMode {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return UI_STANDARDS_DEFAULT_VIEW;
  }
  try {
    const raw = window.localStorage.getItem(UI_STANDARDS_VIEW_STORAGE_KEY);
    return isUiStandardsViewMode(raw) ? raw : UI_STANDARDS_DEFAULT_VIEW;
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
  const value = params.get("view");
  return isUiStandardsViewMode(value) ? value : null;
}
