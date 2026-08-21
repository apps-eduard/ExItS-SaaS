import { z } from "zod";

export const UI_PREFERENCES_STORAGE_KEY = "exits.pos-client.ui-preferences.v1";

export const themePreferenceSchema = z.enum(["system", "light", "dark"]);
export const localePreferenceSchema = z.enum(["en", "fil-PH", "ceb-PH", "ilo-PH", "hil-PH"]);

export const uiPreferencesSchema = z.object({
  theme: themePreferenceSchema,
  locale: localePreferenceSchema,
});

export type ThemePreference = z.infer<typeof themePreferenceSchema>;
export type LocalePreference = z.infer<typeof localePreferenceSchema>;
export type UiPreferences = z.infer<typeof uiPreferencesSchema>;

export const defaultUiPreferences: UiPreferences = {
  theme: "system",
  locale: "en",
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

export function applyLocale(locale: LocalePreference): void {
  document.documentElement.lang = locale;
}
