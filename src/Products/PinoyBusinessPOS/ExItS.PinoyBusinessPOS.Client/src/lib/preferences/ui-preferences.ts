export const UI_PREFERENCES_STORAGE_KEY = "exits.mobile-client.ui-preferences.v1";

export const THEME_VALUES = ["system", "light", "dark"] as const;
export type ThemePreference = (typeof THEME_VALUES)[number];

export const LOCALE_VALUES = ["en", "fil-PH"] as const;
export type LocalePreference = (typeof LOCALE_VALUES)[number];

export type UiPreferences = {
  theme: ThemePreference;
  locale: LocalePreference;
};

export const DEFAULT_UI_PREFERENCES: UiPreferences = {
  theme: "system",
  locale: "en",
};

export function isThemePreference(value: unknown): value is ThemePreference {
  return value === "system" || value === "light" || value === "dark";
}

export function isLocalePreference(value: unknown): value is LocalePreference {
  return value === "en" || value === "fil-PH";
}

export function readUiPreferences(storage: Pick<Storage, "getItem"> = localStorage): UiPreferences {
  try {
    const raw = storage.getItem(UI_PREFERENCES_STORAGE_KEY);
    if (!raw) {
      return { ...DEFAULT_UI_PREFERENCES };
    }
    const parsed: unknown = JSON.parse(raw);
    if (typeof parsed !== "object" || parsed === null) {
      return { ...DEFAULT_UI_PREFERENCES };
    }
    const record = parsed as Record<string, unknown>;
    return {
      theme: isThemePreference(record.theme) ? record.theme : DEFAULT_UI_PREFERENCES.theme,
      locale: isLocalePreference(record.locale) ? record.locale : DEFAULT_UI_PREFERENCES.locale,
    };
  } catch {
    return { ...DEFAULT_UI_PREFERENCES };
  }
}

export function writeUiPreferences(
  preferences: UiPreferences,
  storage: Pick<Storage, "setItem"> = localStorage,
): void {
  storage.setItem(UI_PREFERENCES_STORAGE_KEY, JSON.stringify(preferences));
}

export function applyUiPreferences(
  preferences: UiPreferences,
  root = document.documentElement,
): void {
  root.setAttribute("data-theme", preferences.theme);
  root.lang = preferences.locale;
  root.setAttribute("data-density", "comfortable");
}
