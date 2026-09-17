import {
  PRIMARY_COLOR_OPTIONS,
  type PrimaryColorPreference,
} from "@/lib/preferences/ui-preferences";

/**
 * Canonical light-mode Primary swatch accents (matches Preferences swatches).
 * Decorative use only — never write these into data-primary / preferences storage.
 */
export const PRIMARY_PALETTE_ACCENTS_LIGHT: Record<PrimaryColorPreference, string> = {
  green: "#166534",
  teal: "#0f766e",
  cyan: "#0e7490",
  blue: "#1d4ed8",
  indigo: "#4338ca",
  violet: "#6d28d9",
  fuchsia: "#a21caf",
  rose: "#be123c",
  orange: "#c2410c",
};

/** Dark-mode Primary accents (matches globals.css dark primary tokens). */
export const PRIMARY_PALETTE_ACCENTS_DARK: Record<PrimaryColorPreference, string> = {
  green: "#4ade80",
  teal: "#2dd4bf",
  cyan: "#67e8f9",
  blue: "#60a5fa",
  indigo: "#a5b4fc",
  violet: "#c4b5fd",
  fuchsia: "#e879f9",
  rose: "#fb7185",
  orange: "#fb923c",
};

export const AMBIENT_PRIMARY_COLORS = PRIMARY_COLOR_OPTIONS;

export function resolveAmbientPrimaryAccent(
  color: PrimaryColorPreference,
  dark: boolean,
): string {
  return dark ? PRIMARY_PALETTE_ACCENTS_DARK[color] : PRIMARY_PALETTE_ACCENTS_LIGHT[color];
}

/** Next decorative Primary — never repeats the current key consecutively. */
export function pickNextAmbientPrimary(
  current: PrimaryColorPreference | null,
  options: readonly PrimaryColorPreference[] = AMBIENT_PRIMARY_COLORS,
): PrimaryColorPreference {
  const pool = current ? options.filter((c) => c !== current) : [...options];
  if (pool.length === 0) {
    return options[0] ?? "green";
  }
  const index = Math.floor(Math.random() * pool.length);
  return pool[index] ?? options[0] ?? "green";
}

export function isDocumentThemeDark(): boolean {
  if (typeof document === "undefined") {
    return false;
  }
  const theme = document.documentElement.dataset.theme;
  if (theme === "dark") {
    return true;
  }
  if (theme === "light") {
    return false;
  }
  if (typeof window === "undefined" || typeof window.matchMedia !== "function") {
    return false;
  }
  return window.matchMedia("(prefers-color-scheme: dark)").matches;
}
