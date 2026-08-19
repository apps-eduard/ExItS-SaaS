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
      className="pointer-events-none fixed inset-x-0 bottom-[max(1rem,env(safe-area-inset-bottom))] z-[1300] flex justify-center px-4"
      role="status"
      aria-live="polite"
    >
      <div className="pointer-events-auto flex max-w-sm items-center gap-3 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 shadow-sm">
        <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("pwa.updateAvailable")}
        </p>
        <Button
          type="button"
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
