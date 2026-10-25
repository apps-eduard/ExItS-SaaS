import { Notice } from "@/components/exits/Notice";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";

type Props = {
  visible: boolean;
  onReload: () => void;
  onDismiss?: () => void;
  testId?: string;
};

/**
 * Soft Glass-compatible notice when remote settings change while the local form is dirty.
 * Does not overwrite draft fields — caller must reload deliberately.
 */
export function RealtimeDraftConflictNotice({
  visible,
  onReload,
  onDismiss,
  testId = "realtime-draft-conflict",
}: Props) {
  const { t } = useI18n();
  if (!visible) {
    return null;
  }

  return (
    <Notice tone="warning" testId={testId}>
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <p className="m-0 text-sm">
          {t("realtime.settingsChangedElsewhere")}
        </p>
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            intent="primary"
            appearance="glossy"
            onClick={onReload}
            data-testid={`${testId}-reload`}
          >
            {t("realtime.reloadSettings")}
          </Button>
          {onDismiss ? (
            <Button
              type="button"
              intent="neutral"
              appearance="outline"
              onClick={onDismiss}
              data-testid={`${testId}-keep`}
            >
              {t("realtime.keepEditing")}
            </Button>
          ) : null}
        </div>
      </div>
    </Notice>
  );
}
