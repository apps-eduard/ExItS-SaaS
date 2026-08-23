import { Navigate, Outlet, useParams } from "react-router-dom";
import { PageHeader } from "@/components/exits/PageHeader";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { EmailSettingsPanel } from "@/features/settings/EmailSettingsPanel";
import { GeneralSettingsPanel } from "@/features/settings/GeneralSettingsPanel";
import { PlatformSettingsNav } from "@/features/settings/PlatformSettingsNav";
import { RegionalSettingsPanel } from "@/features/settings/RegionalSettingsPanel";
import { SettingsCapabilityPanel } from "@/features/settings/SettingsCapabilityPanel";
import {
  PLATFORM_SETTINGS_BASE_PATH,
  findSettingsSection,
  settingsSectionHref,
  SETTINGS_SECTIONS,
} from "@/features/settings/settings-sections";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";

export function PlatformSettingsLayout() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const canAccess =
    authorization.status === "loaded" && authorization.isPlatformAdministrator;

  if (authorization.status === "loading") {
    return (
      <section
        aria-busy="true"
        aria-label={t("settings.loading")}
        className="grid max-w-5xl gap-4"
        role="status"
      >
        <DashboardWidgetSkeleton rows={8} />
      </section>
    );
  }

  if (!canAccess) {
    return <ShellNotFoundPage />;
  }

  return (
    <section className="grid min-w-0 max-w-5xl gap-4">
      <PageHeader title={t("nav.platformSettings")} description={t("settings.description")} />
      <div className="min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface">
        <PlatformSettingsNav />
        <div className="p-4">
          <Outlet />
        </div>
      </div>
    </section>
  );
}

export function PlatformSettingsIndexRedirect() {
  return <Navigate to={settingsSectionHref(SETTINGS_SECTIONS[0]!)} replace />;
}

export function PlatformSettingsSectionPage() {
  const { t } = usePreferences();
  const params = useParams();
  const section = findSettingsSection(params.section);

  if (!section) {
    return <Navigate to={`${PLATFORM_SETTINGS_BASE_PATH}/general`} replace />;
  }

  return (
    <div className="grid min-w-0 gap-3" role="tabpanel">
      <p className="text-[length:var(--exits-text-sm)] text-muted">{t(section.descriptionKey)}</p>
      {section.id === "general" ? <GeneralSettingsPanel /> : null}
      {section.id === "email" ? <EmailSettingsPanel /> : null}
      {section.id === "regional" ? <RegionalSettingsPanel /> : null}
      {!section.hasBackendApi ? <SettingsCapabilityPanel section={section} /> : null}
    </div>
  );
}
