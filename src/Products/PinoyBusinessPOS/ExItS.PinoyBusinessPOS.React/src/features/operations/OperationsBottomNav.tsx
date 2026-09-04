import { NavLink, useLocation } from "react-router-dom";
import {
  ArrowLeftRight,
  Boxes,
  Home,
  ListOrdered,
  MoreHorizontal,
  PackagePlus,
  ShoppingCart,
} from "lucide-react";
import {
  buildOperationsBottomNavTabs,
  matchOperationsNavTab,
  type OperationsNavTabId,
} from "@/features/operations/operations-nav-config";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { isAuthenticatedOrColdStartOffline, useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const ICONS: Record<OperationsNavTabId, typeof Home> = {
  home: Home,
  sell: ShoppingCart,
  inventory: Boxes,
  orders: ListOrdered,
  transfers: ArrowLeftRight,
  purchasing: PackagePlus,
  more: MoreHorizontal,
};

/** Operations bottom nav — visible below lg only (tablet + mobile). */
export function OperationsBottomNav() {
  const { t } = useI18n();
  const location = useLocation();
  const { status: sessionStatus } = useSession();
  const { sessionGrant, boundWorkspace } = useWorkspace();

  if (!isAuthenticatedOrColdStartOffline(sessionStatus) || !boundWorkspace) {
    return null;
  }

  const experience = boundWorkspace.experience ?? "operations";
  const tabs = buildOperationsBottomNavTabs({
    grant: sessionGrant,
    experience,
    branchType: boundWorkspace.branchType,
  });
  const activeId = matchOperationsNavTab(location.pathname, tabs);

  if (tabs.length === 0) {
    return null;
  }

  return (
    <nav
      data-testid="operations-bottom-nav"
      aria-label={t("operations.nav.aria")}
      className="org-bottom-nav operations-bottom-nav fixed inset-x-0 bottom-0 z-50 border-t border-border bg-surface pb-[env(safe-area-inset-bottom)] lg:hidden"
    >
      <ul className="org-bottom-nav-inner mx-auto flex w-full max-w-lg items-stretch justify-between gap-0.5 px-2 pt-1 sm:max-w-xl md:max-w-2xl">
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
                <Icon
                  className={cn("size-5 shrink-0", tab.primary ? "size-[1.35rem]" : null)}
                  aria-hidden
                  strokeWidth={tab.primary ? 2.25 : 2}
                />
                <span className="max-w-full truncate">{t(tab.labelKey)}</span>
              </NavLink>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
