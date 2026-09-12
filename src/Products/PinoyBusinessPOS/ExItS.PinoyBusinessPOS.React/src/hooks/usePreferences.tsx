import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";
import {
  applyUiPreferences,
  readUiPreferences,
  writeUiPreferences,
  type ControlShapePreference,
  type DensityPreference,
  type LocalePreference,
  type MotionPreference,
  type PrimaryColorPreference,
  type ThemePreference,
  type UiPreferences,
} from "@/lib/preferences/ui-preferences";

type PreferencesContextValue = {
  preferences: UiPreferences;
  setTheme: (theme: ThemePreference) => void;
  setLocale: (locale: LocalePreference) => void;
  setDensity: (density: DensityPreference) => void;
  setPrimaryColor: (primaryColor: PrimaryColorPreference) => void;
  setControlShape: (controlShape: ControlShapePreference) => void;
  setMotion: (motion: MotionPreference) => void;
};

const PreferencesContext = createContext<PreferencesContextValue | null>(null);

function persist(next: UiPreferences) {
  writeUiPreferences(next);
  applyUiPreferences(next);
}

export function PreferencesProvider({ children }: { children: ReactNode }) {
  const [preferences, setPreferences] = useState<UiPreferences>(() => {
    const initial = readUiPreferences();
    applyUiPreferences(initial);
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

  const setDensity = useCallback((density: DensityPreference) => {
    setPreferences((current) => {
      const next = { ...current, density };
      persist(next);
      return next;
    });
  }, []);

  const setPrimaryColor = useCallback((primaryColor: PrimaryColorPreference) => {
    setPreferences((current) => {
      const next = { ...current, primaryColor };
      persist(next);
      return next;
    });
  }, []);

  const setControlShape = useCallback((controlShape: ControlShapePreference) => {
    setPreferences((current) => {
      const next = { ...current, controlShape };
      persist(next);
      return next;
    });
  }, []);

  const setMotion = useCallback((motion: MotionPreference) => {
    setPreferences((current) => {
      const next = { ...current, motion };
      persist(next);
      return next;
    });
  }, []);

  const value = useMemo(
    () => ({
      preferences,
      setTheme,
      setLocale,
      setDensity,
      setPrimaryColor,
      setControlShape,
      setMotion,
    }),
    [preferences, setTheme, setLocale, setDensity, setPrimaryColor, setControlShape, setMotion],
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
