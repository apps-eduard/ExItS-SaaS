import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { DensityControl } from "@/components/exits/DensityControl";
import { LanguageControl } from "@/components/exits/LanguageControl";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { ThemeControl } from "@/components/exits/ThemeControl";
import {
  clearPreferencesReturnTo,
  resolvePreferencesReturnTo,
} from "@/features/preferences/preferences-return";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav, personalPageBackNav } from "@/navigation/page-back-nav";
import { sessionAccountClass } from "@/session/account-class";
import { useSession } from "@/session/SessionProvider";

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

  useEffect(() => {
    setOpen(true);
  }, []);

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
      <section
        className="rounded-[var(--exits-radius-md)] border border-border bg-surface"
        aria-labelledby="preferences-appearance"
      >
        <div className="border-b border-border px-4 py-3.5">
          <h3
            id="preferences-appearance"
            className="m-0 text-[length:var(--exits-text-md)] font-semibold tracking-tight text-foreground"
          >
            {t("preferences.appearance")}
          </h3>
        </div>
        <div className="divide-y divide-border px-4">
          <LanguageControl />
          <ThemeControl />
          <DensityControl />
        </div>
      </section>
    </SideDrawer>
  );
}
