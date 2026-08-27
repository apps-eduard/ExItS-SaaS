import { WifiOff } from "lucide-react";
import { AppBootLoader } from "@/components/exits/loading/AppBootLoader";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

/**
 * Full-page OnlineRequired for Organization Web cold start / session restore.
 * Visually aligned with AppBootLoader — no endless spinner.
 */
export function OnlineRequiredBoot({
  onRetry,
  retrying = false,
  testId = "online-required-boot",
}: {
  onRetry: () => void | Promise<void>;
  retrying?: boolean;
  testId?: string;
}) {
  const { t } = useI18n();

  return (
    <div
      className={cn(
        "exits-app-boot-loader exits-online-required-boot flex min-h-[100dvh] w-full flex-1 flex-col items-center justify-center gap-5 px-6 py-16",
      )}
      data-testid={testId}
      role="status"
      aria-live="polite"
    >
      <div className="flex flex-col items-center gap-3">
        <p className="exits-app-boot-loader__brand m-0 text-[length:var(--exits-text-lg)] font-semibold tracking-tight text-foreground">
          {t("app.name")}
        </p>
        <WifiOff className="size-10 text-muted" aria-hidden />
      </div>
      <div className="max-w-sm text-center">
        <p className="m-0 text-[length:var(--exits-text-md)] font-semibold text-foreground">
          {t("connectivity.offlineTitle")}
        </p>
        <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
          {t("connectivity.orgWebRequiresInternet")}
        </p>
      </div>
      <Button
        type="button"
        onClick={() => void onRetry()}
        disabled={retrying}
        data-testid="online-required-boot-retry"
      >
        {retrying ? t("connectivity.reconnecting") : t("offline.tryAgain")}
      </Button>
    </div>
  );
}

/** Page-level OnlineRequired when server data has not been loaded. */
export function OnlineRequiredPageState({
  title,
  detail,
  onRetry,
  retrying = false,
  testId = "online-required-page",
}: {
  title: string;
  detail?: string;
  onRetry?: () => void | Promise<void>;
  retrying?: boolean;
  testId?: string;
}) {
  const { t } = useI18n();

  return (
    <div
      className="exits-online-required-page flex min-h-[12rem] flex-col items-center justify-center gap-3 px-4 py-10 text-center"
      data-testid={testId}
      role="status"
      aria-live="polite"
    >
      <WifiOff className="size-8 text-muted" aria-hidden />
      <div className="max-w-sm">
        <p className="m-0 text-[length:var(--exits-text-md)] font-semibold">{title}</p>
        <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {detail ?? t("connectivity.pageNeedsInternet")}
        </p>
      </div>
      {onRetry ? (
        <Button
          type="button"
          variant="outline"
          onClick={() => void onRetry()}
          disabled={retrying}
          data-testid="online-required-page-retry"
        >
          {retrying ? t("connectivity.reconnecting") : t("offline.tryAgain")}
        </Button>
      ) : null}
    </div>
  );
}

/** Keep AppBootLoader import available for visual parity references in tests. */
export { AppBootLoader };
