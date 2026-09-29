import { useLayoutEffect, useRef, useState } from "react";
import { Navigate, Outlet, useLocation, useNavigate } from "react-router-dom";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { usePreferencesOverlay } from "@/features/preferences/PreferencesOverlay";
import {
  clearPreferencesReturnTo,
  peekPreferencesReturnTo,
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
 * Compact panel + icon-only top section nav (Appearance · Language · Navigation · Accessibility).
 *
 * When opened from an in-app page (returnTo present), bounce back to that page and open the
 * floating overlay so the current screen stays visible behind the drawer.
 * Direct deep links keep the route-mounted drawer.
 */
export function PreferencesPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const { session } = useSession();
  const { openPreferences } = usePreferencesOverlay();
  const isPersonal = sessionAccountClass(session) === "Personal";
  const fallbackCloseTo = isPersonal ? personalPageBackNav.more.to : pageBackNav.more.to;
  // Capture once on open so close always returns to the page that opened preferences.
  const closeToRef = useRef(resolvePreferencesReturnTo(location.state, fallbackCloseTo));
  const [open, setOpen] = useState(false);
  const returnTo = peekPreferencesReturnTo(location.state);
  const bouncedRef = useRef(false);

  const activeSection = parsePreferencesSection(location.pathname);

  useLayoutEffect(() => {
    if (!returnTo || bouncedRef.current) {
      return;
    }
    bouncedRef.current = true;
    openPreferences({
      section: activeSection ?? PREFERENCES_DEFAULT_SECTION,
      returnTo,
    });
    navigate(returnTo, { replace: true });
  }, [returnTo, activeSection, navigate, openPreferences]);

  useLayoutEffect(() => {
    if (returnTo) {
      return;
    }
    setOpen(true);
  }, [returnTo]);

  // /settings/preferences → Appearance (primary section)
  if (location.pathname === "/settings/preferences" || location.pathname === "/settings/preferences/") {
    return <Navigate to={preferencesSectionPath(PREFERENCES_DEFAULT_SECTION)} replace />;
  }

  if (!activeSection) {
    return <Navigate to={preferencesSectionPath(PREFERENCES_DEFAULT_SECTION)} replace />;
  }

  // In-app open: overlay host owns the drawer on the return page.
  if (returnTo) {
    return null;
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
      <div className="flex min-w-0 flex-col gap-3" data-testid="preferences-layout">
        <PreferencesSectionNav activeSection={activeSection} />
        <div className="preferences-section-content min-w-0 w-full" data-testid="preferences-section-content">
          <Outlet />
        </div>
      </div>
    </SideDrawer>
  );
}
