import {
  AlertTriangle,
  CheckCircle2,
  Cloud,
  CloudOff,
  RefreshCw,
  RotateCcw,
} from "lucide-react";
import { useQueryClient } from "@tanstack/react-query";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { Button } from "@/components/ui/button";
import { DropdownMenu, useDismissibleOpen } from "@/components/ui/dropdown-menu";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { describeSyncSummary, useOfflineSync } from "@/offline/OfflineSyncProvider";
import { isFullySynced } from "@/offline/types";

export type ShellConnectionButtonProps = {
  testId?: string;
  className?: string;
};

/**
 * Connection & Sync — status from browser connectivity + real outbox counts.
 * Never claims sync without LocalStore outbox evidence.
 */
export function ShellConnectionButton({
  testId = "shell-connection-button",
  className,
}: ShellConnectionButtonProps) {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const menu = useDismissibleOpen(false);
  const { counts, lastSuccessfulSyncAt, refreshCounts, retrySync } = useOfflineSync();
  const summary = describeSyncSummary(counts);
  const title = t("shell.connectionSync.title");
  const synced = summary.kind === "synced";

  let syncLabel = t("shell.connectionSync.allSynced");
  if (summary.kind === "access") {
    syncLabel = t("shell.connectionSync.accessRequired");
  } else if (summary.kind === "attention") {
    syncLabel = t("shell.connectionSync.needsAttention").replace(
      "{count}",
      String(summary.attention),
    );
  } else if (summary.kind === "syncing") {
    syncLabel = t("shell.connectionSync.syncing");
  } else if (summary.kind === "waiting") {
    syncLabel = online
      ? t("shell.connectionSync.waiting").replace("{count}", String(summary.waiting))
      : t("shell.connectionSync.offlineWaiting").replace("{count}", String(summary.waiting));
  }

  const showRetry =
    summary.kind === "waiting" ||
    summary.kind === "attention" ||
    summary.kind === "access" ||
    counts.syncing > 0;

  const SyncIcon =
    summary.kind === "attention" || summary.kind === "access"
      ? AlertTriangle
      : summary.kind === "waiting" || summary.kind === "syncing"
        ? RefreshCw
        : CheckCircle2;

  return (
    <DropdownMenu
      align="end"
      open={menu.open}
      onOpenChange={(open) => {
        menu.setOpen(open);
        if (open) {
          void refreshCounts();
        }
      }}
      menuLabel={title}
      trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
        <button
          type="button"
          id={id}
          data-testid={testId}
          aria-label={title}
          aria-expanded={expanded}
          aria-controls={controls}
          aria-haspopup="menu"
          onClick={onClick}
          onKeyDown={onKeyDown}
          className={cn(
            "shell-connection-trigger inline-flex size-11 min-w-11 shrink-0 items-center justify-center rounded-full transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
            online ? "shell-connection-trigger--online" : "shell-connection-trigger--offline",
            className,
          )}
        >
          {online ? (
            <Cloud className="size-5" aria-hidden data-testid={`${testId}-online-icon`} />
          ) : (
            <CloudOff className="size-5" aria-hidden data-testid={`${testId}-offline-icon`} />
          )}
        </button>
      )}
    >
      <div
        className="shell-connection-panel flex min-w-[16rem] max-w-[20rem] flex-col p-3"
        data-testid={`${testId}-panel`}
        role="group"
        aria-label={title}
      >
        <p className="shell-connection-panel__title m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {title}
        </p>

        <div className="shell-connection-panel__divider" role="separator" />

        <div className="shell-connection-panel__row">
          <span
            className={cn(
              "shell-connection-panel__icon",
              online
                ? "shell-connection-panel__icon--online"
                : "shell-connection-panel__icon--offline",
            )}
            aria-hidden
          >
            {online ? <Cloud className="size-4" /> : <CloudOff className="size-4" />}
          </span>
          <div className="min-w-0 flex-1">
            <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
              {t("shell.connectionSync.connectionSection")}
            </p>
            <p
              className="m-0 text-[length:var(--exits-text-sm)] font-medium"
              data-testid={`${testId}-status`}
            >
              {online ? t("shell.connection.online") : t("shell.connection.offline")}
            </p>
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
              {online ? t("shell.connection.onlineDetail") : t("shell.connection.offlineDetail")}
            </p>
          </div>
        </div>

        <div className="shell-connection-panel__divider" role="separator" />

        <div className="shell-connection-panel__row" data-testid={`${testId}-sync`}>
          <span
            className={cn(
              "shell-connection-panel__icon",
              synced
                ? "shell-connection-panel__icon--synced"
                : summary.kind === "attention" || summary.kind === "access"
                  ? "shell-connection-panel__icon--attention"
                  : "shell-connection-panel__icon--pending",
            )}
            aria-hidden
          >
            <SyncIcon
              className={cn(
                "size-4",
                (summary.kind === "syncing" || summary.kind === "waiting") &&
                  online &&
                  "animate-spin",
              )}
            />
          </span>
          <div className="min-w-0 flex-1">
            <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
              {t("shell.connectionSync.syncSection")}
            </p>
            <p
              className="m-0 text-[length:var(--exits-text-sm)] font-medium"
              data-testid={`${testId}-sync-status`}
            >
              {syncLabel}
            </p>
            {lastSuccessfulSyncAt && isFullySynced(counts) ? (
              <p
                className="m-0 text-[length:var(--exits-text-xs)] text-muted"
                data-testid={`${testId}-last-synced`}
              >
                {t("shell.connectionSync.lastSynced").replace(
                  "{time}",
                  new Date(lastSuccessfulSyncAt).toLocaleString(),
                )}
              </p>
            ) : null}
          </div>
        </div>

        {online || showRetry ? (
          <>
            <div className="shell-connection-panel__divider" role="separator" />
            <div className="flex flex-col gap-1">
              {online ? (
                <Button
                  type="button"
                  variant="ghost"
                  className="shell-connection-panel__action w-full justify-start gap-2"
                  data-testid={`${testId}-refresh`}
                  onClick={() => {
                    void queryClient.invalidateQueries();
                    void refreshCounts();
                    menu.close();
                  }}
                >
                  <RefreshCw className="size-4 shrink-0" aria-hidden />
                  {t("shell.connectionSync.refreshFromServer")}
                </Button>
              ) : null}
              {showRetry ? (
                <Button
                  type="button"
                  variant="ghost"
                  className="shell-connection-panel__action w-full justify-start gap-2"
                  data-testid={`${testId}-retry-sync`}
                  onClick={() => {
                    void retrySync();
                    menu.close();
                  }}
                >
                  <RotateCcw className="size-4 shrink-0" aria-hidden />
                  {t("shell.connectionSync.retrySync")}
                </Button>
              ) : null}
            </div>
          </>
        ) : null}
      </div>
    </DropdownMenu>
  );
}
