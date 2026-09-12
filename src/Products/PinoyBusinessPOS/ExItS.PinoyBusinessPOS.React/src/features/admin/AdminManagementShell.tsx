import type { ReactNode } from "react";
import { useLocation } from "react-router-dom";
import { AdminContextPanel } from "@/features/admin/AdminContextPanel";
import { AdminMobileNav } from "@/features/admin/AdminMobileNav";
import { AdminSidebar } from "@/features/admin/AdminSidebar";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type AdminManagementShellProps = {
  children: ReactNode;
  header?: ReactNode;
};

/**
 * Responsive Manage Business shell.
 * Below lg: stacked content + Admin bottom nav (Home / Manage / Review / More).
 * lg+: full-height sidebar + content column (topbar + main). Reveal overlays content.
 */
export function AdminManagementShell({ children, header }: AdminManagementShellProps) {
  const { t } = useI18n();
  const location = useLocation();
  const { boundWorkspace } = useWorkspace();
  const showContextPanel = location.pathname.startsWith("/org/manage");

  return (
    <div
      className={cn(
        "admin-shell flex h-[100dvh] max-h-[100dvh] w-full min-w-0 flex-col overflow-hidden",
        "px-[max(var(--exits-page-padding),env(safe-area-inset-left))] pr-[max(var(--exits-page-padding),env(safe-area-inset-right))] pt-[env(safe-area-inset-top)]",
        "pb-[max(5.5rem,calc(4.25rem+env(safe-area-inset-bottom)))] lg:pb-0",
        "lg:flex-row lg:gap-0 lg:px-0 lg:pt-0",
      )}
      data-testid="admin-management-shell"
    >
      <a
        href="#main-content"
        className="sr-only z-50 rounded-[var(--exits-radius-md)] bg-primary px-3 py-2 text-primary-foreground"
      >
        {t("app.skipToContent")}
      </a>

      <div
        className="admin-sidebar-rail hidden min-h-0 lg:block"
        data-testid="admin-desktop-sidebar"
      >
        <AdminSidebar />
      </div>

      <div
        className={cn(
          "admin-shell__column flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden",
          /* No padding-inline-start: sidebar border-inline-end is the only divider (no shell gutter). */
          "lg:pe-[max(var(--exits-page-padding),env(safe-area-inset-right))]",
        )}
      >
        {header}

        <header
          className="admin-shell__header shrink-0 lg:hidden mt-2"
          data-testid="admin-mobile-header"
        >
          <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
            {t("admin.shell.manageBusiness")}
          </p>
          <h1 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
            {boundWorkspace?.organizationDisplayName ?? t("admin.shell.productName")}
          </h1>
        </header>

        <div className="admin-shell__body mt-2 flex min-h-0 min-w-0 flex-1 gap-3 overflow-hidden lg:mt-0 lg:gap-3">
          <div className="admin-shell__main flex min-h-0 min-w-0 flex-1 flex-col gap-0 overflow-hidden">
            <main
              id="main-content"
              className="admin-shell__content min-h-0 min-w-0 flex-1 overflow-x-hidden overflow-y-auto overscroll-y-contain"
              tabIndex={-1}
            >
              {children}
            </main>
          </div>

          {showContextPanel ? (
            <div className="hidden min-h-0 xl:block" data-testid="admin-xl-context">
              <AdminContextPanel />
            </div>
          ) : null}
        </div>
      </div>

      <AdminMobileNav />
    </div>
  );
}
