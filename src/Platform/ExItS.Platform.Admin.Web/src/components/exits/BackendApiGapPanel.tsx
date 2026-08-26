import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { usePreferences } from "@/hooks/use-preferences";
import type { MessageKey } from "@/lib/i18n/messages";

type BackendApiGapPanelProps = {
  bodyKey: MessageKey;
  gapCode: string;
  titleKey?: MessageKey;
};

export function BackendApiGapPanel({
  bodyKey,
  gapCode,
  titleKey = "settings.capability.unavailable",
}: BackendApiGapPanelProps) {
  const { t } = usePreferences();

  return (
    <DashboardSection title={t(titleKey)}>
      <div className="grid gap-3" role="status">
        <p className="text-[length:var(--exits-text-sm)] text-muted">
          {t("settings.capability.noLiveValues")}
        </p>

        <details className="rounded-[var(--exits-density-radius)] border border-border bg-surface-muted/30 px-3 py-2">
          <summary className="cursor-pointer text-[length:var(--exits-text-sm)] font-medium text-foreground">
            {t("settings.capability.technicalDetails")}
          </summary>
          <div className="mt-2 grid gap-2 text-[length:var(--exits-text-xs)] text-muted">
            <p>{t(bodyKey)}</p>
            <p className="font-mono break-all">{gapCode}</p>
          </div>
        </details>
      </div>
    </DashboardSection>
  );
}
