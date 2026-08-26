import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import type { SettingsSectionDefinition } from "@/features/settings/settings-sections";
import { usePreferences } from "@/hooks/use-preferences";

export function SettingsCapabilityPanel({ section }: { section: SettingsSectionDefinition }) {
  const { t } = usePreferences();

  if (section.hasBackendApi || !section.gapBodyKey || !section.backendApiGap) {
    return null;
  }

  return (
    <DashboardSection title={t("settings.capability.unavailable")}>
      <div className="grid gap-3" role="status">
        <p className="text-[length:var(--exits-text-sm)] text-muted">
          {t("settings.capability.noLiveValues")}
        </p>

        <details className="rounded-[var(--exits-density-radius)] border border-border bg-surface-muted/30 px-3 py-2">
          <summary className="cursor-pointer text-[length:var(--exits-text-sm)] font-medium text-foreground">
            {t("settings.capability.technicalDetails")}
          </summary>
          <div className="mt-2 grid gap-2 text-[length:var(--exits-text-xs)] text-muted">
            <p>{t(section.gapBodyKey)}</p>
            <p className="font-mono break-all">{section.backendApiGap}</p>
          </div>
        </details>

        {section.ownershipKeys.length > 0 ? (
          <div className="grid gap-1.5 border-t border-border pt-3">
            <h3 className="text-[length:var(--exits-text-sm)] font-medium text-foreground">
              {t("settings.ownership.title")}
            </h3>
            <ul className="list-disc space-y-1 pl-5 text-[length:var(--exits-text-xs)] text-muted">
              {section.ownershipKeys.map((key) => (
                <li key={key}>{t(key)}</li>
              ))}
            </ul>
          </div>
        ) : null}
      </div>
    </DashboardSection>
  );
}
