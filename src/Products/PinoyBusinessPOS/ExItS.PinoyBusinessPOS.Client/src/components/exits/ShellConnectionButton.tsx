import { Cloud, CloudOff } from "lucide-react";
import { useQueryClient } from "@tanstack/react-query";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { Button } from "@/components/ui/button";
import { DropdownMenu, useDismissibleOpen } from "@/components/ui/dropdown-menu";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

export type ShellConnectionButtonProps = {
  testId?: string;
  className?: string;
};

/**
 * Honest online/offline Connection control — not Connection & Sync.
 * Refresh data = query invalidation only (no outbox).
 */
export function ShellConnectionButton({
  testId = "shell-connection-button",
  className,
}: ShellConnectionButtonProps) {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const menu = useDismissibleOpen(false);

  return (
    <DropdownMenu
      align="end"
      open={menu.open}
      onOpenChange={menu.setOpen}
      menuLabel={t("shell.connection.title")}
      trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
        <button
          type="button"
          id={id}
          data-testid={testId}
          aria-label={t("shell.connection.title")}
          aria-expanded={expanded}
          aria-controls={controls}
          aria-haspopup="menu"
          onClick={onClick}
          onKeyDown={onKeyDown}
          className={cn(
            "inline-flex size-11 min-h-11 min-w-11 shrink-0 items-center justify-center rounded-[var(--exits-radius-md)] text-foreground transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
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
        className="flex min-w-[14rem] flex-col gap-3 p-3"
        data-testid={`${testId}-panel`}
        role="group"
        aria-label={t("shell.connection.title")}
      >
        <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("shell.connection.title")}
        </p>
        <div className="flex items-start gap-2">
          <span
            className={cn(
              "mt-1 inline-block size-2 shrink-0 rounded-full",
              online ? "bg-primary" : "bg-muted-foreground",
            )}
            aria-hidden
          />
          <div className="min-w-0">
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
        {online ? (
          <Button
            type="button"
            variant="ghost"
            className="min-h-11 w-full justify-start"
            data-testid={`${testId}-refresh`}
            onClick={() => {
              void queryClient.invalidateQueries();
              menu.close();
            }}
          >
            {t("shell.connection.refreshData")}
          </Button>
        ) : null}
      </div>
    </DropdownMenu>
  );
}
