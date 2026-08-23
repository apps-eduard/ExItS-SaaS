import type { SettingsSectionDefinition } from "@/features/settings/settings-sections";
import { usePreferences } from "@/hooks/use-preferences";

export function SettingsCapabilityPanel({ section }: { section: SettingsSectionDefinition }) {
  const { t } = usePreferences();

  if (section.hasBackendApi || !section.gapBodyKey || !section.backendApiGap) {
    return null;
  }

  return (
    <article
      className="grid gap-4 rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-4"
      aria-labelledby={`settings-section-${section.id}-title`}
    >
      <div className="grid gap-1">
        <div className="flex flex-wrap items-center gap-2">
          <h2
            id={`settings-section-${section.id}-title`}
            className="text-[length:var(--exits-text-base)] font-semibold tracking-tight"
          >
            {t(section.titleKey)}
          </h2>
          <span className="rounded-full border border-border bg-surface-muted px-2 py-0.5 text-[length:var(--exits-text-xs)] text-muted">
            {t("settings.capability.unavailable")}
          </span>
        </div>
        <p className="text-[length:var(--exits-text-sm)] text-muted">{t(section.descriptionKey)}</p>
      </div>

      <div
        className="rounded-[var(--exits-density-radius)] border border-dashed border-border bg-surface-muted/40 px-3 py-3"
        role="status"
      >
        <p className="text-[length:var(--exits-text-sm)] font-medium text-foreground">
          {t("settings.capability.noLiveValues")}
        </p>
        <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">{t(section.gapBodyKey)}</p>
        <p className="mt-2 font-mono text-[length:var(--exits-text-xs)] text-muted break-all">
          {section.backendApiGap}
        </p>
      </div>

      {section.ownershipKeys.length > 0 ? (
        <div className="grid gap-1">
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
    </article>
  );
}