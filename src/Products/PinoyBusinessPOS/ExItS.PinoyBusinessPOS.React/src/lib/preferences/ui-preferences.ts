import { z } from "zod";

export const UI_PREFERENCES_STORAGE_KEY = "exits.pos-client.ui-preferences.v1";

export const themePreferenceSchema = z.enum(["system", "light", "dark"]);
export const localePreferenceSchema = z.enum(["en", "fil-PH", "ceb-PH", "ilo-PH", "hil-PH"]);
export const densityPreferenceSchema = z.enum(["compact", "balance", "comfort"]);
export const primaryColorPreferenceSchema = z.enum(["green", "blue", "violet", "orange", "rose"]);
export const controlShapePreferenceSchema = z.enum(["standard", "pill"]);
export const motionPreferenceSchema = z.enum(["system", "reduced"]);
export const navigationModePreferenceSchema = z.enum(["standard", "compact", "reveal"]);

export const uiPreferencesSchema = z.object({
  theme: themePreferenceSchema,
  locale: localePreferenceSchema,
  /** Missing in older storage → balance (Expiring-stock chip baseline). */
  density: densityPreferenceSchema.default("balance"),
  /** Missing in older storage → green (POS default accent). */
  primaryColor: primaryColorPreferenceSchema.default("green"),
  /** Missing in older storage → standard interactive radius. */
  controlShape: controlShapePreferenceSchema.default("standard"),
  /** Missing in older storage → follow OS prefers-reduced-motion. */
  motion: motionPreferenceSchema.default("system"),
  /** Missing in older storage → labeled desktop sidebar. */
  navigationMode: navigationModePreferenceSchema.default("standard"),
});

export type ThemePreference = z.infer<typeof themePreferenceSchema>;
export type LocalePreference = z.infer<typeof localePreferenceSchema>;
export type DensityPreference = z.infer<typeof densityPreferenceSchema>;
export type PrimaryColorPreference = z.infer<typeof primaryColorPreferenceSchema>;
export type ControlShapePreference = z.infer<typeof controlShapePreferenceSchema>;
export type MotionPreference = z.infer<typeof motionPreferenceSchema>;
export type NavigationModePreference = z.infer<typeof navigationModePreferenceSchema>;
export type UiPreferences = z.infer<typeof uiPreferencesSchema>;

export const PRIMARY_COLOR_OPTIONS = [
  "green",
  "blue",
  "violet",
  "orange",
  "rose",
] as const satisfies readonly PrimaryColorPreference[];

export const defaultUiPreferences: UiPreferences = {
  theme: "system",
  locale: "en",
  density: "balance",
  primaryColor: "green",
  controlShape: "standard",
  motion: "system",
  navigationMode: "standard",
};

export function parseUiPreferences(raw: string | null): UiPreferences {
  if (!raw) {
    return defaultUiPreferences;
  }
  try {
    const parsed = uiPreferencesSchema.safeParse(JSON.parse(raw) as unknown);
    return parsed.success ? parsed.data : defaultUiPreferences;
  } catch {
    return defaultUiPreferences;
  }
}

export function readUiPreferences(): UiPreferences {
  if (typeof window === "undefined") {
    return defaultUiPreferences;
  }
  return parseUiPreferences(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY));
}

export function writeUiPreferences(preferences: UiPreferences): void {
  window.localStorage.setItem(UI_PREFERENCES_STORAGE_KEY, JSON.stringify(preferences));
}

export function applyTheme(theme: ThemePreference): void {
  document.documentElement.dataset.theme = theme;
}

export function applyDensity(density: DensityPreference): void {
  document.documentElement.dataset.density = density;
}

export function applyLocale(locale: LocalePreference): void {
  document.documentElement.lang = locale;
}

/** Root primary accent — also mirrors data-accent for legacy CSS selectors. */
export function applyPrimaryColor(primaryColor: PrimaryColorPreference): void {
  document.documentElement.dataset.primary = primaryColor;
  document.documentElement.dataset.accent = primaryColor;
}

export function applyControlShape(controlShape: ControlShapePreference): void {
  document.documentElement.dataset.controlShape = controlShape;
}

export function applyMotion(motion: MotionPreference): void {
  document.documentElement.dataset.motion = motion;
}

export function applyNavigationMode(navigationMode: NavigationModePreference): void {
  document.documentElement.dataset.navigationMode = navigationMode;
}

export function applyUiPreferences(preferences: UiPreferences): void {
  applyTheme(preferences.theme);
  applyLocale(preferences.locale);
  applyDensity(preferences.density);
  applyPrimaryColor(preferences.primaryColor);
  applyControlShape(preferences.controlShape);
  applyMotion(preferences.motion);
  applyNavigationMode(preferences.navigationMode);
}
