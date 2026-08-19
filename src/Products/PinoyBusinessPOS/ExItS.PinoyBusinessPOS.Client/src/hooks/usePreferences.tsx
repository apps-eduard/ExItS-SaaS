import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";
import {
  applyUiPreferences,
  DEFAULT_UI_PREFERENCES,
  readUiPreferences,
  writeUiPreferences,
  type LocalePreference,
  type ThemePreference,
  type UiPreferences,
} from "@/lib/preferences/ui-preferences";

type PreferencesContextValue = {
  preferences: UiPreferences;
  setTheme: (theme: ThemePreference) => void;
  setLocale: (locale: LocalePreference) => void;
};

const PreferencesContext = createContext<PreferencesContextValue | null>(null);

export function PreferencesProvider({ children }: { children: ReactNode }) {
  const [preferences, setPreferences] = useState<UiPreferences>(() => {
    const initial = typeof window === "undefined" ? DEFAULT_UI_PREFERENCES : readUiPreferences();
    if (typeof document !== "undefined") {
      applyUiPreferences(initial);
    }
    return initial;
  });

  const commit = useCallback((next: UiPreferences) => {
    setPreferences(next);
    writeUiPreferences(next);
    applyUiPreferences(next);
  }, []);

  const value = useMemo<PreferencesContextValue>(
    () => ({
      preferences,
      setTheme: (theme) => commit({ ...preferences, theme }),
      setLocale: (locale) => commit({ ...preferences, locale }),
    }),
    [commit, preferences],
  );

  return <PreferencesContext.Provider value={value}>{children}</PreferencesContext.Provider>;
}

export function usePreferences(): PreferencesContextValue {
  const context = useContext(PreferencesContext);
  if (!context) {
    throw new Error("usePreferences must be used within PreferencesProvider");
  }
  return context;
}
