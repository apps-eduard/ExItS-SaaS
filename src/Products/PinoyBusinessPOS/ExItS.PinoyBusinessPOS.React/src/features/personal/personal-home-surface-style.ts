import { useCallback, useState } from "react";

/** Personal Home page surface treatment (independent of Preferences). */
export const PERSONAL_HOME_SURFACE_STYLES = ["modern-solid", "ehg-glass"] as const;

export type PersonalHomeSurfaceStyle = (typeof PERSONAL_HOME_SURFACE_STYLES)[number];

export const PERSONAL_HOME_SURFACE_STYLE_DEFAULT: PersonalHomeSurfaceStyle = "modern-solid";

export const PERSONAL_HOME_SURFACE_STYLE_STORAGE_KEY = "exits.personal.home.surfaceStyle";

export function isPersonalHomeSurfaceStyle(value: unknown): value is PersonalHomeSurfaceStyle {
  return (
    typeof value === "string" &&
    (PERSONAL_HOME_SURFACE_STYLES as readonly string[]).includes(value)
  );
}

export function parsePersonalHomeSurfaceStyle(raw: unknown): PersonalHomeSurfaceStyle {
  if (isPersonalHomeSurfaceStyle(raw)) {
    return raw;
  }
  return PERSONAL_HOME_SURFACE_STYLE_DEFAULT;
}

export function loadPersonalHomeSurfaceStyle(): PersonalHomeSurfaceStyle {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return PERSONAL_HOME_SURFACE_STYLE_DEFAULT;
  }
  try {
    return parsePersonalHomeSurfaceStyle(
      window.localStorage.getItem(PERSONAL_HOME_SURFACE_STYLE_STORAGE_KEY),
    );
  } catch {
    return PERSONAL_HOME_SURFACE_STYLE_DEFAULT;
  }
}

export function savePersonalHomeSurfaceStyle(style: PersonalHomeSurfaceStyle): void {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return;
  }
  try {
    window.localStorage.setItem(PERSONAL_HOME_SURFACE_STYLE_STORAGE_KEY, style);
  } catch {
    /* ignore quota / private mode */
  }
}

/**
 * Personal Home–only surface style. Persists in localStorage; does not touch
 * global UI Preferences.
 */
export function usePersonalHomeSurfaceStyle(): [
  PersonalHomeSurfaceStyle,
  (next: PersonalHomeSurfaceStyle) => void,
] {
  const [surfaceStyle, setSurfaceStyleState] = useState<PersonalHomeSurfaceStyle>(() =>
    loadPersonalHomeSurfaceStyle(),
  );

  const setSurfaceStyle = useCallback((next: PersonalHomeSurfaceStyle) => {
    const resolved = parsePersonalHomeSurfaceStyle(next);
    setSurfaceStyleState(resolved);
    savePersonalHomeSurfaceStyle(resolved);
  }, []);

  return [surfaceStyle, setSurfaceStyle];
}
