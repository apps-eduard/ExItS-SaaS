import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";
import {
  applyLocale,
  applyTheme,
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

function persist(next: UiPreferences) {
  writeUiPreferences(next);
  applyTheme(next.theme);
  applyLocale(next.locale);
}

export function PreferencesProvider({ children }: { children: ReactNode }) {
  const [preferences, setPreferences] = useState<UiPreferences>(() => {
    const initial = readUiPreferences();
    applyTheme(initial.theme);
    applyLocale(initial.locale);
    return initial;
  });

  const setTheme = useCallback((theme: ThemePreference) => {
    setPreferences((current) => {
      const next = { ...current, theme };
      persist(next);
      return next;
    });
  }, []);

  const setLocale = useCallback((locale: LocalePreference) => {
    setPreferences((current) => {
      const next = { ...current, locale };
      persist(next);
      return next;
    });
  }, []);

  const value = useMemo(
    () => ({ preferences, setTheme, setLocale }),
    [preferences, setTheme, setLocale],
  );

  return <PreferencesContext.Provider value={value}>{children}</PreferencesContext.Provider>;
}

export function usePreferences() {
  const context = useContext(PreferencesContext);
  if (!context) {
    throw new Error("usePreferences must be used within PreferencesProvider");
  }
  return context;
}
