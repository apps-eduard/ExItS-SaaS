import { Navigate, Outlet, useParams } from "react-router-dom";
import { PageHeader } from "@/components/exits/PageHeader";
import { Skeleton } from "@/components/ui/skeleton";
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
      <section aria-busy="true" aria-label={t("settings.loading")}>
        <Skeleton className="h-8 w-56" />
        <Skeleton className="mt-3 h-12 w-full max-w-2xl" />
        <Skeleton className="mt-4 h-40 w-full max-w-3xl" />
      </section>
    );
  }

  if (!canAccess) {
    return <ShellNotFoundPage />;
  }

  return (
    <section className="grid gap-4">
      <PageHeader title={t("nav.platformSettings")} description={t("settings.description")} />
      <PlatformSettingsNav />
      <Outlet />
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
    <div className="grid gap-4">
      <div className="grid gap-1">
        <h2 className="text-[length:var(--exits-text-base)] font-semibold tracking-tight">
          {t(section.titleKey)}
        </h2>
        <p className="max-w-3xl text-[length:var(--exits-text-sm)] text-muted">
          {t(section.descriptionKey)}
        </p>
      </div>
      {section.id === "general" ? <GeneralSettingsPanel /> : null}
      {section.id === "email" ? <EmailSettingsPanel /> : null}
      {section.id === "regional" ? <RegionalSettingsPanel /> : null}
      {!section.hasBackendApi ? <SettingsCapabilityPanel section={section} /> : null}
    </div>
  );
}
