export const UI_STANDARDS_SECTIONS_STORAGE_KEY = "exits.uiStandards.sections.v1";

/**
 * Section open/closed defaults for remaining UI Standards disclosure
 * (locked ExitsTable reference panel under Data filter).
 */
export const UI_STANDARDS_DEFAULT_OPEN: Readonly<Record<string, boolean>> = {
  "tables.simple": false,
  "tables.demo": true,
  "tables.alignment": false,
  "tables.actions": false,
  "tables.inline-row-edit": false,
  "tables.inline-cell-edit": false,
  "tables.validation": false,
  "tables.sticky-actions": false,
  "tables.mobile-edit": false,
  "tables.cheatsheet": false,
};

export type UiStandardsDisclosureState = Record<string, boolean>;

/** Only Tables remains as a disclosable detailed panel. */
export type UiStandardsTab = "tables";

export function createDefaultUiStandardsDisclosure(): UiStandardsDisclosureState {
  return { ...UI_STANDARDS_DEFAULT_OPEN };
}

export function readUiStandardsDisclosure(): UiStandardsDisclosureState {
  const defaults = createDefaultUiStandardsDisclosure();
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return defaults;
  }
  try {
    const raw = window.localStorage.getItem(UI_STANDARDS_SECTIONS_STORAGE_KEY);
    if (!raw) return defaults;
    const parsed = JSON.parse(raw) as unknown;
    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) return defaults;
    const next = { ...defaults };
    for (const [key, value] of Object.entries(parsed as Record<string, unknown>)) {
      if (typeof value === "boolean" && key in defaults) {
        next[key] = value;
      }
    }
    return next;
  } catch {
    return defaults;
  }
}

export function writeUiStandardsDisclosure(state: UiStandardsDisclosureState): void {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") return;
  try {
    window.localStorage.setItem(UI_STANDARDS_SECTIONS_STORAGE_KEY, JSON.stringify(state));
  } catch {
    // Ignore quota / private-mode failures — gallery still works in-session.
  }
}

export function keysForTab(tab: UiStandardsTab, state: UiStandardsDisclosureState): string[] {
  const prefix = `${tab}.`;
  return Object.keys(state).filter((key) => key.startsWith(prefix));
}

export function setTabDisclosure(
  state: UiStandardsDisclosureState,
  tab: UiStandardsTab,
  open: boolean,
): UiStandardsDisclosureState {
  const next = { ...state };
  for (const key of keysForTab(tab, next)) {
    next[key] = open;
  }
  return next;
}
