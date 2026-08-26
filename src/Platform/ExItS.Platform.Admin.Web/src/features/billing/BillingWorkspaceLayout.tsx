import { NavLink, Outlet } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { PageHeader } from "@/components/exits/PageHeader";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { cn } from "@/lib/utils";
import { Skeleton } from "@/components/ui/skeleton";

const linkClass = ({ isActive }: { isActive: boolean }) =>
  cn(
    "rounded-[var(--exits-density-radius)] px-2 py-1 text-[length:var(--exits-text-sm)] font-medium",
    isActive
      ? "bg-surface-muted text-foreground"
      : "text-muted hover:bg-surface-muted/70 hover:text-foreground",
  );

export function BillingWorkspaceLayout() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const canView =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageManualPayments);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!canView) {
    return <ShellNotFoundPage />;
  }

  return (
    <section className="grid min-w-0 gap-4">
      <PageHeader
        title={t("billing.workspace.title")}
        description={t("billing.workspace.description")}
      />
      <nav aria-label={t("billing.workspace.nav")}>
        <ul className="flex flex-wrap gap-1">
          <li>
            <NavLink className={linkClass} end to="/admin/payments/overview">
              {t("billing.workspace.tab.overview")}
            </NavLink>
          </li>
          <li>
            <NavLink className={linkClass} to="/admin/payments/list">
              {t("billing.workspace.tab.payments")}
            </NavLink>
          </li>
          <li>
            <NavLink className={linkClass} to="/admin/payments/issues">
              {t("billing.workspace.tab.issues")}
            </NavLink>
          </li>
        </ul>
      </nav>
      <Outlet />
    </section>
  );
}
