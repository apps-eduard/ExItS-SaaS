export const UI_STANDARDS_SECTIONS_STORAGE_KEY = "exits.uiStandards.sections.v1";

/** Recommended first-load open/closed map (PILOT). */
export const UI_STANDARDS_DEFAULT_OPEN: Readonly<Record<string, boolean>> = {
  "tables.demo": true,
  "tables.cheatsheet": false,

  "buttons.shapes": true,
  "buttons.treatments": true,
  "buttons.samples": true,
  "buttons.icon-only": false,
  "buttons.motion": false,
  "buttons.states": false,
  "buttons.cheatsheet": false,

  "buttons.samples.primary": true,
  "buttons.samples.success": true,
  "buttons.samples.muted": true,
  "buttons.samples.outline": false,
  "buttons.samples.ghost": false,
  "buttons.samples.info": false,
  "buttons.samples.warning": false,
  "buttons.samples.danger": false,
  "buttons.samples.danger-strong": false,
};

export type UiStandardsDisclosureState = Record<string, boolean>;

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

export function keysForTab(tab: "tables" | "buttons", state: UiStandardsDisclosureState): string[] {
  const prefix = `${tab}.`;
  return Object.keys(state).filter((key) => key.startsWith(prefix));
}

export function setTabDisclosure(
  state: UiStandardsDisclosureState,
  tab: "tables" | "buttons",
  open: boolean,
): UiStandardsDisclosureState {
  const next = { ...state };
  for (const key of keysForTab(tab, next)) {
    next[key] = open;
  }
  return next;
}
