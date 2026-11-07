import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import {
  applyDocumentAppearance,
  persistAppearance,
  readStoredAppearance,
  type AppearanceMode,
} from "./appearance";

type ThemeContextValue = {
  appearance: AppearanceMode;
  setAppearance: (mode: AppearanceMode) => void;
  toggleAppearance: () => void;
};

const ThemeContext = createContext<ThemeContextValue | null>(null);

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [appearance, setAppearanceState] = useState<AppearanceMode>(() =>
    readStoredAppearance(),
  );

  useEffect(() => {
    applyDocumentAppearance(appearance);
  }, [appearance]);

  const setAppearance = useCallback((mode: AppearanceMode) => {
    setAppearanceState(mode);
    persistAppearance(mode);
    applyDocumentAppearance(mode);
  }, []);

  const toggleAppearance = useCallback(() => {
    setAppearanceState((current) => {
      const next: AppearanceMode = current === "light" ? "dark" : "light";
      persistAppearance(next);
      applyDocumentAppearance(next);
      return next;
    });
  }, []);

  const value = useMemo(
    () => ({ appearance, setAppearance, toggleAppearance }),
    [appearance, setAppearance, toggleAppearance],
  );

  return (
    <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
  );
}

export function useTheme() {
  const ctx = useContext(ThemeContext);
  if (!ctx) {
    throw new Error("useTheme must be used within ThemeProvider");
  }
  return ctx;
}
