import type { SettingsSectionDefinition } from "@/features/settings/settings-sections";
import { SettingsSectionCard } from "@/features/settings/SettingsFormShell";
import { usePreferences } from "@/hooks/use-preferences";

export function SettingsCapabilityPanel({ section }: { section: SettingsSectionDefinition }) {
  const { t } = usePreferences();

  if (section.hasBackendApi || !section.gapBodyKey || !section.backendApiGap) {
    return null;
  }

  return (
    <SettingsSectionCard>
      <div className="grid gap-3" role="status">
        <div className="grid gap-2">
          <h3 className="text-[length:var(--exits-text-base)] font-semibold text-foreground">
            {t("settings.capability.unavailable")}
          </h3>
          <p className="max-w-2xl text-[length:var(--exits-text-sm)] text-muted">
            {t("settings.capability.noLiveValues")}
          </p>
        </div>

        <details className="rounded-[var(--exits-density-radius)] border border-border bg-surface-muted/40 px-3 py-2">
          <summary className="cursor-pointer text-[length:var(--exits-text-sm)] font-medium text-foreground">
            {t("settings.capability.technicalDetails")}
          </summary>
          <div className="mt-2 grid gap-2 text-[length:var(--exits-text-sm)] text-muted">
            <p>{t(section.gapBodyKey)}</p>
            <p className="font-mono text-[length:var(--exits-text-xs)] break-all text-muted">
              {section.backendApiGap}
            </p>
          </div>
        </details>
      </div>

      {section.ownershipKeys.length > 0 ? (
        <div className="grid gap-2 border-t border-border pt-4">
          <h3 className="text-[length:var(--exits-text-sm)] font-medium text-foreground">
            {t("settings.ownership.title")}
          </h3>
          <ul className="list-disc space-y-1 pl-5 text-[length:var(--exits-text-sm)] text-muted">
            {section.ownershipKeys.map((key) => (
              <li key={key}>{t(key)}</li>
            ))}
          </ul>
        </div>
      ) : null}
    </SettingsSectionCard>
  );
}
