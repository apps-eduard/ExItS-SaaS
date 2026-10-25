import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import {
  ORG_EHG_PLAYGROUND_BG_DEFAULT,
  ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY,
  type OrgEhgPlaygroundBgOption,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/orgEhgPlaygroundBackground";
import { usePreferences } from "@/hooks/usePreferences";
import { isDocumentThemeDark } from "@/lib/primary-palette-accents";

export type UiStandardsShellTheme = "light" | "dark";

type UiStandardsShellAppearanceValue = {
  background: OrgEhgPlaygroundBgOption;
  setBackground: (next: OrgEhgPlaygroundBgOption) => void;
  theme: UiStandardsShellTheme;
  setTheme: (next: UiStandardsShellTheme) => void;
  toggleTheme: () => void;
};

const UiStandardsShellAppearanceContext = createContext<UiStandardsShellAppearanceValue | null>(
  null,
);

function resolveShellTheme(preferenceTheme: string): UiStandardsShellTheme {
  if (preferenceTheme === "dark") return "dark";
  if (preferenceTheme === "light") return "light";
  return isDocumentThemeDark() ? "dark" : "light";
}

/**
 * DEV / UI Standards only. Shares Background + light/dark across the whole
 * `/ui-standards` shell (sidenav + main). Does not change production Org/Personal pages.
 */
export function UiStandardsShellAppearanceProvider({ children }: { children: ReactNode }) {
  const [background, setBackground] = useState<OrgEhgPlaygroundBgOption>(ORG_EHG_PLAYGROUND_BG_DEFAULT);
  const { preferences, setTheme: setPreferenceTheme } = usePreferences();
  const theme = resolveShellTheme(preferences.theme);

  const setTheme = useCallback(
    (next: UiStandardsShellTheme) => {
      setPreferenceTheme(next);
    },
    [setPreferenceTheme],
  );

  const toggleTheme = useCallback(() => {
    setPreferenceTheme(theme === "dark" ? "light" : "dark");
  }, [setPreferenceTheme, theme]);

  const value = useMemo(
    () => ({
      background,
      setBackground,
      theme,
      setTheme,
      toggleTheme,
    }),
    [background, setTheme, theme, toggleTheme],
  );

  return (
    <UiStandardsShellAppearanceContext.Provider value={value}>
      {children}
    </UiStandardsShellAppearanceContext.Provider>
  );
}

/** Shared shell appearance; falls back to local background when no page provider. */
export function useUiStandardsShellAppearance(): UiStandardsShellAppearanceValue {
  const ctx = useContext(UiStandardsShellAppearanceContext);
  const [localBackground, setLocalBackground] = useState<OrgEhgPlaygroundBgOption>(
    ORG_EHG_PLAYGROUND_BG_DEFAULT,
  );
  const { preferences, setTheme: setPreferenceTheme } = usePreferences();
  const theme = resolveShellTheme(preferences.theme);

  const setTheme = useCallback(
    (next: UiStandardsShellTheme) => {
      setPreferenceTheme(next);
    },
    [setPreferenceTheme],
  );

  const toggleTheme = useCallback(() => {
    setPreferenceTheme(theme === "dark" ? "light" : "dark");
  }, [setPreferenceTheme, theme]);

  if (ctx) return ctx;

  return {
    background: localBackground,
    setBackground: setLocalBackground,
    theme,
    setTheme,
    toggleTheme,
  };
}

/** Page root: paints experimental Hybrid Glass backgrounds behind the whole UI Standards layout. */
export function UiStandardsShellRoot({
  children,
  product,
}: {
  children: ReactNode;
  product: string;
}) {
  const { background, theme } = useUiStandardsShellAppearance();

  return (
    <div
      className="exits-page ui-standards-page ui-standards-shell-bg org-ehg-pg-bg flex min-h-0 min-w-0 flex-1 flex-col"
      data-testid="ui-standards-page"
      data-exits-product={product}
      data-ui-standards-product={product}
      data-bg-option={background}
      data-org-ehg-theme={theme}
      style={{
        ["--org-ehg-bg-intensity" as string]: String(ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY),
      }}
    >
      <div className="org-ehg-bg-lab__base" aria-hidden />
      <div className="org-ehg-bg-lab__decor" aria-hidden />
      <div className="ui-standards-shell-bg__content">{children}</div>
    </div>
  );
}

