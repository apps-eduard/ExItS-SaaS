import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import { applyPwaUpdateIfAllowed, type PwaUpdateApplyGuard } from "@/pwa/apply-pwa-update";

export function PwaUpdateNotice({
  visible,
  onRefresh,
  guard,
}: {
  visible: boolean;
  onRefresh: () => void;
  guard?: PwaUpdateApplyGuard;
}) {
  const { t } = useI18n();

  if (!visible) {
    return null;
  }

  return (
    <div
      className="pointer-events-none fixed inset-x-0 bottom-[calc(var(--exits-bottom-nav-height)+env(safe-area-inset-bottom)+0.5rem)] z-[var(--exits-z-notice)] flex justify-center px-[var(--exits-page-padding)] lg:bottom-[max(1rem,env(safe-area-inset-bottom))]"
      role="status"
      aria-live="polite"
    >
      <div className="pointer-events-auto flex max-w-sm items-center gap-3 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 shadow-[var(--exits-shadow-md)]">
        <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("pwa.updateAvailable")}
        </p>
        <Button
          type="button"
          size="default"
          onClick={() => {
            applyPwaUpdateIfAllowed(onRefresh, guard);
          }}
        >
          {t("pwa.refresh")}
        </Button>
      </div>
    </div>
  );
}
