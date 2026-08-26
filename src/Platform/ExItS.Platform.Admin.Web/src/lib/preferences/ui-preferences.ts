export const UI_PREFERENCES_STORAGE_KEY = "exits.platform-admin-web.ui-preferences.v1";

export const themeModes = ["system", "light", "dark"] as const;
export type ThemeMode = (typeof themeModes)[number];

export const languages = ["en", "fil-PH"] as const;
export type Language = (typeof languages)[number];

export const densities = ["comfortable", "balanced", "compact"] as const;
export type Density = (typeof densities)[number];

export type UiPreferences = {
  theme: ThemeMode;
  language: Language;
  density: Density;
  sidebarCollapsed: boolean;
};

export const defaultUiPreferences: UiPreferences = {
  theme: "system",
  language: "en",
  density: "balanced",
  sidebarCollapsed: false,
};

function isThemeMode(value: unknown): value is ThemeMode {
  return themeModes.includes(value as ThemeMode);
}

function isLanguage(value: unknown): value is Language {
  return languages.includes(value as Language);
}

function isDensity(value: unknown): value is Density {
  return densities.includes(value as Density);
}

export function parseUiPreferences(value: unknown): UiPreferences {
  if (typeof value !== "object" || value === null) {
    return { ...defaultUiPreferences };
  }

  const record = value as Record<string, unknown>;
  return {
    theme: isThemeMode(record.theme) ? record.theme : defaultUiPreferences.theme,
    language: isLanguage(record.language) ? record.language : defaultUiPreferences.language,
    density: isDensity(record.density) ? record.density : defaultUiPreferences.density,
    sidebarCollapsed:
      typeof record.sidebarCollapsed === "boolean"
        ? record.sidebarCollapsed
        : defaultUiPreferences.sidebarCollapsed,
  };
}

export function readUiPreferences(storage: Pick<Storage, "getItem"> | null): UiPreferences {
  if (!storage) {
    return { ...defaultUiPreferences };
  }

  try {
    const raw = storage.getItem(UI_PREFERENCES_STORAGE_KEY);
    if (!raw) {
      return { ...defaultUiPreferences };
    }
    return parseUiPreferences(JSON.parse(raw) as unknown);
  } catch {
    return { ...defaultUiPreferences };
  }
}

export function writeUiPreferences(
  storage: Pick<Storage, "setItem"> | null,
  preferences: UiPreferences,
): void {
  if (!storage) {
    return;
  }
  storage.setItem(UI_PREFERENCES_STORAGE_KEY, JSON.stringify(preferences));
}

export function applyUiPreferencesToDocument(preferences: UiPreferences, root: HTMLElement): void {
  root.dataset.theme = preferences.theme;
  root.dataset.density = preferences.density;
  root.lang = preferences.language;
}
