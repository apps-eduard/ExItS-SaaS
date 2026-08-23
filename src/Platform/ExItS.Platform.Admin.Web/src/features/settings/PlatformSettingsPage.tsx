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
        <div className="mt-4 grid gap-4 lg:grid-cols-[minmax(13.125rem,15rem)_minmax(0,1fr)]">
          <Skeleton className="h-64 w-full" />
          <Skeleton className="h-64 w-full" />
        </div>
      </section>
    );
  }

  if (!canAccess) {
    return <ShellNotFoundPage />;
  }

  return (
    <section className="grid min-w-0 gap-4">
      <PageHeader title={t("nav.platformSettings")} description={t("settings.description")} />
      <div className="grid min-w-0 gap-4 lg:grid-cols-[minmax(13.125rem,15rem)_minmax(0,1fr)] lg:items-start lg:gap-6">
        <aside className="min-w-0 max-w-full lg:sticky lg:top-4">
          <div className="min-w-0 max-w-full lg:rounded-[var(--exits-density-radius)] lg:border lg:border-border lg:bg-surface lg:p-2">
            <PlatformSettingsNav />
          </div>
        </aside>
        <div className="min-w-0">
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
    <div className="grid min-w-0 gap-4">
      <header className="grid gap-1">
        <h2 className="text-[length:var(--exits-text-lg)] font-semibold tracking-tight text-foreground">
          {t(section.titleKey)}
        </h2>
        <p className="max-w-3xl text-[length:var(--exits-text-sm)] text-muted">
          {t(section.descriptionKey)}
        </p>
      </header>
      {section.id === "general" ? <GeneralSettingsPanel /> : null}
      {section.id === "email" ? <EmailSettingsPanel /> : null}
      {section.id === "regional" ? <RegionalSettingsPanel /> : null}
      {!section.hasBackendApi ? <SettingsCapabilityPanel section={section} /> : null}
    </div>
  );
}
