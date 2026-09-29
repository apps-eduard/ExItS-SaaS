import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { AccessibilityPreferences } from "@/features/preferences/AccessibilityPreferences";
import { AppearancePreferences } from "@/features/preferences/AppearancePreferences";
import { LanguageRegionPreferences } from "@/features/preferences/LanguageRegionPreferences";
import { NavigationPreferences } from "@/features/preferences/NavigationPreferences";
import { PreferencesSectionNav } from "@/features/preferences/PreferencesSectionNav";
import {
  clearPreferencesReturnTo,
  rememberPreferencesReturnTo,
} from "@/features/preferences/preferences-return";
import {
  PREFERENCES_DEFAULT_SECTION,
  type PreferencesSectionId,
} from "@/features/preferences/preferences-sections";
import { useI18n } from "@/i18n/I18nProvider";

type PreferencesOverlayContextValue = {
  open: boolean;
  section: PreferencesSectionId;
  openPreferences: (options?: {
    section?: PreferencesSectionId;
    returnTo?: string;
  }) => void;
  setSection: (section: PreferencesSectionId) => void;
  closePreferences: () => void;
};

const PreferencesOverlayContext = createContext<PreferencesOverlayContextValue | null>(null);

export function PreferencesOverlayProvider({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const [section, setSection] = useState<PreferencesSectionId>(PREFERENCES_DEFAULT_SECTION);

  const openPreferences = useCallback(
    (options?: { section?: PreferencesSectionId; returnTo?: string }) => {
      if (options?.returnTo) {
        rememberPreferencesReturnTo(options.returnTo);
      }
      setSection(options?.section ?? PREFERENCES_DEFAULT_SECTION);
      setOpen(true);
    },
    [],
  );

  const closePreferences = useCallback(() => {
    setOpen(false);
    clearPreferencesReturnTo();
  }, []);

  const value = useMemo(
    () => ({
      open,
      section,
      openPreferences,
      setSection,
      closePreferences,
    }),
    [open, section, openPreferences, closePreferences],
  );

  return (
    <PreferencesOverlayContext.Provider value={value}>{children}</PreferencesOverlayContext.Provider>
  );
}

export function usePreferencesOverlay(): PreferencesOverlayContextValue {
  const value = useContext(PreferencesOverlayContext);
  if (!value) {
    throw new Error("usePreferencesOverlay must be used within PreferencesOverlayProvider");
  }
  return value;
}

function PreferencesOverlaySection({ section }: { section: PreferencesSectionId }) {
  switch (section) {
    case "appearance":
      return <AppearancePreferences />;
    case "language-region":
      return <LanguageRegionPreferences />;
    case "navigation":
      return <NavigationPreferences />;
    case "accessibility":
      return <AccessibilityPreferences />;
    default:
      return <AppearancePreferences />;
  }
}

/** Floating preferences drawer that overlays the current page (does not replace the route). */
export function PreferencesOverlayHost() {
  const { t } = useI18n();
  const { open, section, setSection, closePreferences } = usePreferencesOverlay();

  return (
    <SideDrawer
      open={open}
      onClose={closePreferences}
      title={t("preferences.title")}
      description={t("preferences.lede")}
      testId="preferences-drawer"
      closeLabel={t("preferences.close")}
      closeTestId="preferences-close"
      panelClassName="exits-side-drawer__panel--preferences"
    >
      <div className="flex min-w-0 flex-col gap-3" data-testid="preferences-layout">
        <PreferencesSectionNav
          activeSection={section}
          mode="overlay"
          onSectionSelect={setSection}
        />
        <div
          className="preferences-section-content min-w-0 w-full"
          data-testid="preferences-section-content"
        >
          <PreferencesOverlaySection section={section} />
        </div>
      </div>
    </SideDrawer>
  );
}
