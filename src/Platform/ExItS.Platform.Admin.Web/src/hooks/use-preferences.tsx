import { createContext, useContext, type ReactNode } from "react";
import { useCallback, useMemo, useState } from "react";
import { translate, type MessageKey } from "@/lib/i18n/messages";
import {
  applyUiPreferencesToDocument,
  readUiPreferences,
  writeUiPreferences,
  type Density,
  type Language,
  type ThemeMode,
  type UiPreferences,
} from "@/lib/preferences/ui-preferences";

type PreferencesContextValue = UiPreferences & {
  t: (key: MessageKey) => string;
  setTheme: (theme: ThemeMode) => void;
  setLanguage: (language: Language) => void;
  setDensity: (density: Density) => void;
};

const PreferencesContext = createContext<PreferencesContextValue | null>(null);

function persist(next: UiPreferences): void {
  writeUiPreferences(window.localStorage, next);
  applyUiPreferencesToDocument(next, document.documentElement);
}

export function PreferencesProvider({ children }: { children: ReactNode }) {
  const [preferences, setPreferences] = useState<UiPreferences>(() => {
    const initial = readUiPreferences(window.localStorage);
    applyUiPreferencesToDocument(initial, document.documentElement);
    return initial;
  });

  const update = useCallback((patch: Partial<UiPreferences>) => {
    setPreferences((current) => {
      const next = { ...current, ...patch };
      persist(next);
      return next;
    });
  }, []);

  const value = useMemo<PreferencesContextValue>(
    () => ({
      ...preferences,
      t: (key) => translate(preferences.language, key),
      setTheme: (theme) => update({ theme }),
      setLanguage: (language) => update({ language }),
      setDensity: (density) => update({ density }),
    }),
    [preferences, update],
  );

  return <PreferencesContext.Provider value={value}>{children}</PreferencesContext.Provider>;
}

export function usePreferences(): PreferencesContextValue {
  const context = useContext(PreferencesContext);
  if (!context) {
    throw new Error("usePreferences must be used within PreferencesProvider.");
  }
  return context;
}
