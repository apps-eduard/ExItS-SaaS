import { useEffect, useRef, useState } from "react";
import { Navigate, Outlet, useLocation, useNavigate } from "react-router-dom";
import { SideDrawer } from "@/components/exits/SideDrawer";
import {
  clearPreferencesReturnTo,
  resolvePreferencesReturnTo,
} from "@/features/preferences/preferences-return";
import { PreferencesSectionNav } from "@/features/preferences/PreferencesSectionNav";
import {
  parsePreferencesSection,
  PREFERENCES_DEFAULT_SECTION,
  preferencesSectionPath,
} from "@/features/preferences/preferences-sections";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav, personalPageBackNav } from "@/navigation/page-back-nav";
import { sessionAccountClass } from "@/session/account-class";
import { useSession } from "@/session/SessionProvider";

/**
 * Preferences shell — personal UI experience (not organization Settings).
 * Sections: Appearance · Language & Region · Navigation · Accessibility
 */
export function PreferencesPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const { session } = useSession();
  const isPersonal = sessionAccountClass(session) === "Personal";
  const fallbackCloseTo = isPersonal ? personalPageBackNav.more.to : pageBackNav.more.to;
  // Capture once on open so close always returns to the page that opened preferences.
  const closeToRef = useRef(resolvePreferencesReturnTo(location.state, fallbackCloseTo));
  const [open, setOpen] = useState(false);

  const activeSection = parsePreferencesSection(location.pathname);

  useEffect(() => {
    setOpen(true);
  }, []);

  // /settings/preferences → Appearance (primary section)
  if (location.pathname === "/settings/preferences" || location.pathname === "/settings/preferences/") {
    return <Navigate to={preferencesSectionPath(PREFERENCES_DEFAULT_SECTION)} replace />;
  }

  if (!activeSection) {
    return <Navigate to={preferencesSectionPath(PREFERENCES_DEFAULT_SECTION)} replace />;
  }

  return (
    <SideDrawer
      open={open}
      onClose={() => setOpen(false)}
      onExited={() => {
        clearPreferencesReturnTo();
        navigate(closeToRef.current, { replace: true });
      }}
      title={t("preferences.title")}
      description={t("preferences.lede")}
      testId="preferences-drawer"
      closeLabel={t("preferences.close")}
      closeTestId="preferences-close"
      panelClassName="exits-side-drawer__panel--preferences"
    >
      <div
        className="flex min-w-0 flex-col gap-3 sm:flex-row sm:items-start sm:gap-4"
        data-testid="preferences-layout"
      >
        <div className="min-w-0 sm:w-[11.5rem] sm:shrink-0 sm:border-e sm:border-border sm:pe-3">
          <PreferencesSectionNav activeSection={activeSection} />
        </div>
        <div className="min-w-0 flex-1" data-testid="preferences-section-content">
          <Outlet />
        </div>
      </div>
    </SideDrawer>
  );
}
