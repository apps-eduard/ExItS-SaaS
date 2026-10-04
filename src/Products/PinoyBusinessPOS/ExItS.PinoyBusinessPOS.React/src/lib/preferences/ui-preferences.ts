import { z } from "zod";

export const UI_PREFERENCES_STORAGE_KEY = "exits.pos-client.ui-preferences.v1";

export const themePreferenceSchema = z.enum(["system", "light", "dark"]);
export const localePreferenceSchema = z.enum(["en", "fil-PH", "ceb-PH", "ilo-PH", "hil-PH"]);
export const densityPreferenceSchema = z.enum(["compact", "balance", "comfort"]);
export const primaryColorPreferenceSchema = z.enum([
  "green",
  "teal",
  "cyan",
  "blue",
  "indigo",
  "violet",
  "fuchsia",
  "rose",
  "orange",
]);
export const controlShapePreferenceSchema = z.enum(["standard", "soft", "pill"]);
export const motionPreferenceSchema = z.enum(["system", "reduced"]);
export const navigationModePreferenceSchema = z.enum([
  "standard",
  "compact",
  "hidden",
  "reveal",
]);
/** Page section bars: underline text, or the current tab chips. */
export const tabStylePreferenceSchema = z.enum(["underline", "tabs"]);

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
  /** Missing in older storage → current section chips. */
  tabStyle: tabStylePreferenceSchema.default("tabs"),
});

export type ThemePreference = z.infer<typeof themePreferenceSchema>;
export type LocalePreference = z.infer<typeof localePreferenceSchema>;
export type DensityPreference = z.infer<typeof densityPreferenceSchema>;
export type PrimaryColorPreference = z.infer<typeof primaryColorPreferenceSchema>;
export type ControlShapePreference = z.infer<typeof controlShapePreferenceSchema>;
export type MotionPreference = z.infer<typeof motionPreferenceSchema>;
export type NavigationModePreference = z.infer<typeof navigationModePreferenceSchema>;
export type TabStylePreference = z.infer<typeof tabStylePreferenceSchema>;
export type UiPreferences = z.infer<typeof uiPreferencesSchema>;

/** Topbar cycle: full → icons → hidden → full. Reveal joins by restoring to full. */
export const NAVIGATION_MODE_CYCLE = [
  "standard",
  "compact",
  "hidden",
] as const satisfies readonly NavigationModePreference[];

export function nextNavigationModeCycle(
  current: NavigationModePreference,
): (typeof NAVIGATION_MODE_CYCLE)[number] {
  if (current === "standard") {
    return "compact";
  }
  if (current === "compact") {
    return "hidden";
  }
  // hidden, reveal, or unknown → restore full labeled sidebar
  return "standard";
}

export const PRIMARY_COLOR_OPTIONS = [
  "green",
  "teal",
  "cyan",
  "blue",
  "indigo",
  "violet",
  "fuchsia",
  "rose",
  "orange",
] as const satisfies readonly PrimaryColorPreference[];

export const CONTROL_SHAPE_OPTIONS = [
  "standard",
  "soft",
  "pill",
] as const satisfies readonly ControlShapePreference[];

export const defaultUiPreferences: UiPreferences = {
  theme: "system",
  locale: "en",
  density: "balance",
  primaryColor: "green",
  controlShape: "standard",
  motion: "system",
  navigationMode: "standard",
  tabStyle: "tabs",
};

export function parseUiPreferences(raw: string | null): UiPreferences {
  if (!raw) {
    return defaultUiPreferences;
  }
  try {
    const parsedJson = JSON.parse(raw) as Record<string, unknown>;
    // Retired Amber swatch → Orange (warm primary still available).
    if (parsedJson.primaryColor === "amber") {
      parsedJson.primaryColor = "orange";
    }
    const parsed = uiPreferencesSchema.safeParse(parsedJson);
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

export function applyTabStyle(tabStyle: TabStylePreference): void {
  document.documentElement.dataset.tabStyle = tabStyle;
}

export function applyUiPreferences(preferences: UiPreferences): void {
  applyTheme(preferences.theme);
  applyLocale(preferences.locale);
  applyDensity(preferences.density);
  applyPrimaryColor(preferences.primaryColor);
  applyControlShape(preferences.controlShape);
  applyMotion(preferences.motion);
  applyNavigationMode(preferences.navigationMode);
  applyTabStyle(preferences.tabStyle);
}
