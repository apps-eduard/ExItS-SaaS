import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { DensityControl } from "@/components/exits/DensityControl";
import { LanguageControl } from "@/components/exits/LanguageControl";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav, personalPageBackNav } from "@/navigation/page-back-nav";
import { sessionAccountClass } from "@/session/account-class";
import { useSession } from "@/session/SessionProvider";

export function PreferencesPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { session } = useSession();
  const isPersonal = sessionAccountClass(session) === "Personal";
  const closeTo = isPersonal ? personalPageBackNav.more.to : pageBackNav.more.to;
  const [open, setOpen] = useState(false);

  useEffect(() => {
    setOpen(true);
  }, []);

  return (
    <SideDrawer
      open={open}
      onClose={() => setOpen(false)}
      onExited={() => {
        navigate(closeTo, { replace: true });
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
