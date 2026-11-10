import { createContext, useContext, useEffect, useLayoutEffect, useMemo, useState, type ReactNode } from "react";
import {
  DEFAULT_APPEARANCE,
  applyAppearanceAttributes,
  readAppearance,
  resolveTheme,
  systemPrefersDark,
  writeAppearance,
  type Appearance,
  type Density,
  type Palette,
  type ThemeMode,
  type Radius,
} from "./appearance";

type AppearanceContextValue = Appearance & {
  resolvedTheme: "light" | "dark";
  setTheme: (theme: ThemeMode) => void;
  setPalette: (palette: Palette) => void;
  setRadius: (radius: Radius) => void;
  setDensity: (density: Density) => void;
  reset: () => void;
};

const AppearanceContext = createContext<AppearanceContextValue | null>(null);

export function AppearanceProvider({ children }: { children: ReactNode }) {
  const [appearance, setAppearance] = useState<Appearance>(() => readAppearance(window.localStorage));
  const [systemDark, setSystemDark] = useState(systemPrefersDark);

  useEffect(() => {
    const query = window.matchMedia?.("(prefers-color-scheme: dark)");
    if (!query) return;
    const onChange = () => setSystemDark(query.matches);
    onChange();
    query.addEventListener("change", onChange);
    return () => query.removeEventListener("change", onChange);
  }, []);

  useLayoutEffect(() => {
    applyAppearanceAttributes(document.documentElement, appearance, systemDark);
  }, [appearance, systemDark]);

  useEffect(() => {
    writeAppearance(window.localStorage, appearance);
  }, [appearance]);

  const value = useMemo<AppearanceContextValue>(() => {
    return {
      ...appearance,
      resolvedTheme: resolveTheme(appearance.theme, systemDark),
      setTheme: (theme) => setAppearance((current) => ({ ...current, theme })),
      setPalette: (palette) => setAppearance((current) => ({ ...current, palette })),
      setRadius: (radius) => setAppearance((current) => ({ ...current, radius })),
      setDensity: (density) => setAppearance((current) => ({ ...current, density })),
      reset: () => setAppearance(DEFAULT_APPEARANCE),
    };
  }, [appearance, systemDark]);

  return <AppearanceContext.Provider value={value}>{children}</AppearanceContext.Provider>;
}

export function useAppearance() {
  const value = useContext(AppearanceContext);
  if (!value) throw new Error("useAppearance must be used within AppearanceProvider");
  return value;
}
