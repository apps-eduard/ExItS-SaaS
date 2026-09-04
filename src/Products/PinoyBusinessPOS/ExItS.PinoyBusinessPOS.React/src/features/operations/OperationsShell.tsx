import type { ReactNode } from "react";
import { OperationsBottomNav } from "@/features/operations/OperationsBottomNav";
import { OperationsSidebar } from "@/features/operations/OperationsSidebar";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { isWarehouseBranch } from "@/features/branches/branch-type";

type OperationsShellProps = {
  children: ReactNode;
  header?: ReactNode;
  /** Sell floor uses near-fullscreen content width. */
  sellFloor?: boolean;
  /** Hide bottom nav (e.g. sell transaction chrome). */
  hideBottomNav?: boolean;
};

/**
 * Manager / Operations responsive shell.
 * <1024: bottom nav (Retail or Warehouse).
 * >=1024: persistent left sidebar (no tablet rail).
 * Completely separate IA from AdminManagementShell.
 */
export function OperationsShell({
  children,
  header,
  sellFloor = false,
  hideBottomNav = false,
}: OperationsShellProps) {
  const { t } = useI18n();
  const { boundWorkspace } = useWorkspace();
  const warehouse = isWarehouseBranch(boundWorkspace?.branchType);

  return (
    <div
      className={cn(
        "operations-shell flex min-h-[100dvh] w-full min-w-0 flex-col overflow-x-hidden",
        "px-[max(var(--exits-page-padding),env(safe-area-inset-left))] pr-[max(var(--exits-page-padding),env(safe-area-inset-right))] pt-[env(safe-area-inset-top)]",
        hideBottomNav
          ? "pb-[max(2rem,env(safe-area-inset-bottom))]"
          : "pb-[max(5.5rem,calc(4.25rem+env(safe-area-inset-bottom)))] lg:pb-[max(2rem,env(safe-area-inset-bottom))]",
        sellFloor && "operations-shell--sell-floor",
      )}
      data-testid="operations-shell"
      data-branch-kind={warehouse ? "warehouse" : "retail"}
    >
      <a
        href="#main-content"
        className="sr-only z-50 rounded-[var(--exits-radius-md)] bg-primary px-3 py-2 text-primary-foreground"
      >
        {t("app.skipToContent")}
      </a>
      {header}

      {!sellFloor ? (
        <header className="operations-shell__header lg:hidden mt-2" data-testid="operations-mobile-header">
          <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
            {t("operations.shell.operations")}
          </p>
          <h1 className="m-0 truncate text-[length:var(--exits-text-lg)] font-semibold">
            {boundWorkspace?.organizationDisplayName ?? t("operations.shell.productName")}
            {boundWorkspace?.branchName ? (
              <span className="font-medium text-muted">
                {" "}
                · {boundWorkspace.branchName}
              </span>
            ) : null}
          </h1>
        </header>
      ) : null}

      <div
        className={cn(
          "operations-shell__body flex min-h-0 min-w-0 flex-1 gap-4 lg:gap-6",
          sellFloor ? "mt-2" : "mt-3",
        )}
      >
        <div className="hidden min-h-0 lg:block" data-testid="operations-desktop-sidebar">
          <OperationsSidebar />
        </div>

        <div
          className={cn(
            "operations-shell__main flex min-h-0 min-w-0 flex-1 flex-col",
            sellFloor ? "gap-0" : "gap-4",
          )}
        >
          <main
            id="main-content"
            className={cn(
              "operations-shell__content min-h-0 min-w-0 flex-1",
              !sellFloor && "pt-1",
            )}
            tabIndex={-1}
          >
            {children}
          </main>
        </div>
      </div>

      {!hideBottomNav ? <OperationsBottomNav /> : null}
    </div>
  );
}
