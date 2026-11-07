export type AppearanceMode = "light" | "dark";

export const APPEARANCE_STORAGE_KEY = "exits-v2-appearance";

export function isAppearanceMode(value: unknown): value is AppearanceMode {
  return value === "light" || value === "dark";
}

export function getSystemPreference(): AppearanceMode {
  if (typeof window === "undefined") {
    return "light";
  }
  return window.matchMedia("(prefers-color-scheme: dark)").matches
    ? "dark"
    : "light";
}

export function persistAppearance(mode: AppearanceMode) {
  try {
    window.localStorage.setItem(APPEARANCE_STORAGE_KEY, mode);
  } catch {
    // Ignore quota / private-mode failures.
  }
}

/**
 * Reads the saved appearance.
 * New users default to Light.
 * Legacy "system" values are resolved once from the OS preference and rewritten.
 */
export function readStoredAppearance(): AppearanceMode {
  if (typeof window === "undefined") {
    return "light";
  }
  try {
    const stored = window.localStorage.getItem(APPEARANCE_STORAGE_KEY);
    if (isAppearanceMode(stored)) {
      return stored;
    }
    if (stored === "system") {
      const resolved = getSystemPreference();
      persistAppearance(resolved);
      return resolved;
    }
    return "light";
  } catch {
    return "light";
  }
}

export function applyDocumentAppearance(mode: AppearanceMode) {
  const root = document.documentElement;
  root.classList.toggle("dark", mode === "dark");
  root.style.colorScheme = mode;
}
