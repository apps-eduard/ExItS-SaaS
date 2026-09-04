import { NavLink, useLocation } from "react-router-dom";
import { BarChart3, Home, LayoutGrid, MoreHorizontal } from "lucide-react";
import {
  buildAdminMobileTabs,
  matchAdminMobileTab,
  type AdminMobileTabId,
} from "@/features/admin/admin-nav-config";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const ICONS: Record<AdminMobileTabId, typeof Home> = {
  home: Home,
  manage: LayoutGrid,
  review: BarChart3,
  more: MoreHorizontal,
};

export function AdminMobileNav() {
  const { t } = useI18n();
  const location = useLocation();
  const { sessionGrant } = useWorkspace();
  const tabs = buildAdminMobileTabs(sessionGrant);
  const activeId = matchAdminMobileTab(location.pathname, tabs);

  if (tabs.length === 0) {
    return null;
  }

  return (
    <nav
      data-testid="admin-mobile-nav"
      aria-label={t("admin.mobile.aria")}
      className="admin-mobile-nav lg:hidden fixed inset-x-0 bottom-0 z-50 border-t border-border bg-surface pb-[env(safe-area-inset-bottom)]"
    >
      <ul className="mx-auto flex w-full max-w-lg items-stretch justify-between gap-0.5 px-2 pt-1">
        {tabs.map((tab) => {
          const Icon = ICONS[tab.id];
          const isActive = activeId === tab.id;
          return (
            <li key={tab.id} className="min-w-0 flex-1">
              <NavLink
                to={tab.to}
                end={tab.end}
                data-testid={tab.testId}
                aria-current={isActive ? "page" : undefined}
                className={cn(
                  "flex min-h-11 flex-col items-center justify-center gap-0.5 rounded-[var(--exits-radius-md)] px-1 py-1 text-center text-[length:var(--exits-text-xs)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                  isActive
                    ? "font-semibold text-primary"
                    : "font-medium text-muted hover:text-foreground",
                )}
              >
                <Icon className="size-5 shrink-0" aria-hidden />
                <span className="max-w-full truncate">{t(tab.labelKey)}</span>
              </NavLink>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
