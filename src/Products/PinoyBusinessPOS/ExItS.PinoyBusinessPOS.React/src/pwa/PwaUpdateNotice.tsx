import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";

export function PwaUpdateNotice({
  visible,
  onRefresh,
}: {
  visible: boolean;
  onRefresh: () => void;
  guard?: () => boolean;
}) {
  const { t } = useI18n();

  if (!visible) {
    return null;
  }

  return (
    <div
      className="fixed inset-0 z-[2000] flex items-center justify-center bg-black/60 p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="pwa-update-title"
      data-testid="pwa-update-dialog"
    >
      <div className="w-full max-w-lg rounded-[var(--exits-radius-md)] border border-border bg-surface p-8 text-center shadow-lg">
        <h2 id="pwa-update-title" className="m-0 text-[length:var(--exits-text-xl)] font-semibold">
          {t("pwa.updateTitle")}
        </h2>
        <p className="mb-0 mt-4 text-[length:var(--exits-text-base)] text-muted">
          {t("pwa.updateBody")}
        </p>
        <Button
          type="button"
          className="mt-6 w-full"
          onClick={onRefresh}
        >
          {t("pwa.refresh")}
        </Button>
      </div>
    </div>
  );
}
