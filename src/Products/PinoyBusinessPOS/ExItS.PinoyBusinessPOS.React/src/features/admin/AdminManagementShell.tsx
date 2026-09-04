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
 * Mobile: stacked content + admin bottom nav (no desktop sidebar).
 * Tablet (md): compact left rail.
 * Desktop (lg): expanded sidebar.
 * XL: optional usage context panel when capacity APIs return data.
 */
export function AdminManagementShell({ children, header }: AdminManagementShellProps) {
  const { t } = useI18n();
  const location = useLocation();
  const { boundWorkspace } = useWorkspace();
  const showContextPanel =
    location.pathname === "/org" ||
    location.pathname === "/org/" ||
    location.pathname.startsWith("/org/manage");

  return (
    <div
      className={cn(
        "admin-shell flex min-h-[100dvh] w-full min-w-0 flex-col overflow-x-hidden",
        "px-[max(var(--exits-page-padding),env(safe-area-inset-left))] pr-[max(var(--exits-page-padding),env(safe-area-inset-right))] pt-[env(safe-area-inset-top)]",
        "pb-[max(5.5rem,calc(4.25rem+env(safe-area-inset-bottom)))] md:pb-[max(2rem,env(safe-area-inset-bottom))]",
      )}
      data-testid="admin-management-shell"
    >
      <a
        href="#main-content"
        className="sr-only z-50 rounded-[var(--exits-radius-md)] bg-primary px-3 py-2 text-primary-foreground"
      >
        {t("app.skipToContent")}
      </a>
      {header}

      <header
        className="admin-shell__header md:hidden mt-2"
        data-testid="admin-mobile-header"
      >
        <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
          {t("admin.shell.manageBusiness")}
        </p>
        <h1 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
          {boundWorkspace?.organizationDisplayName ?? t("admin.shell.productName")}
        </h1>
      </header>

      <div className="admin-shell__body mt-3 flex min-w-0 flex-1 gap-4 lg:gap-6">
        <div className="hidden md:block lg:hidden" data-testid="admin-tablet-rail">
          <AdminSidebar rail />
        </div>
        <div className="hidden lg:block" data-testid="admin-desktop-sidebar">
          <AdminSidebar />
        </div>

        <div className="admin-shell__main flex min-w-0 flex-1 flex-col gap-4">
          <main id="main-content" className="admin-shell__content min-w-0 flex-1" tabIndex={-1}>
            {children}
          </main>
        </div>

        {showContextPanel ? (
          <div className="hidden xl:block" data-testid="admin-xl-context">
            <AdminContextPanel />
          </div>
        ) : null}
      </div>

      <AdminMobileNav />
    </div>
  );
}
