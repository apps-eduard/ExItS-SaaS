import { useEffect, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";

export function PwaUpdateNotice({
  visible,
  onRefresh,
}: {
  visible: boolean;
  onRefresh: () => void | Promise<void>;
  guard?: () => boolean;
}) {
  const { t } = useI18n();
  const [progress, setProgress] = useState<number | null>(null);
  const running = useRef(false);
  const loading = progress !== null && progress < 100;

  useEffect(() => {
    if (!loading) {
      return;
    }
    const timer = window.setInterval(() => {
      setProgress((current) => {
        if (current === null || current >= 92) {
          return current;
        }
        const step = current < 30 ? 8 : current < 60 ? 5 : 2;
        return Math.min(92, current + step);
      });
    }, 200);
    return () => window.clearInterval(timer);
  }, [loading]);

  async function refresh() {
    if (running.current) {
      return;
    }
    running.current = true;
    setProgress(4);
    try {
      await Promise.race([
        Promise.resolve(onRefresh()),
        new Promise<void>((resolve) => {
          window.setTimeout(resolve, 8000);
        }),
      ]);
    } finally {
      setProgress(100);
      window.setTimeout(() => {
        try {
          window.location.reload();
        } catch {
          // Some test environments do not implement navigation.
        }
      }, 150);
    }
  }

  if (!visible) {
    return null;
  }

  const loadingLabel = progress === null
    ? ""
    : t("pwa.updateLoading").replace("{percent}", String(progress));

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
          {progress === null ? t("pwa.updateBody") : loadingLabel}
        </p>
        {progress !== null ? (
          <div
            className="mt-4 h-2 overflow-hidden rounded-full bg-[var(--exits-surface-muted)]"
            role="progressbar"
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={progress}
            aria-label={loadingLabel}
            data-testid="pwa-update-progress"
          >
            <div
              className="h-full bg-[var(--exits-primary)] transition-[width] duration-200"
              style={{ width: `${progress}%` }}
            />
          </div>
        ) : null}
        <Button
          type="button"
          className="mt-6 w-full"
          disabled={progress !== null}
          onClick={() => refresh()}
        >
          {progress === null ? t("pwa.refresh") : loadingLabel}
        </Button>
      </div>
    </div>
  );
}
